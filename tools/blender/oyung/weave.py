"""Weave: real basket-weave geometry on the inside of a revolved wall.

Vertical STAKES follow the wall; horizontal WEAVERS pass in front of / behind alternate
stakes, and every other row flips phase - the classic plain weave.
Strands are swept tubes (sweep.py): low side count, outward normals, UVs in meters.
Each strand gets a random U offset so neighbours show different parts of the texture.
"""
import math
import random
import bmesh
import bpy
from .lathe import radius_at
from .sweep import tube


def _new_mesh():
    bm = bmesh.new()
    return bm, bm.loops.layers.uv.new("UVMap")


def _to_object(name, bm):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def inner_weave(name, wall_profile, z_from, z_to, stakes=48, strand_r=0.012,
                stake_r=0.008, samples_per_stake=2, sides=5,
                stake_bottom=None, stake_top=None, end_ease=0.25, seed=1):
    """Weave strands lining the inside of `wall_profile`; weavers fill z_from..z_to.

    wall_profile: (r, z) points of the wall's inner surface, rising in z.
    stakes must be even so the weave pattern closes around the circle.
    stake_bottom / stake_top: z range of the stakes (extend past the weave to bury the ends).
    end_ease: fraction of stake length at each end that eases into the wall.
    """
    stakes += stakes % 2
    amp = (stake_r + strand_r) * 0.9        # how far a weaver swings in/out around a stake
    inset = amp + strand_r + 0.002          # weave centerline distance from wall: never pokes through
    rnd = random.Random(seed)
    bm, uv = _new_mesh()

    # Weavers: horizontal rings, one per row.
    row = 0
    z = z_from + strand_r
    n = stakes * samples_per_stake
    while z <= z_to - strand_r:
        base = radius_at(wall_profile, z) - inset
        phase = math.pi * (row % 2)
        pts = []
        for i in range(n):
            a = 2 * math.pi * i / n
            r = base + amp * math.cos(stakes / 2 * a + phase)
            pts.append((r * math.cos(a), r * math.sin(a), z))
        tube(bm, uv, pts, strand_r, sides, cyclic=True, u_offset=rnd.uniform(0, 10))
        z += 2 * strand_r
        row += 1

    # Stakes: open-ended tubes (both ends buried, caps would be invisible).
    # They follow the wall at `inset`, easing toward the wall over `end_ease` of their length
    # at each end, so the bottom sinks into the base and the top into the rim.
    z0 = z_from if stake_bottom is None else stake_bottom
    z1 = z_to if stake_top is None else stake_top
    steps = 10
    for k in range(stakes):
        a = 2 * math.pi * k / stakes
        pts = []
        for s in range(steps + 1):
            f = s / steps
            zz = z0 + (z1 - z0) * f
            edge = min(f, 1 - f) / end_ease                   # 0 at the ends, 1 in the middle part
            ease = 1 - (1 - min(edge, 1.0)) ** 2
            r = radius_at(wall_profile, zz) - inset * ease - stake_r * 0.3 * (1 - ease)
            pts.append((r * math.cos(a), r * math.sin(a), zz))
        tube(bm, uv, pts, stake_r, sides, u_offset=rnd.uniform(0, 10))

    return _to_object(name, bm)


def braid(name, ring_r, ring_z, strands=3, strand_r=0.02, twists=40, samples_per_twist=16,
          sides=5, seed=2):
    """A ring of `strands` branches twisted around each other (a woven basket rim).

    Each strand spirals around the ring line; `twists` = full turns around the ring.
    """
    spread = strand_r * 1.05                # distance of each strand from the ring line
    samples = max(twists * samples_per_twist, 48)
    rnd = random.Random(seed)
    bm, uv = _new_mesh()
    for s in range(strands):
        offset = 2 * math.pi * s / strands
        pts = []
        for i in range(samples):
            a = 2 * math.pi * i / samples
            t = twists * a + offset
            r = ring_r + spread * math.cos(t)
            pts.append((r * math.cos(a), r * math.sin(a), ring_z + spread * math.sin(t)))
        tube(bm, uv, pts, strand_r, sides, cyclic=True, u_offset=rnd.uniform(0, 10))
    return _to_object(name, bm)
