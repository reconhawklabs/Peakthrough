using System;
using System.Collections.Generic;
namespace PeakCreativeMode.Core {
 public sealed class EffectState {
  private readonly Dictionary<string,(double end,EffectPlan plan)> _active=new Dictionary<string,(double,EffectPlan)>();
  public void Clear()=>_active.Clear();
  public float Apply(string id,double seconds,int amp,double now){float duration=EffectPlan.Duration(seconds);if(string.IsNullOrEmpty(id)||id.Length>128||duration<=0||!double.IsFinite(now))return 0;var plan=EffectPlan.For(id,amp);bool existing=_active.TryGetValue(id,out var old)&&old.end>now;if(!existing&&_active.Count>=64)return 0;_active[id]=(now+duration,plan);return existing?0:plan.InstantInjuryRelief;}
  public EffectPlan At(double now){float fall=1,jump=1,speed=1,healing=0;bool drag=false;foreach(var key in new List<string>(_active.Keys)){var e=_active[key];if(e.end<=now){_active.Remove(key);continue;}fall=Math.Min(fall,e.plan.FallDamageScale);jump=Math.Max(jump,e.plan.JumpMultiplier);speed=Math.Max(speed,e.plan.SpeedMultiplier);healing+=e.plan.InjuryReliefPerSecond;drag|=e.plan.ExtraDrag;}return new EffectPlan(fall,jump,speed,healing,drag:drag);}
 }
}
