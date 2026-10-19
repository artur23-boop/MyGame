using UnityEngine;

namespace WinterYard
{
    public struct SnowSample
    {
        public float Depth, Speed, Acceleration, Damping, Dash, Jump;
        public bool IsIce;
    }

    // The mesh and movement controller sample the same continuous depth field.
    public static class SnowSurface
    {
        public static SnowSample Sample(float x,float z)
        {
            float radius=Mathf.Sqrt(x*x+z*z);
            if(radius<=2.25f)return new SnowSample{IsIce=true,Speed=5,Acceleration=6,Damping=.12f,Dash=9,Jump=6.5f};
            if(radius<7)return new SnowSample{Depth=.08f,Speed=4.4f,Acceleration=18,Damping=.7f,Dash=8,Jump=6.3f};
            float pathDistance=Mathf.Min(Mathf.Abs(x),Mathf.Abs(z));
            float loose=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.9f,3,pathDistance));
            float variation=.85f+.15f*Mathf.Sin(x*.35f)*Mathf.Sin(z*.3f);
            float rim=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,9,radius));
            float depth=Mathf.Lerp(.045f,.5f,loose*variation)*rim;
            float viscosity=Mathf.InverseLerp(.045f,.5f,depth);
            return new SnowSample{Depth=depth,Speed=Mathf.Lerp(4.8f,2.65f,viscosity),
                Acceleration=Mathf.Lerp(22,9,viscosity),Damping=Mathf.Lerp(.8f,2.4f,viscosity),
                Dash=Mathf.Lerp(9,5.5f,viscosity),Jump=Mathf.Lerp(6.5f,5.5f,viscosity)};
        }
        public static float Height(float x,float z)
        {
            if(x*x+z*z<49)return 0; // The hill's collider provides its own surface.
            return Sample(x,z).Depth;
        }
        public static void Build(Transform parent)
        {
            const int size=81;const float spacing=.5f;
            var vertices=new Vector3[size*size];
            var triangles=new int[(size-1)*(size-1)*6];
            for(int z=0;z<size;z++)for(int x=0;x<size;x++)
            {
                float X=(x-40)*spacing,Z=(z-40)*spacing;int index=z*size+x;
                vertices[index]=new Vector3(X,Height(X,Z),Z);
            }
            int n=0;
            for(int z=0;z<size-1;z++)for(int x=0;x<size-1;x++)
            {
                int a=z*size+x;
                triangles[n++]=a;triangles[n++]=a+size;triangles[n++]=a+1;
                triangles[n++]=a+1;triangles[n++]=a+size;triangles[n++]=a+size+1;
            }
            var mesh=new Mesh{name="Variable depth snow",vertices=vertices,triangles=triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var obj=new GameObject("Deep snow and packed crossing paths");obj.transform.SetParent(parent,false);obj.layer=Arena.GroundLayer;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=Arena.Snow;
            obj.AddComponent<MeshCollider>().sharedMesh=mesh;
            obj.GetComponent<Collider>().sharedMaterial=new PhysicsMaterial("Snow surface"){dynamicFriction=.25f,staticFriction=.25f};
            obj.AddComponent<SnowFootprints>();
            var packed=Arena.Material(new Color(.66f,.77f,.83f));
            for(int arm=0;arm<4;arm++)
            {
                float angle=arm*90;
                var path=Arena.Shape("Packed footpath",PrimitiveType.Cube,
                    Quaternion.Euler(0,angle,0)*new Vector3(0,.04f,14.5f),new Vector3(1.8f,.015f,11),packed,false,parent);
                path.transform.localRotation=Quaternion.Euler(0,angle,0);
            }
        }
    }
}
