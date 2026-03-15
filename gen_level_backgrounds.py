"""
Generate 11 level-specific gradient background PNGs (8×256 each).
Unity stretches them to fill the screen via Image.type = Simple + preserveAspect=false.
"""
import struct, zlib, os, math

def write_png(path, width, height, rgba_rows):
    def chunk(tag, data):
        crc = zlib.crc32(tag + data) & 0xffffffff
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', crc)
    raw = b''
    for row in rgba_rows:
        raw += b'\x00'
        for r, g, b, a in row:
            raw += bytes([r, g, b, a])
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = (b'\x89PNG\r\n\x1a\n'
            + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9))
            + chunk(b'IEND', b''))
    with open(path, 'wb') as f:
        f.write(data)
    print(f"  wrote {path}")

def lerp(a, b, t):
    return a + (b - a) * t

def clamp(v): return max(0, min(255, int(v)))

def gradient_png(path, top_rgb, bot_rgb, accent_rgb=None, accent_band=(0.3, 0.6)):
    """8×256 gradient with optional accent band highlight in the middle."""
    W, H = 8, 256
    rows = []
    for y in range(H):
        t = y / (H - 1)   # 0=top, 1=bottom
        # Base gradient
        r = lerp(top_rgb[0], bot_rgb[0], t)
        g = lerp(top_rgb[1], bot_rgb[1], t)
        b = lerp(top_rgb[2], bot_rgb[2], t)
        # Optional accent band (brighten midrange)
        if accent_rgb:
            a0, a1 = accent_band
            band_t = max(0.0, 1.0 - abs(t - (a0 + a1) * 0.5) / ((a1 - a0) * 0.5 + 1e-6))
            band_t = band_t ** 2
            r = lerp(r, accent_rgb[0], band_t * 0.25)
            g = lerp(g, accent_rgb[1], band_t * 0.25)
            b = lerp(b, accent_rgb[2], band_t * 0.25)
        row = [(clamp(r), clamp(g), clamp(b), 255)] * W
        rows.append(row)
    write_png(path, W, H, rows)

OUT = "Assets/Resources/Sprites/LevelBG"

# ── 11 themes ────────────────────────────────────────────────────────────────

# 1. Bedroom — Counting 1-10 — cozy starry night: midnight blue → deep indigo
gradient_png(f"{OUT}/bg_bedroom.png",
    top_rgb=(8,  10, 50),
    bot_rgb=(25, 5, 80),
    accent_rgb=(60, 40, 120), accent_band=(0.35, 0.65))

# 2. Kitchen — Counting 1-20 — warm cooking glow: rich amber → deep brown
gradient_png(f"{OUT}/bg_kitchen.png",
    top_rgb=(120, 50, 8),
    bot_rgb=(60,  20, 5),
    accent_rgb=(200, 100, 30), accent_band=(0.2, 0.5))

# 3. Living Room — Addition to 10 — ocean teal: deep teal → cyan-blue
gradient_png(f"{OUT}/bg_livingroom.png",
    top_rgb=(5,   90, 110),
    bot_rgb=(5,   40,  75),
    accent_rgb=(30, 160, 180), accent_band=(0.25, 0.55))

# 4. Bathroom — Subtraction to 10 — deep ocean: cobalt → deep sea
gradient_png(f"{OUT}/bg_bathroom.png",
    top_rgb=(10,  40, 160),
    bot_rgb=(5,   15,  80),
    accent_rgb=(30, 80, 220), accent_band=(0.3, 0.6))

# 5. Garage — Addition to 20 — industrial night: charcoal → near-black
gradient_png(f"{OUT}/bg_garage.png",
    top_rgb=(35, 45, 60),
    bot_rgb=(12, 15, 22),
    accent_rgb=(80, 100, 140), accent_band=(0.4, 0.7))

# 6. Hallway — Subtraction to 20 — twilight: violet → deep magenta
gradient_png(f"{OUT}/bg_hallway.png",
    top_rgb=(80, 20, 110),
    bot_rgb=(35,  8,  55),
    accent_rgb=(140, 40, 180), accent_band=(0.25, 0.55))

# 7. Backyard — Multiplication — forest: deep green → pine
gradient_png(f"{OUT}/bg_backyard.png",
    top_rgb=(15, 90, 40),
    bot_rgb=(5,  40, 18),
    accent_rgb=(30, 150, 70), accent_band=(0.3, 0.6))

# 8. Attic — Division — warm amber: golden → dark brown
gradient_png(f"{OUT}/bg_attic.png",
    top_rgb=(110, 70, 15),
    bot_rgb=(50,  28,  6),
    accent_rgb=(180, 130, 40), accent_band=(0.25, 0.55))

# 9. Rooftop — Number Ordering — open sky: cornflower → azure
gradient_png(f"{OUT}/bg_rooftop.png",
    top_rgb=(40, 100, 200),
    bot_rgb=(15,  50, 120),
    accent_rgb=(80, 150, 240), accent_band=(0.3, 0.6))

# 10. Grand Hall — Shape Counting — royal: deep purple → black-blue
gradient_png(f"{OUT}/bg_grandhall.png",
    top_rgb=(50, 20, 100),
    bot_rgb=(18,  6,  45),
    accent_rgb=(100, 50, 180), accent_band=(0.2, 0.5))

# 11. Secret Lab — Mixed Review — neon abyss: near-black → electric cyan tint
gradient_png(f"{OUT}/bg_secretlab.png",
    top_rgb=(5,  25, 40),
    bot_rgb=(2,  10, 20),
    accent_rgb=(0, 80, 120), accent_band=(0.35, 0.65))

print("Done.")
