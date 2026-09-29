import numpy as np, wave, os

OUT = "Assets/_Project/Audio"   # run from the project root
os.makedirs(OUT, exist_ok=True)
SR = 44100

def t(seconds):
    return np.linspace(0, seconds, int(SR * seconds), endpoint=False)

def env(n, attack=0.005, decay=None):
    a = int(SR * attack)
    e = np.ones(n)
    e[:a] = np.linspace(0, 1, a)
    if decay is None:
        decay = n - a
    tail = np.exp(-np.linspace(0, 6, n - a))
    e[a:] = tail
    return e

def save(name, signal):
    signal = signal / (np.max(np.abs(signal)) + 1e-9) * 0.85
    data = (signal * 32767).astype(np.int16)
    with wave.open(f"{OUT}/{name}.wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())

rng = np.random.default_rng(7)

# dig: short crunchy noise burst, band-limited, pitch sliding down
n = int(SR * 0.09)
noise = rng.uniform(-1, 1, n)
tt = t(0.09)
tone = np.sin(2 * np.pi * (700 - 500 * tt / 0.09) * tt)
save("dig", (noise * 0.6 + tone * 0.4) * env(n, 0.002))

# land: low thump (sine 90 Hz sliding down) + click
n = int(SR * 0.16)
tt = t(0.16)
thump = np.sin(2 * np.pi * (110 - 60 * tt / 0.16) * tt)
click = rng.uniform(-1, 1, n) * np.exp(-tt * 120)
save("land", (thump * 0.9 + click * 0.3) * env(n, 0.002))

# clear: three rising square-ish notes
parts = []
for f in (523.25, 659.25, 880.0):
    tt = t(0.09)
    s = np.sign(np.sin(2 * np.pi * f * tt)) * 0.5 + np.sin(2 * np.pi * f * tt) * 0.5
    parts.append(s * env(len(tt), 0.003))
save("clear", np.concatenate(parts))

# crush: descending buzz + noise
n = int(SR * 0.35)
tt = t(0.35)
buzz = np.sign(np.sin(2 * np.pi * (260 - 200 * tt / 0.35) * tt))
noise = rng.uniform(-1, 1, n)
save("crush", (buzz * 0.6 + noise * 0.5) * env(n, 0.002))

# capsule: bright two-note bubble
parts = []
for f in (880.0, 1318.5):
    tt = t(0.08)
    parts.append(np.sin(2 * np.pi * f * tt) * env(len(tt), 0.004))
save("capsule", np.concatenate(parts))

# gameover: four descending notes
parts = []
for f in (659.25, 523.25, 392.0, 261.63):
    tt = t(0.18)
    s = np.sin(2 * np.pi * f * tt) * 0.7 + np.sin(2 * np.pi * f * 2 * tt) * 0.3
    parts.append(s * env(len(tt), 0.005))
save("gameover", np.concatenate(parts))
print("audio ok")
