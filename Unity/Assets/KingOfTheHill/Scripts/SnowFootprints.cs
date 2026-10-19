using UnityEngine;

namespace WinterYard
{
    public sealed class SnowFootprints : MonoBehaviour
    {
        public static SnowFootprints Instance {get;private set;}
        const int Capacity=240;
        readonly GameObject[] marks=new GameObject[Capacity];
        readonly Renderer[] renderers=new Renderer[Capacity];
        readonly float[] remaining=new float[Capacity];
        int next;
        Mesh mesh;
        Material material;
        MaterialPropertyBlock tint;
        ParticleSystem powder;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        void Awake()
        {
            Instance=this;tint=new MaterialPropertyBlock();
            material=Arena.Material(new Color(.5f,.63f,.72f));
            mesh=new Mesh{name="Boot imprint",vertices=new[]{new Vector3(-.085f,0,-.16f),new Vector3(.085f,0,-.16f),new Vector3(-.085f,0,.16f),new Vector3(.085f,0,.16f)},triangles=new[]{0,2,1,1,2,3}};
            mesh.RecalculateNormals();
            for(int i=0;i<Capacity;i++)
            {
                var obj=new GameObject("Footprint");obj.transform.SetParent(transform,false);
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;renderers[i]=obj.AddComponent<MeshRenderer>();
                renderers[i].sharedMaterial=material;renderers[i].enabled=false;
                renderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                marks[i]=obj;
            }
            var fx=new GameObject("Snow kicked by boots");fx.transform.SetParent(transform,false);
            powder=fx.AddComponent<ParticleSystem>();powder.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=powder.main;main.loop=true;main.playOnAwake=false;main.startLifetime=.45f;main.startSpeed=0;
            main.startSize=.055f;main.startColor=new Color(.85f,.92f,1);main.gravityModifier=.5f;main.maxParticles=160;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=powder.emission;emission.enabled=false;var shape=powder.shape;shape.enabled=false;
            var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if(shader)fx.GetComponent<ParticleSystemRenderer>().sharedMaterial=materialForParticles=new Material(shader);
            powder.Play();
        }
        Material materialForParticles;
        public void Step(Vector3 point,Vector3 normal,Vector3 direction,float depth,bool left)
        {
            if(!GameSession.Instance.Running)return;
            direction=Vector3.ProjectOnPlane(direction,normal).normalized;
            if(direction.sqrMagnitude<.01f)direction=Vector3.ProjectOnPlane(Vector3.forward,normal).normalized;
            Vector3 side=Vector3.Cross(normal,direction)*(left?-.18f:.18f);
            int i=next;next=(next+1)%Capacity;
            marks[i].transform.SetPositionAndRotation(point+normal*.018f+side,Quaternion.LookRotation(direction,normal));
            remaining[i]=12;renderers[i].enabled=true;
            if(depth>.12f)
            {
                for(int j=0;j<5;j++)
                {
                    var particle=new ParticleSystem.EmitParams{position=point+side+normal*.06f,
                        velocity=normal*Random.Range(.4f,1.2f)-direction*.5f+side*Random.Range(-2f,2f),
                        startSize=Random.Range(.03f,.085f),startLifetime=Random.Range(.25f,.55f)};
                    powder.Emit(particle,1);
                }
            }
        }
        void Update()
        {
            if(!GameSession.Instance.Running)return;
            for(int i=0;i<Capacity;i++)if(remaining[i]>0)
            {
                remaining[i]=Mathf.Max(0,remaining[i]-Time.deltaTime);
                if(remaining[i]<=0){renderers[i].enabled=false;continue;}
                tint.SetColor(BaseColor,Color.Lerp(Arena.Snow.color,new Color(.48f,.61f,.7f),Mathf.Min(1,remaining[i]/4)));
                renderers[i].SetPropertyBlock(tint);
            }
        }
        public void Clear()
        {
            for(int i=0;i<Capacity;i++){remaining[i]=0;renderers[i].enabled=false;}
            powder.Clear();next=0;
        }
        void OnDestroy()
        {
            if(Instance==this)Instance=null;
            if(mesh)Destroy(mesh);if(material)Destroy(material);if(materialForParticles)Destroy(materialForParticles);
        }
    }
}
