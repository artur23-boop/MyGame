"""Rebuild four original winter characters with Blender 4.3+.
Run: blender --background --python tools/build_character_models.py -- /absolute/path/to/MyGame
Exports editable .blend, FBX, GLB, and mesh data used by the Unity articulated rig.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else Path.cwd()
OUT=ROOT/'Models/Characters';DATA=ROOT/'Unity/Assets/KingOfTheHill/Resources/Characters';OUT.mkdir(parents=True,exist_ok=True);DATA.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
REST=[(0,-.28,0),(0,.2,0),(0,.76,.02),(-.52,.18,0),(-.58,-.22,.08),(.52,.18,0),(.58,-.22,.08),(-.2,-.56,0),(-.2,-.91,.06),(.2,-.56,0),(.2,-.91,.06)]
PARENTS=[-1,0,1,1,3,1,5,0,7,0,9]
def bv(v):return Vector((v[0],-v[2],v[1]))
def uv(v):return (v.x,v.z,-v.y)
materials={}
def mat(name,color):
 if name in materials:return materials[name]
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.78
 materials[name]=m;return m
skin=mat('Warm skin',(.72,.46,.32));cheek=mat('Rosy cheeks',(.68,.25,.22));eye=mat('Eyes',(.025,.035,.045));white=mat('Eyes ivory',(.92,.9,.8));hair=mat('Chestnut hair',(.12,.055,.025));pants=mat('Navy trousers',(.045,.065,.10));sole=mat('Rubber soles',(.025,.033,.04));stitch=mat('Seams',(.13,.12,.1));cream=mat('Cream wool',(.72,.66,.50))
characters=[('Player','Игрок',(.72,.40,.065),'pompom',.97,1.0),('Mishka','Мишка',(.58,.13,.10),'ushanka',1.15,.97),('Sanyok','Санёк',(.055,.29,.27),'beanie',.93,1.08),('Dimon','Димон',(.27,.17,.39),'ribbed',.98,.97)]
all_roots=[];stats={}
for index,(key,title,color,hat,width,height) in enumerate(characters):
 pieces=[];coat=mat(key+' coat',color);hatmat=cream if index==0 else hair if index==1 else mat(key+' cap',(.61,.22,.07) if index==2 else (.065,.07,.16));scarf=cream if index<2 else mat(key+' scarf',(.58,.29,.12) if index==2 else (.42,.31,.52));bootmat=mat(key+' boots',(.19,.105,.055) if index==0 else (.18,.17,.15) if index==2 else (.055,.065,.075))
 def finish(obj,name,bone,material):
  obj.name=key+'_'+name;obj.data.materials.append(material);obj['rig_bone']=bone
  for p in obj.data.polygons:p.use_smooth=True
  pieces.append(obj);return obj
 def ball(name,bone,position,radii,material):
  bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=bv(position));o=bpy.context.object;o.scale=(radii[0],radii[2],radii[1]);return finish(o,name,bone,material)
 def box(name,bone,position,scale,material,bevel=.04):
  bpy.ops.mesh.primitive_cube_add(size=1,location=bv(position));o=bpy.context.object;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  b=o.modifiers.new('Soft edges','BEVEL');b.width=bevel;b.segments=2;bpy.ops.object.modifier_apply(modifier=b.name);return finish(o,name,bone,material)
 def tube(name,bone,position,radius,depth,material):
  bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=depth,location=bv(position));return finish(bpy.context.object,name,bone,material)
 # Quilted winter body with soft overlapping horizontal padded bands.
 ball('Pants hips',0,(0,-.30,0),(.32*width,.22,.25),pants)
 for j in range(5):
  y=-.12+j*.135
  ball('Puffer band '+str(j),1,(0,y,0),((.39+(2-abs(j-2))*.014)*width,.104,.29),coat)
 box('Jacket zipper',1,(0,.15,.294),(.025,.62,.015),sole,.006)
 for side in [-1,1]:
  box('Pocket',1,(side*.235*width,-.045,.268),(.14,.13,.05),coat,.025)
  box('Pocket opening',1,(side*.235*width,.0,.295),(.12,.013,.014),stitch,.004)
 # Head and readable facial features looking along Unity +Z / Blender -Y.
 hy=.76+(height-1)*.65
 ball('Face',2,(0,hy,.018),(.27*width,.30,.24),skin)
 for side in [-1,1]:
  ball('Ear',2,(side*.265*width,hy,.01),(.065,.09,.055),skin)
  ball('Cheek',2,(side*.15,hy-.055,.219),(.065,.04,.013),cheek)
  ball('Eye white',2,(side*.10,hy+.045,.224),(.068,.068,.025),white)
  ball('Pupil',2,(side*.10,hy+.043,.249),(.029,.036,.012),eye)
  ball('Eye glint',2,(side*.09,hy+.055,.260),(.008,.01,.004),white)
  brow=box('Eyebrow',2,(side*.105,hy+.135,.23),(.12,.026,.025),hair,.01);brow.rotation_euler[1]=side*.12
 ball('Nose',2,(0,hy-.015,.259),(.052,.055,.065),skin)
 # A small smile built from a curved row of dark segments.
 for j in range(7):
  x=(j-3)*.021;y=hy-.13+.018*(abs(j-3)/3)**2
  ball('Smile',2,(x,y,.235),(.014,.009,.008),hair)
 for j in range(6):
  ball('Hair fringe',2,((j-2.5)*.075,hy+.225,.16),(.065,.065,.10),hair)
 ball('Hair back',2,(0,hy+.12,-.13),(.26,.19,.13),hair)
 if hat=='ushanka':
  ball('Ushanka crown',2,(0,hy+.27,-.02),(.32,.19,.28),hatmat)
  box('Fur forehead',2,(0,hy+.235,.225),(.53,.16,.09),cream,.06)
  for side in [-1,1]:ball('Ear flap',2,(side*.29,hy+.035,-.02),(.11,.22,.17),hatmat)
 else:
  ball('Knit cap crown',2,(0,hy+.265,-.005),(.285*width,.19,.26),hatmat)
  tube('Knit folded brim',2,(0,hy+.19,0),.287*width,.105,hatmat)
  for j in range(16):
   a=j*math.tau/16
   ball('Knit rib',2,(math.cos(a)*.277*width,hy+.19,math.sin(a)*.277),(.013,.05,.013),hatmat)
  if hat=='pompom':ball('Pom pom',2,(0,hy+.47,0),(.10,.10,.10),cream)
  if index==3:ball('Hood behind neck',1,(0,.48,-.15),(.34,.31,.16),coat)
 # Scarf collar plus hanging end stays attached to chest.
 for j in range(2):ball('Scarf collar',1,(0,.45+j*.05,.025),(.35,.065,.29),scarf)
 box('Scarf hanging end',1,(.14,.12,.318),(.14,.53,.055),scarf,.025)
 if index==2:
  for j in range(5):box('Scarf stripe',1,(.14,-.08+j*.10,.351),(.143,.027,.014),cream,.004)
 for j in range(5):box('Scarf fringe',1,(.09+j*.025,-.18,.32),(.014,.06,.03),scarf,.005)
 for side,upper,lower in [(-1,3,4),(1,5,6)]:
  for j in range(3):ball('Sleeve padding',upper,(side*.51,.32-j*.14,0),(.155,.10,.17),coat)
  ball('Forearm sleeve',lower,(side*.58,-.13,.07),(.15,.16,.17),coat)
  ball('Mitten',lower,(side*.59,-.34,.10),(.15,.14,.15),scarf)
  ball('Thumb',lower,(side*.47,-.30,.19),(.07,.08,.07),scarf)
 for side,upper,lower in [(-1,7,8),(1,9,10)]:
  ball('Trouser thigh',upper,(side*.20,-.57,0),(.16,.24,.18),pants)
  ball('Trouser calf',lower,(side*.20,-.84,.005),(.145,.18,.16),pants)
  box('Boot',lower,(side*.20,-.96,.10),(.31,.25,.43),bootmat,.07)
  box('Boot sole',lower,(side*.20,-1.08,.105),(.33,.065,.46),sole,.025)
  if index==2:ball('Felt boot cuff',lower,(side*.20,-.82,0),(.165,.16,.18),bootmat)
  else:
   for j in range(3):box('Boot laces',lower,(side*.20,-.87-j*.047,.284),(.14,.013,.015),cream,.005)
 if index==2:
  box('Parka hem',0,(0,-.32,0),(.68,.2,.53),coat,.09)
 # Capture evaluated triangulated meshes in each physics segment's local coordinates.
 deps=bpy.context.evaluated_depsgraph_get();groups={}
 for o in pieces:
  bone=int(o['rig_bone']);material=o.data.materials[0];bucket=groups.setdefault((bone,material.name),{'bone':bone,'vertices':[],'normals':[],'triangles':[],'color':dict(zip(['r','g','b','a'],material.diffuse_color))})
  evaluated=o.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles();base=len(bucket['vertices'])//3
  for vertex in mesh.vertices:
   world=o.matrix_world@vertex.co;local=world-bv(REST[bone]);normal=(o.matrix_world.to_3x3().inverted().transposed()@vertex.normal).normalized()
   bucket['vertices'].extend(round(c,6) for c in uv(local));bucket['normals'].extend(round(c,6) for c in uv(normal))
  for triangle in mesh.loop_triangles:bucket['triangles'].extend(base+i for i in triangle.vertices)
  evaluated.to_mesh_clear()
 (DATA/(key+'.json')).write_text(json.dumps({'name':title,'parts':list(groups.values())},separators=(',',':')))
 # Real armature and rigid weights for editable Blender/FBX/GLB exports.
 bpy.ops.object.select_all(action='DESELECT');armdata=bpy.data.armatures.new(key+' skeleton');arm=bpy.data.objects.new(key,armdata);bpy.context.collection.objects.link(arm);bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for i,p in enumerate(REST):
  b=armdata.edit_bones.new('B%02d'%i);b.head=bv(p);b.tail=b.head+Vector((0,0,.18))
  if PARENTS[i]>=0:b.parent=armdata.edit_bones['B%02d'%PARENTS[i]]
 bpy.ops.object.mode_set(mode='OBJECT')
 for o in pieces:
  o.parent=arm;group=o.vertex_groups.new(name='B%02d'%int(o['rig_bone']));group.add(list(range(len(o.data.vertices))),1,'REPLACE');mod=o.modifiers.new('Winter rig','ARMATURE');mod.object=arm
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
 for o in pieces:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
 bpy.ops.export_scene.gltf(filepath=str(OUT/(key+'.glb')),use_selection=True,export_format='GLB',export_animations=False)
 stats[key]={'triangles':sum(len(g['triangles'])//3 for g in groups.values()),'parts':len(groups),'bones':11}
 arm.location.x=(index-1.5)*1.45;arm.location.z=1.12;all_roots.append(arm)
(OUT/'model-stats.json').write_text(json.dumps(stats,indent=2))
# Source scene and lit preview of actual generated meshes.
floor=mat('Preview backdrop',(.12,.18,.25));bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(floor)
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.33,.45,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4
for loc,power,size in [((-4,-5,7),1600,7),((5,1,5),2100,6),((0,-4,3),400,5)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-11,4.5));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1.2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=7.1
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.render.resolution_x=1400;scene.render.resolution_y=850;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'Preview.png');scene.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'WinterYard-Characters.blend'));bpy.ops.render.render(write_still=True)
print('CHARACTER_MODEL_STATS',json.dumps(stats))
