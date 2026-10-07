using System.Collections.Generic;
namespace PeakCreativeMode.Core {
 /// Confirmed map data survives geometry/character loading; transport identity owns its lifetime.
 public sealed class TerraformQueue {
  private readonly Dictionary<string,TerraformVolume> _known=new Dictionary<string,TerraformVolume>();
  public readonly Dictionary<string,TerraformVolume> Pending=new Dictionary<string,TerraformVolume>();public readonly HashSet<string> Applied=new HashSet<string>(),Failed=new HashSet<string>();
  public void Add(TerraformVolume v){if(!_known.ContainsKey(v.Id)&&_known.Count>=256)return;_known[v.Id]=v;if(!Applied.Contains(v.Id)&&!Failed.Contains(v.Id))Pending[v.Id]=v;}
  public void ResetGeometry(){Pending.Clear();Applied.Clear();Failed.Clear();foreach(var pair in _known)Pending[pair.Key]=pair.Value;}
  public void Clear(){_known.Clear();ResetGeometry();}
 }
}
