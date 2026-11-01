"""Reference line planner + preview from the baked clips.

Mirrors Speech's C# LinePlanner (same hash, syllable count, prosody), so
it doubles as the source of expected values for the C# tests.
Run: python tools/voice/preview_line.py "English line" out.wav
"""
import os, re, sys, numpy as np, soundfile as sf
sys.path.insert(0, os.path.dirname(__file__))
from bake_bups import SYLLABLES, VARIANTS, OUT as CLIPS, fnv1a
import voice_lab as vl

FUNCTION_WORDS = set("a an the this that is are was our my your his her him "
                     "we you i it so to of and what will".split())
STRESS, DRIFT = 0.12, 0.015
DUR_STRESSED, DUR_PLAIN, DUR_FINAL = 0.30, 0.20, 0.42
WORD_GAP, PHRASE_GAP = 0.03, 0.35


def count_syllables(w):
    n = len(re.findall(r"[aeiouy]+", w))
    if w.endswith("e") and n > 1:
        n -= 1
    return max(1, n)


def word_syllables(w):
    return [SYLLABLES[fnv1a(f"{w}#{i}") % len(SYLLABLES)]
            for i in range(count_syllables(w))]


def plan(text):
    """-> list of (start, duration, syllable, pitch_from, pitch_to,
    stressed, word_index)"""
    events, t, word_index = [], 0.0, -1
    for phrase, mark in re.findall(r"([^.!?]+)([.!?]?)", text):
        words = re.findall(r"[a-z']+", phrase.lower())
        if not words:
            continue
        mark = mark or "."
        sy = [(wi, s, j == 0 and w not in FUNCTION_WORDS)
              for wi, w in enumerate(words)
              for j, s in enumerate(word_syllables(w))]
        base = 1.08 if mark == "!" else 1.0
        for k, (wi, s, stressed) in enumerate(sy):
            if k > 0 and sy[k - 1][0] != wi:
                t += WORD_GAP
            r = base * (1 - DRIFT * k) + (STRESS if stressed else 0)
            r0 = r1 = r
            dur = DUR_STRESSED if stressed else DUR_PLAIN
            if k == len(sy) - 1:
                dur = DUR_FINAL
                if mark == "?": r0, r1 = r, r * 1.35
                elif mark == "!": r0, r1 = r * 1.12, r * 0.88
                else: r0, r1 = r, r * 0.9
            events.append((round(t, 4), dur, s, round(r0, 4), round(r1, 4),
                           stressed, word_index + 1 + wi))
            t += dur
        word_index += len(words)
        t += PHRASE_GAP
    return events


def render(events):
    clips = {f"{s}_{v}": vl.load(os.path.join(CLIPS, f"{s}_{v}.wav"))
             for s in SYLLABLES for v in range(VARIANTS)}
    end = events[-1][0] + 2.0; out = np.zeros(int(end * vl.SR))
    for n, (t, dur, s, r0, r1, stressed, wi) in enumerate(events):
        g = clips[f"{s}_{n % VARIANTS}"]; amp = 1.0 if stressed else 0.75
        r = np.linspace(r0, r1, len(g)); p = np.cumsum(r); p = p[p < len(g) - 1]
        k = p.astype(int); f = p - k; y = (g[k] * (1 - f) + g[k + 1] * f) * amp
        i = int(t * vl.SR); out[i:i + len(y)] += y
    # stand-in for the runtime mixer reverb
    n = len(out); pd = int(.025 * vl.SR)
    L = np.roll(np.convolve(out, vl.noise_ir(1.2, .28, 7, 2500))[:n], pd)
    R = np.roll(np.convolve(out, vl.noise_ir(1.2, .28, 8, 2500))[:n], pd)
    s = np.sqrt((out ** 2).mean() / ((L ** 2).mean() + 1e-12))
    st = np.stack([out + 0.22 * s * L, out + 0.22 * s * R], 1)
    return 0.75 * st / np.abs(st).max()


if __name__ == "__main__":
    ev = plan(sys.argv[1])
    for e in ev: print(e)
    sf.write(sys.argv[2], render(ev), vl.SR, subtype="PCM_16")
