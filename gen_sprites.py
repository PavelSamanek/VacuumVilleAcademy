"""
Generate decorative sprites for the Home screen:
  - bg_home.png      : 64x128 vertical gradient (deep blue → teal)
  - star_particle.png: 16x16 soft diamond sparkle
Uses only Python stdlib (zlib, struct).
"""
import zlib, struct, os, math

def png_bytes(width, height, rgba_rows):
    """Encode a list-of-lists of (R,G,B,A) tuples as a PNG byte string."""
    def chunk(tag, data):
        crc = zlib.crc32(tag + data) & 0xffffffff
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', crc)

    ihdr_data = struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)  # 8-bit RGBA
    raw = b''
    for row in rgba_rows:
        raw += b'\x00'
        for r, g, b, a in row:
            raw += bytes([r, g, b, a])

    sig   = b'\x89PNG\r\n\x1a\n'
    ihdr  = chunk(b'IHDR', ihdr_data)
    idat  = chunk(b'IDAT', zlib.compress(raw, 9))
    iend  = chunk(b'IEND', b'')
    return sig + ihdr + idat + iend

def write_png(path, width, height, rgba_rows):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'wb') as f:
        f.write(png_bytes(width, height, rgba_rows))
    print(f"  wrote {path} ({width}x{height})")

def lerp(a, b, t):
    return int(a + (b - a) * t)

def lerp_color(c0, c1, t):
    return tuple(lerp(c0[i], c1[i], t) for i in range(4))

# ── bg_home.png ─────────────────────────────────────────────────────────────
# Vertical gradient: top = deep indigo, mid = royal blue, bottom = dark teal
# 64×128 pixels — Unity stretches it to fill the screen.

TOP    = (13,  35, 110, 255)   # deep indigo-blue #0D236E
MID    = (10,  80, 140, 255)   # ocean blue       #0A508C
BOTTOM = ( 0,  90, 100, 255)   # dark teal        #005A64

W, H = 64, 128

def gradient_color(y):
    t = y / (H - 1)
    if t < 0.5:
        return lerp_color(TOP, MID, t * 2)
    else:
        return lerp_color(MID, BOTTOM, (t - 0.5) * 2)

bg_rows = []
for y in range(H):
    c = gradient_color(y)
    row = []
    for x in range(W):
        # Add very subtle horizontal shimmer via sine wave (+/- 6 on blue channel)
        shimmer = int(math.sin(x / W * math.pi * 2 + y / H * math.pi) * 6)
        r = max(0, min(255, c[0] + shimmer))
        g = max(0, min(255, c[1]))
        b = max(0, min(255, c[2] + shimmer))
        row.append((r, g, b, 255))
    bg_rows.append(row)

write_png("Assets/Resources/Sprites/bg_home.png", W, H, bg_rows)

# ── star_particle.png ────────────────────────────────────────────────────────
# 16×16 soft four-pointed star / diamond shape, white with alpha falloff.

SW = SH = 16
cx = cy = 7.5

star_rows = []
for y in range(SH):
    row = []
    for x in range(SW):
        dx = x - cx
        dy = y - cy
        # Diamond metric: |dx|/rx + |dy|/ry < 1
        rx, ry = 6.5, 6.5
        diamond = abs(dx) / rx + abs(dy) / ry
        # Soft glow: Gaussian-ish based on euclidean distance
        dist = math.sqrt(dx*dx + dy*dy)
        glow = max(0.0, 1.0 - dist / 7.0) ** 2
        # Cross shape: thin bright arms
        arm_x = max(0.0, 1.0 - abs(dy) / 2.0) * max(0.0, 1.0 - abs(dx) / 6.0)
        arm_y = max(0.0, 1.0 - abs(dx) / 2.0) * max(0.0, 1.0 - abs(dy) / 6.0)
        alpha = min(1.0, glow * 0.6 + arm_x * 0.7 + arm_y * 0.7)
        a = int(alpha * 255)
        row.append((255, 255, 220, a))  # warm white
    star_rows.append(row)

write_png("Assets/Resources/Sprites/star_particle.png", SW, SH, star_rows)

# ── dot_decor.png ─────────────────────────────────────────────────────────────
# Small 8×8 soft circle dot — used as subtle background dots.

DW = DH = 8
dot_rows = []
for y in range(DH):
    row = []
    for x in range(DW):
        dx = x - 3.5
        dy = y - 3.5
        dist = math.sqrt(dx*dx + dy*dy)
        alpha = max(0.0, 1.0 - dist / 3.5)
        row.append((180, 220, 255, int(alpha * 255)))
    dot_rows.append(row)

write_png("Assets/Resources/Sprites/dot_decor.png", DW, DH, dot_rows)

print("Done.")
