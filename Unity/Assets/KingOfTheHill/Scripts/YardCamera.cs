using UnityEngine;

namespace WinterYard
{
    public sealed class YardCamera : MonoBehaviour
    {
        public Fighter Target;
        float yaw, pitch=25,renderedPitch=25,followDistance=7;
        void LateUpdate()
        {
            if(!Target)return;
            if(GameSession.Instance.InMainMenu)
            {
                // Establishing view of the courtyard behind the welcome/settings screens.
                var focusPoint=new Vector3(0,2,0);
                var menuPosition=new Vector3(13,11,-17);
                transform.position=Vector3.Lerp(transform.position,menuPosition,1-Mathf.Exp(-Time.unscaledDeltaTime*4));
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(focusPoint-transform.position),1-Mathf.Exp(-Time.unscaledDeltaTime*4));
                return;
            }
            if(GameSession.Instance.Running)
            {
                yaw+=Input.GetAxisRaw("Mouse X")*GameSession.Instance.Sensitivity;
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*GameSession.Instance.Sensitivity,8,65);
            }
            bool aiming=Target.IsCharging;
            followDistance=Mathf.Lerp(followDistance,aiming?13:7,1-Mathf.Exp(-Time.unscaledDeltaTime*5));
            var view=GetComponent<Camera>();
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,aiming?78:65,1-Mathf.Exp(-Time.unscaledDeltaTime*5));
            renderedPitch=Mathf.Lerp(renderedPitch,aiming?Mathf.Clamp(pitch,12,28):pitch,1-Mathf.Exp(-Time.unscaledDeltaTime*10));
            var rotation=Quaternion.Euler(renderedPitch,yaw,0);
            var focus=Target.transform.position+Vector3.up*.75f;
            var offset=rotation*new Vector3(0,0,-followDistance);
            float distance=offset.magnitude;
            if(Physics.SphereCast(focus,.25f,offset.normalized,out var hit,distance,1<<Arena.GroundLayer))
                distance=Mathf.Max(.4f,hit.distance-.2f);
            var desired=focus+offset.normalized*distance;
            transform.position=Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-Time.unscaledDeltaTime*15));
            transform.rotation=rotation;
        }
    }
}
