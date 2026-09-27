"""Texture recipe: a single dried willow strand (tileable), for weavers, stakes and the rim.

Outputs Assets/Art/Textures/T_Strand_Albedo / _Normal .png (fine fiber streaks in the normal only).
Image X = along the strand, Y = around it (matches sweep.py UVs, in meters).
One tile covers TILE meters -> in Unity set tiling = 1 / TILE.

Run: exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read()); run_tex("strand")
"""
import numpy as np
from oyung import texgen
import palettes

PARAMS = dict(
    size=512,
    tile=0.25,              # meters covered by one tile (documentation; Unity tiling = 4)
    lines=(30, 90),         # streak frequencies across the tile (fine fiber streaks, like the reference)
    line_count=7,           # layered -> irregular streaks
    line_height=0.5,        # streak depth (normal map only, not in color)
    normal_strength=1.5,
    normal_blur=1,          # light smoothing only - the streaks must survive
    wobble=0.25,            # lines drift slightly instead of being ruler-straight
    nodes=0,                # growth nodes per tile along the strand (0 = none)
    node_amount=0.25,
    tint_amount=0.12,       # slow color drift along / across strands (the shell's variety)
    base=0.58,              # overall brightness position in the palette
    bands=0,                # smooth (banding turned tint drift into stripes)
    soften=8,
    seed=11,
    palette=palettes.WICKER,
)


def build(p=PARAMS):
    n = p["size"]
    u, v = texgen.grid(n)
    rng = np.random.default_rng(p["seed"])
    tau = 2 * np.pi

    # Fiber lines running along the strand: layered sines across V, slightly wobbling along U.
    wob = p["wobble"] * np.sin(tau * (2 * u + rng.random()))
    lines = np.zeros_like(u)
    for k in rng.integers(*p["lines"], p["line_count"]):
        lines += rng.uniform(0.5, 1.0) * np.sin(tau * k * v + rng.uniform(0, tau) + wob)
    lines /= np.abs(lines).max()

    # Growth nodes: thin darker rings across the strand.
    if p["nodes"]:
        d = (u * p["nodes"]) % 1.0 - 0.5
        node = np.exp(-(d / 0.015) ** 2)
    else:
        node = np.zeros_like(u)

    # Slow tint drift (small integer frequencies keep it tileable).
    tint = np.zeros_like(u)
    for i, j in ((1, 0), (2, 0)):                  # along the strand only (no spiral stripes)
        tint += np.sin(tau * (i * u + j * v) + rng.uniform(0, tau))
    tint /= np.abs(tint).max()

    t = p["base"] + p["tint_amount"] * tint - p["node_amount"] * node
    albedo = texgen.blur(texgen.ramp(t, p["palette"], p["bands"]), p["soften"])
    height = 0.5 + 0.5 * lines * p["line_height"] - 0.4 * node
    return [texgen.save_png(albedo, "T_Strand_Albedo"),
            texgen.save_png(texgen.normal_from_height(texgen.blur(height, p["normal_blur"]), p["normal_strength"]),
                            "T_Strand_Normal", linear=True)]
