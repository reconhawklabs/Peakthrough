using System;
namespace PeakCreativeMode.Core {
 public sealed class ItemSwing {
  public const float Duration=.38f;
  private float _elapsed=Duration;
  public bool Active=>_elapsed<Duration;
  public float Arc=>Active?(float)Math.Sin(Math.PI*_elapsed/Duration):0;
  public void Start(){if(!Active)_elapsed=0;}
  public void Advance(float seconds){if(float.IsFinite(seconds)&&seconds>0)_elapsed=Math.Min(Duration,_elapsed+seconds);}
  public void Reset()=>_elapsed=Duration;
 }
}
