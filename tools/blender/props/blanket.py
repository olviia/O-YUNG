"""Recipe: the knitted baby blanket, folded in layers and hung over the crib rim (prop sheet #1).

Shaped procedurally from the crib's own numbers (no cloth sim - at this scale Blender's solver crumples).
    path    - one cross-section in (radius, height): the folded end rests on the mattress edge, climbs over the
              weave strands and the braid, then hangs straight down outside (gravity) and bends onto the floor
    stack   - the folded blanket is one slab of `layers` layers:
                folded side  - the inside end and one long side (+t) are a rounded fold (half tube)
                open side    - the other long side (-t) shows the layers as grooves
                hem          - the outside ends are staggered: each layer above ends a bit shorter
    plan    - bends along the round rim, so it sits on the braid across its whole width
Every point is placed by P(s, t, d): s = along the path (0 at the rim top, + down the outside),
t = across the width, d = up from the crib's surface through the stack.
VARIANTS are several blankets from the same recipe; one can lie on another (lies_on) - it is lifted by
the other's thickness where they overlap - and slide along the rim (shift).
The mesh is modeled at +X; where it hangs is set in Unity by rotating the prefab around the crib's center.
Materials: M_Fabric_Knit (T_Knit), M_Fabric_Corduroy_Rose (T_Corduroy) - tintable cream textures. UVs in meters, knit columns run down the drape.

Run: exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read()); run("blanket", export=True)
"""
import math
import bmesh
import bpy
from mathutils import Matrix, Vector
from oyung import scene, materials, export, lathe
import crib

PARAMS = dict(
    width=0.25,             # along the rim, meters
    inside=0.17,            # length from the rim top into the crib, to the folded end
    hang=0.50,              # length from the rim top down the outside, for the bottom layer
    layers=3,               # folded layers
    stagger=0.035,          # outside hem: each layer above ends this much shorter
    thickness=0.005,        # one layer
    groove=0.0015,          # open side: how deep the lines between layers cut in
    clearance=0.004,        # gap to the crib and the floor
    weave_r=0.476,          # innermost reach of the crib's weave strands (measured from Crib_Weave)
    floor_bend=0.05,        # height where the hanging part turns onto the floor
    fold=0.005,             # soft vertical folds across the width (depth), hanging part only
    fold_len=0.09,          # distance between those folds
    cells=(110, 10, 8),     # samples: along the length, across the width, around a rounded fold
    fabric=("M_Fabric_Knit", (0.90, 0.86, 0.78)),
    shift=0.0,              # moved along the rim (meters at the braid) - for blankets hung next to others
    lies_on=None,           # name of a blanket in VARIANTS this one rests on where they overlap
    rests_in=None,          # how far past the rim top it rests on that blanket inside the crib (None = all the way)
    ease=0.03,              # how wide the slope is where it drapes off the edge of the blanket below
    stiff=False,            # inside: False = hugs the weave; True = one wide curve from the braid onto the mattress
    land=0.14,              # stiff only: how far in front of the weave it lands on the mattress
    tension=60,             # smoothing passes over the rim: the fabric bridges the dips between braid and weave
    tension_band=0.12,      # ...only this far below the rim top, so floor and mattress bends stay as they are
)
# One FBX each. Values here override PARAMS.
VARIANTS = {
    "Blanket": {},
    "Blanket_Rose": dict(fabric=("M_Fabric_Corduroy_Rose", (0.95, 0.76, 0.70)),
                         inside=0.30, hang=0.40,          # longer in the crib; hem just reaches the floor
                         thickness=0.008,                 # thicker knit -> stiffer: bridges instead of hugging
                         stiff=True, ease=0.08,
                         shift=0.15, lies_on="Blanket", rests_in=0.07),  # half over the cream one
}


def build(p=PARAMS):
    col = scene.fresh_collection("Prop_Blanket")
    parts = []
    for name, over in VARIANTS.items():
        v = dict(p, **over)
        obj = _mesh(name, _path(crib.PARAMS, v), v, dict(p, **VARIANTS[v["lies_on"]]) if v["lies_on"] else None)
        materials.assign(obj, materials.placeholder(*v["fabric"]))
        scene.link(obj, col)
        parts.append(obj)
    return parts


# ---- cross-section -------------------------------------------------------------------------------------

def _path(c, p):
    """The crib-side surface of the blanket as a function s -> ((r, z), normal away from the crib).
    s = 0 at the rim top, negative toward the inside end, positive down the outside."""
    clear = p["clearance"]
    ring_r = c["outer"][-1][0] - c["wall"] / 2
    ring_z = c["outer"][-1][1] + c["rim_strand"]
    over = 2 * c["rim_strand"] + clear            # the braid is ~2 strands thick around the ring
    r_in = ring_r - over                          # braid's inner side
    r_w = p["weave_r"] - clear                    # hang just in front of the weave strands
    z_m = c["mattress_top"] + clear
    bend_in = 0.03

    if p["stiff"]:
        # inside, from the far end: flat on the mattress, then one wide curve up to the braid's inner side,
        # leaving the braid inward-down so it clears the top of the weave
        r_l = r_w - p["land"]
        pts = [(r_l - 0.01 * i, z_m) for i in range(30, 0, -1)]
        b = [(r_l, z_m), (r_l + 0.08, z_m), (r_in - 0.04, ring_z - 0.04), (r_in, ring_z)]
        for i in range(24):
            u = i / 24
            w = ((1 - u) ** 3, 3 * u * (1 - u) ** 2, 3 * u * u * (1 - u), u ** 3)
            pts.append((sum(k * q[0] for k, q in zip(w, b)), sum(k * q[1] for k, q in zip(w, b))))
    else:
        # inside, from the far end: flat on the mattress, turn up, straight up in front of the weave,
        # then an S-curve back out to the braid's inner side
        pts = [(r_w - bend_in - 0.01 * i, z_m) for i in range(20, 0, -1)]
        pts += [(r_w - bend_in * (1 - math.sin(a)), z_m + bend_in * (1 - math.cos(a)))
                for a in (math.pi / 2 * i / 8 for i in range(9))]
        z_s = ring_z - over * 0.2 - 0.03              # where the S-curve toward the braid starts
        pts += [(r_w, z_m + bend_in + (z_s - z_m - bend_in) * i / 8) for i in range(1, 9)]
        for i in range(1, 13):
            e = i / 12
            e = e * e * (3 - 2 * e)
            pts.append((r_w + (r_in - r_w) * e, z_s + (ring_z - z_s) * i / 12))
    # over the braid, inner side -> top -> outer side
    pts += [(ring_r + over * math.cos(a), ring_z + over * math.sin(a))
            for a in (math.pi * (1 - i / 24) for i in range(1, 25))]
    # outside: straight to the shell's widest point, then vertical (gravity) down to the floor bend
    widest_z = max((z for _, z in c["outer"]), key=lambda z: lathe.radius_at(c["outer"], z))
    r_v = lathe.radius_at(c["outer"], widest_z) + clear
    r0, z0 = pts[-1]
    pts += [(r0 + (r_v - r0) * i / 10, z0 + (widest_z - z0) * i / 10) for i in range(1, 11)]
    floor, bend = clear, max(p["floor_bend"], clear + 0.01)
    rad = bend - floor
    pts += [(r_v, widest_z - (widest_z - bend) * i / 20) for i in range(1, 21)]
    pts += [(r_v + rad * (1 - math.cos(a)), bend - rad * math.sin(a))
            for a in (math.pi / 2 * i / 16 for i in range(1, 17))]
    pts += [(r_v + rad + 0.01 * i, floor) for i in range(1, 80)]
    pts = _tension(pts, ring_z - p["tension_band"], p["tension"])

    acc = [0.0]
    for (a, b), (d, e) in zip(pts, pts[1:]):
        acc.append(acc[-1] + math.hypot(d - a, e - b))
    top = max(range(len(pts)), key=lambda i: pts[i][1])
    acc = [s - acc[top] for s in acc]

    def at(s):
        i = next((k for k in range(len(acc) - 1) if acc[k + 1] >= s), len(acc) - 2)
        f = (s - acc[i]) / max(acc[i + 1] - acc[i], 1e-9)
        (a, b), (d, e) = pts[i], pts[i + 1]
        length = math.hypot(d - a, e - b) or 1.0
        n = (-(e - b) / length, (d - a) / length)   # tangent turned 90 deg: away from the crib
        return (a + (d - a) * f, b + (e - b) * f), n
    return at


def _tension(pts, z_min, passes):
    """Round off the path's corners the way stretched fabric does: each point may move toward the average
    of its neighbours, but only away from the crib - so dips get bridged, while the curve over the braid
    (where the crib holds the fabric up) stays put. Only points above z_min take part."""
    pts = [list(q) for q in pts]
    for _ in range(passes):
        for i in range(1, len(pts) - 1):
            (a, b), (x, z), (d, e) = pts[i - 1], pts[i], pts[i + 1]
            if z < z_min:
                continue
            tr, tz = d - a, e - b                      # local direction of travel
            length = math.hypot(tr, tz) or 1.0
            nr, nz = -tz / length, tr / length         # away from the crib (same rule as the path normal)
            mr, mz = (a + d) / 2 - x, (b + e) / 2 - z  # pull toward the neighbours
            push = mr * nr + mz * nz
            if push > 0:                               # moving away from the crib only
                pts[i] = [x + 0.5 * push * nr, z + 0.5 * push * nz]
    return [tuple(q) for q in pts]


# ---- stack ---------------------------------------------------------------------------------------------

def _under(p, base):
    """How far this blanket is lifted by the one it lies on, at (s, t): the base's full thickness where
    they overlap, easing to 0 over `ease` past the base's edges (the fabric slopes down over them)."""
    if base is None:
        return lambda s, t: 0.0
    ease = p["ease"]
    lift = base["layers"] * base["thickness"] + base["fold"] + 0.002
    half = base["width"] / 2 + base["layers"] * base["thickness"] / 2   # + the rounded fold's bulge
    # inside the crib the two paths only coincide near the rim when this one is stiff - rests_in says how far
    s_end = -(p["rests_in"] or base["inside"]) - base["layers"] * base["thickness"] / 2

    def smooth(x):
        x = min(max(x, 0.0), 1.0)
        return x * x * (3 - 2 * x)

    def lifted(s, t):
        g = t + p["shift"] - base["shift"]                               # position across the base
        return lift * smooth((half + ease - abs(g)) / ease) * smooth((s - s_end + ease) / ease)
    return lifted


def _mesh(name, at, p, base=None):
    ring_r = crib.PARAMS["outer"][-1][0] - crib.PARAMS["wall"] / 2
    nl, nw, nr = p["cells"]
    w2, th, L = p["width"] / 2, p["thickness"], p["layers"]
    s_in = -p["inside"]
    ends = [p["hang"] - k * p["stagger"] for k in range(L)]      # outside end of each layer
    k_fold = 2 * math.pi / p["fold_len"]
    under = _under(p, base)

    def P(s, t, d):
        """Point on the blanket: along the path, across the width, up through the stack."""
        (r, z), (nr_, nz) = at(s)
        hanging = min(max(s / 0.1, 0.0), 1.0)
        d += under(s, t)
        d += p["fold"] * hanging * (0.5 + 0.5 * math.sin(k_fold * t + 1.3 * s))
        rr, zz = r + nr_ * d, z + nz * d
        sag = ring_r - math.sqrt(max(ring_r ** 2 - t ** 2, 0.0))   # follow the round rim
        return Vector((rr - sag, t, zz))

    def top(s):
        """Stack thickness at s: all layers, fewer toward the staggered outside hem."""
        return th * sum(1 for e in ends if e >= s - 1e-9)

    # s samples: even along the length, plus every hem so the stair steps land on sample rows
    ss = sorted(set([s_in + (ends[0] - s_in) * i / nl for i in range(nl + 1)] + ends))
    ts = [-w2 + 2 * w2 * j / nw for j in range(nw + 1)]
    arc = [math.pi * i / nr for i in range(nr + 1)]              # 0 = crib side ... pi = top side

    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    cache = {}

    def vert(s, t, d):
        key = (round(s, 6), round(t, 6), round(d, 7))
        if key not in cache:
            cache[key] = bm.verts.new(P(s, t, d))
        return cache[key]

    def face(params, uvs, mid):
        """Quad or tri from (s, t, d) params; oriented away from `mid`, a (s, t, d) inside the stack.
        A None in `mid` means "same as the face's own center" - keeps the test local where the plan bends."""
        vs = [vert(*q) for q in params]
        if len(set(vs)) < 3:
            return
        f = bm.faces.new(list(dict.fromkeys(vs)))
        f.normal_update()
        avg = [sum(q[i] for q in params) / len(params) for i in range(3)]
        inside = P(*[avg[i] if m is None else m for i, m in enumerate(mid)])
        if f.normal.dot(f.calc_center_median() - inside) < 0:
            f.normal_flip()
        for loop in f.loops:
            q = params[vs.index(loop.vert)]
            loop[uv].uv = uvs[params.index(q)]

    rows = list(zip(ss, ss[1:]))
    for s0, s1 in rows:
        sm = (s0 + s1) / 2
        h = top(sm)
        mid = (None, None, h / 2)
        for t0, t1 in zip(ts, ts[1:]):
            # crib-side surface and top surface
            face([(s0, t0, 0), (s0, t1, 0), (s1, t1, 0), (s1, t0, 0)],
                 [(t0, s0), (t1, s0), (t1, s1), (t0, s1)], mid)
            face([(s0, t0, h), (s0, t1, h), (s1, t1, h), (s1, t0, h)],
                 [(t0, s0), (t1, s0), (t1, s1), (t0, s1)], mid)
        # folded long side (+t): half tube around the stack's edge
        for a0, a1 in zip(arc, arc[1:]):
            q = lambda s, a: (s, w2 + h / 2 * math.sin(a), h / 2 - h / 2 * math.cos(a))
            face([q(s0, a0), q(s0, a1), q(s1, a1), q(s1, a0)],
                 [(w2 + a0 * h / 2, s0), (w2 + a1 * h / 2, s0), (w2 + a1 * h / 2, s1), (w2 + a0 * h / 2, s1)],
                 (None, w2 - 0.01, h / 2))
        # open long side (-t): one strip per layer, grooves between them
        prof = []
        for k in range(int(round(h / th))):
            prof += [(0.0, k * th + (0.0005 if k else 0)), (0.0, (k + 1) * th - 0.0005)]
            if (k + 1) * th < h - 1e-9:
                prof.append((p["groove"], (k + 1) * th))
        prof[-1] = (0.0, h)
        prof[0] = (0.0, 0.0)
        for (g0, d0), (g1, d1) in zip(prof, prof[1:]):
            face([(s0, -w2 + g0, d0), (s0, -w2 + g1, d1), (s1, -w2 + g1, d1), (s1, -w2 + g0, d0)],
                 [(-w2 - d0, s0), (-w2 - d1, s0), (-w2 - d1, s1), (-w2 - d0, s1)], (None, -w2 + 0.01, h / 2))

    # outside hems: a riser at each layer's end, closed with a half disc under the folded side
    for k, e in enumerate(ends):
        lo, hi = k * th, (k + 1) * th
        mid = (e - 0.01, None, hi / 2)
        for t0, t1 in zip(ts, ts[1:]):
            face([(e, t0, lo), (e, t1, lo), (e, t1, hi), (e, t0, hi)],
                 [(t0, e), (t1, e), (t1, e + th), (t0, e + th)], mid)
        for a0, a1 in zip(arc, arc[1:]):
            q = lambda a: (e, w2 + hi / 2 * math.sin(a), hi / 2 - hi / 2 * math.cos(a))
            face([(e, w2, hi / 2), q(a0), q(a1)], [(0, 0), (0.005, 0), (0, 0.005)], (e - 0.01, w2 - 0.01, hi / 2))

    # folded inside end: half tube across the width, a quarter sphere at the folded-side corner,
    # and a half disc closing the open side
    H = top(s_in)
    R = H / 2
    for a0, a1 in zip(arc, arc[1:]):
        q = lambda t, a: (s_in - R * math.sin(a), t, R - R * math.cos(a))
        for t0, t1 in zip(ts, ts[1:]):
            face([q(t0, a0), q(t1, a0), q(t1, a1), q(t0, a1)],
                 [(t0, s_in - a0 * R), (t1, s_in - a0 * R), (t1, s_in - a1 * R), (t0, s_in - a1 * R)],
                 (s_in + 0.01, None, R))
        face([(s_in, -w2, R), q(-w2, a0), q(-w2, a1)], [(0, 0), (0.005, 0), (0, 0.005)], (s_in + 0.01, -w2 + 0.01, R))
        for b0, b1 in zip(arc[: nr // 2 + 1], arc[1: nr // 2 + 1]):   # psi: 0 = end tube ... pi/2 = side tube
            c = lambda a, b: (s_in - R * math.sin(a) * math.cos(b), w2 + R * math.sin(a) * math.sin(b),
                              R - R * math.cos(a))
            face([c(a0, b0), c(a0, b1), c(a1, b1), c(a1, b0)],
                 [(w2 + b0 * R, s_in - a0 * R), (w2 + b1 * R, s_in - a0 * R),
                  (w2 + b1 * R, s_in - a1 * R), (w2 + b0 * R, s_in - a1 * R)], (s_in + 0.01, w2 - 0.01, R))

    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-6)
    if p["shift"]:   # slide along the rim: turn around the crib's axis by the matching angle
        bmesh.ops.rotate(bm, verts=bm.verts[:], cent=(0, 0, 0),
                         matrix=Matrix.Rotation(p["shift"] / ring_r, 3, 'Z'))
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def export_to_unity(parts):
    return [export.fbx([o], "Nursery/Models/" + o.name) for o in parts]
