#!/usr/bin/env python3
"""Export sounds from the user's Minecraft asset objects; exported audio stays outside Git."""
import argparse,hashlib,json,re
from pathlib import Path,PurePosixPath

def export(index,objects,target):
 entries=json.loads(Path(index).read_text())['objects'];target=Path(target);manifest={}
 for name,entry in entries.items():
  if name!='minecraft/sounds.json' and not name.startswith('minecraft/sounds/'):continue
  rel=PurePosixPath(name.removeprefix('minecraft/'))
  if any(part in ('..','.') for part in rel.parts) or rel.is_absolute() or '\\' in name:raise ValueError('Unsafe sound path')
  h=entry['hash']
  if not re.fullmatch('[a-f0-9]{40}',h):raise ValueError('Invalid asset hash')
  data=(Path(objects)/h[:2]/h).read_bytes()
  if hashlib.sha1(data).hexdigest()!=h:raise ValueError('Asset hash mismatch')
  dest=target.joinpath(*rel.parts);dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data);manifest[str(rel)]=h
 if 'sounds.json' not in manifest:raise ValueError('Native sounds.json missing')
 (target/'sound-manifest.json').write_text(json.dumps(dict(version=1,files=manifest),sort_keys=True)+'\n');return len(manifest)
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--assets',type=Path,default=Path.home()/'.gradle/caches/fabric-loom/assets');parser.add_argument('--cache',type=Path,default=Path.home()/'.local/share/PeakCreativeMode/asset-cache/26.3');args=parser.parse_args()
 print('Exported',export(args.assets/'indexes/26.3-34.json',args.assets/'objects',args.cache),'private sound assets')
