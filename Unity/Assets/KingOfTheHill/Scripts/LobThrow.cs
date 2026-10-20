using UnityEngine;

namespace WinterYard
{
    public static class LobThrow
    {
        public const float ChargeSeconds=1.2f;
        public const float DefaultAngle=55;
        public const float MinAngle=30,MaxAngle=75;
        public static Vector3 Velocity(Vector3 direction,float power,float angle)
        {
            direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)direction=Vector3.forward;
            float radians=Mathf.Clamp(angle,MinAngle,MaxAngle)*Mathf.Deg2Rad;
            float speed=Mathf.Lerp(8,17,Mathf.Clamp01(power));
            return direction*(Mathf.Cos(radians)*speed)+Vector3.up*(Mathf.Sin(radians)*speed);
        }
        public static float BotPower(float distance)
        {
            float speed=Mathf.Sqrt(Mathf.Max(0,distance)*Physics.gravity.magnitude/Mathf.Sin(DefaultAngle*2*Mathf.Deg2Rad));
            return Mathf.InverseLerp(8,17,speed);
        }
    }
    public sealed class LobCharge
    {
        public bool Active {get;private set;}
        public bool Super {get;private set;}
        public float Power {get;private set;}
        public void Begin(bool super){if(Active)return;Active=true;Super=super;Power=0;}
        public void Tick(float dt){if(Active)Power=Mathf.Clamp01(Power+Mathf.Max(0,dt)/LobThrow.ChargeSeconds);}
        public bool Release(out float power,out bool super)
        {
            power=Power;super=Super;if(!Active)return false;Cancel();return true;
        }
        public void Cancel(){Active=false;Super=false;Power=0;}
    }
}
