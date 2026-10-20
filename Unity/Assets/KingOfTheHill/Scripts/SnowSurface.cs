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
        const int GridSize=81;
        static readonly float[] packed=new float[GridSize*GridSize];
        public static float[] CapturePacking() => (float[])packed.Clone();
        public static void RestorePacking(float[] snapshot)
        {
            if(snapshot==null||snapshot.Length!=packed.Length)throw new System.ArgumentException("Invalid snow snapshot");
            System.Array.Copy(snapshot,packed,packed.Length);if(SnowDeformation.Instance)SnowDeformation.Instance.RefreshAll();
        }
        public static void ResetPacking(){System.Array.Clear(packed,0,packed.Length);if(SnowDeformation.Instance)SnowDeformation.Instance.RefreshAll();}
        static float Packing(float x,float z)
        {
            float gx=Mathf.Clamp((x+20)*2,0,80),gz=Mathf.Clamp((z+20)*2,0,80);
            int ix=Mathf.Min(79,Mathf.FloorToInt(gx)),iz=Mathf.Min(79,Mathf.FloorToInt(gz));
            float fx=gx-ix,fz=gz-iz;
            return Mathf.Lerp(Mathf.Lerp(packed[iz*81+ix],packed[iz*81+ix+1],fx),Mathf.Lerp(packed[(iz+1)*81+ix],packed[(iz+1)*81+ix+1],fx),fz);
        }
        public static void Pack(Vector3 point,float radius=.7f,float strength=.045f)
        {
            if(point.x*point.x+point.z*point.z<49||radius<=0||strength<=0)return;
            int minX=Mathf.Clamp(Mathf.FloorToInt((point.x-radius+20)*2),0,80),maxX=Mathf.Clamp(Mathf.CeilToInt((point.x+radius+20)*2),0,80);
            int minZ=Mathf.Clamp(Mathf.FloorToInt((point.z-radius+20)*2),0,80),maxZ=Mathf.Clamp(Mathf.CeilToInt((point.z+radius+20)*2),0,80);
            for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)
            {
                float X=x*.5f-20,Z=z*.5f-20;
                float weight=Mathf.Clamp01(1-Vector2.Distance(new Vector2(X,Z),new Vector2(point.x,point.z))/radius);
                int index=z*81+x;float limit=Mathf.Max(0,BaseDepth(X,Z)-.045f);
                float previous=packed[index];packed[index]=Mathf.Min(limit,previous+strength*weight);
                if(packed[index]>previous&&SnowDeformation.Instance)SnowDeformation.Instance.MarkDirty(X,Z);
            }
        }
        static float BaseDepth(float x,float z)
        {
            float radius=Mathf.Sqrt(x*x+z*z);
            if(radius<7)return 0;
            float pathDistance=Mathf.Min(Mathf.Abs(x),Mathf.Abs(z));
            float loose=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.9f,3,pathDistance));
            float variation=.85f+.15f*Mathf.Sin(x*.35f)*Mathf.Sin(z*.3f);
            float rim=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,9,radius));
            return Mathf.Lerp(.045f,.5f,loose*variation)*rim;
        }
        public static SnowSample Sample(float x,float z)
        {
            float radius=Mathf.Sqrt(x*x+z*z);
            if(radius<=2.25f)return new SnowSample{IsIce=true,Speed=5,Acceleration=6,Damping=.12f,Dash=9,Jump=6.5f};
            if(radius<7)return new SnowSample{Depth=.08f,Speed=4.4f,Acceleration=18,Damping=.7f,Dash=8,Jump=6.3f};
            float original=BaseDepth(x,z);
            float depth=Mathf.Max(Mathf.Min(.045f,original),original-Packing(x,z));
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
            System.Array.Clear(packed,0,packed.Length);
            var obj=new GameObject("Deformable snow field");obj.transform.SetParent(parent,false);
            obj.AddComponent<SnowDeformation>();obj.AddComponent<SnowFootprints>();
            var pathMaterial=Arena.Material(new Color(.66f,.77f,.83f));
            for(int arm=0;arm<4;arm++)
            {
                float angle=arm*90;
                var path=Arena.Shape("Packed footpath",PrimitiveType.Cube,
                    Quaternion.Euler(0,angle,0)*new Vector3(0,.04f,14.5f),new Vector3(1.8f,.015f,11),pathMaterial,false,parent);
                path.transform.localRotation=Quaternion.Euler(0,angle,0);
            }
        }
    }
}
