using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PeakCreativeMode.Core
{
    public enum BridgeStatus { Offline, Connecting, Connected, Rejected }

    /// Background TCP client. Game code calls Send/TryReceive from its main thread; all I/O is on worker threads.
    public sealed class BridgeClient : IBridgeTransport
    {
        private readonly string _host;
        private readonly int _port;
        private readonly Func<string> _helloFactory;
        private readonly Action<string> _log;
        private readonly int _retryMs;
        private readonly ConcurrentQueue<JObject> _inbox = new ConcurrentQueue<JObject>();
        private readonly BlockingCollection<string> _outbox = new BlockingCollection<string>(new ConcurrentQueue<string>(), 10000);
        private volatile BridgeStatus _status = BridgeStatus.Offline;
        private volatile bool _running;
        private Thread _thread;
        private TcpClient _tcp;

        public BridgeStatus Status => _status;
        public string LastError { get; private set; }

        public BridgeClient(string host, int port, Func<string> helloFactory, Action<string> log, int retryMs = 2000)
        {
            _host = host; _port = port; _helloFactory = helloFactory; _log = log; _retryMs = retryMs;
        }

        public void Tick() { }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "PeakBridge" };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            _status = BridgeStatus.Offline;
            try { _tcp?.Close(); } catch (Exception) { }
        }

        /// Queues a line for sending. Returns false (and drops it) unless connected.
        public bool Send(string line)
        {
            if (line == null || _status != BridgeStatus.Connected) return false;
            return _outbox.TryAdd(line);
        }

        public bool TryReceive(out JObject msg) => _inbox.TryDequeue(out msg);

        private void Run()
        {
            bool loggedOffline = false;
            while (_running)
            {
                try
                {
                    _status = BridgeStatus.Connecting;
                    using (var tcp = new TcpClient())
                    {
                        _tcp = tcp;
                        tcp.NoDelay = true;
                        tcp.Connect(_host, _port);
                        loggedOffline = false;
                        _log($"[bridge] connected to {_host}:{_port}, waiting for welcome");
                        RunConnection(tcp);
                    }
                }
                catch (Exception e) when (e is SocketException || e is IOException || e is ObjectDisposedException || e is ArgumentException)
                {
                    // fall through to offline handling
                }
                if (!_running) break;
                if (_status == BridgeStatus.Rejected) return;
                if (_status == BridgeStatus.Connected) _log("[bridge] connection lost");
                _status = BridgeStatus.Offline;
                if (!loggedOffline)
                {
                    _log($"[bridge] Minecraft server offline, retrying every {_retryMs} ms");
                    loggedOffline = true;
                }
                while (_outbox.TryTake(out _)) { } // drop stale outgoing data
                Thread.Sleep(_retryMs);
            }
        }

        private void RunConnection(TcpClient tcp)
        {
            var stream = tcp.GetStream();
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
            writer.WriteLine(_helloFactory());
            writer.Flush();

            var input = new StreamReader(stream, Encoding.UTF8);
            var reader = new Thread(() => ReadLoop(input)) { IsBackground = true, Name = "PeakBridge-read" };
            reader.Start();

            while (_running && reader.IsAlive)
            {
                if (_outbox.TryTake(out var line, 100))
                {
                    writer.WriteLine(line);
                    if (_outbox.Count == 0) writer.Flush();
                }
            }
            tcp.Close();
            reader.Join(500);
        }

        private void ReadLoop(StreamReader reader)
        {
            bool warned = false;
            try
            {
                string line;
                while (_running && (line = reader.ReadLine()) != null)
                {
                    var msg = Protocol.Parse(line);
                    if (msg == null)
                    {
                        if (!warned) { _log("[bridge] dropped malformed line from Minecraft"); warned = true; }
                        continue;
                    }
                    var t = (string)msg["t"];
                    if (t == "welcome")
                    {
                        if (msg["protocol"]?.Type != JTokenType.Integer || (int)msg["protocol"] != Protocol.Version)
                        {
                            LastError = "protocol mismatch in welcome";
                            _status = BridgeStatus.Rejected;
                            _log("[bridge] " + LastError);
                            return;
                        }
                        LastError = null;
                        _status = BridgeStatus.Connected;
                        _log($"[bridge] welcome from Minecraft {(string)msg["mcVersion"]}");
                    }
                    else if (t == "error")
                    {
                        LastError = (string)msg["reason"];
                        // The previous transport can still be awaiting its disconnect on the server thread.
                        // Never retry protocol/version/save failures; only this known transient identity race.
                        if (LastError == "player already connected in this run")
                        {
                            _status = BridgeStatus.Connecting;
                            return;
                        }
                        _status = BridgeStatus.Rejected;
                        _log($"[bridge] rejected by Minecraft: {LastError}");
                        return;
                    }
                    _inbox.Enqueue(msg);
                }
            }
            catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is ArgumentException)
            {
            }
        }
    }
}
