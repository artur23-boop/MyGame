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
            Debug.Log("Winter Yard: snow surface checks passed. Check physical movement, footprint placement and particles in Play mode.");
        }
    }
}
