# Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
"""Build with CPython 3.12 + PyInstaller; smoke-test the exact executable.
Run: python tools/build_refined.py (Windows). GPL-3.0, modifications 2026-10-29.
"""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]

def clean_env():
    env = dict(os.environ)
    for key in list(env):
        if key.startswith(('PYTHON', 'CONDA', 'ARKLOCALIZER_', '_CE_')) or key in ('TCL_LIBRARY','TK_LIBRARY','TCLLIBPATH'):
            del env[key]
    env['PYTHONUTF8'] = '1'
    return env

def main():
    if os.name != 'nt': raise RuntimeError('Windows build required')
    out = ROOT/'work/launcher-build'
    out.mkdir(parents=True, exist_ok=True)
    env = clean_env()
    base = Path(sys.base_prefix)
    # Support standard CPython and Conda without taking DLLs from other installs.
    tcl = base/('Library/lib' if (base/'Library/lib/tcl8.6').exists() else 'tcl')
    env['TCL_LIBRARY'] = str(tcl/'tcl8.6')
    env['TK_LIBRARY'] = str(tcl/'tk8.6')
    subprocess.run([sys.executable,str(ROOT/'launcher_entry.pyw'),'--startup-check',str(out/'source.json')],env=env,check=True)
    subprocess.run([sys.executable,'-m','PyInstaller','--noconfirm','--clean','--onefile','--windowed',
        '--name','ArknightsRefined','--distpath',str(ROOT),'--workpath',str(out/'build'),
        '--specpath',str(out),'--paths',str(ROOT),'--exclude-module','arklocalizer.cn_font',
        '--add-data',str(ROOT/'tools/runtime/BepInEx.cfg')+';tools/runtime',str(ROOT/'launcher_entry.pyw')],
        env=env,cwd=ROOT,check=True)
    exe=ROOT/'ArknightsRefined.exe'
    results={}
    for name,add in [('clean',{}),('hostile',{'TCL_LIBRARY':'Z:/missing-tcl','TK_LIBRARY':'Z:/missing-tk','PYTHONHOME':'Z:/missing-python','PYTHONPATH':'Z:/missing-python'})]:
        testenv=clean_env();testenv.update(add)
        report=out/(name+'.json')
        subprocess.run([str(exe),'--startup-check',str(report)],cwd=out,env=testenv,check=True,timeout=60)
        result=json.loads(report.read_text(encoding='utf8'))
        if not result.get('ok') or not result.get('frozen'):raise RuntimeError(result)
        results[name]=result
    results['sha256']=hashlib.sha256(exe.read_bytes()).hexdigest()
    (out/'verified.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf8')
    print('Verified executable:',exe,flush=True)

if __name__=='__main__':main()
