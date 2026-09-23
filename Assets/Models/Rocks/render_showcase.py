import bpy
import bmesh
import math
import os
import sys

# Add directory to sys.path
sys.path.append(os.path.abspath("Assets/Models/Rocks"))
import generate_rocks as gr

# Reset
bpy.ops.wm.read_factory_settings(use_empty=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
for mesh in list(bpy.data.meshes):
    bpy.data.meshes.remove(mesh)

rock_configs = [
    (gr.make_rock_background_cliff, (-9.0, 9.0, 0.0), 0.55),
    (gr.make_rock_sharp_spire, (-4.2, 4.0, 0.0), 1.0),
    (gr.make_rock_big_boulder, (0.2, 4.0, 0.0), 1.0),
    (gr.make_rock_big_boulder_moss, (4.6, 4.0, 0.0), 1.0),
    (gr.make_rock_platform_sharp, (-5.2, -0.6, 0.0), 1.0),
    (gr.make_rock_platform_wide, (-0.2, -0.6, 0.0), 1.0),
    (gr.make_rock_platform_moss, (5.0, -0.6, 0.0), 1.0),
    (gr.make_rock_path_stepping_stone, (-2.5, -4.0, 0.0), 1.3),
    (gr.make_rock_path_pebble_cluster, (2.2, -4.0, 0.0), 1.3),
]

created_objs = []
for func, pos, scale in rock_configs:
    obj = func()
    obj.location = pos
    obj.scale = (scale, scale, scale)
    created_objs.append(obj)

# Create Ground Pedestal Plane
bpy.ops.mesh.primitive_plane_add(size=50, location=(0, 2.0, -0.05))
ground = bpy.context.active_object
ground.name = "Showcase_Ground"
mat_g = bpy.data.materials.new("M_Ground")
mat_g.use_nodes = True
bsdf_g = mat_g.node_tree.nodes.get("Principled BSDF")
if bsdf_g:
    bsdf_g.inputs["Base Color"].default_value = (0.16, 0.20, 0.16, 1.0)
    bsdf_g.inputs["Roughness"].default_value = 0.95
ground.data.materials.append(mat_g)

# Camera setup
cam_data = bpy.data.cameras.new("Showcase_Camera")
cam_data.lens = 42
cam_obj = bpy.data.objects.new("Showcase_Camera", cam_data)
bpy.context.collection.objects.link(cam_obj)
cam_obj.location = (0.0, -14.5, 7.5)
cam_obj.rotation_euler = (math.radians(65.0), 0.0, 0.0)
bpy.context.scene.camera = cam_obj

# Key Light (Sun)
light_data = bpy.data.lights.new(name="SunLight", type="SUN")
light_data.energy = 3.5
light_data.color = (1.0, 0.96, 0.90)
light_obj = bpy.data.objects.new(name="SunLight", object_data=light_data)
bpy.context.collection.objects.link(light_obj)
light_obj.rotation_euler = (math.radians(52.0), math.radians(20.0), math.radians(-38.0))

# Fill Light (Sky/Ambient)
fill_data = bpy.data.lights.new(name="FillLight", type="SUN")
fill_data.energy = 1.2
fill_data.color = (0.65, 0.75, 0.92)
fill_obj = bpy.data.objects.new(name="FillLight", object_data=fill_data)
bpy.context.collection.objects.link(fill_obj)
fill_obj.rotation_euler = (math.radians(60.0), math.radians(-30.0), math.radians(140.0))

# World background color
world = bpy.context.scene.world
if not world:
    world = bpy.data.worlds.new("World")
    bpy.context.scene.world = world
world.use_nodes = True
bg_node = world.node_tree.nodes.get("Background")
if bg_node:
    bg_node.inputs["Color"].default_value = (0.28, 0.32, 0.38, 1.0)
    bg_node.inputs["Strength"].default_value = 0.8

# Render settings
scene = bpy.context.scene
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"

output_path = os.path.abspath("Assets/Models/Rocks/Rocks_Showcase.png")
scene.render.filepath = output_path
bpy.ops.render.render(write_still=True)
print(f"RENDER_COMPLETED: {output_path}")
