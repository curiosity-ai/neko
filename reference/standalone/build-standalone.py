#!/usr/bin/env python3
"""
Builds Neko/Resources/standalone/ - the third-party files presentation decks and
paged documents inline into their HTML, so a generated deck or document is one
self-contained file with no CDN, font host or /assets/ request.

Ordinary documentation pages keep loading these from their CDNs; only the
standalone pages (HtmlGenerator.Standalone.cs) use the vendored copies, and
CopyAssetsAsync never copies this folder into a site's assets/.

  katex.min.css, katex.min.js, auto-render.min.js, KaTeX_*.woff2   KaTeX 0.16.8
      (the stylesheet is rewritten to reference only the woff2 faces, by bare
      file name, so each can be inlined as a data URI)
  mermaid.min.js                       Mermaid 10.9.8 (what mermaid@10 resolves to)
  panzoom.min.js                       panzoom 9.4.0
  highlightjs-line-numbers.min.js      highlightjs-line-numbers.js 2.8.0
  leader-line.min.js                   leader-line-new 1.1.9
  deck-google-fonts.css, *.woff2       the midnight / daylight deck typefaces
      (Archivo, Source Serif 4, IBM Plex Mono; latin + latin-ext subsets) that
      decks otherwise pull from Google Fonts
  twemoji.zip                          the Twemoji 14.0.2 SVGs emoji.css points at

    python3 reference/standalone/build-standalone.py
"""
import io, os, re, sys, tarfile, urllib.request, zipfile
from concurrent.futures import ThreadPoolExecutor

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
OUT = os.path.join(ROOT, 'Neko', 'Resources', 'standalone')

NPM = [
    # (package, version, {path in package: output name})
    ('katex', '0.16.8', {
        'dist/katex.min.js': 'katex.min.js',
        'dist/contrib/auto-render.min.js': 'auto-render.min.js',
    }),
    ('mermaid', '10.9.8', {'dist/mermaid.min.js': 'mermaid.min.js'}),
    ('panzoom', '9.4.0', {'dist/panzoom.min.js': 'panzoom.min.js'}),
    ('highlightjs-line-numbers.js', '2.8.0', {'dist/highlightjs-line-numbers.min.js': 'highlightjs-line-numbers.min.js'}),
    ('leader-line-new', '1.1.9', {'leader-line.min.js': 'leader-line.min.js'}),
]

GOOGLE_FONTS = ('https://fonts.googleapis.com/css2?family=Archivo:wght@500;600;800'
                '&family=Source+Serif+4:opsz,wght@8..60,400;8..60,600'
                '&family=IBM+Plex+Mono:wght@400;500&display=swap')
# A browser user agent, so Google Fonts answers with woff2 + unicode-range subsets.
BROWSER_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36'
KEEP_SUBSETS = ('latin', 'latin-ext')

TWEMOJI = 'https://cdn.jsdelivr.net/gh/twitter/twemoji@14.0.2/assets/svg/'


def get(url, ua=None):
    req = urllib.request.Request(url, headers={'User-Agent': ua or 'neko-build'})
    with urllib.request.urlopen(req) as res:
        return res.read()


def write(name, data):
    with open(os.path.join(OUT, name), 'wb') as f:
        f.write(data if isinstance(data, bytes) else data.encode('utf-8'))


def npm_files():
    for package, version, files in NPM:
        tgz = get(f'https://registry.npmjs.org/{package}/-/{package}-{version}.tgz')
        with tarfile.open(fileobj=io.BytesIO(tgz)) as tar:
            members = {m.name[len('package/'):]: m for m in tar.getmembers()}
            for src, dst in files.items():
                write(dst, tar.extractfile(members[src]).read())
            if package == 'katex':
                css = tar.extractfile(members['dist/katex.min.css']).read().decode('utf-8')
                # Keep only the woff2 source of each face, by bare file name.
                css = re.sub(r'src:url\(fonts/([^)]+\.woff2)\) format\("woff2"\)[^;}]*',
                             r'src:url(\1) format("woff2")', css)
                write('katex.min.css', css)
                for path, member in members.items():
                    if path.startswith('dist/fonts/') and path.endswith('.woff2'):
                        write(os.path.basename(path), tar.extractfile(member).read())


def google_fonts():
    css = get(GOOGLE_FONTS, BROWSER_UA).decode('utf-8')
    # Google repeats a variable font's file for every weight asked for; one face
    # with a weight range covers them, so each file is inlined only once.
    faces = {}
    fetched = {}
    for subset, body in re.findall(r'/\* ([a-z-]+) \*/\s*@font-face\s*\{([^}]*)\}', css):
        if subset not in KEEP_SUBSETS:
            continue
        props = dict((k.strip(), v.strip()) for k, v in
                     (p.split(':', 1) for p in body.strip().split(';') if ':' in p))
        family = props['font-family'].strip("'")
        url = re.search(r'url\(([^)]+)\)', props['src']).group(1)
        if url not in fetched:
            name = f"{family.replace(' ', '')}-{subset}-{len(fetched)}.woff2"
            write(name, get(url))
            fetched[url] = name
        key = (family, props.get('font-style', 'normal'), fetched[url], props['unicode-range'])
        faces.setdefault(key, []).append(int(props['font-weight']))
    out = ['/* Generated by reference/standalone/build-standalone.py. Fonts: SIL Open Font License 1.1. */']
    for (family, style, file, urange), weights in faces.items():
        weight = str(weights[0]) if len(weights) == 1 else f'{min(weights)} {max(weights)}'
        out.append(f'@font-face{{font-family:"{family}";font-style:{style};font-weight:{weight};'
                   f'font-display:swap;src:url({file}) format("woff2");unicode-range:{urange}}}')
    write('deck-google-fonts.css', '\n'.join(out) + '\n')


def twemoji():
    emoji_css = open(os.path.join(ROOT, 'Neko', 'Resources', 'emoji.css'), encoding='utf-8').read()
    names = sorted(set(re.findall(r'assets/svg/([0-9a-f-]+\.svg)', emoji_css)))
    with ThreadPoolExecutor(16) as pool:
        svgs = list(pool.map(lambda n: get(TWEMOJI + n), names))
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for name, svg in zip(names, svgs):
            info = zipfile.ZipInfo(name, date_time=(2022, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            z.writestr(info, svg)
    write('twemoji.zip', buf.getvalue())


def main():
    os.makedirs(OUT, exist_ok=True)
    npm_files()
    google_fonts()
    twemoji()
    print('Wrote', OUT)


if __name__ == '__main__':
    sys.exit(main())
