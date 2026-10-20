using UnityEngine;

namespace WinterYard
{
    // 16 chunks, 800 triangles each: only touched chunks have their collider recooked.
    public sealed class SnowDeformation : MonoBehaviour
    {
        public static SnowDeformation Instance {get;private set;}
        const int Across=4,Cells=20,Vertices=21;
        readonly Mesh[] meshes=new Mesh[16];
        readonly MeshCollider[] colliders=new MeshCollider[16];
        readonly Vector3[][] positions=new Vector3[16][];
        readonly bool[] dirty=new bool[16];
        PhysicsMaterial friction;
        float nextUpdate;
        void Awake()
        {
            Instance=this;friction=new PhysicsMaterial("Packed and loose snow"){dynamicFriction=.25f,staticFriction=.25f};
            var triangles=new int[Cells*Cells*6];int n=0;
            for(int z=0;z<Cells;z++)for(int x=0;x<Cells;x++)
            {
                int a=z*Vertices+x;triangles[n++]=a;triangles[n++]=a+Vertices;triangles[n++]=a+1;
                triangles[n++]=a+1;triangles[n++]=a+Vertices;triangles[n++]=a+Vertices+1;
            }
            for(int z=0;z<Across;z++)for(int x=0;x<Across;x++)
            {
                int i=z*Across+x;var obj=new GameObject("Snow chunk "+x+","+z);obj.transform.SetParent(transform,false);obj.layer=Arena.GroundLayer;
                var vertices=new Vector3[Vertices*Vertices];
                for(int vz=0;vz<Vertices;vz++)for(int vx=0;vx<Vertices;vx++)vertices[vz*Vertices+vx]=new Vector3(-20+x*10+vx*.5f,0,-20+z*10+vz*.5f);
                positions[i]=vertices;meshes[i]=new Mesh{name=obj.name};meshes[i].MarkDynamic();
                meshes[i].vertices=vertices;meshes[i].triangles=triangles;
                obj.AddComponent<MeshFilter>().sharedMesh=meshes[i];obj.AddComponent<MeshRenderer>().sharedMaterial=Arena.Snow;
                colliders[i]=obj.AddComponent<MeshCollider>();colliders[i].sharedMaterial=friction;
                Rebuild(i);
            }
        }
        void Rebuild(int index)
        {
            var vertices=positions[index];
            for(int i=0;i<vertices.Length;i++)vertices[i].y=SnowSurface.Height(vertices[i].x,vertices[i].z);
            meshes[index].vertices=vertices;meshes[index].RecalculateNormals();meshes[index].RecalculateBounds();
            colliders[index].sharedMesh=null;colliders[index].sharedMesh=meshes[index];dirty[index]=false;
        }
        public void MarkDirty(float x,float z)
        {
            // Include neighboring chunks at shared edges to keep colliders watertight.
            for(int cz=0;cz<Across;cz++)for(int cx=0;cx<Across;cx++)
                if(x>=-20+cx*10-.01f&&x<=-10+cx*10+.01f&&z>=-20+cz*10-.01f&&z<=-10+cz*10+.01f)dirty[cz*Across+cx]=true;
        }
        void FixedUpdate()
        {
            if(!GameSession.Instance.Running||Time.time<nextUpdate)return;
            nextUpdate=Time.time+.2f;
            for(int i=0;i<16;i++)if(dirty[i])Rebuild(i);
        }
        public void RefreshAll(){for(int i=0;i<16;i++)if(meshes[i])Rebuild(i);nextUpdate=0;}
        void OnDestroy(){if(Instance==this)Instance=null;foreach(var mesh in meshes)if(mesh)Destroy(mesh);if(friction)Destroy(friction);}
    }
}
