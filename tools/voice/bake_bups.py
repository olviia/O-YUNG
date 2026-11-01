"""Bake the parents' bup voice into short clips for Unity.

One clip per syllable x variant, already tuned to BASE_HZ, shaped by the
syllable's vowel, tinted by an animal hue, warmed (EQ, even harmonics,
chest). Depth reverb and muffle are left to the runtime mixer.

Sources (not in git) live in D:/Projects/O-YUNG-AudioWork/Voice:
  Blups/BankOwn  - single bups cut from Olviia's recording
  Grains/Raw     - recorded syllables (vowel shapes)
  Animals, Carriers - hue sources
Run: python tools/voice/bake_bups.py
"""
import glob, os, sys, numpy as np, soundfile as sf
sys.path.insert(0, os.path.dirname(__file__))
import voice_lab as vl

SR = vl.SR
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(REPO, "Assets", "Content", "Audio", "Voices", "Bups")
ONSETS, VOWELS = "mblnw", "aouei"
SYLLABLES = [o + v for o in ONSETS for v in VOWELS]   # 25, as in C#
VARIANTS = 3
BASE_HZ = 180.0
VOWEL_AMT, HUE_AMT = 0.5, 0.25
HUES = ["Goose Baby", "ANMLHors", "Agapornis"]       # one per variant


def fnv1a(text):
    """32-bit FNV-1a over UTF-8; must match Speech's C# hash."""
    h = 2166136261
    for b in text.encode("utf-8"):
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return h


def logenv(x, lift=30):
    L = np.log(np.abs(vl.stft(x)).mean(0) + 1e-9)
    c = np.fft.irfft(L); c[lift:-lift] = 0
    e = np.fft.rfft(c).real
    return e - e.mean()


def vowel_envelopes():
    """Average tone curve of each vowel over the recorded syllables."""
    env = {}
    for v in VOWELS:
        curves = []
        for p in glob.glob(f"{vl.VOICE}/Grains/Raw/*_?{v}_n.wav"):
            x = vl.load(p); a = np.abs(x)
            on = np.where(a > a.max() * 0.05)[0]; x = x[on[0]:on[-1]]
            third = len(x) // 3
            curves.append(logenv(x[third:2 * third + third // 2]))
        env[v] = np.mean(curves, 0)
    return env


def bank():
    out = []
    for p in sorted(glob.glob(f"{vl.VOICE}/Blups/BankOwn/*.wav")):
        hz = float(os.path.basename(p).split("_")[1][:-2]); g, _ = sf.read(p)
        if 100 <= hz <= 500 and len(g) > int(.08 * SR):
            out.append((hz, g / np.abs(g).max()))
    return out


def retune(g, ratio):
    p = np.arange(0, len(g) - 1, ratio); k = p.astype(int); f = p - k
    y = g[k] * (1 - f) + g[k + 1] * f
    m = min(int(.01 * SR), len(y) // 4); y[-m:] *= np.linspace(1, 0, m)
    return y


def morph(x, partner, hue):
    p = np.resize(partner, len(x)); X = vl.stft(x); P = vl.stft(p)
    M = np.abs(X) ** (1 - hue) * (np.abs(P) + 1e-9) ** hue
    return vl.istft(M * np.exp(1j * np.angle(X)), len(x))


def shape(x, venv, amt):
    g = np.exp(amt * (venv - logenv(x)))
    return vl.istft(vl.stft(x) * g[None, :], len(x))


def warm(y):
    """Per-clip part of the warm body chain (no compression, no depth)."""
    y = np.concatenate([y, np.zeros(int(.08 * SR))]); n = len(y)
    y = vl.eq(y, lambda f: vl.db(vl.bell(f, 250, 3.5, 1.2)
              + vl.bell(f, 120, 2, 1.5) - np.clip((f - 4000) / 6000, 0, 1) * 6))
    y = np.tanh(1.4 * y / np.abs(y).max() + 0.12) - np.tanh(0.12)
    y = vl.eq(y, lambda f: vl.db(vl.bell(f, 180, 4, 2) + vl.bell(f, 430, 3, 2.5))
              / np.sqrt(1 + (f / 1800) ** 4))
    ch = np.convolve(y, vl.noise_ir(.06, .015, 5, 1200))[:n]
    ch *= np.sqrt((y ** 2).mean() / ((ch ** 2).mean() + 1e-12))
    y = y + 0.5 * ch
    f = int(.004 * SR); y[:f] *= np.linspace(0, 1, f)
    return y - y.mean()


def main():
    os.makedirs(OUT, exist_ok=True)
    B = bank(); venv = vowel_envelopes(); lib = vl.carriers()
    hue_src = {h: next(v for k, v in lib.items() if h in k) for h in HUES}
    clips = {}
    for s in SYLLABLES:
        h = fnv1a(s)
        for v in range(VARIANTS):
            hz, g = B[(h + v) % len(B)]          # same syllable, same family
            y = retune(g, BASE_HZ / hz)
            y = morph(y, vl.loudest(hue_src[HUES[v]], len(y) / SR + .05), HUE_AMT)
            y = warm(shape(y, venv[s[1]], VOWEL_AMT))
            clips[f"{s}_{v}"] = y / vl.rms_loud(y)
    peak = max(np.abs(c).max() for c in clips.values())
    for name, c in clips.items():
        sf.write(os.path.join(OUT, name + ".wav"), 0.89 * c / peak, SR,
                 subtype="PCM_16")
    print(len(clips), "clips ->", OUT)


if __name__ == "__main__":
    main()
