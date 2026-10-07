using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 public interface IBridgeTransport
 {
  BridgeStatus Status {get;}string LastError{get;}void Start();void Stop();void Tick();bool Send(string line);bool TryReceive(out JObject message);
 }
}
