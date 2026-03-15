"""
Generate 3D number badge sprite, chip particle, and answer-explosion sound.
"""
import wave, struct, math, zlib, os

RATE = 44100

# ── PNG encoder ─────────────────────────────────────────────────────────────

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
    print(f"  wrote {path} ({width}x{height})")

def clamp(v, lo=0, hi=255):
    return max(lo, min(hi, int(v)))

# ── badge_3d.png ─────────────────────────────────────────────────────────────
# 128×128 white-shaded badge. Unity tints it with Image.color.
# Shape: rounded rect (radius 24). Shading simulates a convex 3D surface:
#   - highlight stripe (bright) across top 18 %
#   - central face
#   - bottom shadow

def gen_badge_3d():
    W = H = 128
    R = 24   # corner radius

    def inside(x, y):
        cx = max(R, min(W - 1 - R, x))
        cy = max(R, min(H - 1 - R, y))
        d = math.sqrt((x - cx) ** 2 + (y - cy) ** 2)
        return d, d <= R + 0.5

    rows = []
    for y in range(H):
        row = []
        for x in range(W):
            d, ok = inside(x, y)
            if not ok:
                row.append((0, 0, 0, 0))
                continue

            fy = y / (H - 1)   # 0=top, 1=bottom
            fx = abs(x / W - 0.5) * 2  # 0=center, 1=edge

            # Base brightness (top lighter, bottom darker)
            bright = 1.0 - 0.28 * fy

            # Highlight stripe: top 18%
            if fy < 0.18:
                highlight = (1.0 - fy / 0.18) ** 1.5 * 0.45
                bright += highlight

            # Slight edge dimming
            bright -= 0.08 * fx

            # Soft anti-alias on corners
            alpha = 255
            if d > R - 1.5:
                alpha = clamp((R - d) / 1.5 * 255)

            b = clamp(bright * 255)
            row.append((b, b, b, alpha))
        rows.append(row)

    write_png("Assets/Resources/Sprites/badge_3d.png", W, H, rows)

# ── chip_particle.png ────────────────────────────────────────────────────────
# 16×16 white rounded square — tinted at runtime for colourful confetti.

def gen_chip():
    W = H = 16
    R = 4.0
    rows = []
    for y in range(H):
        row = []
        for x in range(W):
            cx = max(R, min(W - 1 - R, x))
            cy = max(R, min(H - 1 - R, y))
            d = math.sqrt((x - cx) ** 2 + (y - cy) ** 2)
            if d > R + 0.5:
                row.append((0, 0, 0, 0))
            else:
                a = clamp((R + 0.5 - d) / 1.0 * 255)
                row.append((255, 255, 255, a))
        rows.append(row)
    write_png("Assets/Resources/Sprites/chip_particle.png", W, H, rows)

# ── answer_explode.wav ───────────────────────────────────────────────────────
# Deep thump at t=0, sparkle cascade, shimmer tail. ~0.9 s.

def sine(freq, t):
    return math.sin(2 * math.pi * freq * t)

def pnoise(x):
    return math.sin(x * 127.1 + 311.7) * math.sin(x * 269.5 + 183.3)

def adsr(t, a, d, s, r, total):
    if t < a: return t / a
    if t < a + d: return 1.0 - (1.0 - s) * (t - a) / d
    if t < total - r: return s
    rem = total - t
    return s * rem / r if r > 0 else 0.0

def write_wav(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    clamped = [max(-32767, min(32767, int(s))) for s in samples]
    with wave.open(path, 'w') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(struct.pack(f'<{len(clamped)}h', *clamped))
    print(f"  wrote {path} ({len(samples)/RATE:.2f}s)")

def gen_answer_explode():
    dur = 0.95
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE

        # ── Initial thump (0 – 0.12 s) ──
        thump = 0.0
        if t < 0.12:
            env = math.exp(-t * 35) * (1.0 - math.exp(-t * 300))
            thump  = sine(65, t)  * env * 0.70
            thump += sine(110, t) * env * 0.35
            thump += pnoise(t * 6 + 3) * env * 0.25

        # ── Sparkle cascade: 10 tones rising at offset intervals ──
        sparkle = 0.0
        for k in range(10):
            t0 = 0.04 + k * 0.055
            t1 = t0 + 0.18
            if t0 <= t <= t1:
                env = math.exp(-(t - t0) * 22)
                freq = 700 + k * 280
                sparkle += sine(freq, t) * env * 0.13
                sparkle += sine(freq * 1.5, t) * env * 0.05

        # ── Shimmer noise tail (0.08 – end) ──
        shimmer = 0.0
        if t > 0.08:
            sh_env = math.exp(-(t - 0.08) * 6) * 0.28
            shimmer = pnoise(t * 18 + 99) * sh_env

        # ── Global envelope ──
        amp = adsr(t, 0.002, 0.06, 0.35, 0.4, dur)
        s = (thump + sparkle + shimmer) * amp
        samples.append(s * 28000)

    write_wav("Assets/Resources/Audio/SFX/answer_explode.wav", samples)

# ── run ──────────────────────────────────────────────────────────────────────

print("Generating 3D badge assets...")
gen_badge_3d()
gen_chip()
gen_answer_explode()
print("Done.")
