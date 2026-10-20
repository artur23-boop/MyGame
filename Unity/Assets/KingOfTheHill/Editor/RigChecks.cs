using System;
using UnityEditor;
using UnityEngine;
using WinterYard;

namespace WinterYardEditor
{
    public static class RigChecks
    {
        [MenuItem("Winter Yard/Run ragdoll structure checks (Play Mode)")]
        public static void Run()
        {
            if(!Application.isPlaying||!GameSession.Instance)throw new Exception("Open WinterYard and enter Play Mode first.");
            var obj=new GameObject("Temporary ragdoll verification");obj.transform.position=new Vector3(0,20,0);
            Fighter actor=null;GameObject hillActor=null;
            try
            {
                actor=obj.AddComponent<Fighter>();actor.Initialize("Rig check",Color.gray,false);
                var rig=obj.GetComponentInChildren<CharacterRig>();
                var limbs=rig.GetComponentsInChildren<Rigidbody>();var joints=rig.GetComponentsInChildren<CharacterJoint>();
                if(limbs.Length!=11||joints.Length!=10)throw new Exception("Expected 11 bodies and 10 joints.");
                foreach(var joint in joints)if(!joint.connectedBody||!joint.enableProjection)throw new Exception("Unconnected or unbounded joint.");
                if(!actor.Hit(Vector3.forward*8,5,true)||!actor.IsRagdoll)throw new Exception("Super must activate ragdoll.");
                if(obj.GetComponent<Collider>().enabled||!obj.GetComponent<Rigidbody>().isKinematic)throw new Exception("Controller must yield to ragdoll.");
                foreach(var body in limbs)if(body.isKinematic)throw new Exception("Ragdoll limbs must be dynamic.");
                if(actor.Hit(Vector3.forward,5,true))throw new Exception("Downed target must reject repeated hits.");
                if(actor.Combat.KnockdownRemaining!=5)throw new Exception("Super duration must remain five seconds.");
                hillActor=new GameObject("Temporary hill push verification");hillActor.transform.position=new Vector3(0,4.06f,1);
                var hillFighter=hillActor.AddComponent<Fighter>();hillFighter.Initialize("Hill check",Color.gray,false);
                Physics.SyncTransforms();
                if(!hillFighter.Grounded)throw new Exception("Hill test requires the generated courtyard ground.");
                if(!hillFighter.Hit(Vector3.forward*8+Vector3.up*2,.6f)||!hillFighter.IsRagdoll)throw new Exception("A regular push on the hill must activate ragdoll.");
                if(hillFighter.Stun<HillImpact.MinimumFallSeconds)throw new Exception("Hill fall must not recover immediately.");
                Debug.Log("Ragdoll structure, super activation and hill-push activation checks passed. Observe actual rolling/get-up in a real match separately.");
            }
            finally { UnityEngine.Object.Destroy(obj);if(hillActor)UnityEngine.Object.Destroy(hillActor); }
        }
    }
}
