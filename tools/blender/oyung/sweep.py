"""Sweep: a low-sided tube along a polyline, written straight into a bmesh.

Replaces Blender curve bevels for strands: we control the side count (poly budget),
normals face outward by construction, and UVs are in meters
(U = distance along the strand, V = distance around it) like lathe.py.
Tube ends are left open - use it where ends are buried or the tube is a closed loop.
"""
import math
from mathutils import Vector

Z = Vector((0.0, 0.0, 1.0))


def tube(bm, uv, points, radius, sides=5, cyclic=False, u_offset=0.0):
    """Append a tube around `points` [(x, y, z), ...] to bmesh `bm` using uv layer `uv`."""
    pts = [Vector(p) for p in points]
    n = len(pts)

    # Distance along the strand at each point (plus the closing segment if cyclic).
    arc = [0.0]
    for a, b in zip(pts, pts[1:] + ([pts[0]] if cyclic else [])):
        arc.append(arc[-1] + (b - a).length)

    rings = []
    for i, p in enumerate(pts):
        prev = pts[i - 1] if (cyclic or i > 0) else p
        nxt = pts[(i + 1) % n] if (cyclic or i < n - 1) else p
        t = (nxt - prev).normalized()
        # Side axis: the radial direction (away from the Z axis) made perpendicular to the tangent.
        side = Vector((p.x, p.y, 0.0))
        side -= t * side.dot(t)
        if side.length < 1e-5:
            side = Z - t * Z.dot(t)
        side.normalize()
        up = t.cross(side)
        rings.append([bm.verts.new(p + radius * (math.cos(a) * side + math.sin(a) * up))
                      for a in (2 * math.pi * s / sides for s in range(sides))])

    around = 2 * math.pi * radius
    for i in range(n if cyclic else n - 1):
        j = (i + 1) % n
        ua, ub = arc[i] + u_offset, arc[i + 1] + u_offset
        for s in range(sides):
            s1 = (s + 1) % sides
            va, vb = around * s / sides, around * (s + 1) / sides
            # Winding (around, then along) makes the face normal point outward.
            face = bm.faces.new((rings[i][s], rings[i][s1], rings[j][s1], rings[j][s]))
            for loop, co in zip(face.loops, ((ua, va), (ua, vb), (ub, vb), (ub, va))):
                loop[uv].uv = co
            face.smooth = True
