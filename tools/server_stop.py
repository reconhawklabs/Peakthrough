#!/usr/bin/env python3
"""Request a clean stop only from the owner of this private server."""
import argparse,json,os
from pathlib import Path

def request_stop(root,owner,windows=None):
 if windows is None:windows=os.name=='nt'
 file=Path(root)/'owned-process.json'
 if not file.exists():return False
 info=json.loads(file.read_text())
 if info['owner']!=owner:return False
 if windows:
  temporary=Path(root)/'stop-request.tmp'
  temporary.write_text(json.dumps({'owner':owner,'pid':info['pid']}));temporary.replace(Path(root)/'stop-request.json');return True
 try:
  fd=os.open(Path(root)/'console.fifo',os.O_WRONLY|os.O_NONBLOCK)
  try:os.write(fd,b'stop\n')
  finally:os.close(fd)
  return True
 except (FileNotFoundError,BlockingIOError):return False
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--owner',required=True);args=p.parse_args()
 request_stop(Path(__file__).resolve().parent,args.owner)
