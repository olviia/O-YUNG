"""Texture generation with numpy: tileable patterns -> albedo / normal PNGs for Unity.

Everything works on the unit square with wrap-around math (np.roll, periodic functions),
so every texture tiles seamlessly by construction.
Also usable to stylize existing textures (load -> palette map -> posterize -> save).
"""
import os
import time
import numpy as np
import bpy

UNITY_TEX = r"D:/Projects/O-YUNG/Assets/Art/Textures"


def load(path, size, gray=False):
    """Load an image file (png/jpg/tiff) resized to size x size -> float array (n, n, 3) or (n, n).

    Values are as stored in the file (sRGB stays sRGB, data maps stay data).
    """
    img = bpy.data.images.load(path, check_existing=False)
    img.colorspace_settings.name = 'Non-Color'          # read raw values, no conversion
    img.scale(size, size)
    px = np.empty(size * size * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    rgb = px.reshape(size, size, 4)[..., :3]
    return rgb.mean(axis=-1) if gray else rgb


def luminance(rgb):
    return rgb[..., 0] * 0.2126 + rgb[..., 1] * 0.7152 + rgb[..., 2] * 0.0722


def normalize(a, lo_pct=2, hi_pct=98):
    """Stretch `a` so its lo/hi percentiles map to 0/1 (robust to outlier pixels)."""
    lo, hi = np.percentile(a, [lo_pct, hi_pct])
    return np.clip((a - lo) / max(hi - lo, 1e-6), 0, 1)


def count_rows(height):
    """How many horizontal strips a tileable height map has (for matching real-world scale)."""
    profile = height.mean(axis=1)
    profile = profile - profile.mean()
    return int(np.sum((profile[:-1] < 0) & (profile[1:] >= 0)))


def grid(size):
    """(u, v) coordinates in [0, 1), shape (size, size). v runs up the image."""
    t = np.arange(size) / size
    return np.meshgrid(t, t)


def normal_from_height(h, strength=4.0):
    """Tangent-space normal map (OpenGL / Unity convention) from a tileable height field."""
    n = h.shape[0]
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5 * n / 64 * strength
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5 * n / 64 * strength
    nrm = np.stack([-dx, -dy, np.ones_like(h)], axis=-1)
    nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
    return nrm * 0.5 + 0.5


def ramp(t, stops, bands=0):
    """Map t in [0,1] through color `stops` [(pos, (r,g,b)), ...].

    bands > 0 quantizes t first: flat painterly steps instead of a smooth gradient.
    """
    t = np.clip(t, 0, 1)
    if bands > 0:
        t = np.floor(t * bands) / max(bands - 1, 1)
        t = np.clip(t, 0, 1)
    pos = [p for p, _ in stops]
    return np.stack([np.interp(t, pos, [c[i] for _, c in stops]) for i in range(3)], axis=-1)


def blur(img, passes=1):
    """Cheap wrap-around box blur (softens hard steps, keeps tiling)."""
    for _ in range(passes):
        img = (img + np.roll(img, 1, 0) + np.roll(img, -1, 0) + np.roll(img, 1, 1) + np.roll(img, -1, 1)) / 5
    return img


def masked_blur(img, mask, passes=1):
    """Blur `img` only inside `mask` (colors never bleed across the mask border)."""
    m = mask.astype(float)[..., None] if img.ndim == 3 else mask.astype(float)
    return blur(img * m, passes) / np.maximum(blur(m, passes), 1e-6)


def smooth_max(a, b, k=24.0):
    """Max of two height fields with a rounded fillet instead of a crease."""
    return np.logaddexp(k * a, k * b) / k


def save_png(rgb, name, linear=False):
    """Write an (n, n, 3) array to Assets/Art/Textures/<name>.png. linear=True for normal maps."""
    n = rgb.shape[0]
    os.makedirs(UNITY_TEX, exist_ok=True)
    path = os.path.join(UNITY_TEX, name + ".png")
    img = bpy.data.images.get(name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(name, n, n, alpha=False, float_buffer=False)
    img.colorspace_settings.name = 'Non-Color' if linear else 'sRGB'
    rgba = np.concatenate([rgb, np.ones((n, n, 1))], axis=-1).astype(np.float32)
    img.pixels.foreach_set(rgba.ravel())
    # Write next to the target, then swap in: Unity may be reading the old file mid-reimport.
    tmp = path + ".tmp"                       # Unity ignores *.tmp files
    img.filepath_raw = tmp
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)
    for attempt in range(20):
        try:
            os.replace(tmp, path)
            break
        except PermissionError:
            time.sleep(0.25)
    else:
        raise RuntimeError(f"{path} stayed locked (Unity importing?) - run again")
    return path
