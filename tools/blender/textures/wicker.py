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
    stake_peak=0.92,        # stake top relative to the weaver's highest point (slightly lower)
    stake_height=0.45,      # height map only: constant stake band level (x 0.7), below a front weaver
    shape_dive=0.6,         # height map only: weaver dive; deep enough for straight overlap edges
    height_weaver=0.85,     # height PNG only: weavers scaled down
    height_stake=1.8,       # height PNG only: stakes scaled up (lighter)
    grooves=(10, 28),       # fiber streak frequencies across a strand (normal map only)
    groove_count=7,         # layered -> irregular streaks, not a few clean lines
    groove_depth=0.035,
    variation=0.10,         # per-strand tint variation (handmade feel)
    top_light=0.25,         # painted light from above: upper half of each weaver brighter
    occlusion=0.45,         # darkening where a strand disappears under another
    soften=3,               # blur passes inside each strand (keeps strand borders crisp)
    normal_strength=3.0,
    weaver_tilt=0.75,       # normal: max up/down tilt at a weaver's edge (linear across it)
    streak_band=0.45,       # normal: weaver fiber streaks only where |t| < this (0 center, 1 edge)
    weaver_side=0.25,       # normal: how much of the along-strand (left/right) slope a weaver keeps
    normal_blur=3,          # smooths the height before deriving the normal (no dark creases)
    blend_sharpness=24,     # how softly crossing strands meet in height (higher = tighter)
    seed=7,
    palette=palettes.WICKER,
    out_dir=None,           # None = Assets/Art/Textures (live in Unity); a folder path = preview only
)


def build(p=PARAMS):
    n, NX, NY = p["size"], p["stakes"], p["weavers"]
    u, v = texgen.grid(n)
    x, y = u * NX, v * NY
    cx, cy = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = x - cx, y - cy
    round_ = lambda s: 1 - (1 - s) ** 2                     # rounded bulge, finite slope at edges

    # Over/under along a weaver: +1 where it passes over a stake, -1 where it goes behind.
    # Constant across the weaver's width (user: a bent round strand stays round - no square look).
    lift = np.sin(np.pi * x) * np.where(cy % 2 == 0, 1, -1)
    across_w = np.sin(np.pi * fy)                           # 0 at weaver edges, 1 at its center
    d = (fx - 0.5) / (p["stake_width"] / 2)
    across_s = np.clip(1 - d * d, 0, 1)                     # 0 at stake edges, 1 at its center
    h_weaver = round_(across_w) * (0.7 + p["dive"] * lift)
    # The stake exists only where it is in front (weaver behind it), ending under the weavers
    # above/below; its top sits slightly below the weaver's highest point.
    row_edge = np.clip(across_w / 0.3, 0, 1)
    row_edge = row_edge * row_edge * (3 - 2 * row_edge)
    stake_front = lift < 0
    h_stake = np.where(stake_front,
                       round_(across_s) * row_edge * p["stake_peak"] * (0.7 + p["dive"]), 0.0)
    on_stake = h_stake > h_weaver                           # approved albedo uses this mask - keep it

    # Height/normal-only stake shape (user reference, docs/art/images/Screenshot 2026-09-28 004408):
    # ONE continuous straight band per stake, round across, constant along its whole length.
    # Its level sits between "weaver behind" and "weaver in front", so plain overlap does the
    # weaving: a weaver in front covers the band, a weaver behind stops at the band's sides.
    # Deeper dive for the height/normal weaver so the overlap lines come out straight: a front
    # weaver covers nearly its full width, a back weaver is fully below the band at its sides.
    h_weaver_shape = round_(across_w) * (0.7 + p["shape_dive"] * lift)
    h_stake_shape = across_s * p["stake_height"] * 0.7
    shape = np.maximum(h_weaver_shape, h_stake_shape)       # crisp overlap edges, as in the reference
    # Height map only (normal approved as is): weavers a bit lower, stakes a bit higher.
    # The stake shows only in rows where it is in front; where a weaver is in front it is fully
    # hidden, so it never notches the weaver's edges (user feedback on the height map).
    stake_front_row = (cx + cy) % 2 == 1
    height_map = np.maximum(h_weaver_shape * p["height_weaver"],
                            np.where(stake_front_row, h_stake_shape * p["height_stake"], 0.0))
    height_map /= height_map.max()                          # fit 0..1 (no clipped white plateaus)

    # --- albedo: painted form + occlusion + tint, mapped through a hue-shifting palette ---
    rng = np.random.default_rng(p["seed"])
    row_tint, col_tint = rng.normal(0, 1, NY), rng.normal(0, 1, NX)
    tint = np.where(on_stake, col_tint[cx % NX], row_tint[cy % NY]) * p["variation"]
    # Weaver highlight peaks where it passes over a stake -> a rounded, oval light spot.
    form_w = across_w * (0.55 + 0.45 * np.clip(lift, 0, 1))
    form = np.where(on_stake, across_s, form_w)             # center of strand = lit
    top = np.where(on_stake, 0.0, (fy - 0.5) * 2)           # weavers: +1 top edge, -1 bottom
    # Weaver going behind a stake: darker, most on its round edges. Stake: darker toward its
    # ends, where it goes under the weavers above/below.
    occl_w = np.clip(-lift, 0, 1) * p["occlusion"] * (0.6 + 0.4 * (1 - across_w))
    occl_s = (1 - across_w) * p["occlusion"]
    occl = np.where(on_stake, occl_s, occl_w)
    t = 0.25 + 0.55 * form + p["top_light"] * top * form - occl + tint
    albedo = texgen.ramp(t, p["palette"])
    albedo = np.where(on_stake[..., None],
                      texgen.masked_blur(albedo, on_stake, p["soften"]),
                      texgen.masked_blur(albedo, ~on_stake, p["soften"]))

    # --- normal: weave shape + fine, irregular fiber streaks along each strand ---
    # (user reference: screenshot 2026-09-28 000020, top). Streaks are added AFTER the blur,
    # so they stay fine; the weave shape itself is blurred exactly as before.
    grng = np.random.default_rng(p["seed"] + 1)
    across_coord = np.where(on_stake, fx, fy)
    along_coord = np.where(on_stake, v, u)
    streaks = np.zeros_like(u)
    for k in grng.integers(*p["grooves"], p["groove_count"]):
        drift = 0.4 * np.sin(2 * np.pi * grng.integers(1, 4) * along_coord + grng.uniform(0, 6.3))
        streaks += grng.uniform(0.4, 1.0) * np.sin(2 * np.pi * k * across_coord + drift + grng.uniform(0, 6.3))
    streaks /= np.abs(streaks).max()
    # Weaver as a true cylinder for the normal (user: uniform green->purple, no edge lines).
    # A circle's normal tilts linearly across it; scale so the circle's height in pixels equals
    # its radius after normal_from_height's scaling (32 = 0.5 * 64 in texgen).
    t = 2 * fy - 1
    cyl = np.sqrt(np.clip(1 - t * t, 0, 1))
    cyl_scale = 32 / (NY * 0.7 * p["normal_strength"])
    weaver_n = cyl * (0.7 + p["shape_dive"] * lift) * cyl_scale
    shape_n = np.maximum(weaver_n, h_stake_shape)
    # Weavers: fiber streaks only in a central band, none near the top/bottom edges (user).
    band = np.clip((p["streak_band"] - np.abs(t)) / 0.15, 0, 1)
    band = band * band * (3 - 2 * band)
    streaks = np.where(weaver_n >= h_stake_shape, streaks * band, streaks)
    detail = texgen.blur(shape_n, p["normal_blur"]) + p["groove_depth"] * streaks * shape_n
    normal = texgen.normal_from_height(detail, p["normal_strength"])
    # Weavers: up/down tilt set directly and LINEARLY across the strand (textbook horizontal
    # cylinder: green top edge -> neutral middle -> purple bottom edge, evenly). Left/right
    # component and fiber streaks are kept from the computed normal.
    n = normal * 2 - 1
    streak_n = texgen.normal_from_height(p["groove_depth"] * streaks * shape_n, p["normal_strength"]) * 2 - 1
    ny = np.clip(t * p["weaver_tilt"] + streak_n[..., 1], -0.99, 0.99)
    nx = n[..., 0] * p["weaver_side"]                      # soften the along-strand slope (cheese-wheel look)
    nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0.01, 1))
    lin = np.stack([nx, ny, nz], axis=-1)
    lin /= np.linalg.norm(lin, axis=-1, keepdims=True)
    weaver_px = weaver_n >= h_stake_shape
    normal = np.where(weaver_px[..., None], lin * 0.5 + 0.5, normal)

    out = p["out_dir"]
    return [texgen.save_png(albedo, "T_Wicker_Albedo", out_dir=out),
            texgen.save_png(normal, "T_Wicker_Normal", linear=True, out_dir=out),
            texgen.save_png(np.repeat(height_map[..., None], 3, axis=-1), "T_Wicker_Height", linear=True, out_dir=out)]
