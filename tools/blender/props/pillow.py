"""Recipe: the crib's two pillow shapes - a soft square and a round one. Each exports to its own FBX.

Both are one shape with different settings: a superellipsoid (a rounded box whose corner sharpness is a number).
    outline  - footprint shape: 2 = circle, higher = squarer with round corners
    edge     - side profile: 2 = fully round edge, higher = flatter sides
    sag      - how much thinner the border is than the plump middle
One side (+Z) has a button pulling a dimple in; the other is plain - flip a copy to show either side.
Pillows with bends=True also carry blend shapes (BENDS) - sliders that fold/curve/squash each copy in Unity.
Mesh: a subdivided cube projected onto the shape (even quads, no poles). UVs: box projection in meters.

Run: exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read()); run("pillow", export=True)
"""
import math
import bmesh
import bpy
from oyung import scene, materials, export

SHAPES = {
    "Pillow_Square": dict(size=(0.38, 0.38), thick=0.175, outline=5.0, edge=2.2, sag=0.25,
                          fabric=("M_Fabric_Sage", (0.62, 0.70, 0.58)), bends=True),
    "Pillow_Round":  dict(size=(0.36, 0.36), thick=0.15, outline=2.0, edge=2.8, sag=0.1,
                          fabric=("M_Fabric_Rose", (0.80, 0.62, 0.56))),
}
PARAMS = dict(
    cuts=14,            # cube subdivisions: resolution vs tris
    dimple=0.035,       # how deep the button pulls the top (+Z) side in
    dimple_r=0.05,      # width of that pull
    button_r=0.018,     # button radius
    button_h=0.008,     # button half height
)
# Blend shapes (sliders in Unity) for pillows with bends=True. They add up; all fold toward -Z, the plain side.
BENDS = dict(
    FoldHalf=dict(start=-0.02, length=0.16, angle=100, side=-1),  # half folds over the middle; side -1 = -Y half, away from the corner fold
    FoldCorner=dict(start=0.08, length=0.10, angle=70),  # one corner flops, measured along the diagonal
    Curve=dict(radius=0.30),                             # whole pillow arcs sideways, e.g. to hug the crib wall
    Squash=dict(depth=0.70, radius=0.15),                # middle pressed flat: fraction of thickness, width
)


def build(p=PARAMS):
    col = scene.fresh_collection("Prop_Pillows")
    parts = []
    for i, (name, s) in enumerate(SHAPES.items()):
        o = _pillow(name, s, p)
        o.location.x = 0.5 * i  # side by side for viewing; the mesh itself stays centered on its origin
        materials.assign(o, materials.placeholder(*s["fabric"]))
        if s.get("bends"):
            _add_bends(o, s)
        scene.link(o, col)
        parts.append(o)
    return parts


def _pillow(name, s, p):
    """Pillow body with a button of the same fabric sitting in a dimple on the +Z side."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=p["cuts"], use_grid_fill=True)
    a, b = s["size"][0] / 2, s["size"][1] / 2
    c = s["thick"] / 2
    for v in bm.verts:
        d = v.co.normalized()
        x, y, z = _on_surface(d, s["outline"], s["edge"])
        u = (abs(x) ** s["outline"] + abs(y) ** s["outline"]) ** (1 / s["outline"])  # 0 center .. 1 border
        v.co = (x * a, y * b, z * c * (1 - s["sag"] * u * u))
        if z > 0:
            v.co.z -= p["dimple"] * math.exp(-(v.co.x ** 2 + v.co.y ** 2) / p["dimple_r"] ** 2) * z
    top = c - p["dimple"]
    ret = bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=6, radius=1.0)
    for v in ret["verts"]:
        v.co = (v.co.x * p["button_r"], v.co.y * p["button_r"], top + v.co.z * p["button_h"])
    _box_uvs(bm)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _add_bends(obj, s, bends=BENDS):
    """One shape key per BENDS entry; each is the rest mesh pushed through its deform function."""
    obj.shape_key_add(name="Basis")
    c = s["thick"] / 2
    for name, b in bends.items():
        key = obj.shape_key_add(name=name, from_mix=False)
        key.value = 0.0  # Blender adds keys switched on; a fresh pillow should rest flat
        for i, v in enumerate(obj.data.vertices):
            key.data[i].co = _DEFORMS[name](v.co.copy(), b, c)


def _fold(along, z, start, length, angle):
    """Bend a strip: flat until `start`, then wrap around an arc of `length` toward -Z, then straight again.
    Returns the new (along, z). The arc radius stays larger than the pillow's half thickness so it can't invert."""
    if along <= start:
        return along, z
    a = math.radians(angle)
    r = length / a
    t = min(along - start, length) / length * a
    extra = max(along - start - length, 0.0)
    return (start + (r + z) * math.sin(t) + extra * math.cos(t),
            -r + (r + z) * math.cos(t) - extra * math.sin(t))


def _fold_half(co, b, c):
    y, co.z = _fold(co.y * b["side"], co.z, b["start"], b["length"], b["angle"])
    co.y = y * b["side"]
    return co


def _fold_corner(co, b, c):
    k = math.sqrt(0.5)
    u, w = (co.x + co.y) * k, (co.x - co.y) * k  # u points at the +X+Y corner
    u, co.z = _fold(u, co.z, b["start"], b["length"], b["angle"])
    co.x, co.y = (u + w) * k, (u - w) * k
    return co


def _curve(co, b, c):
    r = b["radius"]
    t = co.x / r
    co.x, co.z = (r + co.z) * math.sin(t), -r + (r + co.z) * math.cos(t)
    return co


def _squash(co, b, c):
    co.z *= 1 - b["depth"] * math.exp(-(co.x ** 2 + co.y ** 2) / b["radius"] ** 2)
    return co


_DEFORMS = dict(FoldHalf=_fold_half, FoldCorner=_fold_corner, Curve=_curve, Squash=_squash)


def _on_surface(d, outline, edge):
    """Scale direction d onto the unit superellipsoid ((|x|^o + |y|^o)^(e/o) + |z|^e = 1) by bisection."""
    def f(t):
        x, y, z = abs(d.x * t), abs(d.y * t), abs(d.z * t)
        return (x ** outline + y ** outline) ** (edge / outline) + z ** edge - 1
    lo, hi = 0.0, 2.0
    for _ in range(30):
        mid = (lo + hi) / 2
        lo, hi = (mid, hi) if f(mid) < 0 else (lo, mid)
    t = (lo + hi) / 2
    return d.x * t, d.y * t, d.z * t


def _box_uvs(bm):
    """UVs in meters by box projection: each face uses the two axes its normal doesn't point along."""
    uv = bm.loops.layers.uv.new("UVMap")
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        a, b = [i for i in range(3) if i != ax]
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co[a], loop.vert.co[b])


def export_to_unity(parts):
    paths = []
    for o in parts:
        loc = o.location.copy()
        o.location = (0, 0, 0)  # each FBX gets its pillow at the origin
        paths.append(export.fbx([o], "Nursery/Models/" + o.name))
        o.location = loc
    return paths
