#!/usr/bin/env python3
"""Own exactly one native Java process; lock, FIFO shutdown, durable saves and PID token."""
import argparse,hashlib,json,os,signal,subprocess,time,sys
if os.name=="nt":import msvcrt
else:import fcntl
from pathlib import Path

def java_options(heap_mb=4096,processors=4):
 if not isinstance(heap_mb,int) or not 1024<=heap_mb<=8192:raise ValueError('Heap must be 1024..8192 MB')
 if not isinstance(processors,int) or not 1<=processors<=16:raise ValueError('Worker processor budget must be 1..16')
 return ['-Xms256m',f'-Xmx{heap_mb}m',f'-XX:ActiveProcessorCount={processors}']

def run():
 p=argparse.ArgumentParser();p.add_argument('--owner',default='manual');p.add_argument('--port',type=int,default=47655);p.add_argument('--foreground',action='store_true');p.add_argument('--native',action='store_true');p.add_argument('--heap-mb',type=int,default=4096);args=p.parse_args();java_options(args.heap_mb)
 root=Path(__file__).resolve().parent
 manifest=json.loads((root/'runtime.json').read_text())
 with (root/'launcher.log').open('a') as log:log.write('Launch request owner='+args.owner+' native='+str(args.native)+' java='+manifest['javaExecutable']+' exists='+str(Path(manifest['javaExecutable']).exists())+'\n')
 if not args.native and not Path(manifest['javaExecutable']).exists() and Path('/run/host'+manifest['javaExecutable']).exists():
  # Steam's pressure-vessel mount lacks the host JDK's /etc security configuration.
  launch=Path('/usr/bin/steam-runtime-launch-client')
  if not launch.exists():launch=Path.home()/'.steam/steam/steamapps/common/SteamLinuxRuntime_sniper/pressure-vessel/bin/steam-runtime-launch-client'
  if not launch.exists():raise RuntimeError('Steam host launcher missing; use tools/run_peak.sh')
  command=[str(launch),'--alongside-steam','--clear-env','--directory='+str(root),'--','/usr/bin/python3',str(__file__),'--native','--owner',args.owner,'--port',str(args.port),'--heap-mb',str(args.heap_mb)]
  if args.foreground:command.append('--foreground')
  with (root/'launcher.log').open('ab') as log:result=subprocess.run(command,stdout=log,stderr=log)
  sys.exit(result.returncode)
 if not args.foreground:
  with (root/'launcher.log').open('ab') as log:
   subprocess.Popen([sys.executable,str(__file__),'--foreground','--owner',args.owner,'--port',str(args.port),'--heap-mb',str(args.heap_mb)],stdin=subprocess.DEVNULL,stdout=log,stderr=log,start_new_session=True)
  return
 lock=(root/'server.lock').open('a');
 try:
  if os.name=='nt':
   lock.seek(0);lock.write('0');lock.flush();lock.seek(0);msvcrt.locking(lock.fileno(),msvcrt.LK_NBLCK,1)
  else:fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
 except (BlockingIOError,OSError):return
 manifest=json.loads((root/'runtime.json').read_text())
 java=manifest['javaExecutable']
 if not Path(java).exists() and Path('/run/host'+java).exists():java='/run/host'+java
 version=subprocess.run([java,'-version'],capture_output=True,text=True).stderr
 if not ('version "25' in version or ' 25' in version):raise RuntimeError('Java 25 required: '+version.splitlines()[0])
 paths=[]
 for entry in manifest['classpath']:
  file=(root/entry['path']).resolve()
  if root not in file.parents or hashlib.sha256(file.read_bytes()).hexdigest()!=entry['sha256']:raise RuntimeError('Runtime path/hash mismatch')
  paths.append(str(file))
 fifo=root/'console.fifo';fd=None
 if os.name!='nt':
  if fifo.exists():fifo.unlink()
  os.mkfifo(fifo,0o600);fd=os.open(fifo,os.O_RDWR|os.O_NONBLOCK)
 # Development discovery loads locally installed Fabric modules from the verified classpath.
 command=[java]+java_options(args.heap_mb)+['-Dfabric.development=true','-Dfabric.defaultModDistributionNamespace=official','-Dfabric.defaultMixinRemapType=static','-Dfabric.gameJarPath='+str(root/manifest['game']),'-Dpeakbridge.port='+str(args.port),'-cp',os.pathsep.join(paths),'net.fabricmc.loader.impl.launch.knot.KnotServer','nogui']
 process=subprocess.Popen(command,cwd=root,stdin=subprocess.PIPE)
 pidfile=root/'owned-process.json';pidfile.write_text(json.dumps(dict(pid=process.pid,owner=args.owner,supervisor=os.getpid(),started=time.time())))
 stopping=False
 def stop(signum=None,frame=None):
  nonlocal stopping
  if not stopping and process.poll() is None:
   stopping=True;process.stdin.write(b'stop\n');process.stdin.flush()
 signal.signal(signal.SIGTERM,stop);signal.signal(signal.SIGINT,stop)
 try:
  while process.poll() is None:
   try:data=os.read(fd,65536) if fd is not None else b''
   except BlockingIOError:data=b''
   request=root/'stop-request.json'
   if request.exists():
    try:
     message=json.loads(request.read_text())
     if message.get('owner')==args.owner and message.get('pid')==process.pid:stop()
    finally:request.unlink(missing_ok=True)
   if data:
    process.stdin.write(data);process.stdin.flush()
   time.sleep(.1)
  if process.returncode:raise RuntimeError('Native server exited '+str(process.returncode))
 finally:
  stop()
  try:process.wait(timeout=45)
  except subprocess.TimeoutExpired:process.terminate();process.wait(timeout=15)
  if fd is not None:os.close(fd);fifo.unlink(missing_ok=True)
  pidfile.unlink(missing_ok=True)
if __name__=='__main__':run()
