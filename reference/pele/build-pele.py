#!/usr/bin/env python3
"""
Vendors Pele (https://pele.run, https://github.com/obsidianmd/pele) as
Neko/Resources/pele.min.js - the renderer behind ```mermaid blocks.

Pele is a dependency-free Mermaid-syntax-to-SVG renderer. The file is the npm
package's browser build, dist/pele.global.js (minified; it defines the global
`pele` and carries its own licence banner), copied unchanged:

  * a site page loads it from assets/pele.min.js, on demand, only when the page has
    a diagram (HtmlGenerator.Head.cs, RenderHeadMermaid);
  * a presentation deck or paged document inlines it (HtmlGenerator.Standalone.cs).

Neko never loads it from a CDN. To update, bump VERSION and run:

    python3 reference/pele/build-pele.py
"""
import hashlib, io, os, tarfile, urllib.request

VERSION = '1.6.0'

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Neko', 'Resources', 'pele.min.js')


def main():
    url = f'https://registry.npmjs.org/pele/-/pele-{VERSION}.tgz'
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'neko-build'})) as res:
        archive = res.read()
    with tarfile.open(fileobj=io.BytesIO(archive), mode='r:gz') as tar:
        data = tar.extractfile('package/dist/pele.global.js').read()
    if not data.startswith(b'/*! Pele ' + VERSION.encode()):
        raise SystemExit(f'unexpected banner in pele {VERSION}: {data[:80]!r}')
    with open(OUT, 'wb') as f:
        f.write(data)
    print(f'{os.path.normpath(OUT)}: {len(data):,} bytes, sha256 {hashlib.sha256(data).hexdigest()[:16]}')


if __name__ == '__main__':
    main()
