"""Export to Unity: FBX, Y-up, meters, transforms baked so the prefab root is clean."""
import os
import bpy

UNITY_ART = r"D:/Projects/O-YUNG/Assets/Art"


def fbx(objects, relative_path):
    """Export `objects` to Assets/Art/<relative_path>.fbx and return the full path."""
    path = os.path.join(UNITY_ART, relative_path + ".fbx")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={'MESH', 'EMPTY'},
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )
    return path
