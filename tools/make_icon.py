#!/usr/bin/env python3
"""Draws the PingHeim artwork with the standard library only:
   src/Resources/ping_icon.png  the item's inventory icon (transparent background, 128 px)
   package/icon.png             the Thunderstore icon (256 px, dark background)"""
import math, os, struct, zlib

ROOT = os.path.join(os.path.dirname(__file__), "..")


def scene(x, y, background):
    """Colour (r, g, b, a) at x, y in [-1, 1]."""
    layers = []
    if background:
        d = math.hypot(x, y)
        t = min(1.0, d / 1.2)
        layers.append((16 - 8 * t, 34 - 18 * t, 46 - 26 * t, 1.0))

    def ring(radius, width, color, alpha):
        d = abs(math.hypot(x, y) - radius)
        a = max(0.0, 1.0 - d / width)
        layers.append((*color, alpha * a))
        glow = max(0.0, 1.0 - d / (width * 3.2))
        layers.append((*color, alpha * 0.22 * glow * glow))

    ring(0.30, 0.040, (90, 215, 232), 1.0)
    ring(0.56, 0.040, (80, 200, 222), 0.72)
    ring(0.82, 0.040, (70, 185, 212), 0.42)

    def dot(angle, radius, color, size, square=False):
        cx, cy = radius * math.cos(math.radians(angle)), radius * math.sin(math.radians(angle))
        if square:
            d = max(abs(x - cx), abs(y - cy))
        else:
            d = math.hypot(x - cx, y - cy)
        a = max(0.0, min(1.0, (size - d) / 0.02 + 0.5))
        layers.append((*color, a))

    dot(38, 0.56, (232, 64, 60), 0.075)
    dot(158, 0.82, (245, 245, 245), 0.07)
    dot(252, 0.56, (74, 134, 255), 0.075)
    dot(325, 0.82, (150, 150, 150), 0.062, square=True)

    d = math.hypot(x, y)
    glow = max(0.0, 1.0 - d / 0.26)
    layers.append((190, 250, 255, 0.35 * glow * glow))
    layers.append((222, 252, 255, max(0.0, min(1.0, (0.085 - d) / 0.02 + 0.5))))

    # composite bottom to top
    r = g = b = a = 0.0
    for (lr, lg, lb, la) in layers:
        out_a = la + a * (1 - la)
        if out_a <= 0:
            continue
        r = (lr * la + r * a * (1 - la)) / out_a
        g = (lg * la + g * a * (1 - la)) / out_a
        b = (lb * la + b * a * (1 - la)) / out_a
        a = out_a
    return r, g, b, a


def render(size, background, samples=3):
    rows = []
    for py in range(size):
        row = bytearray()
        for px in range(size):
            acc = [0.0, 0.0, 0.0, 0.0]
            for sy in range(samples):
                for sx in range(samples):
                    x = ((px + (sx + 0.5) / samples) / size) * 2 - 1
                    y = 1 - ((py + (sy + 0.5) / samples) / size) * 2
                    r, g, b, a = scene(x, y, background)
                    acc[0] += r * a; acc[1] += g * a; acc[2] += b * a; acc[3] += a
            n = samples * samples
            a = acc[3] / n
            if a > 0:
                row += bytes([min(255, int(acc[0] / acc[3])), min(255, int(acc[1] / acc[3])), min(255, int(acc[2] / acc[3])), int(a * 255)])
            else:
                row += bytes([0, 0, 0, 0])
        rows.append(bytes(row))
    return rows


def write_png(path, rows, size):
    raw = b"".join(b"\x00" + r for r in rows)

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "wb").write(png)
    print("wrote", path, len(png), "bytes")


write_png(os.path.join(ROOT, "src", "Resources", "ping_icon.png"), render(128, False), 128)
write_png(os.path.join(ROOT, "package", "icon.png"), render(256, True), 256)
