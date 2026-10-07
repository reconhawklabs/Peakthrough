using System.Linq;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
    internal sealed class WorldDiagnostics:MonoBehaviour
    {
        private Character _measured;
        public static bool Playing(Character ch)=>ch!=null&&!ch.inAirport&&!ch.data.dead;
        public static Vector3 Feet(Character ch)
        {
            Vector3 p=ch.Center;float lowest=float.PositiveInfinity;
            // Read public Bodypart identity; GetBodypart is internal in this PEAK build.
            foreach(var part in ch.GetComponentsInChildren<Bodypart>())
            {
                if(part.partType!=BodypartType.Foot_L&&part.partType!=BodypartType.Foot_R)continue;
                foreach(var col in part.GetComponentsInChildren<Collider>())if(col.enabled&&!col.isTrigger)lowest=Mathf.Min(lowest,col.bounds.min.y);
            }
            if(!float.IsInfinity(lowest))p.y=lowest;
            else p.y=ch.Head.y-ch.data.currentHeadHeight;
            return p;
        }
        private void Update()
        {
            var ch=Character.localCharacter;if(ch==null||ch.inAirport||ch==_measured)return;_measured=ch;
            var progress=Object.FindAnyObjectByType<MountainProgressHandler>();
            float summit=progress!=null?progress.progressPoints.Where(p=>p.transform!=null).Max(p=>p.transform.position.y):float.NaN;
            Plugin.Log.LogInfo($"[height] beach feet y={Feet(ch).y:F3}, torso y={ch.Center.y:F3}, summit progress y={summit:F3}, block size=1 (user-accepted)");
        }
    }
}
