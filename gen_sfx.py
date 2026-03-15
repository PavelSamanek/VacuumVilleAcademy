"""
Thematic SFX generator for VacuumVille Academy minigames.
Uses only Python stdlib: wave, math, struct, os, array
"""
import wave, math, struct, os, array

RATE = 44100

def write_wav(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    clamped = [max(-32767, min(32767, int(s))) for s in samples]
    with wave.open(path, 'w') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(struct.pack(f'<{len(clamped)}h', *clamped))
    print(f"  wrote {path} ({len(samples)/RATE:.2f}s)")

def adsr(t, attack, decay, sustain_level, release, total):
    if t < attack:
        return t / attack
    elif t < attack + decay:
        return 1.0 - (1.0 - sustain_level) * (t - attack) / decay
    elif t < total - release:
        return sustain_level
    else:
        remaining = total - t
        return sustain_level * remaining / release if release > 0 else 0.0

def sine(freq, t):
    return math.sin(2 * math.pi * freq * t)

def sawtooth(freq, t):
    p = (t * freq) % 1.0
    return 2 * p - 1

def square(freq, t):
    return 1.0 if (t * freq) % 1.0 < 0.5 else -1.0

def pnoise(x):
    # Deterministic pseudo-noise via sine hash
    return math.sin(x * 127.1 + 311.7) * math.sin(x * 269.5 + 183.3)

def noise(t, seed=0):
    return pnoise(t * 440.0 + seed * 100.0)

def lerp(a, b, t):
    return a + (b - a) * t

ROOT = "Assets/Resources/Audio/SFX"

# ── shared ──────────────────────────────────────────────────────────────────

def gen_vacuum_start():
    dur = 1.2
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        freq = lerp(80, 220, frac ** 0.5)
        amp = adsr(t, 0.05, 0.1, 0.85, 0.2, dur)
        # motor hum: fundamental + harmonics
        s = (sine(freq, t) * 0.5
           + sine(freq * 2, t) * 0.25
           + sine(freq * 3, t) * 0.12
           + noise(t, 1) * 0.08)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/shared/vacuum_start.wav", samples)

def gen_vacuum_suction():
    dur = 0.6
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.02, 0.05, 0.7, 0.15, dur)
        # upward noise sweep
        s = noise(t * lerp(1.0, 4.0, frac), 7) * 0.6
        s += sine(lerp(120, 400, frac), t) * 0.3
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/shared/vacuum_suction.wav", samples)

# ── SockSortSweep ────────────────────────────────────────────────────────────

def gen_sock_collect():
    dur = 0.35
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.01, 0.05, 0.5, 0.15, dur)
        freq = lerp(300, 900, frac ** 0.6)
        s = sine(freq, t) * 0.4 + noise(t, 3) * lerp(0.6, 0.0, frac)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/socksortsweep/sock_collect.wav", samples)

def gen_sock_wrong():
    dur = 0.45
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.05, 0.4, 0.25, dur)
        freq = lerp(180, 100, frac)
        wobble = math.sin(2 * math.pi * 8 * t)
        s = sine(freq * (1 + 0.03 * wobble), t) * 0.5 + noise(t, 5) * 0.3
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/socksortsweep/sock_wrong.wav", samples)

# ── CrumbCollectCountdown ────────────────────────────────────────────────────

def gen_crumb_collect():
    dur = 0.2
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.03, 0.3, 0.1, dur)
        # crunchy: noise burst + high click
        s = noise(t * 8, 11) * lerp(1.0, 0.0, frac)
        s += sine(2000, t) * lerp(1.0, 0.0, frac * 3) * 0.3
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/crumbcollect/crumb_collect.wav", samples)

def gen_crumb_despawn():
    dur = 0.18
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.02, 0.4, 0.1, dur)
        freq = lerp(600, 2000, frac)
        s = sine(freq, t) * 0.4 + noise(t, 2) * (1 - frac) * 0.3
        samples.append(s * amp * 20000)
    write_wav(f"{ROOT}/crumbcollect/crumb_despawn.wav", samples)

# ── CushionCannonCatch ───────────────────────────────────────────────────────

def gen_cannon_launch():
    dur = 0.3
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.04, 0.5, 0.12, dur)
        freq = lerp(200, 800, frac ** 0.4)
        s = sawtooth(freq, t) * 0.35 + noise(t, 9) * 0.15
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/cushioncannon/cannon_launch.wav", samples)

def gen_cushion_merge():
    dur = 0.3
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.01, 0.05, 0.4, 0.15, dur)
        # fluffy thump: low freq + soft noise
        freq = lerp(120, 60, frac)
        s = sine(freq, t) * 0.5 + noise(t * 0.5, 4) * lerp(0.5, 0.0, frac)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/cushioncannon/cushion_merge.wav", samples)

def gen_cushion_land():
    dur = 0.28
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.04, 0.35, 0.15, dur)
        freq = lerp(150, 80, frac)
        bounce = abs(math.sin(2 * math.pi * 6 * t)) * lerp(0.3, 0.0, frac)
        s = sine(freq, t) * 0.4 + noise(t, 6) * lerp(0.5, 0.0, frac) + bounce
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/cushioncannon/cushion_land.wav", samples)

# ── DrainDefense ─────────────────────────────────────────────────────────────

def gen_duck_quack():
    dur = 0.5
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.02, 0.06, 0.5, 0.2, dur)
        # quack contour: rise then fall
        contour = math.sin(math.pi * frac)
        freq = 200 + contour * 300
        vibrato = math.sin(2 * math.pi * 15 * t) * 0.04
        s = sawtooth(freq * (1 + vibrato), t) * 0.5
        s += sine(freq * 2, t) * 0.2
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/draindefense/duck_quack.wav", samples)

def gen_duck_splash():
    dur = 0.4
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.05, 0.4, 0.2, dur)
        # noise burst then tone
        s = noise(t * 6, 13) * lerp(1.0, 0.2, frac * 2 if frac < 0.5 else 1.0)
        freq = lerp(600, 200, frac)
        s += sine(freq, t) * lerp(0.0, 0.4, frac)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/draindefense/duck_splash.wav", samples)

def gen_duck_drain():
    dur = 0.7
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.02, 0.1, 0.5, 0.25, dur)
        # glug glug: 3 pulses going down
        glug_freq = 6.0
        glug = (math.sin(2 * math.pi * glug_freq * t) + 1) * 0.5
        freq = lerp(300, 80, frac)
        s = sine(freq, t) * 0.4 * glug + noise(t * 2, 8) * 0.2 * glug
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/draindefense/duck_drain.wav", samples)

# ── BoxTowerBuilder ──────────────────────────────────────────────────────────

def gen_box_select():
    dur = 0.15
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.02, 0.4, 0.08, dur)
        # cardboard tap: band-pass noise + mid thump
        s = noise(t * 20, 17) * lerp(1.0, 0.0, frac) * 0.5
        s += sine(400, t) * lerp(1.0, 0.0, frac * 4) * 0.3
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/boxtower/box_select.wav", samples)

def gen_box_fall_land():
    dur = 0.3
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.003, 0.04, 0.3, 0.18, dur)
        freq = lerp(200, 100, frac)
        s = sine(freq, t) * 0.4 + noise(t * 5, 19) * lerp(0.8, 0.0, frac)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/boxtower/box_fall_land.wav", samples)

def gen_box_vacuum():
    dur = 0.5
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.02, 0.05, 0.6, 0.2, dur)
        freq = lerp(100, 350, frac ** 0.5)
        s = sine(freq, t) * 0.4 + noise(t * lerp(1, 3, frac), 23) * 0.4
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/boxtower/box_vacuum.wav", samples)

# ── StreamerUntangleSprint ───────────────────────────────────────────────────

def gen_running():
    dur = 0.6
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        # 3 rhythmic footstep pulses
        pulse = sum(
            math.exp(-((t - 0.1 * k) ** 2) / 0.001) for k in [1, 3, 5]
        )
        s = noise(t * 4, 29) * pulse * 0.7
        s += sine(lerp(60, 80, t / dur), t) * pulse * 0.3
        amp = adsr(t, 0.01, 0.05, 0.6, 0.15, dur)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/streamer/running.wav", samples)

def gen_knot_snap():
    dur = 0.2
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.002, 0.01, 0.3, 0.15, dur)
        # sharp transient + high freq decay
        s = noise(t * 30, 31) * lerp(1.0, 0.0, frac * 3 if frac < 0.33 else 1.0)
        s += sine(3000, t) * lerp(1.0, 0.0, frac * 6 if frac < 0.17 else 1.0) * 0.4
        samples.append(s * amp * 30000)
    write_wav(f"{ROOT}/streamer/knot_snap.wav", samples)

def gen_streamer_free():
    dur = 0.4
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.01, 0.03, 0.5, 0.2, dur)
        freq = lerp(800, 300, frac)
        s = sine(freq, t) * 0.3 + noise(t * lerp(3, 1, frac), 37) * 0.4
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/streamer/streamer_free.wav", samples)

def gen_bump():
    dur = 0.25
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.04, 0.3, 0.15, dur)
        freq = lerp(100, 60, frac)
        s = sine(freq, t) * 0.5 + noise(t * 3, 41) * lerp(0.6, 0.0, frac)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/streamer/bump.wav", samples)

# ── FlowerBedFrenzy ──────────────────────────────────────────────────────────

def gen_flower_drop():
    dur = 0.22
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.03, 0.4, 0.12, dur)
        freq = lerp(500, 200, frac)
        s = sine(freq, t) * 0.4 + noise(t * 2, 43) * lerp(0.4, 0.0, frac)
        samples.append(s * amp * 24000)
    write_wav(f"{ROOT}/flowerbed/flower_drop.wav", samples)

def gen_water_spray():
    dur = 0.5
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.01, 0.05, 0.5, 0.2, dur)
        # hiss + trickle
        s = noise(t * 10, 47) * 0.6
        trickle_freq = 1200 + math.sin(2 * math.pi * 8 * t) * 400
        s += sine(trickle_freq, t) * 0.2
        samples.append(s * amp * 24000)
    write_wav(f"{ROOT}/flowerbed/water_spray.wav", samples)

def gen_wilt():
    dur = 0.6
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.05, 0.1, 0.4, 0.3, dur)
        freq = lerp(440, 220, frac ** 0.5)
        vibrato = math.sin(2 * math.pi * lerp(6, 2, frac) * t) * 0.03
        s = sine(freq * (1 + vibrato), t) * 0.5
        s += sine(freq * 0.5, t) * 0.2
        samples.append(s * amp * 22000)
    write_wav(f"{ROOT}/flowerbed/wilt.wav", samples)

# ── AtticBinBlitz ────────────────────────────────────────────────────────────

def gen_item_swipe():
    dur = 0.25
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.03, 0.4, 0.12, dur)
        freq = lerp(200, 600, frac)
        s = sine(freq, t) * 0.3 + noise(t * lerp(2, 5, frac), 53) * 0.4
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/atticbin/item_swipe.wav", samples)

def gen_bin_land():
    dur = 0.28
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.003, 0.04, 0.35, 0.15, dur)
        freq = lerp(180, 90, frac)
        # hollow thunk: resonance + noise burst
        s = sine(freq, t) * 0.45 + noise(t * 4, 59) * lerp(0.7, 0.0, frac * 3 if frac < 0.33 else 1.0)
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/atticbin/bin_land.wav", samples)

def gen_bin_overflow():
    dur = 0.55
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.05, 0.5, 0.25, dur)
        # initial crash then clatter
        crash = noise(t * 8, 61) * math.exp(-t * 8) * 0.8
        clatter = noise(t * 15, 67) * 0.3 * (0.5 + 0.5 * math.sin(2 * math.pi * 12 * t))
        samples.append((crash + clatter) * amp * 28000)
    write_wav(f"{ROOT}/atticbin/bin_overflow.wav", samples)

# ── SequenceSprinkler ────────────────────────────────────────────────────────

def gen_water_burst():
    dur = 0.35
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.04, 0.55, 0.15, dur)
        # pressurized spray: noise + pitch rise
        s = noise(t * 12, 71) * 0.6
        freq = lerp(400, 1200, frac ** 0.5)
        s += sine(freq, t) * 0.25
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/sprinkler/water_burst.wav", samples)

def gen_wrong_splash():
    dur = 0.35
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.04, 0.4, 0.2, dur)
        s = noise(t * 8, 73) * lerp(1.0, 0.3, frac)
        freq = lerp(800, 200, frac)
        s += sine(freq, t) * 0.2
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/sprinkler/wrong_splash.wav", samples)

# ── GrandHallRestoration ─────────────────────────────────────────────────────

def gen_tile_whoosh():
    dur = 0.3
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.01, 0.04, 0.5, 0.15, dur)
        freq = lerp(1200, 200, frac ** 0.5)
        s = sine(freq, t) * 0.35 + noise(t * lerp(4, 1, frac), 79) * 0.4
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/grandhall/tile_whoosh.wav", samples)

def gen_tile_snap():
    dur = 0.15
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.002, 0.01, 0.3, 0.1, dur)
        s = noise(t * 25, 83) * lerp(1.0, 0.0, frac * 4 if frac < 0.25 else 1.0)
        s += sine(1800, t) * lerp(1.0, 0.0, frac * 8 if frac < 0.12 else 1.0) * 0.5
        samples.append(s * amp * 30000)
    write_wav(f"{ROOT}/grandhall/tile_snap.wav", samples)

def gen_tile_bounce():
    dur = 0.35
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.04, 0.4, 0.2, dur)
        freq_base = 300
        decay = math.exp(-t * 6)
        s = sine(freq_base, t) * decay * 0.5
        s += sine(freq_base * 1.5, t) * decay * 0.25
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/grandhall/tile_bounce.wav", samples)

# ── ScramblerShutdown ────────────────────────────────────────────────────────

def gen_panel_hum():
    dur = 0.6
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        amp = adsr(t, 0.1, 0.05, 0.7, 0.2, dur)
        # 60Hz electrical buzz with harmonics
        s = (sine(60, t) * 0.5
           + sine(120, t) * 0.3
           + sine(180, t) * 0.15
           + sine(240, t) * 0.08
           + noise(t * 0.5, 89) * 0.05)
        samples.append(s * amp * 20000)
    write_wav(f"{ROOT}/scrambler/panel_hum.wav", samples)

def gen_panel_solve():
    dur = 0.4
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.05, 0.5, 0.2, dur)
        freq = lerp(300, 1200, frac ** 0.5)
        s = sawtooth(freq, t) * 0.3 + sine(freq * 1.5, t) * 0.2
        samples.append(s * amp * 28000)
    write_wav(f"{ROOT}/scrambler/panel_solve.wav", samples)

def gen_panel_wrong():
    dur = 0.45
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.005, 0.05, 0.4, 0.25, dur)
        freq = lerp(400, 200, frac)
        # buzz + tritone dissonance
        s = sawtooth(freq, t) * 0.35
        s += sawtooth(freq * 1.414, t) * 0.2  # tritone ratio ~sqrt(2)
        s += noise(t, 97) * 0.1
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/scrambler/panel_wrong.wav", samples)

def gen_shutdown():
    dur = 1.2
    n = int(dur * RATE)
    samples = []
    for i in range(n):
        t = i / RATE
        frac = t / dur
        amp = adsr(t, 0.02, 0.1, 0.7, 0.4, dur)
        # motor decelerating: freq drops, harmonics fade
        freq = lerp(220, 30, frac ** 1.5)
        s = (sine(freq, t) * 0.5
           + sine(freq * 2, t) * 0.25 * (1 - frac)
           + sine(freq * 3, t) * 0.12 * (1 - frac)
           + noise(t, 101) * 0.05 * (1 - frac))
        samples.append(s * amp * 26000)
    write_wav(f"{ROOT}/scrambler/shutdown.wav", samples)

# ── run all ──────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    print("Generating shared SFX...")
    gen_vacuum_start()
    gen_vacuum_suction()

    print("SockSortSweep...")
    gen_sock_collect()
    gen_sock_wrong()

    print("CrumbCollectCountdown...")
    gen_crumb_collect()
    gen_crumb_despawn()

    print("CushionCannonCatch...")
    gen_cannon_launch()
    gen_cushion_merge()
    gen_cushion_land()

    print("DrainDefense...")
    gen_duck_quack()
    gen_duck_splash()
    gen_duck_drain()

    print("BoxTowerBuilder...")
    gen_box_select()
    gen_box_fall_land()
    gen_box_vacuum()

    print("StreamerUntangleSprint...")
    gen_running()
    gen_knot_snap()
    gen_streamer_free()
    gen_bump()

    print("FlowerBedFrenzy...")
    gen_flower_drop()
    gen_water_spray()
    gen_wilt()

    print("AtticBinBlitz...")
    gen_item_swipe()
    gen_bin_land()
    gen_bin_overflow()

    print("SequenceSprinkler...")
    gen_water_burst()
    gen_wrong_splash()

    print("GrandHallRestoration...")
    gen_tile_whoosh()
    gen_tile_snap()
    gen_tile_bounce()

    print("ScramblerShutdown...")
    gen_panel_hum()
    gen_panel_solve()
    gen_panel_wrong()
    gen_shutdown()

    print("\nDone! All SFX generated.")
