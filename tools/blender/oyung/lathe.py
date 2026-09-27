"""Lathe: spin a 2D profile around the Z axis into a mesh.

A profile is a list of (r, z) points. Points with r == 0 become a single pole vertex,
so a profile may start and/or end on the axis (closed bottom, closed top).
"""
import math
import bmesh
import bpy


def revolve(name, profile, segments=48, closed_loop=False, smooth=True):
    """Build a mesh object by revolving `profile` around Z.

    closed_loop: connect the last profile point back to the first (e.g. a torus cross-section).
    """
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        if r <= 1e-6:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            rings.append([
                bm.verts.new((r * math.cos(a), r * math.sin(a), z))
                for a in (2 * math.pi * i / segments for i in range(segments))
            ])
    # UVs in meters: U = distance around the widest ring, V = distance along the profile.
    # Same texture tiling then gives the same texel size on every lathe prop.
    pts = profile + ([profile[0]] if closed_loop else [])
    v = [0.0]
    for (r0, z0), (r1, z1) in zip(pts, pts[1:]):
        v.append(v[-1] + math.hypot(r1 - r0, z1 - z0))
    u_len = 2 * math.pi * max(r for r, _ in profile)

    idx = list(range(len(rings)))
    pairs = list(zip(idx, idx[1:]))
    if closed_loop:
        pairs.append((len(rings) - 1, 0))
    uv = bm.loops.layers.uv.new("UVMap")
    for n, (ia, ib) in enumerate(pairs):
        a, b = rings[ia], rings[ib]
        va, vb = v[n], v[n + 1]
        for i in range(segments):
            j = (i + 1) % segments
            if len(a) == 1:
                corners = ((a[0], i + 0.5, va), (b[i], i, vb), (b[j], i + 1, vb))
            elif len(b) == 1:
                corners = ((a[i], i, va), (b[0], i + 0.5, vb), (a[j], i + 1, va))
            else:
                corners = ((a[i], i, va), (b[i], i, vb), (b[j], i + 1, vb), (a[j], i + 1, va))
            face = bm.faces.new([c[0] for c in corners])
            for loop, (_, seg, vv) in zip(face.loops, corners):
                loop[uv].uv = (seg / segments * u_len, vv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = smooth
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def circle_profile(center_r, center_z, radius, steps=12):
    """Cross-section of a ring (use with closed_loop=True to get a torus)."""
    return [(center_r + radius * math.cos(2 * math.pi * i / steps),
             center_z + radius * math.sin(2 * math.pi * i / steps)) for i in range(steps)]


def radius_at(profile, z):
    """Linearly interpolate the profile radius at height z (profile must rise in z)."""
    for (r0, z0), (r1, z1) in zip(profile, profile[1:]):
        if z0 <= z <= z1 and z1 > z0:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    return profile[-1][0] if z > profile[-1][1] else profile[0][0]
