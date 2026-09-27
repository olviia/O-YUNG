"""Scene helpers. Each prop recipe owns one collection and rebuilds it from scratch."""
import bpy


def fresh_collection(name):
    """Return an empty collection called `name`, deleting whatever a previous run left in it."""
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(col)
    for obj in list(col.objects):
        data = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        if data is not None and data.users == 0:
            if isinstance(data, bpy.types.Mesh):
                bpy.data.meshes.remove(data)
            elif isinstance(data, bpy.types.Curve):
                bpy.data.curves.remove(data)
    return col


def link(obj, col):
    """Put `obj` into `col` only."""
    for c in obj.users_collection:
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj
