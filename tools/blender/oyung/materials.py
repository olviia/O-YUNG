"""Named placeholder materials.

Blender colors are only for previewing. The NAME is the contract: Unity remaps each
material name to the project's real material, so all props share one material set.
"""
import bpy


def placeholder(name, rgb):
    """Get or create a flat material `name` with base color `rgb`."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1.0)
    if mat.node_tree:
        bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bsdf:
            bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    return mat


def assign(obj, mat):
    """Give `obj` exactly one material slot holding `mat`."""
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj
