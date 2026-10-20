"""Validate exported model assets without Unity or Blender; run from repository root."""
import json,math,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for name in ['Player','Mishka','Sanyok','Dimon']:
 data=json.loads((ROOT/'Unity/Assets/KingOfTheHill/Resources/Characters'/(name+'.json')).read_text())
 bones=set();count=0
 for part in data['parts']:
  bones.add(part['bone']);v=part['vertices'];n=part['normals'];t=part['triangles'];size=len(v)//3
  assert len(v)%3==0 and len(n)==len(v) and len(t)%3==0 and size>0,name
  assert all(math.isfinite(c) for c in v+n),name
  assert all(0<=i<size for i in t),name
  assert all(.98<sum(c*c for c in n[i:i+3])<1.02 for i in range(0,len(n),3)),name
  count+=len(t)//3
 assert bones==set(range(11)),(name,bones)
 glb=(ROOT/'Models/Characters'/(name+'.glb')).read_bytes()
 magic,version,length=struct.unpack_from('<III',glb);assert magic==0x46546c67 and version==2 and length==len(glb)
 chunk,kind=struct.unpack_from('<II',glb,12);assert kind==0x4e4f534a
 gltf=json.loads(glb[20:20+chunk]);assert gltf['skins'] and len(gltf['skins'][0]['joints'])==11,(name,'skin')
 assert (ROOT/'Models/Characters'/(name+'.fbx')).read_bytes().startswith(b'Kaydara FBX Binary'),name
 print(name,':',count,'triangles,',len(data['parts']),'material/segment groups, 11 skin joints, FBX + GLB valid')
assert (ROOT/'Models/Characters/WinterYard-Characters.blend').read_bytes().startswith(b'BLENDER')
print('Editable Blender source validated.')
