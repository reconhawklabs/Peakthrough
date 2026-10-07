namespace PeakCreativeMode.Core {
 public readonly struct ServerCommand {
  public readonly string Executable,Arguments;
  private ServerCommand(string executable,string arguments){Executable=executable;Arguments=arguments;}
  public static ServerCommand Create(string directory,bool windows,bool stop,string arguments){
   if(windows){string path=directory.TrimEnd('\\','/')+"\\"+(stop?"server_stop.py":"server_runtime.py");return new ServerCommand("py.exe","-3 \""+path+"\" "+arguments);}
   string native=directory.Replace('\\','/').TrimEnd('/');if(native.StartsWith("Z:/",System.StringComparison.OrdinalIgnoreCase))native=native.Substring(2);
   return new ServerCommand(@"C:\windows\system32\start.exe","/unix \""+native+"/"+(stop?"server_stop.py":"server_start.sh")+"\" "+arguments);
  }
 }
}
