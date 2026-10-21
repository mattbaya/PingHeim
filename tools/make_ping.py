#!/usr/bin/env python3
"""Synthesizes the ping sound (a sonar-style 'ping' with two soft echoes) into package/sounds/ping.wav.

Plain standard library, no downloads: one sine tone with a few harmonics, a fast attack and a long exponential decay,
repeated twice more, quieter, as echoes."""
import math, struct, wave, os

RATE = 44100
LENGTH = 2.6
OUT = os.path.join(os.path.dirname(__file__), "..", "package", "sounds", "ping.wav")


def ping(t):
    """One ping starting at t = 0."""
    if t < 0:
        return 0.0
    attack = min(1.0, t / 0.004)
    env = attack * math.exp(-t * 4.2)
    f = 1318.5  # E6
    s = math.sin(2 * math.pi * f * t) + 0.35 * math.sin(2 * math.pi * f * 2.0 * t) * math.exp(-t * 9) + 0.18 * math.sin(2 * math.pi * f * 3.01 * t) * math.exp(-t * 14)
    return env * s


samples = []
for i in range(int(RATE * LENGTH)):
    t = i / RATE
    v = ping(t) + 0.34 * ping(t - 0.42) + 0.14 * ping(t - 0.86)
    samples.append(v)

peak = max(abs(v) for v in samples)
gain = 0.8 / peak
fade = int(RATE * 0.05)
data = bytearray()
for i, v in enumerate(samples):
    if i > len(samples) - fade:
        v *= (len(samples) - i) / fade
    data += struct.pack("<h", int(max(-1.0, min(1.0, v * gain)) * 32767))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with wave.open(OUT, "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes(bytes(data))
print("wrote", OUT, os.path.getsize(OUT), "bytes")
