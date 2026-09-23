import bpy
import bmesh
import math
import random
from mathutils import Vector, Matrix
import os

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)

def get_or_create_material(name, base_color, roughness=0.85):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = base_color
        bsdf.inputs["Roughness"].default_value = roughness
    return mat

def apply_planar_chops(bm, num_chops=16, radius=1.5, seed=123):
    random.seed(seed)
    for i in range(num_chops):
        theta = random.uniform(0, math.pi * 2)
        phi = random.uniform(-math.pi * 0.42, math.pi * 0.42)
        nx = math.cos(phi) * math.cos(theta)
        ny = math.cos(phi) * math.sin(theta)
        nz = math.sin(phi)
        normal = Vector((nx, ny, nz)).normalized()
        dist = random.uniform(radius * 0.78, radius * 1.12)
        plane_pt = normal * dist
        bmesh.ops.bisect_plane(
            bm,
            geom=bm.faces[:] + bm.edges[:] + bm.verts[:],
            plane_co=plane_pt,
            plane_no=normal,
            clear_outer=True,
            clear_inner=False
        )

def setup_materials_and_vertex_colors(obj, has_moss=False):
    mat_rock = get_or_create_material("M_Rock_Base", (0.42, 0.45, 0.49, 1.0), roughness=0.9)
    obj.data.materials.append(mat_rock)
    
    if has_moss:
        mat_moss = get_or_create_material("M_Moss_Top", (0.22, 0.48, 0.20, 1.0), roughness=0.95)
        obj.data.materials.append(mat_moss)

    col_attr = obj.data.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="POINT")
    min_z = min(v.co.z for v in obj.data.vertices)
    max_z = max(v.co.z for v in obj.data.vertices)
    z_range = max(0.001, max_z - min_z)
    
    moss_mat_idx = 1 if has_moss else 0
    for poly in obj.data.polygons:
        if has_moss and poly.normal.z > 0.42 and (poly.center.z - min_z) / z_range > 0.35:
            poly.material_index = moss_mat_idx
        else:
            poly.material_index = 0

    for i, v in enumerate(obj.data.vertices):
        height_factor = (v.co.z - min_z) / z_range
        if has_moss and v.normal.z > 0.38 and height_factor > 0.38:
            col_attr.data[i].color = (0.24, 0.52, 0.20, 1.0)
        else:
            ao = 0.35 + 0.38 * height_factor
            col_attr.data[i].color = (ao, ao, ao, 1.0)

def unwrap_and_smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    
    for p in obj.data.polygons:
        p.use_smooth = True

def set_pivot_to_bottom(obj):
    min_z = min(v.co.z for v in obj.data.vertices)
    for v in obj.data.vertices:
        v.co.z -= min_z
    obj.data.update()

def finalize_rock_mesh(obj, decimate_ratio=0.55, has_moss=False):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    
    if decimate_ratio < 1.0 and len(obj.data.polygons) > 20:
        mod = obj.modifiers.new(name="Decimate", type="DECIMATE")
        mod.ratio = decimate_ratio
        bpy.ops.object.modifier_apply(modifier=mod.name)
        
    set_pivot_to_bottom(obj)
    unwrap_and_smooth(obj)
    setup_materials_and_vertex_colors(obj, has_moss=has_moss)

# ---------------- ROCK GENERATORS ----------------

def make_rock_platform_wide():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Platform_Wide_01")
    obj = bpy.data.objects.new("Rock_Platform_Wide_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    for v in bm.verts:
        v.co.x *= 2.6
        v.co.y *= 1.45
        v.co.z *= 0.8
        if v.co.z < 0:
            v.co.x *= 0.72
            v.co.y *= 0.72
            
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=5, use_grid_fill=True)
    for v in bm.verts:
        if v.co.z > 0.48:
            v.co.z = 0.80 + math.sin(v.co.x * 1.4) * math.cos(v.co.y * 1.4) * 0.05
        else:
            n = math.sin(v.co.x * 1.8) * math.cos(v.co.y * 2.0) * 0.4 + math.sin(v.co.z * 3.5) * 0.2
            v.co += v.co.normalized() * n
            
    apply_planar_chops(bm, num_chops=20, radius=1.8, seed=101)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.55, has_moss=False)
    return obj

def make_rock_platform_moss():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Platform_Moss_01")
    obj = bpy.data.objects.new("Rock_Platform_Moss_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    for v in bm.verts:
        v.co.x *= 2.1
        v.co.y *= 1.35
        v.co.z *= 0.75
        if v.co.z < 0:
            v.co.x *= 0.7
            v.co.y *= 0.7
            
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=5, use_grid_fill=True)
    for v in bm.verts:
        if v.co.z > 0.42:
            v.co.z = 0.75 + math.sin(v.co.x * 1.6) * 0.04
        else:
            n = math.sin(v.co.x * 2.1) * math.sin(v.co.y * 2.0) * 0.35
            v.co += v.co.normalized() * n
            
    apply_planar_chops(bm, num_chops=18, radius=1.6, seed=202)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.55, has_moss=True)
    return obj

def make_rock_platform_sharp():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Platform_Sharp_01")
    obj = bpy.data.objects.new("Rock_Platform_Sharp_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    for v in bm.verts:
        v.co.x *= 2.3
        v.co.y *= 1.25
        v.co.z *= 0.95
        v.co.x += v.co.z * 0.5
        if v.co.z < 0:
            v.co.x *= 0.65
            v.co.y *= 0.65
            
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
    for v in bm.verts:
        if v.co.z > 0.65:
            v.co.z = 0.95 + math.sin(v.co.x * 2.2) * 0.04
            
    apply_planar_chops(bm, num_chops=26, radius=1.7, seed=303)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.5, has_moss=False)
    return obj

def make_rock_big_boulder():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Big_Boulder_01")
    obj = bpy.data.objects.new("Rock_Big_Boulder_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=4, radius=1.85)
    for v in bm.verts:
        v.co.x *= 1.35
        v.co.y *= 1.12
        v.co.z *= 0.95
        n = math.sin(v.co.x * 1.7) * math.cos(v.co.y * 1.7) * 0.35 + math.sin(v.co.z * 2.8) * 0.18
        v.co += v.co.normalized() * n
        
    apply_planar_chops(bm, num_chops=22, radius=1.85, seed=404)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.5, has_moss=False)
    return obj

def make_rock_big_boulder_moss():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Big_Boulder_Moss_01")
    obj = bpy.data.objects.new("Rock_Big_Boulder_Moss_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=4, radius=2.15)
    for v in bm.verts:
        v.co.x *= 1.28
        v.co.y *= 1.20
        v.co.z *= 1.05
        n = math.sin(v.co.x * 1.4) * math.cos(v.co.y * 1.6) * 0.32
        v.co += v.co.normalized() * n
        
    apply_planar_chops(bm, num_chops=20, radius=2.15, seed=505)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.5, has_moss=True)
    return obj

def make_rock_sharp_spire():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Sharp_Spire_01")
    obj = bpy.data.objects.new("Rock_Sharp_Spire_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, segments=10, cap_ends=True, radius1=1.5, radius2=0.18, depth=6.5)
    for v in bm.verts:
        v.co.x += (v.co.z + 3.2) * 0.16
        v.co.y *= 0.82
        
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
    apply_planar_chops(bm, num_chops=28, radius=2.3, seed=606)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.45, has_moss=False)
    return obj

def make_rock_path_stepping_stone():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Path_SteppingStone_01")
    obj = bpy.data.objects.new("Rock_Path_SteppingStone_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, segments=16, cap_ends=True, radius1=0.9, radius2=0.9, depth=0.32)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=2, use_grid_fill=True)
    for v in bm.verts:
        v.co.x *= 1.15
        v.co.y *= 0.92
        if v.co.z > 0.05:
            v.co.z = 0.16 + math.sin(v.co.x * 2.8) * 0.02
        else:
            v.co.z = -0.16
            
    apply_planar_chops(bm, num_chops=14, radius=0.92, seed=707)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.6, has_moss=True)
    return obj

def make_rock_path_pebble_cluster():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Path_PebbleCluster_01")
    obj = bpy.data.objects.new("Rock_Path_PebbleCluster_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    offsets = [
        (0.0, 0.0, 0.50),
        (-0.70, 0.45, 0.36),
        (0.75, 0.32, 0.40),
        (0.28, -0.60, 0.30),
        (-0.55, -0.40, 0.25)
    ]
    
    for ox, oy, scale in offsets:
        bm_stone = bmesh.new()
        bmesh.ops.create_icosphere(bm_stone, subdivisions=3, radius=scale)
        for v in bm_stone.verts:
            v.co.z *= 0.65
        apply_planar_chops(bm_stone, num_chops=8, radius=scale * 0.98, seed=int(ox * 100 + oy * 50 + 999))
        for v in bm_stone.verts:
            v.co.x += ox
            v.co.y += oy
            
        vert_map = {}
        for v in bm_stone.verts:
            vert_map[v] = bm.verts.new(v.co)
        bm.verts.ensure_lookup_table()
        for f in bm_stone.faces:
            try:
                bm.faces.new([vert_map[v] for v in f.verts])
            except ValueError:
                pass
        bm_stone.free()
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.55, has_moss=True)
    return obj

def make_rock_background_cliff():
    reset_scene()
    mesh = bpy.data.meshes.new("Rock_Background_Cliff_01")
    obj = bpy.data.objects.new("Rock_Background_Cliff_01", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    for v in bm.verts:
        v.co.x *= 6.5
        v.co.y *= 3.2
        v.co.z *= 8.5
        if v.co.z > 0:
            factor = 1.0 - (v.co.z / 17.0) * 0.35
            v.co.x *= factor
            v.co.y *= factor
            
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
    for v in bm.verts:
        layer = math.sin(v.co.z * 1.1) * 0.7
        v.co.x += layer
        n = math.sin(v.co.x * 0.7) * math.cos(v.co.z * 0.45) * 0.9
        v.co.y += n
        
    apply_planar_chops(bm, num_chops=30, radius=8.5, seed=808)
    bm.to_mesh(mesh)
    bm.free()
    finalize_rock_mesh(obj, decimate_ratio=0.4, has_moss=False)
    return obj

GENERATORS = [
    ("Rock_Platform_Wide_01", make_rock_platform_wide),
    ("Rock_Platform_Moss_01", make_rock_platform_moss),
    ("Rock_Platform_Sharp_01", make_rock_platform_sharp),
    ("Rock_Big_Boulder_01", make_rock_big_boulder),
    ("Rock_Big_Boulder_Moss_01", make_rock_big_boulder_moss),
    ("Rock_Sharp_Spire_01", make_rock_sharp_spire),
    ("Rock_Path_SteppingStone_01", make_rock_path_stepping_stone),
    ("Rock_Path_PebbleCluster_01", make_rock_path_pebble_cluster),
    ("Rock_Background_Cliff_01", make_rock_background_cliff),
]

output_dir = os.path.abspath("Assets/Models/Rocks")
os.makedirs(output_dir, exist_ok=True)

print(f"Generating {len(GENERATORS)} rock assets...")
for name, func in GENERATORS:
    print(f"--> Building {name}...")
    obj = func()
    fbx_path = os.path.join(output_dir, f"{name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        mesh_smooth_type="FACE"
    )
    print(f"SUCCESS: {name}.fbx -> Verts: {len(obj.data.vertices)}, Polys: {len(obj.data.polygons)}")

print("ALL_ROCKS_GENERATED_SUCCESSFULLY")
