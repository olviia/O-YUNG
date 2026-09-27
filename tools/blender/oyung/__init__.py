"""O-YUNG Blender toolkit: small reusable building blocks for prop recipes.

Modules:
    scene     - collections, idempotent rebuilds
    lathe     - revolve a 2D profile around Z (bowls, rims, poufs, lamp bases)
    sweep     - low-sided tubes along a polyline, UVs in meters
    weave     - woven strands (stakes + weavers, braids) built from sweeps
    materials - named placeholder materials (real materials live in Unity)
    export    - FBX export with Unity axis/scale settings
    texgen    - numpy texture generation (tileable albedo / normal PNGs)
"""
import importlib


def reload_all():
    """Re-import every loaded oyung.* module so edits on disk apply without restarting Blender.

    Scans sys.modules instead of a hard-coded list, so new modules are picked up too.
    """
    import sys
    for name in sorted(n for n in sys.modules if n.startswith(__name__ + ".")):
        importlib.reload(sys.modules[name])
    from . import scene, lathe, sweep, weave, materials, export, texgen  # noqa: F401 (first load)
