"""Texture recipe: adapt a DOWNLOADED texture set to the O-YUNG look.

Sources live in art-src/ (gitignored, third-party licenses). Output goes to Assets/Art/Textures.
Steps, each controlled by the source's config below:
    color    - luminance of the source, softened (photo grain removed), mapped onto our palette;
               keeps the source's light/dark variety, but in our color family
    ao       - source AO multiplied softly into color (painted depth where strands dive under)
    height   - source displacement, blurred to round hard edges -> parallax height map
    normal   - rebuilt from the rounded height (+ a little source grain), never copied raw
    scale    - counts strips in the height map, reports Unity tiling so a strip = `row_m` meters

Run: exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read()); run_tex("adapt")
"""
import os
import numpy as np
from oyung import texgen
import palettes

ART_SRC = r"D:/Projects/O-YUNG/art-src"

SOURCES = {
    # Candidate A: Poliigon Rattan Weave (Poliigon license - keep raw files out of git).
    "WickerA": dict(
        folder="Poliigon_RattanWeave_6945/2K",
        albedo="Poliigon_RattanWeave_6945_BaseColor.jpg",
        height="Poliigon_RattanWeave_6945_Displacement.tiff",
        ao="Poliigon_RattanWeave_6945_AmbientOcclusion.jpg",
        normal="Poliigon_RattanWeave_6945_Normal.png",
        soften=6,           # blur passes on color: removes photo wood grain
        round_edges=3,      # blur passes on height before building the normal
        ao_amount=0.3,
        tone=(0.35, 0.95),  # part of the palette the source's dark..light maps onto (contrast)
        grain_in_normal=0.15,
        normal_strength=3.0,
        row_m=0.034,        # one strip = 3.4 cm (crib weave row spacing)
        stretch_y=2.0,      # strips twice as thick as the source proportions
    ),
    # Candidate B: 3dtextures.me Wood Wicker 002 (CC0).
    "WickerB": dict(
        folder="Wood_Wicker_002_SD-20260927T223544Z-1-001/Wood_Wicker_002_SD",
        albedo="Wood_Wicker_002_basecolor.jpg",
        height="Wood_Wicker_002_height.png",
        ao="Wood_Wicker_002_ambientOcclusion.jpg",
        normal="Wood_Wicker_002_normal.jpg",
        soften=4,
        round_edges=6,      # this one has hard strand edges -> round them more
        ao_amount=0.3,
        tone=(0.4, 0.95),
        grain_in_normal=0.1,
        normal_strength=3.0,
        row_m=0.034,
        stretch_y=1.0,
    ),
}

PARAMS = dict(size=1024, palette=palettes.WICKER, only=None,   # only="WickerA" to run one
              mode="recolor")   # "recolor": source untouched, only hue -> palette | "adapt": full processing


def recolor(name, c, size, palette):
    """Source maps as they are; albedo keeps its exact brightness, takes its hue from the palette."""
    path = lambda f: os.path.join(ART_SRC, c["folder"], f)
    src = texgen.load(path(c["albedo"]), size)
    lum = texgen.luminance(src)
    tinted = texgen.ramp(texgen.normalize(lum), palette)
    albedo = np.clip(tinted * (lum / np.maximum(texgen.luminance(tinted), 1e-4))[..., None], 0, 1)
    normal = texgen.load(path(c["normal"]), size)
    height = texgen.load(path(c["height"]), size, gray=True)
    rows = texgen.count_rows(texgen.normalize(height))
    print(f"{name}: {rows} strips per tile -> uniform Unity tiling {1.0 / (rows * c['row_m']):.2f}")
    return [texgen.save_png(albedo, f"T_{name}_Albedo"),
            texgen.save_png(normal, f"T_{name}_Normal", linear=True),
            texgen.save_png(np.repeat(texgen.normalize(height)[..., None], 3, axis=-1), f"T_{name}_Height", linear=True)]


def adapt(name, c, size, palette):
    path = lambda f: os.path.join(ART_SRC, c["folder"], f)
    src = texgen.load(path(c["albedo"]), size)
    height = texgen.normalize(texgen.load(path(c["height"]), size, gray=True))
    ao = texgen.load(path(c["ao"]), size, gray=True)

    lum = texgen.luminance(src)
    lo, hi = c["tone"]
    albedo = texgen.ramp(lo + (hi - lo) * texgen.normalize(texgen.blur(lum, c["soften"])), palette)
    albedo *= (1 - c["ao_amount"] + c["ao_amount"] * ao)[..., None]

    rounded = texgen.blur(height, c["round_edges"])
    grain = texgen.normalize(lum) - texgen.normalize(texgen.blur(lum, 8))
    normal = texgen.normal_from_height(rounded + c["grain_in_normal"] * grain, c["normal_strength"])

    rows = texgen.count_rows(height)
    tiling_y = 1.0 / (rows * c["row_m"])
    tiling = (tiling_y * c["stretch_y"], tiling_y)
    print(f"{name}: {rows} strips per tile -> Unity tiling X={tiling[0]:.2f} Y={tiling[1]:.2f}")

    return [texgen.save_png(albedo, f"T_{name}_Albedo"),
            texgen.save_png(normal, f"T_{name}_Normal", linear=True),
            texgen.save_png(np.repeat(rounded[..., None], 3, axis=-1), f"T_{name}_Height", linear=True)]


def build(p=PARAMS):
    out = []
    for name, c in SOURCES.items():
        if p["only"] in (None, name):
            step = recolor if p["mode"] == "recolor" else adapt
            out += step(name, c, p["size"], p["palette"])
    return out
