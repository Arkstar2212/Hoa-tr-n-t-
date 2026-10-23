import bpy
import math
import os

def create_smooth_cutscene_observation():
    # 1. Reset scene
    bpy.ops.wm.read_factory_settings(use_empty=True)
    
    # 2. Import base character model
    fbx_source = r"C:\Users\Arkstarz\Documents\GitHub\Hoa-tr-n-t-\Assets\Models\Characters\Character_An.fbx"
    if not os.path.exists(fbx_source):
        print(f"Error: {fbx_source} not found")
        return
        
    bpy.ops.import_scene.fbx(filepath=fbx_source)
    
    # Find Armature
    arm_obj = None
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            arm_obj = obj
            break
            
    if not arm_obj:
        print("Error: No armature found!")
        return

    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')
    
    # 3. Create or clear Action
    action_name = "An_Cutscene_Observation"
    action = bpy.data.actions.new(name=action_name)
    if not arm_obj.animation_data:
        arm_obj.animation_data_create()
    arm_obj.animation_data.action = action
    
    # Define timing (at 30 FPS, 210 frames = 7.0 seconds)
    fps = 30
    bpy.context.scene.render.fps = fps
    bpy.context.scene.frame_start = 0
    bpy.context.scene.frame_end = 210
    
    pose_bones = arm_obj.pose.bones
    
    # Target bones for subtle acting
    b_head = pose_bones.get("CC_Base_Head")
    b_neck = pose_bones.get("CC_Base_NeckTwist01")
    b_spine2 = pose_bones.get("CC_Base_Spine02")
    b_spine1 = pose_bones.get("CC_Base_Spine01")
    b_hip = pose_bones.get("CC_Base_Hip")
    b_r_arm = pose_bones.get("CC_Base_R_Upperarm")
    b_l_arm = pose_bones.get("CC_Base_L_Upperarm")
    b_r_forearm = pose_bones.get("CC_Base_R_Forearm")
    b_l_forearm = pose_bones.get("CC_Base_L_Forearm")
    
    # Ensure Euler rotation mode for predictable curve interpolation
    key_bones = [b_head, b_neck, b_spine2, b_spine1, b_hip, b_r_arm, b_l_arm, b_r_forearm, b_l_forearm]
    for b in key_bones:
        if b:
            b.rotation_mode = 'XYZ'
            
    # Keyframe Helper
    def insert_key(bone, frame, rot_deg=(0,0,0), pos=(0,0,0)):
        if not bone:
            return
        rad = [math.radians(a) for a in rot_deg]
        bone.rotation_euler = rad
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        if pos != (0,0,0):
            bone.location = pos
            bone.keyframe_insert(data_path="location", frame=frame)

    # Keyframe timeline acting beats:
    # Frame 0: Natural baseline rest pose
    insert_key(b_head, 0, (0, 0, 0))
    insert_key(b_neck, 0, (0, 0, 0))
    insert_key(b_spine2, 0, (0, 0, 0))
    insert_key(b_spine1, 0, (0, 0, 0))
    insert_key(b_hip, 0, (0, 0, 0))

    # Frame 35: Settle weight onto back foot/hip, spine relaxes, head lifts slightly looking forward
    insert_key(b_hip, 35, (0, 2.5, -1.0))
    insert_key(b_spine1, 35, (1.2, 0, 0.8))
    insert_key(b_spine2, 35, (-1.5, 0, 0))
    insert_key(b_neck, 35, (-2.0, 0, 0))
    insert_key(b_head, 35, (-4.0, 0, 0)) # Look up slightly at horizon

    # Frame 85: Slowly scan toward left (looking across river/distant fields)
    insert_key(b_spine1, 85, (0.5, 0, 4.0))
    insert_key(b_spine2, 85, (-0.5, 0, 6.0))
    insert_key(b_neck, 85, (-2.5, -1.0, 8.0))
    insert_key(b_head, 85, (-5.0, -1.5, 14.0)) # Head turned left ~14 deg

    # Frame 130: Deep breath in (chest expands, head gently tilts while panning across horizon to right)
    insert_key(b_spine1, 130, (-1.0, 0, -2.0))
    insert_key(b_spine2, 130, (-3.0, 0, -4.0)) # Chest raised
    insert_key(b_neck, 130, (-1.0, 0.5, -5.0))
    insert_key(b_head, 130, (-3.0, 1.0, -11.0)) # Head turned right ~11 deg

    # Frame 175: Exhale softly, head smoothly realigns toward center-forward
    insert_key(b_spine1, 175, (0.5, 0, 0))
    insert_key(b_spine2, 175, (-0.8, 0, 0))
    insert_key(b_neck, 175, (-1.5, 0, 0))
    insert_key(b_head, 175, (-2.0, 0, 0))

    # Frame 210: Full return to ready stance (ready for gameplay input)
    insert_key(b_hip, 210, (0, 0, 0))
    insert_key(b_spine1, 210, (0, 0, 0))
    insert_key(b_spine2, 210, (0, 0, 0))
    insert_key(b_neck, 210, (0, 0, 0))
    insert_key(b_head, 210, (0, 0, 0))

    # 4. CRITICAL: Smooth all F-Curves with Bezier Tangents & Auto-Clamping
    fcurves = []
    if hasattr(action, 'fcurves'):
        fcurves.extend(action.fcurves)
    elif hasattr(action, 'layers'):
        for layer in action.layers:
            for strip in layer.strips:
                if hasattr(strip, 'channelbags'):
                    for cb in strip.channelbags:
                        fcurves.extend(cb.fcurves)
                        
    for fcurve in fcurves:
        for kp in fcurve.keyframe_points:
            kp.interpolation = 'BEZIER'
            kp.handle_left_type = 'AUTO_CLAMPED'
            kp.handle_right_type = 'AUTO_CLAMPED'
                
    print(f"Action '{action_name}' generated with {len(fcurves)} smooth F-curves!")

    # 5. Export to Unity Animations folder
    out_dir = r"C:\Users\Arkstarz\Documents\GitHub\Hoa-tr-n-t-\Assets\Animations\Hero\Reaction"
    os.makedirs(out_dir, exist_ok=True)
    out_fbx = os.path.join(out_dir, "Hero_Reaction_Cutscene_Observation.fbx")
    
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Select armature and mesh
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    for child in arm_obj.children:
        child.select_set(True)
        
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
        check_existing=False,
        use_selection=True,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=1.0,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        armature_nodetype='NULL'
    )
    
    print(f"Successfully exported smooth cutscene animation to: {out_fbx}")

if __name__ == "__main__":
    create_smooth_cutscene_observation()
