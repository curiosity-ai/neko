#!/usr/bin/env python3
"""
Builds Neko/Resources/docx.bundle.js - the vendored copy of dolanmiu/docx that the
Word export (document-docx.js) uses. Never loaded from a CDN.

    python3 reference/docx/build-docx-bundle.py [version]

Needs node/npm (npm pack + terser through npx). One local patch is applied: the
library embeds fonts (Document `fonts`) but never writes <w:embedTrueTypeFonts/> to
settings.xml, so Word forgets the embedded fonts when a reader re-saves the file.
The patch emits it after <w:displayBackgroundShape/> (its place in the CT_Settings
sequence) whenever the document embeds fonts.
"""
import os, subprocess, sys, tarfile, tempfile

version = sys.argv[1] if len(sys.argv) > 1 else '9.8.1'
here = os.path.dirname(os.path.abspath(__file__))
out = os.path.join(here, '..', '..', 'Neko', 'Resources', 'docx.bundle.js')

with tempfile.TemporaryDirectory() as tmp:
    subprocess.check_call(['npm', 'pack', f'docx@{version}', '--silent'], cwd=tmp)
    tgz = [f for f in os.listdir(tmp) if f.endswith('.tgz')][0]
    with tarfile.open(os.path.join(tmp, tgz)) as t:
        t.extract('package/dist/index.umd.cjs', tmp)
    src = os.path.join(tmp, 'package', 'dist', 'index.umd.cjs')
    code = open(src, encoding='utf8').read()

    a = 'this.root.push(new OnOffElement("w:displayBackgroundShape", true));'
    assert a in code
    code = code.replace(a, a + '\n\t\t\tif (options.embedTrueTypeFonts) this.root.push(new OnOffElement("w:embedTrueTypeFonts", true));', 1)
    b = 'compatibility: options.compatibility,\n\t\t\t\tevenAndOddHeaders'
    assert b in code
    code = code.replace(b, 'compatibility: options.compatibility,\n\t\t\t\tembedTrueTypeFonts: !!(options.fonts && options.fonts.length),\n\t\t\t\tevenAndOddHeaders', 1)
    open(src, 'w', encoding='utf8').write(code)
    subprocess.check_call(['npx', '--yes', 'terser', src, '--compress', '--mangle', '-o', out])
print('wrote', os.path.normpath(out))
