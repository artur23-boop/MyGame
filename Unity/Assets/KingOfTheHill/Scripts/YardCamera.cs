using UnityEngine;

namespace WinterYard
{
    public sealed class YardCamera : MonoBehaviour
    {
        public Fighter Target;
        float yaw, pitch=25;
        void LateUpdate()
        {
            if(!Target)return;
            if(GameSession.Instance.Running)
            {
                yaw+=Input.GetAxisRaw("Mouse X")*GameSession.Instance.Sensitivity;
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*GameSession.Instance.Sensitivity,8,65);
            }
            var rotation=Quaternion.Euler(pitch,yaw,0);
            var focus=Target.transform.position+Vector3.up*.75f;
            var offset=rotation*new Vector3(0,0,-7);
            float distance=offset.magnitude;
            if(Physics.SphereCast(focus,.25f,offset.normalized,out var hit,distance,1<<Arena.GroundLayer))
                distance=Mathf.Max(.4f,hit.distance-.2f);
            var desired=focus+offset.normalized*distance;
            transform.position=Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-Time.unscaledDeltaTime*15));
            transform.rotation=rotation;
        }
    }
}
