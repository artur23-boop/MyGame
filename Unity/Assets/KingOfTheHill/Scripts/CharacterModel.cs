using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WinterYard
{
    // Authored Blender geometry bound to the existing 11 physical/animated segment pivots.
    public sealed class CharacterModel
    {
        [Serializable] sealed class ModelData { public string name; public Part[] parts; }
        [Serializable] sealed class Part
        {
            public int bone;
            public float[] vertices,normals;
            public int[] triangles;
            public Color color;
        }
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Material> materials=new List<Material>();
        public bool Attach(string displayName,bool player,IReadOnlyList<Rigidbody> bones)
        {
            string key=player?"Player":displayName=="Мишка"?"Mishka":displayName=="Санёк"?"Sanyok":displayName=="Димон"?"Dimon":null;
            if(key==null)return false;
            var asset=Resources.Load<TextAsset>("Characters/"+key);
            if(!asset){Debug.LogWarning("Missing character mesh: "+key+". Using primitive fallback.");return false;}
            var data=JsonUtility.FromJson<ModelData>(asset.text);
            if(data==null||data.parts==null||data.parts.Length==0)throw new InvalidOperationException("Empty character mesh: "+key);
            foreach(var part in data.parts)
            {
                if(part.bone<0||part.bone>=bones.Count||part.vertices==null||part.vertices.Length%3!=0||part.normals==null||part.normals.Length!=part.vertices.Length||part.triangles==null||part.triangles.Length%3!=0)
                    throw new InvalidOperationException("Invalid character part in "+key);
                var vertices=new Vector3[part.vertices.Length/3];var normals=new Vector3[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    vertices[i]=new Vector3(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2]);
                    normals[i]=new Vector3(part.normals[i*3],part.normals[i*3+1],part.normals[i*3+2]);
                }
                var mesh=new Mesh{name=key+" bone "+part.bone,indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=part.triangles;mesh.RecalculateBounds();mesh.UploadMeshData(true);meshes.Add(mesh);
                var material=Arena.Material(part.color);material.name=key+" outfit";
                if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.18f);
                materials.Add(material);
                var obj=new GameObject(key+" authored mesh");obj.layer=10;obj.transform.SetParent(bones[part.bone].transform,false);
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            return true;
        }
        public void Dispose()
        {
            foreach(var mesh in meshes)if(mesh)UnityEngine.Object.Destroy(mesh);
            foreach(var material in materials)if(material)UnityEngine.Object.Destroy(material);
        }
    }
}
