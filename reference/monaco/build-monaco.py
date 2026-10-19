#!/usr/bin/env python3
"""
Vendors the Monaco editor (https://github.com/microsoft/monaco-editor) as Neko/Resources/monaco.zip -
the editor behind the "edit this page" modal of `neko watch`.

The editor only exists in watch mode, so it is not part of any site: the dev server serves it from the
zip, in memory, at /neko-monaco/ (Neko/Server/MonacoAssets.cs). It is never loaded from a CDN.

Only what the editor uses is kept from the package's min/vs folder: the AMD loader, the editor and its
worker, the codicon font, and the basic-languages grammars of the two languages it opens (Markdown and
YAML). Monaco loads the other languages' grammars on demand, and only when a model asks for them.

    python3 reference/monaco/build-monaco.py
"""
import io, os, re, tarfile, urllib.request, zipfile

VERSION = '0.44.0'

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Neko', 'Resources', 'monaco.zip')

KEEP = (
    'loader.js',
    'editor/',
    'base/',
    'basic-languages/markdown/',
    'basic-languages/yaml/',
)


def main():
    url = f'https://registry.npmjs.org/monaco-editor/-/monaco-editor-{VERSION}.tgz'
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'neko-build'})) as res:
        archive = res.read()

    buf = io.BytesIO()
    with tarfile.open(fileobj=io.BytesIO(archive), mode='r:gz') as tar, \
            zipfile.ZipFile(buf, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for member in sorted(tar.getmembers(), key=lambda m: m.name):
            prefix = 'package/min/vs/'
            if not member.isfile() or not member.name.startswith(prefix):
                continue
            rel = member.name[len(prefix):]
            if not rel.startswith(KEEP):
                continue
            # The editor's UI is English: drop the translated message bundles (editor.main.nls.de.js, ...).
            if re.search(r'nls\.[a-z-]+\.js$', rel):
                continue
            # Fixed timestamps, so the same input is the same zip.
            info = zipfile.ZipInfo('vs/' + rel, date_time=(2023, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            z.writestr(info, tar.extractfile(member).read())

    with open(OUT, 'wb') as f:
        f.write(buf.getvalue())
    print(f'{os.path.normpath(OUT)}: {len(buf.getvalue()):,} bytes')


if __name__ == '__main__':
    main()
