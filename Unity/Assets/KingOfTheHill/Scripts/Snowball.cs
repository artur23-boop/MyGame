using UnityEngine;

namespace WinterYard
{
    public sealed class Snowball : MonoBehaviour
    {
        Fighter owner;
        Rigidbody body;
        float lifetime=3;
        public static void Launch(Fighter source,Vector3 direction)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name="Snowball";obj.transform.localScale=Vector3.one*.24f;
            obj.transform.position=source.transform.position+direction*.8f+Vector3.up*.45f;
            obj.GetComponent<Renderer>().sharedMaterial=Arena.Snow;
            var shot=obj.AddComponent<Snowball>();shot.owner=source;
            shot.body=obj.AddComponent<Rigidbody>();shot.body.mass=.1f;
            shot.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            shot.body.linearVelocity=direction*17+Vector3.up*3.2f;
            Physics.IgnoreCollision(obj.GetComponent<Collider>(),source.GetComponent<Collider>());
        }
        void Update(){if(GameSession.Instance.Running){lifetime-=Time.deltaTime;if(lifetime<=0)Destroy(gameObject);}}
        void OnCollisionEnter(Collision collision)
        {
            var fighter=collision.collider.GetComponentInParent<Fighter>();
            if(fighter&&fighter!=owner)
            {
                var direction=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).normalized;
                bool head=transform.position.y>fighter.transform.position.y+.45f;
                fighter.Hit(direction*(head?10:7)+Vector3.up*2,head?1.8f:.7f);
            }
            Destroy(gameObject);
        }
    }
}
