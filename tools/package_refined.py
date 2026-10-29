# Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
"""Package only reviewed project content. Never include personal configuration or logs."""
import hashlib
import json
from pathlib import Path
import sys
import zipfile

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from arklocalizer.libraries import verify

def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def package(destination):
    destination=Path(destination).resolve();destination.mkdir(parents=True,exist_ok=True)
    verify(ROOT/'runtime/jp-zh-offline-final')
    build=json.loads((ROOT/'work/launcher-build/verified.json').read_text(encoding='utf8'))
    assert digest(ROOT/'ArknightsRefined.exe')==build['sha256']
    allowed={'arklocalizer','plugins','tools','tests','runtime'}
    roots={'README.md','UPSTREAM.md','UPSTREAM_README.md','BUILDING.md','CHANGELOG.md','LICENSE',
           'Directory.Build.props','launcher_entry.pyw','pyproject.toml','requirements.txt','requirements-dev.txt','.gitignore','.gitattributes','ArknightsRefined.exe'}
    banned={'__pycache__','bin','obj','work','.git','.backups','.arklocalizer-backup'}
    files=[]
    for path in sorted(ROOT.rglob('*')):
        rel=path.relative_to(ROOT)
        if not path.is_file() or any(p in banned for p in rel.parts):continue
        if rel.parts[0] not in allowed and str(rel) not in roots:continue
        if path.suffix in {'.pyc','.log'} or path.name=='launcher.json':continue
        # The original Conda-only builder targets the original executable name.
        if rel.as_posix() in {'tools/build_launcher.py','tools/launcher.spec'}:continue
        files.append(path)
    results=[]
    for kind in ['win-x64','source']:
        target=destination/f'Arknights-JP-ZH-Refined-v2.1.1-{kind}.zip'
        selected=[]
        for path in files:
            rel=path.relative_to(ROOT)
            if kind=='source':
                if path.suffix in {'.exe','.dll'}:continue
                if rel.parts[0]=='runtime' and not ('BepInEx/Translation/' in rel.as_posix() or path.name in {'ARKLOCALIZER_MANIFEST.json','WORDLIBRARY.json'}):continue
            selected.append(path)
        with zipfile.ZipFile(target,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
            for path in selected:z.write(path,'Arknights-JP-ZH-Refined/'+path.relative_to(ROOT).as_posix())
        with zipfile.ZipFile(target) as z:
            assert z.testzip() is None
            assert all(not any(p in banned for p in Path(name).parts) for name in z.namelist())
        results.append(dict(name=target.name,bytes=target.stat().st_size,sha256=digest(target),files=len(selected)))
    (destination/'SHA256SUMS.txt').write_text(''.join(f"{x['sha256']}  {x['name']}\n" for x in results),encoding='utf8')
    print(json.dumps(results,indent=2))

if __name__=='__main__':package(sys.argv[1])
