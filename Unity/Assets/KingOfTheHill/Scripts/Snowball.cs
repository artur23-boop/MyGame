using UnityEngine;

namespace WinterYard
{
    public sealed class Snowball : MonoBehaviour
    {
        Fighter owner;
        Rigidbody body;
        bool super, resolved;
        Vector3 flightDirection;
        float lifetime=3;
        public static void Launch(Fighter source,Vector3 direction,bool isSuper=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name=isSuper?"Super snowball":"Snowball";
            obj.transform.localScale=Vector3.one*(isSuper?.48f:.24f);
            obj.transform.position=source.transform.position+direction*.9f+Vector3.up*.45f;
            obj.GetComponent<Renderer>().sharedMaterial=isSuper?Arena.Warm:Arena.Snow;
            var shot=obj.AddComponent<Snowball>();shot.owner=source;shot.super=isSuper;
            shot.flightDirection=direction;
            shot.body=obj.AddComponent<Rigidbody>();shot.body.mass=.1f;
            shot.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            shot.body.linearVelocity=direction*17+Vector3.up*3.2f;
            Physics.IgnoreCollision(obj.GetComponent<Collider>(),source.GetComponent<Collider>());
        }
        void Update()
        {
            if(!GameSession.Instance.Running)return;
            lifetime-=Time.deltaTime;
            if(lifetime<=0)Resolve(false);
        }
        void OnCollisionEnter(Collision collision)
        {
            if(resolved||!GameSession.Instance.Running)return;
            var fighter=collision.collider.GetComponentInParent<Fighter>();
            bool landed=false;
            if(fighter&&fighter!=owner)
            {
                bool head=transform.position.y>fighter.transform.position.y+.45f;
                landed=fighter.Hit(flightDirection*(super?13:head?10:7)+Vector3.up*2,head?1.8f:.7f,super);
            }
            Resolve(landed);
        }
        void Resolve(bool landed)
        {
            if(resolved)return;resolved=true;
            if(owner&&!super)
            {
                if(landed)owner.Combat.RegisterHit();else owner.Combat.BreakCombo();
            }
            Destroy(gameObject);
        }
    }
}
