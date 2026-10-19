using UnityEngine;
using UnityEngine.Rendering;

namespace WinterYard
{
    public static class Arena
    {
        public const int GroundLayer = 8;
        public static Material Snow, Ice, Dark, Warm;

        public static Material Material(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var result = new Material(shader) { color = color };
            return result;
        }

        public static GameObject Shape(string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material, bool solid = true, Transform parent = null)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.layer = GroundLayer;
            if (!solid) Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }

        public static void Build(Transform parent)
        {
            Snow = Material(new Color(.77f, .85f, .9f));
            Ice = Material(new Color(.58f, .77f, .88f));
            Dark = Material(new Color(.12f, .19f, .25f));
            Warm = Material(new Color(1f, .73f, .32f));
            Warm.EnableKeyword("_EMISSION");
            Warm.SetColor("_EmissionColor", new Color(1f, .55f, .16f) * 1.7f);
            var ground = Shape("Snow courtyard", PrimitiveType.Cube, new Vector3(0,-.3f,0),
                new Vector3(44,.6f,44), Snow, true, parent);
            var snowFriction = new PhysicsMaterial("Snow friction") { dynamicFriction = .6f, staticFriction = .6f };
            ground.GetComponent<Collider>().sharedMaterial = snowFriction;
            SnowSurface.Build(parent);
            MakeHill(parent);
            var buildingMat = Material(new Color(.25f,.31f,.37f));
            var windowDark = Material(new Color(.08f,.14f,.2f));
            for (int side=0; side<4; side++)
            {
                var block = new GameObject("Khrushchevka " + side);
                block.transform.SetParent(parent, false);
                block.transform.rotation = Quaternion.Euler(0, side*90, 0);
                Shape("Building", PrimitiveType.Cube, new Vector3(0,7.4f,23), new Vector3(44,15,5), buildingMat, true, block.transform);
                Shape("Roof snow", PrimitiveType.Cube, new Vector3(0,15,23), new Vector3(44.5f,.3f,5.5f), Snow, false, block.transform);
                for (int floor=0; floor<5; floor++)
                    for(int col=0; col<16; col++)
                        Shape("Window", PrimitiveType.Cube, new Vector3(-20+col*2.65f,1.7f+floor*2.7f,20.46f),
                            new Vector3(1.1f,1.5f,.06f), (floor*7+col*3+side)%5<2 ? Warm : windowDark, false, block.transform);
            }
            for(int i=0; i<4; i++)
            {
                float angle = i*Mathf.PI*.5f+Mathf.PI*.25f;
                var p = new Vector3(Mathf.Cos(angle)*15,0,Mathf.Sin(angle)*15);
                Shape("Deep snowbank", PrimitiveType.Sphere, p, new Vector3(5,1.7f,4), Snow, true, parent);
                GameSession.Snowbanks.Add(p);
                Lamp(p+new Vector3(3,0,0), parent);
            }
            for(int i=0; i<2; i++)
            {
                var p = new Vector3(i==0?-12:12,.65f,-10);
                Shape("Bench",PrimitiveType.Cube,p,new Vector3(3,.25f,.8f),Dark,true,parent);
                Shape("Bench snow",PrimitiveType.Cube,p+Vector3.up*.18f,new Vector3(3,.12f,.8f),Snow,false,parent);
                for(int j=-1;j<=1;j+=2)
                    Shape("Bench leg",PrimitiveType.Cube,p+new Vector3(j,-.4f,0),new Vector3(.2f,.8f,.5f),Dark,true,parent);
            }
            Shape("Snowman base", PrimitiveType.Sphere, new Vector3(-14,.85f,5), Vector3.one*1.7f, Snow, true, parent);
            Shape("Snowman head", PrimitiveType.Sphere, new Vector3(-14,2,5), Vector3.one*1.1f, Snow, true, parent);
            Shape("Snowman hat", PrimitiveType.Cylinder, new Vector3(-14,2.65f,5), new Vector3(.9f,.2f,.9f), Dark, false, parent);
            Shape("Summit pole", PrimitiveType.Cylinder, new Vector3(0,4.2f,0), new Vector3(.07f,1.2f,.07f), Dark, false, parent);
            Shape("Summit flag", PrimitiveType.Cube, new Vector3(.65f,5.05f,0), new Vector3(1.3f,.65f,.04f), Warm, false, parent);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.38f,.48f,.65f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.24f,.34f,.46f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .012f;
            var sun = new GameObject("Winter evening").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(.64f,.76f,1);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42,-30,0);
            sun.transform.SetParent(parent);
            Snowfall(parent);
        }

        static void MakeHill(Transform parent)
        {
            const int segments=64;
            float[] radii={0,2.2f,7};
            float[] heights={3,3,0};
            var vertices=new Vector3[segments*3];
            var triangles=new int[segments*2*6];
            for(int ring=0; ring<3; ring++)
                for(int i=0;i<segments;i++)
                {
                    float a=i*Mathf.PI*2/segments;
                    vertices[ring*segments+i]=new Vector3(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]);
                }
            int n=0;
            for(int ring=0;ring<2;ring++)
                for(int i=0;i<segments;i++)
                {
                    int a=ring*segments+i, b=ring*segments+(i+1)%segments, c=a+segments, d=b+segments;
                    triangles[n++]=a;triangles[n++]=b;triangles[n++]=c;
                    triangles[n++]=b;triangles[n++]=d;triangles[n++]=c;
                }
            var mesh=new Mesh { name="Snow hill", vertices=vertices, triangles=triangles };
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var hill=new GameObject("Hill — scoring radius 2.2m");
            hill.transform.SetParent(parent,false);hill.layer=GroundLayer;
            hill.AddComponent<MeshFilter>().sharedMesh=mesh;
            hill.AddComponent<MeshRenderer>().sharedMaterial=Snow;
            hill.AddComponent<MeshCollider>().sharedMesh=mesh;
            Shape("Icy summit",PrimitiveType.Cylinder,new Vector3(0,3.015f,0),new Vector3(4.35f,.015f,4.35f),Ice,false,parent);
            var slippery=new PhysicsMaterial("Hill ice") { dynamicFriction=.1f,staticFriction=.1f,frictionCombine=PhysicsMaterialCombine.Minimum };
            hill.GetComponent<Collider>().sharedMaterial=slippery;
        }

        static void Lamp(Vector3 position, Transform parent)
        {
            Shape("Lamp post",PrimitiveType.Cylinder,position+Vector3.up*2.5f,new Vector3(.12f,2.5f,.12f),Dark,true,parent);
            Shape("Lamp glow",PrimitiveType.Sphere,position+Vector3.up*5,new Vector3(.55f,.75f,.55f),Warm,false,parent);
            var light=new GameObject("Warm street light").AddComponent<Light>();
            light.transform.SetParent(parent);light.transform.position=position+Vector3.up*4.8f;
            light.type=LightType.Point;light.range=12;light.intensity=3;light.color=new Color(1,.73f,.4f);
        }

        static void Snowfall(Transform parent)
        {
            var obj=new GameObject("Falling snow");obj.transform.SetParent(parent);obj.transform.position=Vector3.up*16;
            var ps=obj.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.startLifetime=12;main.startSpeed=0;main.startSize=.07f;
            main.startColor=new Color(.9f,.95f,1,.7f);main.maxParticles=1800;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=110;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(40,1,40);
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=.25f;velocity.y=-1.6f;velocity.z=.1f;
            var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if(shader) obj.GetComponent<ParticleSystemRenderer>().sharedMaterial=new Material(shader);
            ps.Play();
        }
    }
}
