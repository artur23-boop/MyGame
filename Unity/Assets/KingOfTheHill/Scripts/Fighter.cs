using UnityEngine;

namespace WinterYard
{
    [RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider))]
    public sealed class Fighter : MonoBehaviour
    {
        public string DisplayName;
        public Color CoatColor;
        public bool IsPlayer;
        public float Score { get; private set; }
        public float PushCooldown { get; private set; }
        public float DashCooldown { get; private set; }
        public float Stun { get; private set; }
        public readonly CombatState Combat=new CombatState();
        public bool HasSnow => Combat.HasSnow;
        public bool OnSummit { get; private set; }
        Rigidbody body;
        Transform model, leftBoot, rightBoot;
        public SnowSample Surface {get;private set;}
        float sink;
        bool leftStep;
        Vector3 move, facing=Vector3.forward;
        public Vector3 AimDirection => facing;
        public const float PushStrength=3.5f;
        public const float PushLift=.5f;
        float think, stepClock, jumpBuffer, dashBuffer, animationTime;
        bool pushRequested, throwRequested, superRequested;
        public bool Grounded => Physics.Raycast(transform.position, Vector3.down, 1.2f,1<<Arena.GroundLayer,QueryTriggerInteraction.Ignore);

        public void Initialize(string label, Color color, bool player)
        {
            DisplayName=label;CoatColor=color;IsPlayer=player;
            body=GetComponent<Rigidbody>();body.mass=1;body.linearDamping=.8f;body.angularDamping=5;
            body.constraints=RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var capsule=GetComponent<CapsuleCollider>();capsule.height=2.1f;capsule.radius=.42f;
            capsule.sharedMaterial=new PhysicsMaterial("Boots") {dynamicFriction=.15f,staticFriction=.15f};
            var visual=new GameObject("Winter outfit");visual.transform.SetParent(transform,false);model=visual.transform;
            var coat=Arena.Material(color);var skin=Arena.Material(new Color(.87f,.68f,.53f));
            Arena.Shape("Puffer jacket",PrimitiveType.Capsule,new Vector3(0,-.05f,0),new Vector3(.82f,.6f,.65f),coat,false,model);
            Arena.Shape("Face",PrimitiveType.Sphere,new Vector3(0,.7f,.04f),Vector3.one*.55f,skin,false,model);
            Arena.Shape("Hat",PrimitiveType.Cylinder,new Vector3(0,.93f,0),new Vector3(.6f,.14f,.6f),coat,false,model);
            Arena.Shape("Pom pom",PrimitiveType.Sphere,new Vector3(0,1.13f,0),Vector3.one*.2f,Arena.Snow,false,model);
            Arena.Shape("Scarf",PrimitiveType.Cube,new Vector3(0,.43f,.02f),new Vector3(.67f,.13f,.6f),Arena.Warm,false,model);
            for(int side=-1;side<=1;side+=2)
            {
                var boot=Arena.Shape("Boot",PrimitiveType.Capsule,new Vector3(side*.22f,-.73f,.05f),new Vector3(.28f,.25f,.4f),Arena.Dark,false,model);
                if(side<0)leftBoot=boot.transform;else rightBoot=boot.transform;
                Arena.Shape("Mitten",PrimitiveType.Sphere,new Vector3(side*.53f,-.05f,.12f),Vector3.one*.27f,coat,false,model);
            }
            if(player)gameObject.AddComponent<SnowballAim>();
            if(player) Arena.Shape("Your marker",PrimitiveType.Sphere,new Vector3(0,1.5f,0),Vector3.one*.16f,Arena.Warm,false,model);
        }

        void Update()
        {
            if(!GameSession.Instance.Running)return;
            if(IsPlayer)
            {
                float x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
                float z=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
                var camera=GameSession.Instance.View;
                var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
                var right=Vector3.Cross(Vector3.up,forward);
                move=Vector3.ClampMagnitude(forward*z+right*x,1);
                facing=forward;
                if(Input.GetKeyDown(KeyCode.Space))jumpBuffer=.15f;
                if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))dashBuffer=.15f;
                pushRequested|=Input.GetMouseButtonDown(0);
                throwRequested|=Input.GetMouseButtonDown(1);
                superRequested|=Input.GetKeyDown(KeyCode.Q);
            }
            float walkSpeed=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).magnitude;
            animationTime+=Time.deltaTime*Mathf.Lerp(.6f,1.2f,Mathf.Clamp01(walkSpeed/4.8f));
            bool walking=Grounded&&walkSpeed>.25f&&Stun<=0;
            sink=Mathf.MoveTowards(sink,Grounded?Surface.Depth*.4f:0,Time.deltaTime*1.8f);
            if(model)
            {
                float tilt=Combat.KnockdownRemaining>0?85:Stun>0?Mathf.Sin(animationTime*14)*45:0;
                model.localRotation=Quaternion.Euler(tilt,0,0);
                model.localPosition=Combat.KnockdownRemaining>0?new Vector3(0,-.65f,0):Stun>0?Vector3.zero:Vector3.up*((walking?Mathf.Abs(Mathf.Sin(animationTime*7))*.075f:0)-sink);
                float stride=walking?Mathf.Sin(animationTime*7):0;
                float lift=.12f+Surface.Depth*.45f;
                leftBoot.localPosition=new Vector3(-.22f,-.73f+Mathf.Max(0,stride)*lift,.05f+stride*.16f);
                rightBoot.localPosition=new Vector3(.22f,-.73f+Mathf.Max(0,-stride)*lift,.05f-stride*.16f);
            }
        }

        void FixedUpdate()
        {
            if(!GameSession.Instance.Running)return;
            float dt=Time.fixedDeltaTime;
            Combat.Tick(dt);
            bool grounded=Grounded;
            Surface=SnowSurface.Sample(transform.position.x,transform.position.z);
            body.linearDamping=grounded?Surface.Damping:.05f;
            PushCooldown=Mathf.Max(0,PushCooldown-dt);DashCooldown=Mathf.Max(0,DashCooldown-dt);Stun=Mathf.Max(0,Stun-dt);
            if(!IsPlayer)Think(dt);
            if(Stun<=0&&Combat.KnockdownRemaining<=0)
            {
                var velocity=body.linearVelocity;
                var horizontal=new Vector3(velocity.x,0,velocity.z);
                float acceleration=grounded?Surface.Acceleration:6;
                if(grounded&&Surface.IsIce&&move.sqrMagnitude<.01f)acceleration=2;
                var desired=move*(grounded?Surface.Speed:5);
                // Limit acceleration, preserving impulses instead of overwriting knockback.
                body.AddForce(Vector3.ClampMagnitude(desired-horizontal,acceleration*dt),ForceMode.VelocityChange);
                if(facing.sqrMagnitude>.01f)body.MoveRotation(Quaternion.LookRotation(facing));
                if(jumpBuffer>0&&grounded){body.AddForce(Vector3.up*Surface.Jump,ForceMode.VelocityChange);jumpBuffer=0;}
                if(dashBuffer>0&&DashCooldown<=0){body.AddForce((move.sqrMagnitude>.01f?move.normalized:facing)*(grounded?Surface.Dash:9),ForceMode.VelocityChange);DashCooldown=3;dashBuffer=0;}
                if(pushRequested)Push();
                if((superRequested&&Combat.SuperReady)||throwRequested)
                {
                    bool super=superRequested&&Combat.SuperReady;
                    if(Combat.TryThrow(super)){Snowball.Launch(this,facing,super);GameSession.Instance.Beep(super?900:650,.08f);}
                }
            }
            jumpBuffer=Mathf.Max(0,jumpBuffer-dt);dashBuffer=Mathf.Max(0,dashBuffer-dt);
            pushRequested=throwRequested=superRequested=false;
            var p=transform.position;
            OnSummit=new Vector2(p.x,p.z).magnitude<2.2f&&p.y>3.8f&&Grounded&&Stun<=0&&Combat.KnockdownRemaining<=0;
            if(OnSummit){Score+=dt;if(Score>=GameSession.Instance.Target)GameSession.Instance.Finish(this);}
            if(Stun>0&&p.y>1.3f&&!grounded)body.AddForce(new Vector3(p.x,0,p.z).normalized*2,ForceMode.Acceleration);
            if(p.y<-5){body.position=new Vector3(0,2,-12);body.linearVelocity=Vector3.zero;}
            var planarSpeed=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).magnitude;
            if(grounded&&planarSpeed>.65f&&Stun<=0)
            {
                stepClock-=dt;
                if(stepClock<=0)
                {
                    stepClock=Mathf.Lerp(.44f,.26f,Mathf.Clamp01(planarSpeed/5));leftStep=!leftStep;
                    if(!Surface.IsIce&&SnowFootprints.Instance&&Physics.Raycast(transform.position,Vector3.down,out var hit,1.3f,1<<Arena.GroundLayer))
                        SnowFootprints.Instance.Step(hit.point,hit.normal,transform.forward,Surface.Depth,leftStep);
                    if(IsPlayer)GameSession.Instance.Beep(Surface.Depth>.2f?65:95,.055f,.025f+Surface.Depth*.06f);
                }
            }
        }

        void Think(float dt)
        {
            think-=dt;
            if(think>0)return;
            think=GameSession.Instance.Difficulty==0?.65f:GameSession.Instance.Difficulty==2?.18f:.35f;
            var p=transform.position;var destination=Vector3.zero;
            float radius=new Vector2(p.x,p.z).magnitude;
            Fighter enemy=null;float distance=float.MaxValue;
            foreach(var other in GameSession.Instance.Fighters)
            {
                if(other==this)continue;
                float d=Vector3.Distance(p,other.transform.position);
                if(d<distance){enemy=other;distance=d;}
            }
            if(enemy&&distance<3&&radius<8)destination=enemy.transform.position;
            move=Vector3.ProjectOnPlane(destination-p,Vector3.up);
            move=move.magnitude<.35f?Vector3.zero:move.normalized;
            facing=move.sqrMagnitude>.01f?move:transform.forward;
            if(enemy&&distance<2.2f){facing=Vector3.ProjectOnPlane(enemy.transform.position-p,Vector3.up).normalized;pushRequested=true;}
            if(enemy&&distance>4&&distance<11&&HasSnow&&Random.value<.15f)
            {
                facing=Vector3.ProjectOnPlane(enemy.transform.position-p,Vector3.up).normalized;
                if(Combat.SuperReady)superRequested=true;else throwRequested=true;
            }
        }

        void Push()
        {
            if(PushCooldown>0)return;
            PushCooldown=.8f;
            GameSession.Instance.Beep(150,.1f);
            bool landed=false;
            foreach(var other in GameSession.Instance.Fighters)
            {
                if(other==this||other.Stun>1)continue;
                var delta=other.transform.position-transform.position;
                if(delta.magnitude<2.3f&&Vector3.Dot(delta.normalized,facing)>.35f)
                    landed|=other.Hit(facing*PushStrength+Vector3.up*PushLift,.6f);
            }
            if(landed)Combat.RegisterHit();else Combat.BreakCombo();
        }

        public bool Hit(Vector3 impulse,float stun,bool knockdown=false)
        {
            // A downed opponent cannot be farmed for combo or kept down by repeated hits.
            if(Combat.KnockdownRemaining>0)return false;
            Combat.BreakCombo();
            if(knockdown)Combat.KnockDown();
            Stun=Mathf.Max(Stun,knockdown?CombatState.KnockdownSeconds:stun);OnSummit=false;
            body.AddForce(impulse,ForceMode.VelocityChange);
            GameSession.Instance.Beep(knockdown?110:220,.14f);
            return true;
        }
    }
}
