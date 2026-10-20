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
        public bool IsRagdoll => rig&&rig.IsRagdoll;
        [Header("Rigidbody physics")]
        [SerializeField,Min(.1f)] float bodyMass=1;
        [SerializeField,Min(.1f)] float movementForceScale=1;
        [SerializeField,Min(0)] float jumpImpulseScale=1;
        [SerializeField,Min(0)] float dashImpulseScale=1;
        [SerializeField,Min(0)] float pushImpulse=8;
        [SerializeField,Min(0)] float pushUpImpulse=2;
        Rigidbody body;
        CharacterRig rig;
        public SnowSample Surface {get;private set;}
        bool leftStep;
        Vector3 move, facing=Vector3.forward;
        public Vector3 AimDirection => facing;
        public const float PushStrength=8f;
        public const float PushLift=2f;
        float think, stepClock, jumpBuffer, dashBuffer;
        bool pushRequested, throwRequested, superRequested;
        public bool Grounded => Physics.Raycast(transform.position, Vector3.down, 1.2f,1<<Arena.GroundLayer,QueryTriggerInteraction.Ignore);

        public void Initialize(string label, Color color, bool player)
        {
            DisplayName=label;CoatColor=color;IsPlayer=player;
            body=GetComponent<Rigidbody>();body.mass=bodyMass;body.linearDamping=.8f;body.angularDamping=5;
            body.constraints=RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var capsule=GetComponent<CapsuleCollider>();capsule.height=2.1f;capsule.radius=.42f;
            capsule.sharedMaterial=new PhysicsMaterial("Boots") {dynamicFriction=.15f,staticFriction=.15f};
            var visual=new GameObject("Animated winter rig");visual.transform.SetParent(transform,false);
            rig=visual.AddComponent<CharacterRig>();rig.Build(this,color,body.mass);
            if(player)gameObject.AddComponent<SnowballAim>();
        }

        void OnValidate()
        {
            bodyMass=Mathf.Max(.1f,bodyMass);
            if(Application.isPlaying&&TryGetComponent<Rigidbody>(out var rigidbody))rigidbody.mass=bodyMass;
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
            rig.Animate(Time.deltaTime,walkSpeed,Surface.Depth,Grounded,Stun);
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
            if(rig.IsRagdoll)
            {
                OnSummit=false;body.position=rig.HipPosition;
                rig.SimulateHillSlide();
                jumpBuffer=dashBuffer=0;pushRequested=throwRequested=superRequested=false;
                if(Stun<=0&&Combat.KnockdownRemaining<=0&&rig.Recover(body,GetComponent<Collider>()))Stun=.45f;
                return;
            }
            if(!IsPlayer)Think(dt);
            if(Stun<=0&&Combat.KnockdownRemaining<=0)
            {
                var velocity=body.linearVelocity;
                var horizontal=new Vector3(velocity.x,0,velocity.z);
                float acceleration=grounded?Surface.Acceleration:6;
                if(grounded&&Surface.IsIce&&move.sqrMagnitude<.01f)acceleration=2;
                var desired=move*(grounded?Surface.Speed:5);
                // Forces are integrated by PhysX in FixedUpdate. A heavier body accelerates
                // less under the same force; target speed and snow resistance remain intact.
                Vector3 driveForce=Vector3.ClampMagnitude((desired-horizontal)/dt,acceleration)*movementForceScale;
                body.AddForce(driveForce,ForceMode.Force);
                if(facing.sqrMagnitude>.01f)body.MoveRotation(Quaternion.LookRotation(facing));
                if(jumpBuffer>0&&grounded){body.AddForce(Vector3.up*Surface.Jump*jumpImpulseScale,ForceMode.Impulse);jumpBuffer=0;}
                if(dashBuffer>0&&DashCooldown<=0){body.AddForce((move.sqrMagnitude>.01f?move.normalized:facing)*(grounded?Surface.Dash:9)*dashImpulseScale,ForceMode.Impulse);DashCooldown=3;dashBuffer=0;}
                if(pushRequested)Push();
                if((superRequested&&Combat.SuperReady)||throwRequested)
                {
                    bool super=superRequested&&Combat.SuperReady;
                    if(Combat.TryThrow(super)){Snowball.Launch(this,facing,super);rig.PlayAttack(true);GameSession.Instance.Beep(super?900:650,.08f);}
                }
            }
            jumpBuffer=Mathf.Max(0,jumpBuffer-dt);dashBuffer=Mathf.Max(0,dashBuffer-dt);
            pushRequested=throwRequested=superRequested=false;
            var p=transform.position;
            OnSummit=new Vector2(p.x,p.z).magnitude<2.2f&&p.y>3.8f&&Grounded&&Stun<=0&&Combat.KnockdownRemaining<=0;
            if(OnSummit){Score+=dt;if(Score>=GameSession.Instance.Target)GameSession.Instance.Finish(this);}
            if(Stun>0&&p.y>1.3f)body.AddForce(new Vector3(p.x,0,p.z).normalized*6,ForceMode.Acceleration);
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
            PushCooldown=.8f;rig.PlayAttack(false);
            GameSession.Instance.Beep(150,.1f);
            bool landed=false;
            foreach(var other in GameSession.Instance.Fighters)
            {
                if(other==this||other.Stun>1)continue;
                var delta=other.transform.position-transform.position;
                if(delta.magnitude<2.3f&&Vector3.Dot(delta.normalized,facing)>.35f)
                    landed|=other.Hit(facing*pushImpulse+Vector3.up*pushUpImpulse,.6f);
            }
            if(landed)Combat.RegisterHit();else Combat.BreakCombo();
        }

        public bool Hit(Vector3 impulse,float stun,bool knockdown=false)
        {
            // A downed opponent cannot be farmed for combo or kept down by repeated hits.
            if(Combat.KnockdownRemaining>0||rig.IsRagdoll)return false;
            Combat.BreakCombo();
            bool hillFall=HillImpact.IsStandingOnHill(transform.position,Grounded);
            if(knockdown)Combat.KnockDown();
            float duration=knockdown?CombatState.KnockdownSeconds:stun;
            if(hillFall)duration=Mathf.Max(duration,HillImpact.MinimumFallSeconds);
            Stun=Mathf.Max(Stun,duration);OnSummit=false;
            if(stun>=1||knockdown||hillFall)
            {
                Vector3 velocity=body.linearVelocity;
                GetComponent<Collider>().enabled=false;body.isKinematic=true;
                rig.Fall(hillFall?HillImpact.InheritedVelocity(velocity):velocity,hillFall?HillImpact.Kick(impulse):impulse,body.mass,hillFall);
            }
            else body.AddForce(impulse,ForceMode.Impulse);
            GameSession.Instance.Beep(knockdown?110:220,.14f);
            return true;
        }
        public static Fighter FromCollider(Collider collider)
        {
            var fighter=collider.GetComponentInParent<Fighter>();
            if(fighter)return fighter;
            var physicalRig=collider.GetComponentInParent<CharacterRig>();
            return physicalRig?physicalRig.Owner:null;
        }
        void OnDestroy(){if(rig)Destroy(rig.gameObject);}
    }
}
