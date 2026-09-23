import bpy
import bmesh
import math
import random
from mathutils import Vector
import os

# ----------------- CLEANUP -----------------
bpy.ops.wm.read_factory_settings(use_empty=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
for mesh in list(bpy.data.meshes):
    bpy.data.meshes.remove(mesh)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

def make_mat(name, color, roughness=0.82):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if b:
        b.inputs["Base Color"].default_value = color
        b.inputs["Roughness"].default_value = roughness
    return m

# Planet of Lana color palette:
# Slate-grey cool stone with warm sunlight catching edge facets
mat_rock = make_mat("M_Rock_Base", (0.36, 0.40, 0.44, 1.0), roughness=0.80)
# Lush vibrant yellow-green moss shelf top
mat_moss = make_mat("M_Moss_Top", (0.28, 0.54, 0.16, 1.0), roughness=0.92)

def build_faceted_block(bm, pts):
    """Adds a watertight convex polyhedral block to an existing bmesh."""
    verts = [bm.verts.new(p) for p in pts]
    res = bmesh.ops.convex_hull(bm, input=verts)
    hull_geom = set(res["geom"])
    for v in list(bm.verts):
        if v in verts and v not in hull_geom:
            bm.verts.remove(v)

def finalize_asset(name, bm, has_moss=True, bevel_width=0.045):
    """Converts bmesh to mesh object, shifts pivot to Z=0, bevels edges, assigns UVs and materials."""
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    
    # Ground pivot (min Z = 0)
    min_z = min(v.co.z for v in obj.data.vertices)
    for v in obj.data.vertices:
        v.co.z -= min_z
    obj.data.update()
    
    # Bevel modifier for crisp stylized edge bevels
    if bevel_width > 0:
        bev = obj.modifiers.new("Bevel", type="BEVEL")
        bev.width = bevel_width
        bev.segments = 2
        bev.limit_method = "ANGLE"
        bev.angle_limit = math.radians(32.0)
        bpy.ops.object.modifier_apply(modifier="Bevel")
        
    for p in obj.data.polygons:
        p.use_smooth = False
        
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    
    obj.data.materials.append(mat_rock)
    col_attr = obj.data.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="POINT")
    max_z = max(0.001, max(v.co.z for v in obj.data.vertices))
    
    if has_moss:
        obj.data.materials.append(mat_moss)
        for poly in obj.data.polygons:
            # Flat top faces with upward normal receive moss
            if poly.normal.z > 0.68 and poly.center.z > max_z * 0.40:
                poly.material_index = 1
            else:
                poly.material_index = 0
                
    for i, v in enumerate(obj.data.vertices):
        hf = v.co.z / max_z
        if has_moss and v.normal.z > 0.60 and hf > 0.45:
            col_attr.data[i].color = (0.28, 0.54, 0.16, 1.0)
        else:
            # Ambient occlusion gradient: deeper shadow at bottom crevices
            ao = 0.35 + 0.50 * (hf ** 0.8)
            col_attr.data[i].color = (ao, ao, ao, 1.0)
            
    return obj

# ----------------- PLANET OF LANA MULTI-BLOCK ARCHETYPES -----------------

# 1. Cantilever Ledge Platform (Planet of Lana Reference 1 & 2)
# Composed of:
# Block A: Overhanging top shelf slab (Z = 1.2 to 1.8)
# Block B: Diagonal undercut corbel buttress (Z = 0.4 to 1.4)
# Block C: Stepped lower front block (Z = 0.0 to 0.7)
def make_cantilever_ledge():
    bm = bmesh.new()
    
    # Block A: Top Cantilever Slab with overhanging right prow
    pts_a = [
        # Top walk surface (Z = 1.8)
        Vector((-2.4, -1.1, 1.8)),
        Vector((-0.5, -1.2, 1.8)),
        Vector(( 1.2, -1.1, 1.8)),
        Vector(( 2.7, -0.4, 1.8)), # Far right diving ledge
        Vector(( 2.5,  0.7, 1.8)),
        Vector(( 0.8,  1.1, 1.8)),
        Vector((-1.0,  1.1, 1.8)),
        Vector((-2.4,  0.9, 1.8)),
        # Bottom of top slab (Z = 1.25)
        Vector((-2.5, -1.2, 1.25)),
        Vector((-0.4, -1.3, 1.25)),
        Vector(( 1.3, -1.2, 1.25)),
        Vector(( 2.8, -0.3, 1.25)),
        Vector(( 2.4,  0.8, 1.25)),
        Vector(( 0.7,  1.2, 1.25)),
        Vector((-1.0,  1.2, 1.25)),
        Vector((-2.5,  1.0, 1.25)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Angled Corbel Buttress supporting underneath
    pts_b = [
        # Upper contact with top slab (Z = 1.35)
        Vector((-2.2, -0.9, 1.35)),
        Vector(( 0.8, -0.9, 1.35)),
        Vector(( 1.5, -0.2, 1.35)), # Recessed under overhang!
        Vector(( 1.2,  0.7, 1.35)),
        Vector((-2.2,  0.7, 1.35)),
        # Lower taper (Z = 0.35)
        Vector((-1.8, -0.7, 0.35)),
        Vector(( 0.3, -0.7, 0.35)),
        Vector(( 0.2,  0.5, 0.35)),
        Vector((-1.8,  0.5, 0.35)),
    ]
    build_faceted_block(bm, pts_b)
    
    # Block C: Stepped foundation / lower front shelf
    pts_c = [
        # Top of lower step (Z = 0.7)
        Vector((-2.5, -1.4, 0.7)),
        Vector((-0.6, -1.4, 0.7)),
        Vector((-0.6, -0.5, 0.7)),
        Vector((-2.5, -0.5, 0.7)),
        # Base (Z = 0.0)
        Vector((-2.6, -1.5, 0.0)),
        Vector((-0.4, -1.5, 0.0)),
        Vector((-0.4, -0.4, 0.0)),
        Vector((-2.6, -0.4, 0.0)),
    ]
    build_faceted_block(bm, pts_c)
    
    return finalize_asset("Rock_Platform_Wide_01", bm, has_moss=True, bevel_width=0.045)

# 2. Anvil Tabletop Platform (Planet of Lana Reference 2 & 4)
# Composed of:
# Block A: Broad horizontal anvil slab
# Block B: Narrow vertical support column
# Block C: Stepped side terrace block
def make_anvil_platform():
    bm = bmesh.new()
    
    # Block A: Tabletop slab (Z = 1.3 to 1.8)
    pts_a = [
        # Top flat surface (Z = 1.8)
        Vector((-2.5, -1.2, 1.8)),
        Vector((-0.8, -1.3, 1.8)),
        Vector(( 1.2, -1.3, 1.8)),
        Vector(( 2.5, -1.0, 1.8)),
        Vector(( 2.4,  1.1, 1.8)),
        Vector(( 0.0,  1.3, 1.8)),
        Vector((-2.4,  1.1, 1.8)),
        # Underside perimeter with bevel rim (Z = 1.3)
        Vector((-2.6, -1.3, 1.3)),
        Vector(( 2.6, -1.1, 1.3)),
        Vector(( 2.5,  1.2, 1.3)),
        Vector((-2.5,  1.2, 1.3)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Narrow central columnar pedestal (Z = 0.0 to 1.4)
    pts_b = [
        Vector((-0.9, -0.7, 1.4)),
        Vector(( 0.9, -0.7, 1.4)),
        Vector(( 0.8,  0.7, 1.4)),
        Vector((-0.8,  0.7, 1.4)),
        Vector((-1.0, -0.8, 0.0)),
        Vector(( 1.0, -0.8, 0.0)),
        Vector(( 0.9,  0.8, 0.0)),
        Vector((-0.9,  0.8, 0.0)),
    ]
    build_faceted_block(bm, pts_b)
    
    # Block C: Right-side stepped ledge buttress
    pts_c = [
        Vector(( 1.2, -1.0, 0.9)),
        Vector(( 2.2, -0.9, 0.9)),
        Vector(( 2.1,  0.6, 0.9)),
        Vector(( 1.1,  0.6, 0.9)),
        Vector(( 1.1, -1.1, 0.0)),
        Vector(( 2.4, -1.0, 0.0)),
        Vector(( 2.2,  0.7, 0.0)),
        Vector(( 1.0,  0.7, 0.0)),
    ]
    build_faceted_block(bm, pts_c)
    
    return finalize_asset("Rock_Platform_Moss_01", bm, has_moss=True, bevel_width=0.045)

# 3. Wedged Chasm Boulder (Planet of Lana Reference 3)
# The centerpiece boulder wedged between two canyon cliffs:
# Composed of:
# Block A: Main massive diamond polyhedron
# Block B: Flat elevated top lookout perch
# Block C: Sub-boulder wedge underbelly
def make_wedged_boulder():
    bm = bmesh.new()
    
    # Block A: Main faceted body with sharp diagonal equator
    pts_a = [
        # Upper facet rim (Z = 1.7)
        Vector((-1.0, -0.9, 1.7)),
        Vector(( 0.9, -0.8, 1.7)),
        Vector(( 0.7,  0.8, 1.7)),
        Vector((-0.8,  0.8, 1.7)),
        # Broad diamond midriff (Z = 0.9)
        Vector((-1.9,  0.1, 0.9)),
        Vector(( 1.8, -0.1, 0.9)),
        Vector(( 0.2, -1.6, 0.9)),
        Vector((-0.1,  1.5, 0.9)),
        Vector((-1.3, -1.2, 0.9)),
        Vector(( 1.3, -1.1, 0.9)),
        # Bottom keel (Z = 0.2)
        Vector((-0.5, -0.5, 0.2)),
        Vector(( 0.6, -0.4, 0.2)),
        Vector(( 0.4,  0.5, 0.2)),
        Vector((-0.4,  0.4, 0.2)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Flat top mossy observation platform
    pts_b = [
        Vector((-0.8, -0.7, 2.2)),
        Vector(( 0.7, -0.6, 2.2)),
        Vector(( 0.6,  0.6, 2.2)),
        Vector((-0.6,  0.6, 2.2)),
        Vector((-1.1, -0.9, 1.5)),
        Vector(( 1.0, -0.8, 1.5)),
        Vector(( 0.8,  0.8, 1.5)),
        Vector((-0.9,  0.8, 1.5)),
    ]
    build_faceted_block(bm, pts_b)
    
    # Block C: Keel wedge block
    pts_c = [
        Vector((-0.6, -0.5, 0.4)),
        Vector(( 0.7, -0.4, 0.4)),
        Vector(( 0.4,  0.5, 0.4)),
        Vector((-0.5,  0.5, 0.4)),
        Vector(( 0.0,  0.0, 0.0)),
    ]
    build_faceted_block(bm, pts_c)
    
    return finalize_asset("Rock_Platform_Sharp_01", bm, has_moss=True, bevel_width=0.05)

# 4. Monumental Megalith Boulder (Planet of Lana Reference 1 & 3)
# Multi-faceted architectural block assembly:
# Block A: Main angular megalith
# Block B: Flanking angled fracture slab
def make_megalith_boulder():
    bm = bmesh.new()
    
    # Block A: Main monolithic prism
    pts_a = [
        # Top slanted crown (Z = 1.9)
        Vector((-0.9, -0.6, 1.9)),
        Vector(( 0.6, -0.5, 1.9)),
        Vector(( 0.4,  0.7, 1.9)),
        Vector((-0.8,  0.6, 1.9)),
        # Mid planar facets (Z = 1.0)
        Vector((-1.6, -0.3, 1.0)),
        Vector(( 1.4, -0.2, 1.0)),
        Vector(( 0.1, -1.4, 1.0)),
        Vector((-0.2,  1.3, 1.0)),
        Vector((-1.2, -1.1, 1.0)),
        Vector(( 1.1, -1.0, 1.0)),
        # Base (Z = 0.0)
        Vector((-1.4, -0.8, 0.0)),
        Vector(( 1.2, -0.7, 0.0)),
        Vector(( 1.0,  0.8, 0.0)),
        Vector((-1.2,  0.7, 0.0)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Interlocking left fracture facet
    pts_b = [
        Vector((-1.8, -0.6, 1.1)),
        Vector((-0.7, -0.7, 1.1)),
        Vector((-0.8,  0.4, 1.1)),
        Vector((-1.7,  0.3, 1.1)),
        Vector((-2.0, -0.7, 0.0)),
        Vector((-0.8, -0.8, 0.0)),
        Vector((-0.9,  0.5, 0.0)),
        Vector((-1.9,  0.4, 0.0)),
    ]
    build_faceted_block(bm, pts_b)
    
    return finalize_asset("Rock_Big_Boulder_01", bm, has_moss=True, bevel_width=0.045)

# 5. Stepping Platform Boulder (Planet of Lana Reference 2)
# Low resting boulder with flat moss cap and stepped lower footholds
def make_stepping_boulder():
    bm = bmesh.new()
    
    # Block A: Main flat-topped resting boulder
    pts_a = [
        # Broad walkable top (Z = 1.2)
        Vector((-1.2, -0.8, 1.2)),
        Vector(( 0.8, -0.8, 1.2)),
        Vector(( 0.7,  0.8, 1.2)),
        Vector((-1.1,  0.8, 1.2)),
        # Mid facet rim (Z = 0.6)
        Vector((-1.6, -1.1, 0.6)),
        Vector(( 1.1, -1.0, 0.6)),
        Vector(( 1.0,  1.0, 0.6)),
        Vector((-1.4,  1.0, 0.6)),
        # Ground base (Z = 0.0)
        Vector((-1.4, -0.9, 0.0)),
        Vector(( 0.9, -0.9, 0.0)),
        Vector(( 0.8,  0.8, 0.0)),
        Vector((-1.2,  0.8, 0.0)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Stepped lower terrace on the right (Z = 0.6)
    pts_b = [
        Vector(( 0.7, -0.7, 0.65)),
        Vector(( 1.8, -0.6, 0.65)),
        Vector(( 1.6,  0.6, 0.65)),
        Vector(( 0.6,  0.6, 0.65)),
        Vector(( 0.6, -0.8, 0.0)),
        Vector(( 1.9, -0.7, 0.0)),
        Vector(( 1.7,  0.7, 0.0)),
        Vector(( 0.5,  0.7, 0.0)),
    ]
    build_faceted_block(bm, pts_b)
    
    return finalize_asset("Rock_Big_Boulder_Moss_01", bm, has_moss=True, bevel_width=0.04)

# 6. Columnar Cliff Spire (Planet of Lana Reference 1 & 3)
# Tall vertical cliff column composed of 3 vertical faceted blocks with diagonal cleavage
def make_cliff_column():
    bm = bmesh.new()
    
    # Block A: Main tall vertical pillar (Z = 0.0 to 6.8)
    pts_a = [
        # Top apex (Z = 6.8)
        Vector((-0.7, -0.6, 6.8)),
        Vector(( 0.8, -0.5, 6.8)),
        Vector(( 0.6,  0.7, 6.8)),
        Vector((-0.6,  0.6, 6.8)),
        # Mid column facets (Z = 3.5)
        Vector((-1.2, -0.9, 3.5)),
        Vector(( 1.1, -0.8, 3.5)),
        Vector(( 0.9,  1.0, 3.5)),
        Vector((-1.0,  0.9, 3.5)),
        # Base (Z = 0.0)
        Vector((-1.3, -1.0, 0.0)),
        Vector(( 1.2, -0.9, 0.0)),
        Vector(( 1.0,  1.1, 0.0)),
        Vector((-1.1,  1.0, 0.0)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Flanking mid-height columnar buttress on the right (Z = 0.0 to 3.8)
    pts_b = [
        # Shelf top (Z = 3.8)
        Vector(( 0.8, -0.8, 3.8)),
        Vector(( 1.9, -0.7, 3.8)),
        Vector(( 1.6,  0.8, 3.8)),
        Vector(( 0.7,  0.7, 3.8)),
        # Base (Z = 0.0)
        Vector(( 0.8, -0.9, 0.0)),
        Vector(( 2.0, -0.8, 0.0)),
        Vector(( 1.8,  0.9, 0.0)),
        Vector(( 0.7,  0.8, 0.0)),
    ]
    build_faceted_block(bm, pts_b)
    
    # Block C: Diagonal cleave wedge at mid cliff (Z = 2.0 to 5.0)
    pts_c = [
        Vector((-1.4, -0.4, 5.0)),
        Vector((-0.5, -0.5, 5.0)),
        Vector((-0.5,  0.6, 5.0)),
        Vector((-1.3,  0.5, 5.0)),
        Vector((-1.5, -0.6, 2.0)),
        Vector((-0.6, -0.6, 2.0)),
        Vector((-0.6,  0.7, 2.0)),
        Vector((-1.4,  0.6, 2.0)),
    ]
    build_faceted_block(bm, pts_c)
    
    return finalize_asset("Rock_Sharp_Spire_01", bm, has_moss=True, bevel_width=0.055)

# 7. Tabular Flagstone (Planet of Lana Ground Detail)
# Low-profile flat walkable stepping stone with clean beveled perimeter
def make_flagstone():
    bm = bmesh.new()
    pts = [
        # Top walking polygon (Z = 0.28)
        Vector((-1.1, -0.8, 0.28)),
        Vector(( 0.0, -1.0, 0.28)),
        Vector(( 1.1, -0.7, 0.28)),
        Vector(( 1.2,  0.2, 0.28)),
        Vector(( 0.6,  0.9, 0.28)),
        Vector((-0.8,  0.9, 0.28)),
        Vector((-1.2,  0.1, 0.28)),
        # Bottom perimeter (Z = 0.0)
        Vector((-0.9, -0.65, 0.0)),
        Vector(( 0.0, -0.85, 0.0)),
        Vector(( 0.9, -0.55, 0.0)),
        Vector(( 1.0,  0.15, 0.0)),
        Vector(( 0.5,  0.75, 0.0)),
        Vector((-0.7,  0.75, 0.0)),
        Vector((-1.0,  0.05, 0.0)),
    ]
    build_faceted_block(bm, pts)
    return finalize_asset("Rock_Path_SteppingStone_01", bm, has_moss=True, bevel_width=0.035)

# 8. Path Pebble Cluster (Planet of Lana Ground Detail)
# 4 distinct chiseled geometric stones grouped organically
def make_pebble_cluster():
    bm = bmesh.new()
    pebble_defs = [
        # pos, scale, height
        (Vector(( 0.0,   0.0,  0.0)), 0.65, 0.42),
        (Vector((-0.85,  0.35, 0.0)), 0.48, 0.32),
        (Vector(( 0.80,  0.28, 0.0)), 0.52, 0.35),
        (Vector(( 0.25, -0.60, 0.0)), 0.38, 0.25),
    ]
    for pos, s, h in pebble_defs:
        pts = [
            # Top polygon
            Vector((pos.x - 0.5 * s, pos.y - 0.4 * s, h)),
            Vector((pos.x + 0.4 * s, pos.y - 0.5 * s, h)),
            Vector((pos.x + 0.5 * s, pos.y + 0.4 * s, h)),
            Vector((pos.x - 0.4 * s, pos.y + 0.5 * s, h)),
            # Mid perimeter
            Vector((pos.x - 0.7 * s, pos.y,           h * 0.5)),
            Vector((pos.x + 0.7 * s, pos.y,           h * 0.5)),
            Vector((pos.x,           pos.y - 0.6 * s, h * 0.5)),
            Vector((pos.x,           pos.y + 0.6 * s, h * 0.5)),
            # Base
            Vector((pos.x - 0.4 * s, pos.y - 0.3 * s, 0.0)),
            Vector((pos.x + 0.4 * s, pos.y - 0.3 * s, 0.0)),
            Vector((pos.x + 0.3 * s, pos.y + 0.3 * s, 0.0)),
            Vector((pos.x - 0.3 * s, pos.y + 0.3 * s, 0.0)),
        ]
        build_faceted_block(bm, pts)
        
    return finalize_asset("Rock_Path_PebbleCluster_01", bm, has_moss=True, bevel_width=0.02)

# 9. Background Mountain Cliff (Planet of Lana Reference 1 & 4)
# Massive monumental cliff backdrop with stepped horizontal strata and vertical clefts
def make_background_mountain():
    bm = bmesh.new()
    
    # Block A: Main massive cliff ridge (Z = 0.0 to 12.5)
    pts_a = [
        # Peak ridge (Z = 12.5)
        Vector((-2.5, -1.0, 12.5)),
        Vector(( 1.8, -1.0, 12.5)),
        Vector(( 1.4,  1.5, 12.5)),
        Vector((-2.0,  1.5, 12.5)),
        # Mid cliff strata shelf (Z = 6.5)
        Vector((-4.5, -2.5, 6.5)),
        Vector(( 3.8, -2.2, 6.5)),
        Vector(( 3.2,  2.8, 6.5)),
        Vector((-3.8,  2.8, 6.5)),
        # Base (Z = 0.0)
        Vector((-5.8, -3.2, 0.0)),
        Vector(( 5.2, -3.0, 0.0)),
        Vector(( 4.5,  3.5, 0.0)),
        Vector((-5.0,  3.5, 0.0)),
    ]
    build_faceted_block(bm, pts_a)
    
    # Block B: Interlocking left cliff buttress (Z = 0.0 to 8.5)
    pts_b = [
        Vector((-5.2, -1.8, 8.5)),
        Vector((-2.0, -1.9, 8.5)),
        Vector((-2.0,  1.8, 8.5)),
        Vector((-5.0,  1.8, 8.5)),
        Vector((-6.5, -2.8, 0.0)),
        Vector((-2.2, -2.5, 0.0)),
        Vector((-2.2,  2.5, 0.0)),
        Vector((-6.0,  2.5, 0.0)),
    ]
    build_faceted_block(bm, pts_b)
    
    return finalize_asset("Rock_Background_Cliff_01", bm, has_moss=False, bevel_width=0.08)

# ----------------- BUILD ALL -----------------
lana_rocks = {
    "Rock_Platform_Wide_01": make_cantilever_ledge(),
    "Rock_Platform_Moss_01": make_anvil_platform(),
    "Rock_Platform_Sharp_01": make_wedged_boulder(),
    "Rock_Big_Boulder_01": make_megalith_boulder(),
    "Rock_Big_Boulder_Moss_01": make_stepping_boulder(),
    "Rock_Sharp_Spire_01": make_cliff_column(),
    "Rock_Path_SteppingStone_01": make_flagstone(),
    "Rock_Path_PebbleCluster_01": make_pebble_cluster(),
    "Rock_Background_Cliff_01": make_background_mountain(),
}

output_dir = os.path.abspath("Assets/Models/Rocks")
os.makedirs(output_dir, exist_ok=True)

for name, obj in lana_rocks.items():
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    fbx_file = os.path.join(output_dir, f"{name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_file,
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        mesh_smooth_type="FACE"
    )
    print(f"Exported Lana FBX: {name}.fbx (Verts: {len(obj.data.vertices)}, Polys: {len(obj.data.polygons)})")

# ----------------- SHOWCASE ARRANGEMENT & RENDER -----------------
layout = [
    # Back Row (Background mountain & columnar cliff & wedged chasm rock)
    ("Rock_Background_Cliff_01", (-9.2, 8.0, 0.0), (0.45, 0.45, 0.45)),
    ("Rock_Sharp_Spire_01", (-3.5, 4.5, 0.0), (0.85, 0.85, 0.85)),
    ("Rock_Big_Boulder_01", (1.2, 4.0, 0.0), (0.95, 0.95, 0.95)),
    ("Rock_Platform_Sharp_01", (6.5, 4.0, 0.0), (0.95, 0.95, 0.95)),
    
    # Middle Row (Cantilever Ledge & Anvil Platform & Stepping Boulder)
    ("Rock_Big_Boulder_Moss_01", (-6.8, 0.5, 0.0), (1.1, 1.1, 1.1)),
    ("Rock_Platform_Wide_01", (-1.0, 0.5, 0.0), (0.9, 0.9, 0.9)),
    ("Rock_Platform_Moss_01", (5.2, 0.5, 0.0), (0.9, 0.9, 0.9)),
    
    # Front Row (Path flagstone & pebbles)
    ("Rock_Path_SteppingStone_01", (-2.6, -3.5, 0.0), (1.1, 1.1, 1.1)),
    ("Rock_Path_PebbleCluster_01", (2.2, -3.5, 0.0), (1.1, 1.1, 1.1)),
]

for name, pos, sc in layout:
    o = lana_rocks[name]
    o.location = pos
    o.scale = sc

# Pedestal Ground
bpy.ops.mesh.primitive_plane_add(size=50, location=(0, 2.0, -0.02))
ground = bpy.context.active_object
mat_g = make_mat("M_Pedestal", (0.16, 0.18, 0.20, 1.0), roughness=0.98)
ground.data.materials.append(mat_g)

# Side-scrolling 2.5D camera angle (like Planet of Lana)
cam_data = bpy.data.cameras.new("Cam")
cam_data.lens = 38
cam_obj = bpy.data.objects.new("Cam", cam_data)
bpy.context.collection.objects.link(cam_obj)
cam_obj.location = (0.0, -23.0, 7.0)
cam_obj.rotation_euler = (math.radians(76.0), 0.0, 0.0)
bpy.context.scene.camera = cam_obj

# Key Sun Light (Warm golden sunlight like Lana)
sun_data = bpy.data.lights.new("Sun", type="SUN")
sun_data.energy = 4.2
sun_data.color = (1.0, 0.96, 0.88)
sun_obj = bpy.data.objects.new("Sun", sun_data)
bpy.context.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (math.radians(48.0), math.radians(22.0), math.radians(-40.0))

# Ambient Sky Fill Light (Deep cyan/sky blue like Lana sky)
fill_data = bpy.data.lights.new("Fill", type="SUN")
fill_data.energy = 2.0
fill_data.color = (0.50, 0.72, 0.92)
fill_obj = bpy.data.objects.new("Fill", fill_data)
bpy.context.collection.objects.link(fill_obj)
fill_obj.rotation_euler = (math.radians(60.0), math.radians(-25.0), math.radians(135.0))

# World sky
world = bpy.data.worlds.new("World_Sky")
bpy.context.scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs["Color"].default_value = (0.20, 0.28, 0.38, 1.0)
    bg.inputs["Strength"].default_value = 0.90

scene = bpy.context.scene
scene.render.resolution_x = 1440
scene.render.resolution_y = 810
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
render_path = os.path.abspath("Assets/Models/Rocks/Rocks_Showcase.png")
scene.render.filepath = render_path
bpy.ops.render.render(write_still=True)
print(f"LANA_ROCKS_SHOWCASE_RENDERED: {render_path}")
