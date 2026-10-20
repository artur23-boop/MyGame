using UnityEngine;

namespace WinterYard
{
    public static class HillImpact
    {
        public const float MinimumFallSeconds=2.2f;
        public static bool IsStandingOnHill(Vector3 position,bool grounded)
        {
            return grounded&&position.y>1.3f&&position.x*position.x+position.z*position.z<49;
        }
        public static bool ShouldTumble(Vector3 position,bool grounded,Vector3 impulse,Vector3 velocity,float mass,float stun,bool super)
        {
            if(!IsStandingOnHill(position,grounded))return false;
            if(super||stun>=1)return true;
            var radial=new Vector3(position.x,0,position.z);
            // A stable stance in the middle can absorb ordinary pushes. Near the edge,
            // only a sufficiently strong outward hit breaks balance (never a random roll).
            if(radial.magnitude<1.75f)return false;
            float outward=Vector3.Dot(impulse/Mathf.Max(.1f,mass)+velocity*.35f,radial.normalized);
            return outward>=7;
        }
        public static Vector3 Kick(Vector3 incoming)
        {
            // Break balance, rather than launching the entire body into the air.
            return Vector3.ClampMagnitude(Vector3.ProjectOnPlane(incoming,Vector3.up),3.2f)
                +Vector3.up*Mathf.Clamp(incoming.y,0,.35f);
        }
        public static Vector3 InheritedVelocity(Vector3 velocity)
        {
            return Vector3.ClampMagnitude(Vector3.ProjectOnPlane(velocity,Vector3.up),4)
                +Vector3.up*Mathf.Clamp(velocity.y,-3,1.2f);
        }
    }
}
