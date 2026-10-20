using System;
using UnityEditor;
using UnityEngine;
using WinterYard;

namespace WinterYardEditor
{
    public static class SnowChecks
    {
        static void Check(bool condition,string message)
        {
            if(!condition)throw new Exception("Snow check failed: "+message);
        }
        [MenuItem("Winter Yard/Run snow surface checks")]
        public static void Run()
        {
            var snapshot=SnowSurface.CapturePacking();
            try
            {
            SnowSurface.ResetPacking();
            var packed=SnowSurface.Sample(0,12);
            var loose=SnowSurface.Sample(12,12);
            var ice=SnowSurface.Sample(0,0);
            Check(loose.Depth>packed.Depth&&loose.Speed<packed.Speed,"deeper snow is slower");
            Check(loose.Acceleration<packed.Acceleration&&loose.Damping>packed.Damping,"viscous acceleration and braking");
            Check(loose.Dash<packed.Dash&&loose.Jump<packed.Jump,"snow resists dash and takeoff");
            Check(ice.IsIce&&ice.Damping<packed.Damping,"summit remains slippery");
            Check(SnowSurface.Height(0,0)==0,"snow mesh does not cover summit");
            Check(Mathf.Approximately(SnowSurface.Height(12,12),loose.Depth),"rendered snow depth matches movement field");
            for(int x=-20;x<=20;x++)for(int z=-20;z<=20;z++)
            {
                var surface=SnowSurface.Sample(x,z);
                Check(surface.Speed>0&&surface.Acceleration>0&&surface.Damping>0,"movement stays possible");
                Check(surface.Depth>=0&&surface.Depth<=.501f,"depth stays bounded");
                Check(Mathf.Abs(SnowSurface.Height(x+.01f,z)-SnowSurface.Height(x,z))<.02f,"no discontinuity in snow mesh");
            }
            float original=SnowSurface.Height(12,12);
            for(int step=0;step<30;step++)SnowSurface.Pack(new Vector3(12,0,12));
            var trodden=SnowSurface.Sample(12,12);
            Check(trodden.Depth<loose.Depth&&trodden.Speed>loose.Speed,"steps compact snow and improve movement");
            Check(SnowSurface.Height(12,12)<original,"steps lower the actual surface");
            Check(trodden.Depth>=.044f,"compaction does not remove all snow");
            Check(Mathf.Approximately(SnowSurface.Height(-12,-12),SnowSurface.Sample(-12,-12).Depth),"other regions remain coherent");
            SnowSurface.Pack(Vector3.zero);Check(SnowSurface.Sample(0,0).IsIce,"summit stays ice");
            SnowSurface.ResetPacking();Check(Mathf.Approximately(original,SnowSurface.Height(12,12)),"new match restores snow");
            Debug.Log("Winter Yard: snow surface and compaction checks passed. Check collisions and ragdoll in Play mode.");
            }
            finally { SnowSurface.RestorePacking(snapshot); }
        }
    }
}
