#!/usr/bin/env python3
"""Create a private server from this user's verified local Loom runtime. No redistribution."""
import argparse,hashlib,json,os,shutil,sys
from pathlib import Path

def runtime_files(source):
 lines=(source/'build/loom-cache/argFiles/runServer').read_text().splitlines()
 cp=lines[lines.index('-classpath')+1]
 result=[]
 for entry in cp.split(os.pathsep):
  p=Path(entry)
  if p.is_file() and p.suffix=='.jar' and p not in result:result.append(p)
 return result

def package(source,target):
 source=Path(source).resolve();target=Path(target).resolve();eula=source/'run/eula.txt'
 if not eula.exists() or 'eula=true' not in [line.strip() for line in eula.read_text().splitlines()]:raise ValueError('A previously accepted run/eula.txt is required')
 files=runtime_files(source)
 game=next((p for p in files if p.name.startswith('minecraft-common')),None)
 if game is None:raise ValueError('Run ./gradlew build genSources first; Minecraft common runtime missing')
 own=[p for p in (source/'build/libs').glob('*.jar') if not p.name.endswith(('-sources.jar','-dev.jar'))]
 if len(own)!=1:raise ValueError('Expected one built mod jar')
 target.mkdir(parents=True,exist_ok=True);lib=target/'lib';lib.mkdir(exist_ok=True)
 entries=[];game_copy=None
 for i,file in enumerate(files+[own[0]]):
  name=f'{i:03d}-{file.name}';dest=lib/name;shutil.copy2(file,dest)
  entries.append(dict(path='lib/'+name,sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
  if file==game:game_copy='lib/'+name
 manifest=dict(version=1,platform=sys.platform,java=25,javaExecutable=str(Path(shutil.which("java")).resolve()),game=game_copy,classpath=entries,private=True)
 (target/'runtime.json').write_text(json.dumps(manifest,indent=2)+'\n')
 if not (target/'eula.txt').exists():shutil.copy2(eula,target/'eula.txt')
 if not (target/'world').exists() and (source/'run/world').exists():shutil.copytree(source/'run/world',target/'world')
 props=target/'server.properties'
 if not props.exists():props.write_text('server-ip=0.0.0.0\nserver-port=25565\nonline-mode=false\nview-distance=4\nsimulation-distance=4\npause-when-empty-seconds=0\nmax-players=4\n')
 tools=Path(__file__).resolve().parent
 for name in ['server_runtime.py','server_start.sh','server_stop.py','server_start.cmd','server_stop.cmd']:shutil.copy2(tools/name,target/name);(target/name).chmod(0o755)
 return manifest
if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--source',type=Path,default=Path(__file__).resolve().parents[1]/'passpeakthrough-template-26.3');parser.add_argument('--target',type=Path,default=Path.home()/'.local/share/PeakCreativeMode/server')
 args=parser.parse_args();result=package(args.source,args.target);print(f'Installed private Java {result["java"]} runtime ({len(result["classpath"])} jars) into {args.target}')
