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
    # One entry per downloaded set. Keys:
    #   folder, albedo, height, ao, normal  - paths inside art-src/
    #   soften, round_edges, ao_amount, tone, grain_in_normal, normal_strength  - processing knobs
    #   row_m (meters per strip), stretch_y  - scale (woven sources)
    #   size_m (meters one tile covers), palette, brightness  - fabrics
    # Caban fleece (Poly Haven, CC0): soft fabric for mattress and pillows.
    # Light neutral cream - each material tints it (_BaseColor), so one texture serves all fabrics.
    "Fleece": dict(
        folder="polyhaven/caban",
        albedo="caban_diff_2k.jpg",
        normal="caban_nor_gl_2k.jpg",
        size_m=0.274,
        palette=palettes.FLEECE,
        brightness=0.85,    # mean luminance of the albedo
    ),
    # Ribbed corduroy (Poly Haven, CC0): second pillow fabric, visible wales. Same cream base, tinted per material.
    "Corduroy": dict(
        folder="polyhaven/ribbed_corduroy",
        albedo="ribbed_corduroy_diff_2k.jpg",
        normal="ribbed_corduroy_nor_gl_2k.jpg",
        size_m=0.266,
        palette=palettes.FLEECE,
        brightness=0.85,
    ),
    # Chunky stockinette knit (ambientCG Fabric016, CC0): the blanket. No published size; ~23 stitch
    # columns per tile, sized so one stitch is ~1.2 cm wide.
    "Knit": dict(
        folder="ambientcg/Fabric016",
        albedo="Fabric016_2K-JPG_Color.jpg",
        normal="Fabric016_2K-JPG_NormalGL.jpg",
        size_m=0.28,
        palette=palettes.FLEECE,
        brightness=0.85,
    ),
}

PARAMS = dict(size=1024, palette=palettes.WICKER, only=None,   # only="<name>" to run one
              mode="recolor")   # "recolor": source untouched, only hue -> palette | "adapt": full processing


def recolor(name, c, size, palette):
    """Source maps as they are; albedo keeps its exact brightness, takes its hue from the palette."""
    path = lambda f: os.path.join(ART_SRC, c["folder"], f)
    src = texgen.load(path(c["albedo"]), size)
    lum = texgen.luminance(src)
    tinted = texgen.ramp(texgen.normalize(lum), palette)
    albedo = tinted * (lum / np.maximum(texgen.luminance(tinted), 1e-4))[..., None]
    if "brightness" in c:   # tintable fabrics: lift to a light base, keep the source's variation
        albedo *= c["brightness"] / texgen.luminance(albedo).mean()
    albedo = np.clip(albedo, 0, 1)
    normal = texgen.load(path(c["normal"]), size)
    out = [texgen.save_png(albedo, f"T_{name}_Albedo"),
           texgen.save_png(normal, f"T_{name}_Normal", linear=True)]
    if "size_m" in c:
        print(f"{name}: one tile = {c['size_m']} m -> Unity tiling {1.0 / c['size_m']:.2f} (lathe UVs are in meters)")
    if "height" in c:
        height = texgen.load(path(c["height"]), size, gray=True)
        rows = texgen.count_rows(texgen.normalize(height))
        print(f"{name}: {rows} strips per tile -> uniform Unity tiling {1.0 / (rows * c['row_m']):.2f}")
        out.append(texgen.save_png(np.repeat(texgen.normalize(height)[..., None], 3, axis=-1),
                                   f"T_{name}_Height", linear=True))
    return out


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
            out += step(name, c, p["size"], c.get("palette", p["palette"]))
    return out
