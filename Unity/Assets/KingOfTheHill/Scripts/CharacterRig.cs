using System.Collections.Generic;
using UnityEngine;

namespace WinterYard
{
    // Procedurally animated winter outfit; the same articulated parts become a ragdoll.
    public sealed class CharacterRig : MonoBehaviour
    {
        public Fighter Owner {get;private set;}
        public bool IsRagdoll {get;private set;}
        public Vector3 HipPosition => bones[0].position;
        readonly List<Rigidbody> bones=new List<Rigidbody>();
        readonly List<Collider> colliders=new List<Collider>();
        readonly List<CharacterJoint> joints=new List<CharacterJoint>();
        readonly List<Vector3> restPositions=new List<Vector3>();
        readonly List<Quaternion> restRotations=new List<Quaternion>();
        readonly List<Material> ownedMaterials=new List<Material>();
        Transform originalParent;
        float gait,action,recoverBlend,slideStarted;
        bool hillSlide;
        PhysicsMaterial slideMaterial;
        bool throwing;
        public void Build(Fighter owner,Color color,float mass)
        {
            Owner=owner;originalParent=owner.transform;
            var coat=Arena.Material(color);var skin=Arena.Material(new Color(.87f,.68f,.53f));
            ownedMaterials.Add(coat);ownedMaterials.Add(skin);
            Bone("Hips",new Vector3(0,-.28f,0),new Vector3(.64f,.34f,.5f),PrimitiveType.Cube,coat,-1,Vector3.zero,.2f);
            Bone("Jacket chest",new Vector3(0,.2f,0),new Vector3(.78f,.65f,.55f),PrimitiveType.Cube,coat,0,new Vector3(0,-.04f,0),.25f);
            Bone("Head",new Vector3(0,.76f,.02f),Vector3.one*.53f,PrimitiveType.Sphere,skin,1,new Vector3(0,.51f,0),.08f);
            // Indices 3..6 arms, 7..10 legs; joint anchors are in rig-local rest space.
            for(int side=-1;side<=1;side+=2)
            {
                int upper=bones.Count;
                Bone("Upper arm",new Vector3(side*.52f,.18f,0),new Vector3(.27f,.23f,.3f),PrimitiveType.Capsule,coat,1,new Vector3(side*.36f,.4f,0),.045f);
                Bone("Forearm mitten",new Vector3(side*.58f,-.22f,.08f),new Vector3(.27f,.22f,.3f),PrimitiveType.Capsule,coat,upper,new Vector3(side*.55f,-.02f,.03f),.035f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                int upper=bones.Count;
                Bone("Thigh",new Vector3(side*.2f,-.56f,0),new Vector3(.3f,.2f,.32f),PrimitiveType.Capsule,coat,0,new Vector3(side*.2f,-.37f,0),.08f);
                Bone("Shin boot",new Vector3(side*.2f,-.91f,.06f),new Vector3(.3f,.16f,.39f),PrimitiveType.Capsule,Arena.Dark,upper,new Vector3(side*.2f,-.72f,0),.065f);
            }
            Arena.Shape("Hat",PrimitiveType.Cylinder,new Vector3(0,.2f,0),new Vector3(.59f,.13f,.59f),coat,false,bones[2].transform);
            Arena.Shape("Pom pom",PrimitiveType.Sphere,new Vector3(0,.39f,0),Vector3.one*.18f,Arena.Snow,false,bones[2].transform);
            Arena.Shape("Scarf",PrimitiveType.Cube,new Vector3(0,.27f,.02f),new Vector3(.69f,.12f,.58f),Arena.Warm,false,bones[1].transform);
            if(owner.IsPlayer)Arena.Shape("Your marker",PrimitiveType.Sphere,new Vector3(0,.58f,0),Vector3.one*.14f,Arena.Warm,false,bones[2].transform);
            float total=0;foreach(var bone in bones)total+=bone.mass;
            foreach(var bone in bones)bone.mass=mass*bone.mass/total;
            for(int i=0;i<colliders.Count;i++)
            {
                Physics.IgnoreCollision(colliders[i],owner.GetComponent<Collider>());
                for(int j=i+1;j<colliders.Count;j++)Physics.IgnoreCollision(colliders[i],colliders[j]);
                colliders[i].enabled=false;
            }
        }
        void Bone(string name,Vector3 position,Vector3 scale,PrimitiveType type,Material material,int connected,Vector3 anchor,float mass)
        {
            // Keep physics transforms at unit scale; only the rendered/collider primitive is scaled.
            var pivot=new GameObject(name);pivot.transform.SetParent(transform,false);pivot.transform.localPosition=position;pivot.layer=10;
            var visual=Arena.Shape(name+" mesh",type,Vector3.zero,scale,material,true,pivot.transform);visual.layer=10;
            var rb=pivot.AddComponent<Rigidbody>();rb.mass=mass;rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.solverIterations=10;rb.maxAngularVelocity=15;
            rb.linearDamping=.15f;rb.angularDamping=1.5f;
            if(connected>=0)
            {
                var joint=pivot.AddComponent<CharacterJoint>();joint.connectedBody=bones[connected];
                joint.autoConfigureConnectedAnchor=false;
                joint.anchor=anchor-position;joint.connectedAnchor=anchor-restPositions[connected];
                joint.axis=Vector3.right;joint.swingAxis=Vector3.forward;
                joint.lowTwistLimit=new SoftJointLimit{limit=-35};joint.highTwistLimit=new SoftJointLimit{limit=35};
                joint.swing1Limit=new SoftJointLimit{limit=45};joint.swing2Limit=new SoftJointLimit{limit=25};
                joint.enableProjection=true;joint.projectionDistance=.08f;joint.projectionAngle=15;joint.enableCollision=false;
                joints.Add(joint);
            }
            bones.Add(rb);colliders.Add(visual.GetComponent<Collider>());restPositions.Add(position);restRotations.Add(Quaternion.identity);
        }
        public void PlayAttack(bool isThrow){action=.35f;throwing=isThrow;}
        public void Animate(float dt,float speed,float depth,bool grounded,float stun)
        {
            if(IsRagdoll)return;
            action=Mathf.Max(0,action-dt);recoverBlend=Mathf.MoveTowards(recoverBlend,0,dt*2);
            gait+=dt*Mathf.Lerp(3,8,Mathf.Clamp01(speed/4.8f));
            float stride=grounded&&stun<=0?Mathf.Sin(gait)*Mathf.Clamp01(speed/2):0;
            for(int i=0;i<bones.Count;i++){bones[i].transform.localPosition=restPositions[i];bones[i].transform.localRotation=restRotations[i];}
            float sink=grounded?depth*.3f:0;
            transform.localPosition=new Vector3(0,-sink+Mathf.Abs(stride)*.045f-recoverBlend*.25f,0);
            transform.localRotation=Quaternion.Euler(stun>0?Mathf.Sin(gait*4)*12:recoverBlend*35,0,0);
            for(int side=0;side<2;side++)
            {
                float step=stride*(side==0?1:-1);int leg=7+side*2,arm=3+side*2;
                bones[leg].transform.localRotation=Quaternion.Euler(step*28,0,0);
                bones[leg+1].transform.localPosition+=new Vector3(0,Mathf.Max(0,step)*(.12f+depth*.35f),step*.12f);
                bones[leg+1].transform.localRotation=Quaternion.Euler(-Mathf.Max(0,step)*35,0,0);
                bones[arm].transform.localRotation=Quaternion.Euler(-step*24,0,side==0?8:-8);
                bones[arm+1].transform.localPosition+=Vector3.forward*(-step*.12f);
                if(!grounded){bones[arm].transform.localRotation=Quaternion.Euler(-25,0,side==0?35:-35);}
                if(action>0&&(!throwing||side==1))
                {
                    float reach=Mathf.Sin(action/.35f*Mathf.PI);
                    bones[arm].transform.localRotation=Quaternion.Euler(-reach*85,0,0);
                    bones[arm+1].transform.localPosition+=Vector3.forward*reach*.48f;
                }
            }
        }
        public void Fall(Vector3 velocity,Vector3 impulse,float controllerMass,bool fromHill=false)
        {
            if(IsRagdoll)return;
            IsRagdoll=true;hillSlide=fromHill;slideStarted=Time.time;
            if(!slideMaterial)slideMaterial=new PhysicsMaterial("Ragdoll sliding in snow"){dynamicFriction=.12f,staticFriction=.18f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};
            transform.SetParent(GameSession.Instance.transform,true);
            float previousMass=0;foreach(var bone in bones)previousMass+=bone.mass;
            foreach(var bone in bones)bone.mass=Mathf.Max(.1f,controllerMass)*bone.mass/previousMass;
            float total=0;foreach(var bone in bones)total+=bone.mass;
            // Return to the coherent joint rest pose before physics takes ownership.
            transform.rotation=Owner.transform.rotation;
            for(int i=0;i<bones.Count;i++)
            {
                bones[i].transform.localPosition=restPositions[i];bones[i].transform.localRotation=restRotations[i];
                colliders[i].sharedMaterial=fromHill?slideMaterial:null;
                colliders[i].enabled=true;bones[i].isKinematic=false;
                bones[i].linearDamping=fromHill?.1f:.15f;bones[i].angularDamping=fromHill?.6f:1.5f;
                bones[i].linearVelocity=velocity;bones[i].angularVelocity=Vector3.zero;
                bones[i].AddForce(impulse*(bones[i].mass/total),ForceMode.Impulse);
            }
            if(fromHill)
            {
                Vector3 horizontal=Vector3.ProjectOnPlane(impulse,Vector3.up).normalized;
                if(horizontal.sqrMagnitude<.01f)horizontal=Owner.transform.forward;
                Vector3 axis=Vector3.Cross(Vector3.up,horizontal);
                bones[0].AddTorque(axis*.15f*total,ForceMode.Impulse);
                bones[1].AddTorque(axis*.1f*total,ForceMode.Impulse);
            }
            else bones[1].AddTorque(Owner.transform.right*1.5f,ForceMode.Impulse);
        }
        public void SimulateHillSlide()
        {
            if(!IsRagdoll||!hillSlide)return;
            var p=HipPosition;
            if(p.x*p.x+p.z*p.z>53||!Physics.Raycast(p+Vector3.up*.25f,Vector3.down,out var hit,.95f,1<<Arena.GroundLayer))return;
            // Only contact on an actual slope gets assistance; no radial force in mid-air.
            if(hit.normal.y>=.98f||hit.normal.y<.4f)return;
            Vector3 downhill=Vector3.ProjectOnPlane(Physics.gravity,hit.normal)*.25f;
            foreach(var bone in bones)if(bone.linearVelocity.magnitude<7)bone.AddForce(downhill,ForceMode.Acceleration);
        }
        public bool Recover(Rigidbody controller,Collider capsule)
        {
            // Let the body finish rolling instead of snapping upright on the slope.
            if(hillSlide&&Time.time-slideStarted<4.5f&&bones[0].linearVelocity.magnitude>1.25f)return false;
            Vector3 hips=HipPosition;
            if(hips.y<-5)hips=new Vector3(0,1,-11);
            for(int i=0;i<9;i++)
            {
                float angle=i*Mathf.PI*.25f;float radius=i==0?0:.85f;
                Vector3 candidate=hips+new Vector3(Mathf.Cos(angle)*radius,2,Mathf.Sin(angle)*radius);
                if(!Physics.Raycast(candidate,Vector3.down,out var hit,5,1<<Arena.GroundLayer))continue;
                if(hit.normal.y<.5f||hips.y-hit.point.y>1.5f)continue;
                Vector3 foot=hit.point+Vector3.up*.16f;
                if(Physics.CheckCapsule(foot+Vector3.up*.43f,foot+Vector3.up*1.68f,.42f,1<<Arena.GroundLayer))continue;
                for(int b=0;b<bones.Count;b++){bones[b].linearVelocity=Vector3.zero;bones[b].angularVelocity=Vector3.zero;bones[b].isKinematic=true;colliders[b].enabled=false;}
                controller.position=foot+Vector3.up*1.05f;controller.isKinematic=false;controller.linearVelocity=Vector3.zero;
                transform.SetParent(originalParent,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;
                for(int b=0;b<bones.Count;b++){bones[b].transform.localPosition=restPositions[b];bones[b].transform.localRotation=restRotations[b];}
                capsule.enabled=true;IsRagdoll=false;hillSlide=false;recoverBlend=1;return true;
            }
            return false;
        }
        void OnDestroy(){foreach(var material in ownedMaterials)if(material)Destroy(material);if(slideMaterial)Destroy(slideMaterial);}
    }
}
