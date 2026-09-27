"""Texture recipe: stylized plain-weave wicker (tileable), hand-painted look.

Outputs Assets/Art/Textures/T_Wicker_Albedo / _Normal / _Height (parallax) .png.
One tile = WEAVERS rows; with row spacing ~3.4 cm (matches crib geometry),
a tile covers ~WEAVERS * 0.034 m -> in Unity set tiling = 1 / that (UVs are in meters).

Structure (real plain weave): weavers run horizontally, stakes vertically. Along a weaver
the stakes alternate front/back; along a stake the weavers alternate. Both follow the same
continuous wave, so a stake visibly dives UNDER the weaver above and below it.

Stylized painting (albedo): per-strand form gradient (light center/top, dark edges),
occlusion where a strand passes under another, hue-shifted shadows, per-strand tint.
Fiber grooves live in the normal map only; the height map carries the weave shape only.

Run: exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read()); run_tex("wicker")
"""
import numpy as np
from oyung import texgen
import palettes

PARAMS = dict(
    size=1024,
    stakes=6,               # vertical ribs per tile (even, so rows alternate cleanly)
    stake_width=0.35,       # rib width as a fraction of the gap between ribs
    weavers=12,             # horizontal strands per tile (even); more than stakes = long lying weave
    dive=0.35,              # how deep a strand dips when it passes behind another
    grooves=3,              # broad fiber grooves along each strand (normal map only)
    groove_depth=0.06,
    variation=0.10,         # per-strand tint variation (handmade feel)
    top_light=0.25,         # painted light from above: upper half of each weaver brighter
    occlusion=0.45,         # darkening where a strand disappears under another
    soften=3,               # blur passes inside each strand (keeps strand borders crisp)
    normal_strength=3.0,
    normal_blur=3,          # smooths the height before deriving the normal (no dark creases)
    blend_sharpness=24,     # how softly crossing strands meet in height (higher = tighter)
    seed=7,
    palette=palettes.WICKER,
)


def build(p=PARAMS):
    n, NX, NY = p["size"], p["stakes"], p["weavers"]
    u, v = texgen.grid(n)
    x, y = u * NX, v * NY
    cx, cy = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = x - cx, y - cy
    round_ = lambda s: 1 - (1 - s) ** 2                     # rounded bulge, finite slope at edges

    # One continuous over/under wave: +1 where the weaver is in front, -1 where the stake is.
    # sin(pi*x) flips sign stake to stake, sin(pi*y) flips row to row - no jumps anywhere.
    wave = np.sin(np.pi * x) * np.sin(np.pi * y)
    across_w = np.sin(np.pi * fy)                           # 0 at weaver edges, 1 at its center
    d = (fx - 0.5) / (p["stake_width"] / 2)
    across_s = np.clip(1 - d * d, 0, 1)                     # 0 at stake edges, 1 at its center
    h_weaver = round_(across_w) * (0.7 + p["dive"] * wave)
    h_stake = round_(across_s) * (0.7 - p["dive"] * wave)
    on_stake = h_stake > h_weaver
    shape = texgen.smooth_max(h_weaver, h_stake, p["blend_sharpness"])

    # --- albedo: painted form + occlusion + tint, mapped through a hue-shifting palette ---
    rng = np.random.default_rng(p["seed"])
    row_tint, col_tint = rng.normal(0, 1, NY), rng.normal(0, 1, NX)
    tint = np.where(on_stake, col_tint[cx % NX], row_tint[cy % NY]) * p["variation"]
    form = np.where(on_stake, across_s, across_w)           # center of strand = lit
    top = np.where(on_stake, 0.0, (fy - 0.5) * 2)           # weavers: +1 top edge, -1 bottom
    dive_here = np.where(on_stake, -wave, wave)             # -1 where this strand goes behind
    occl = np.clip(-dive_here, 0, 1) * p["occlusion"]
    t = 0.25 + 0.55 * form + p["top_light"] * top * form - occl + tint
    albedo = texgen.ramp(t, p["palette"])
    albedo = np.where(on_stake[..., None],
                      texgen.masked_blur(albedo, on_stake, p["soften"]),
                      texgen.masked_blur(albedo, ~on_stake, p["soften"]))

    # --- normal: weave shape + a few broad, soft grooves along each strand ---
    groove = np.cos(2 * np.pi * p["grooves"] * np.where(on_stake, fx, fy))
    detail = shape + p["groove_depth"] * groove * shape
    normal = texgen.normal_from_height(texgen.blur(detail, p["normal_blur"]), p["normal_strength"])

    return [texgen.save_png(albedo, "T_Wicker_Albedo"),
            texgen.save_png(normal, "T_Wicker_Normal", linear=True),
            texgen.save_png(np.repeat(shape[..., None], 3, axis=-1), "T_Wicker_Height", linear=True)]
