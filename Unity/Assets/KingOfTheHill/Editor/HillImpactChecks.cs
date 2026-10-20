using System;
using UnityEditor;
using UnityEngine;
using WinterYard;

namespace WinterYardEditor
{
    public static class HillImpactChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Hill impact check failed: "+message);}
        [MenuItem("Winter Yard/Run hill impact rules checks")]
        public static void Run()
        {
            Check(HillImpact.IsStandingOnHill(new Vector3(0,4.05f,1),true),"summit counts");
            Check(HillImpact.IsStandingOnHill(new Vector3(4,3,0),true),"slope counts");
            Check(!HillImpact.IsStandingOnHill(new Vector3(0,4.05f,1),false),"airborne fighter does not count");
            Check(!HillImpact.IsStandingOnHill(new Vector3(12,1.1f,0),true),"flat yard does not count");
            Vector3 center=new Vector3(0,4.05f,1),edge=new Vector3(0,4.05f,1.9f);
            Check(!HillImpact.ShouldTumble(center,true,Vector3.forward*8,Vector3.zero,1,.6f,false),"stable center absorbs an ordinary push");
            Check(HillImpact.ShouldTumble(edge,true,Vector3.forward*8,Vector3.zero,1,.6f,false),"strong outward edge push tumbles");
            Check(!HillImpact.ShouldTumble(edge,true,Vector3.back*8,Vector3.zero,1,.6f,false),"inward edge push does not tumble");
            Check(!HillImpact.ShouldTumble(edge,true,Vector3.right*8,Vector3.zero,1,.6f,false),"tangential push does not tumble");
            Check(!HillImpact.ShouldTumble(edge,true,Vector3.forward*3,Vector3.zero,1,.6f,false),"weak edge push does not tumble");
            Check(!HillImpact.ShouldTumble(edge,true,Vector3.forward*8,Vector3.zero,2,.6f,false),"mass resists loss of balance");
            Check(HillImpact.ShouldTumble(center,true,Vector3.forward*8,Vector3.zero,1,1.8f,false),"head hit still tumbles");
            Check(HillImpact.ShouldTumble(center,true,Vector3.forward*8,Vector3.zero,1,.6f,true),"super still tumbles");
            var kick=HillImpact.Kick(new Vector3(8,2,0));
            Check(Vector3.ProjectOnPlane(kick,Vector3.up).magnitude<=3.201f&&kick.y<=.351f,"hill kick stays small and low");
            Check(kick.x>0&&Mathf.Abs(kick.z)<.001f,"hit direction preserved");
            var velocity=HillImpact.InheritedVelocity(new Vector3(20,10,0));
            Check(velocity.x<=4.001f&&velocity.y<=1.201f,"existing dash cannot cause a high launch");
            Check(HillImpact.MinimumFallSeconds>.6f,"fall lasts longer than regular stagger");
            Debug.Log("Hill impact rules checks passed. Observe rolling, slope contact and get-up in Play Mode.");
        }
    }
}
