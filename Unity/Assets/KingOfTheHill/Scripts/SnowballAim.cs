using UnityEngine;

namespace WinterYard
{
    [RequireComponent(typeof(Fighter))]
    public sealed class SnowballAim : MonoBehaviour
    {
        Fighter owner;
        LineRenderer arc, marker;
        Material material;
        readonly RaycastHit[] hits=new RaycastHit[24];
        readonly Vector3[] points=new Vector3[161];
        void Awake()
        {
            owner=GetComponent<Fighter>();material=Arena.Material(new Color(.75f,.9f,1));
            arc=MakeLine("Snowball trajectory",.035f);
            marker=MakeLine("Snowball impact marker",.045f);marker.loop=true;
        }
        LineRenderer MakeLine(string name,float width)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;
            line.sharedMaterial=material;line.widthMultiplier=width;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;line.enabled=false;return line;
        }
        void LateUpdate()
        {
            bool visible=owner.IsPlayer&&GameSession.Instance.Running&&owner.Stun<=0&&!owner.IsRagdoll;
            arc.enabled=marker.enabled=visible;if(!visible)return;
            // Preview Q when a super is available; both shots share the same velocity.
            bool super=owner.Combat.SuperReady;
            Vector3 position=Snowball.SpawnPosition(owner,owner.AimDirection);
            Vector3 velocity=Snowball.LaunchVelocity(owner.AimDirection);
            Vector3 normal=Vector3.up;bool impact=false;
            float dt=Time.fixedDeltaTime;
            int count=1;points[0]=position;
            int steps=Mathf.Min(160,Mathf.CeilToInt(Snowball.Lifetime/dt));
            for(int i=0;i<steps;i++)
            {
                velocity+=Physics.gravity*dt;
                Vector3 next=position+velocity*dt;
                Vector3 delta=next-position;
                int found=Physics.SphereCastNonAlloc(position,Snowball.Radius(super),delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
                float closest=float.MaxValue;int selected=-1;
                for(int j=0;j<found;j++)
                {
                    if(Fighter.FromCollider(hits[j].collider)==owner)continue;
                    if(hits[j].distance<closest){closest=hits[j].distance;selected=j;}
                }
                if(selected>=0)
                {
                    position=hits[selected].point;normal=hits[selected].normal;impact=true;
                    points[count++]=position;break;
                }
                position=next;points[count++]=position;
            }
            arc.positionCount=count;
            // Avoid SetPositions with unused trailing points in the fixed buffer.
            for(int i=0;i<count;i++)arc.SetPosition(i,points[i]);
            Color color=!owner.HasSnow?new Color(.45f,.5f,.55f):super?new Color(1,.7f,.18f):new Color(.65f,.9f,1);
            material.color=color;arc.startColor=arc.endColor=Color.white;
            marker.enabled=impact;
            if(!impact)return;
            marker.positionCount=32;
            var rotation=Quaternion.FromToRotation(Vector3.up,normal);
            for(int i=0;i<32;i++)
            {
                float angle=i*Mathf.PI*2/32;
                marker.SetPosition(i,position+normal*.025f+rotation*new Vector3(Mathf.Cos(angle)*.3f,0,Mathf.Sin(angle)*.3f));
            }
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
