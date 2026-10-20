using UnityEngine;

namespace WinterYard
{
    public sealed class Snowball : MonoBehaviour
    {
        Fighter owner;
        Rigidbody body;
        bool super, resolved;
        Vector3 flightDirection;
        public const float Lifetime=3;
        float lifetime=Lifetime;
        public static float Radius(bool super) => super ? .24f : .12f;
        public static Vector3 SpawnPosition(Fighter source,Vector3 direction) => source.transform.position+direction*.9f+Vector3.up*.45f;
        public static Vector3 LaunchVelocity(Vector3 direction) => direction*17+Vector3.up*3.2f;
        public static void Launch(Fighter source,Vector3 direction,bool isSuper=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name=isSuper?"Super snowball":"Snowball";
            obj.transform.localScale=Vector3.one*Radius(isSuper)*2;
            obj.transform.position=SpawnPosition(source,direction);
            obj.GetComponent<Renderer>().sharedMaterial=isSuper?Arena.Warm:Arena.Snow;
            var shot=obj.AddComponent<Snowball>();shot.owner=source;shot.super=isSuper;
            shot.flightDirection=direction;
            shot.body=obj.AddComponent<Rigidbody>();shot.body.mass=.1f;
            shot.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            shot.body.linearDamping=0;shot.body.useGravity=true;
            shot.body.linearVelocity=LaunchVelocity(direction);
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
            var fighter=Fighter.FromCollider(collision.collider);
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
