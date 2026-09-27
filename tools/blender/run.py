"""Entry point for running prop recipes inside Blender.

    exec(open(r"D:/Projects/O-YUNG/tools/blender/run.py").read())
    run("crib")                 # build only, to preview
    run("crib", export=True)    # build and export FBX into Assets/Art
"""
import importlib
import sys

TOOLS = r"D:/Projects/O-YUNG/tools/blender"
for sub in (TOOLS, TOOLS + "/props", TOOLS + "/textures"):
    if sub not in sys.path:
        sys.path.insert(0, sub)


def _reload_tools():
    """Reload every already-imported module that lives under tools/blender (toolkit, palettes, ...)."""
    import os
    import oyung
    oyung.reload_all()
    root = os.path.normcase(os.path.abspath(TOOLS))
    for name, mod in list(sys.modules.items()):
        f = getattr(mod, "__file__", None)
        if f and os.path.normcase(os.path.abspath(f)).startswith(root) and not name.startswith("oyung"):
            importlib.reload(mod)


def run_tex(recipe_name):
    """Generate a texture recipe from tools/blender/textures and write PNGs into Assets/Art/Textures."""
    _reload_tools()
    recipe = importlib.reload(importlib.import_module(recipe_name))
    for path in recipe.build():
        print("texture ->", path)


def run(prop, export=False):
    _reload_tools()
    recipe = importlib.reload(importlib.import_module(prop))
    parts = recipe.build()
    tris = sum(sum(len(poly.vertices) - 2 for poly in o.data.polygons) for o in parts)
    print(f"{prop}: {len(parts)} parts, {tris} tris")
    if export:
        print("exported ->", recipe.export_to_unity(parts))
    return parts
