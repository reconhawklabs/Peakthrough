#!/usr/bin/env python3
"""Player setup; private Minecraft files stay on the player's computer."""
import argparse, os, re, shutil, subprocess, sys, time
from pathlib import Path
from export_sounds import export as export_sounds
from install_server import package
ROOT=Path(__file__).resolve().parents[1]

def peak_path(path,windows=None):
 if windows is None:windows=os.name=='nt'
 return str(path) if windows else 'Z:'+str(path).replace('/','\\')

def validate_peak(path):
 path=Path(path).expanduser().resolve()
 if not (path/'PEAK.exe').is_file():raise ValueError('Choose the PEAK game folder: Steam > PEAK > Manage > Browse local files.')
 if not (path/'BepInEx/core/BepInEx.dll').is_file():raise ValueError('Install BepInExPack_PEAK first, then launch and close PEAK once.')
 return path

def update_config(text,settings):
 lines=text.splitlines();section=None;seen={};out=[]
 for line in lines:
  match=re.fullmatch(r'\s*\[([^]]+)\]\s*',line)
  if match:
   if section in settings:
    for k,v in settings[section].items():
     if k not in seen.setdefault(section,set()):out.append(k+' = '+v);seen[section].add(k)
   section=match.group(1)
  key=line.split('=',1)[0].strip() if '=' in line and not line.lstrip().startswith(('#',';')) else None
  if section in settings and key in settings[section]:line=key+' = '+settings[section][key];seen.setdefault(section,set()).add(key)
  out.append(line)
 if section in settings:
  for k,v in settings[section].items():
   if k not in seen.setdefault(section,set()):out.append(k+' = '+v);seen[section].add(k)
 for name,values in settings.items():
  missing=[k for k in values if k not in seen.get(name,set())]
  if missing:
   if name not in seen:out+=['','['+name+']']
   out.extend(k+' = '+values[k] for k in missing)
 return '\n'.join(out)+'\n'

def configured_path(text,section,key,fallback):
 current=None
 for line in text.splitlines():
  if line.strip().startswith('['):current=line.strip().strip('[]')
  if current==section and '=' in line and line.split('=',1)[0].strip()==key:
   value=line.split('=',1)[1].strip()
   if not value:break
   if os.name!='nt' and value.lower().startswith('z:\\'):return Path(value[2:].replace('\\','/'))
   if os.name!='nt' and not value.startswith('/'):raise ValueError('Existing '+section+'.'+key+' is not a Linux/Wine Z: path; keep its data and update the path before setup.')
   return Path(value)
 return fallback

def run(command,cwd=None,env=None):
 print('Working…',flush=True);subprocess.run(command,cwd=cwd,env=env,check=True)

def main():
 parser=argparse.ArgumentParser(description='Install Peakthrough after installing BepInExPack_PEAK.')
 parser.add_argument('--role',choices=['host','client']);parser.add_argument('--peak',type=Path)
 args=parser.parse_args()
 role=args.role or input('Are you the PEAK host or a joining client? [host/client]: ').strip().lower()
 if role not in ('host','client'):raise ValueError('Choose host or client.')
 peak=validate_peak(args.peak or input('Paste your PEAK game folder (without quotation marks): ').strip().strip('"'))
 if os.name=='nt':
  tasks=subprocess.run(['tasklist','/FI','IMAGENAME eq PEAK.exe','/FO','CSV','/NH'],capture_output=True,text=True).stdout
  if re.search(r'\"PEAK\.exe\"',tasks,re.I):raise ValueError('Close PEAK before running setup.')
 elif shutil.which('pgrep') and subprocess.run(['pgrep','-x','PEAK.exe'],stdout=subprocess.DEVNULL).returncode==0:raise ValueError('Close PEAK before running setup.')
 binaries=ROOT/'plugin'
 for name in ('Peakthrough.dll','PeakCreativeMode.Core.dll'):
  if not (binaries/name).is_file():raise ValueError('Download and extract the complete Peakthrough v1 ZIP linked in the README.')
 java=shutil.which('java')
 if not java:raise ValueError('Install Java 25 JDK, reopen this setup window, and try again.')
 version=subprocess.run([java,'-version'],capture_output=True,text=True)
 if not re.search(r'version "25(?:[.\"+-])',version.stderr+version.stdout):raise ValueError('Java 25 JDK is required. Reopen this window after installing it.')
 if role=='host':
  answer=input('Read https://aka.ms/MinecraftEULA. Do you accept the Minecraft server EULA? [yes/no]: ').strip().lower()
  if answer!='yes':raise ValueError('Server setup cancelled; the Minecraft EULA must be accepted by you.')
 destination=peak/'BepInEx/plugins/Peakthrough'
 config=peak/'BepInEx/config/com.bornparanoid.peakcreativemode.cfg';previous=config.read_text(encoding='utf-8-sig') if config.exists() else ''
 cache=configured_path(previous,'Assets','CachePath',destination/'data/asset-cache/26.3')
 server=configured_path(previous,'Server','Path',destination/'data/server')
 if role=='host' and (server/'owned-process.json').exists():raise ValueError('Close the existing Minecraft server before updating. Your saved world is retained.')
 cache.mkdir(parents=True,exist_ok=True)
 source=ROOT/('minecraft-bridge' if (ROOT/'minecraft-bridge').exists() else 'passpeakthrough-template-26.3');wrapper=source/('gradlew.bat' if os.name=='nt' else 'gradlew')
 if os.name!='nt':wrapper.chmod(wrapper.stat().st_mode|0o111)
 env=os.environ.copy();env['JAVA_TOOL_OPTIONS']=env.get('JAVA_TOOL_OPTIONS','')+' -Dpeak.setup.exportOnly=true'
 # launch.cfg takes care of assets. JVM properties are quoted by Gradle through its run configuration.
 env['PEAK_ASSETS_CACHE']=str(cache)
 started=time.time();run([str(wrapper),'runClient'],source,env)
 model_manifest=cache/'entity-models/manifest.json'
 if not model_manifest.is_file() or model_manifest.stat().st_mtime<started-2:raise ValueError('Minecraft model export failed. See the setup output; no plugin was installed.')
 jars=list((source/'.gradle/loom-cache/minecraftMaven').glob('**/minecraft-clientOnly-*-26.3.jar'))
 if len(jars)!=1:raise ValueError('Expected one locally downloaded Minecraft 26.3 client jar.')
 run([java,str(ROOT/'tools/ExportAssets.java'),str(jars[0]),str(cache)])
 asset_root=Path(os.environ.get('GRADLE_USER_HOME',str(Path.home()/'.gradle')))/'caches/fabric-loom/assets'
 export_sounds(asset_root/'indexes/26.3-34.json',asset_root/'objects',cache)
 if role=='host':
  run([str(wrapper),'preparePrivateServer'],source)
  (source/'run').mkdir(exist_ok=True);(source/'run/eula.txt').write_text('eula=true\n')
  # Do not copy a development world into a player's new server.
  (server/'world').mkdir(parents=True,exist_ok=True)
  package(source,server)
 destination.mkdir(parents=True,exist_ok=True)
 for name in ('Peakthrough.dll','PeakCreativeMode.Core.dll'):shutil.copy2(binaries/name,destination/name)
 for name in ('PeakCreativeMode.Plugin.dll','PeakCreativeMode.Core.dll'):
  legacy=peak/'BepInEx/plugins/PeakCreativeMode'/name
  if legacy.exists():
   saved=legacy.with_suffix('.dll.previous');shutil.copy2(legacy,saved);legacy.unlink()
 config=peak/'BepInEx/config/com.bornparanoid.peakcreativemode.cfg';config.parent.mkdir(parents=True,exist_ok=True)
 settings={'Assets':{'CachePath':peak_path(cache)},'Server':{'Path':peak_path(server),'AutoStart':str(role=='host').lower()},'Diagnostics':{'EnableAutomation':'false'}}
 config.write_text(update_config(config.read_text(encoding='utf-8-sig') if config.exists() else '',settings),encoding='utf-8')
 print('\nPeakthrough installed. Start PEAK '+('and host a room.' if role=='host' else 'and join your host’s room.')+' Keep this folder for updates.')
if __name__=='__main__':
 try:main()
 except (ValueError,subprocess.CalledProcessError,OSError) as e:print('\nSetup stopped: '+str(e),file=sys.stderr);sys.exit(1)
