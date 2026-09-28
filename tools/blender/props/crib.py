"""Recipe: the newborn's wicker crib (prop sheet #1).

Shell  - thin nest-shaped revolved wall; outside gets a wicker TEXTURE in Unity.
Weave  - real strand geometry lining the inside (the player's camera lives in here).
Rim    - braid of a few branches, slightly thicker than the weave strands.
Mattress - only the visible top: flat disc + rounded edge tucked against the wall (no hidden bottom).

Run from Blender (via MCP):  exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read()); run("crib")
Units: meters. Z is up in Blender; export converts to Unity's Y-up.
"""
import math
from oyung import scene, lathe, weave, materials, export

PARAMS = dict(
    # Outer silhouette, bottom center -> rim. (radius, height). Widest mid-height, rim tucks in = nest.
    outer=[(0.0, 0.0), (0.30, 0.0), (0.45, 0.03), (0.55, 0.10), (0.59, 0.19), (0.58, 0.27), (0.54, 0.33)],
    wall=0.012,             # shell thickness
    stakes=36,              # vertical ribs inside (even)
    strand=0.017,           # weaver strand radius
    bend_samples=4,         # points per stake along a weaver; higher = rounder bends, more tris
    weave_from=0.07,        # weave starts above the floor (mattress hides the rest)
    rim_strands=4,          # branches braided into the rim
    rim_strand=0.015,       # rim branch radius
    rim_twists=5,           # full twists around the ring; low = long lying strands
    mattress_top=0.227,     # mattress surface height
    mattress_edge=0.04,     # radius of its rounded edge
    mattress_tuck=0.02,     # how far the edge continues down the wall, hiding the gap
    mattress_gap=0.088,     # clearance to the shell (clears the inner weave)
    segments=48,
)


def build(p=PARAMS):
    col = scene.fresh_collection("Prop_Crib")
    t = p["wall"]
    rim_r, rim_z = p["outer"][-1]

    # Inner surface: outer profile pushed inward by the wall thickness (floor raised by t).
    inner = [(max(r - t, 0.0), z + t if z < 0.05 else z) for r, z in p["outer"]]
    inner[0] = (0.0, t)
    shell = lathe.revolve("Crib_Shell", p["outer"] + list(reversed(inner)), p["segments"])

    ring_r, ring_z = rim_r - t / 2, rim_z + p["rim_strand"]
    rim = weave.braid("Crib_Rim", ring_r, ring_z,
                      strands=p["rim_strands"], strand_r=p["rim_strand"], twists=p["rim_twists"])

    # Stakes run a bit past the weave: bottom sinks into the base wall, top into the rim.
    strands = weave.inner_weave("Crib_Weave", inner[1:], p["weave_from"], rim_z,
                                stakes=p["stakes"], strand_r=p["strand"], samples_per_stake=p["bend_samples"],
                                stake_bottom=p["weave_from"] - 0.02, stake_top=ring_z - p["rim_strand"])

    materials.assign(shell, materials.placeholder("M_WickerShell", (0.55, 0.40, 0.25)))
    materials.assign(rim, materials.placeholder("M_WickerStrand", (0.62, 0.45, 0.28)))
    materials.assign(strands, materials.placeholder("M_WickerStrand", (0.62, 0.45, 0.28)))

    pad = lathe.revolve("Crib_Mattress", _mattress_profile(inner[1:], p), p["segments"])
    materials.assign(pad, materials.placeholder("M_Mattress", (0.86, 0.80, 0.68)))

    parts = [shell, rim, strands, pad]
    for o in parts:
        scene.link(o, col)
    return parts


def _mattress_profile(wall, p):
    """Visible surface only, (r, z): tucked edge at the wall -> rounded edge -> flat top to center."""
    top, e, g = p["mattress_top"], p["mattress_edge"], p["mattress_gap"]
    z_tuck = top - e - p["mattress_tuck"]
    pts = [(lathe.radius_at(wall, z_tuck) - g, z_tuck)]
    side_r = lathe.radius_at(wall, top - e) - g
    pts.append((side_r, top - e))
    cx = side_r - e
    pts += [(cx + e * math.cos(a), top - e + e * math.sin(a))
            for a in (math.pi / 2 * i / 8 for i in range(1, 9))]
    pts += [(cx * 0.5, top), (0.0, top)]
    return pts


def export_to_unity(parts):
    return export.fbx(parts, "Nursery/Models/Crib")
