"""
Generates individual number + operator voice clips for equation voicing.
Uses Microsoft Edge Neural TTS (edge-tts) for natural-sounding voices.

Usage:
  pip install edge-tts
  python gen_equation_voice.py            # skip existing files
  python gen_equation_voice.py --force    # overwrite all files

Outputs MP3s to:
  Assets/Resources/Audio/Voice/cs-CZ/  (eq_prompt, num_0..55, op_*)
  Assets/Resources/Audio/Voice/en-US/  (same structure)

The AudioManager.PlayEquationVoice() chains these clips at runtime to say
e.g. "kolik je dva plus dva" / "what is two plus two".
"""

import asyncio
import os
import sys

try:
    import edge_tts
except ImportError:
    print("ERROR: edge-tts is not installed. Run:  pip install edge-tts")
    raise

VOICE_ROOT = "Assets/Resources/Audio/Voice"
FORCE      = "--force" in sys.argv

# Neural voices — natural, child-friendly
CZ_VOICE = "cs-CZ-VlastaNeural"   # female Czech
EN_VOICE = "en-US-JennyNeural"    # female English (warm, clear)

# ── Czech ────────────────────────────────────────────────────────────────────

CZ_NUMBERS = {
    0:  "nula",
    1:  "jedna",
    2:  "dva",
    3:  "tři",
    4:  "čtyři",
    5:  "pět",
    6:  "šest",
    7:  "sedm",
    8:  "osm",
    9:  "devět",
    10: "deset",
    11: "jedenáct",
    12: "dvanáct",
    13: "třináct",
    14: "čtrnáct",
    15: "patnáct",
    16: "šestnáct",
    17: "sedmnáct",
    18: "osmnáct",
    19: "devatenáct",
    20: "dvacet",
    21: "dvacet jedna",
    22: "dvacet dva",
    23: "dvacet tři",
    24: "dvacet čtyři",
    25: "dvacet pět",
    26: "dvacet šest",
    27: "dvacet sedm",
    28: "dvacet osm",
    29: "dvacet devět",
    30: "třicet",
    31: "třicet jedna",
    32: "třicet dva",
    33: "třicet tři",
    34: "třicet čtyři",
    35: "třicet pět",
    36: "třicet šest",
    37: "třicet sedm",
    38: "třicet osm",
    39: "třicet devět",
    40: "čtyřicet",
    41: "čtyřicet jedna",
    42: "čtyřicet dva",
    43: "čtyřicet tři",
    44: "čtyřicet čtyři",
    45: "čtyřicet pět",
    46: "čtyřicet šest",
    47: "čtyřicet sedm",
    48: "čtyřicet osm",
    49: "čtyřicet devět",
    50: "padesát",
    51: "padesát jedna",
    52: "padesát dva",
    53: "padesát tři",
    54: "padesát čtyři",
    55: "padesát pět",
}

CZ_OPS = {
    "op_plus":   "plus",
    "op_minus":  "mínus",
    "op_times":  "krát",
    "op_divide": "děleno",
}

CZ_PROMPT = "Kolik je"

# ── English ──────────────────────────────────────────────────────────────────

EN_NUMBERS = {
    0:  "zero",
    1:  "one",
    2:  "two",
    3:  "three",
    4:  "four",
    5:  "five",
    6:  "six",
    7:  "seven",
    8:  "eight",
    9:  "nine",
    10: "ten",
    11: "eleven",
    12: "twelve",
    13: "thirteen",
    14: "fourteen",
    15: "fifteen",
    16: "sixteen",
    17: "seventeen",
    18: "eighteen",
    19: "nineteen",
    20: "twenty",
    21: "twenty-one",
    22: "twenty-two",
    23: "twenty-three",
    24: "twenty-four",
    25: "twenty-five",
    26: "twenty-six",
    27: "twenty-seven",
    28: "twenty-eight",
    29: "twenty-nine",
    30: "thirty",
    31: "thirty-one",
    32: "thirty-two",
    33: "thirty-three",
    34: "thirty-four",
    35: "thirty-five",
    36: "thirty-six",
    37: "thirty-seven",
    38: "thirty-eight",
    39: "thirty-nine",
    40: "forty",
    41: "forty-one",
    42: "forty-two",
    43: "forty-three",
    44: "forty-four",
    45: "forty-five",
    46: "forty-six",
    47: "forty-seven",
    48: "forty-eight",
    49: "forty-nine",
    50: "fifty",
    51: "fifty-one",
    52: "fifty-two",
    53: "fifty-three",
    54: "fifty-four",
    55: "fifty-five",
}

EN_OPS = {
    "op_plus":   "plus",
    "op_minus":  "minus",
    "op_times":  "times",
    "op_divide": "divided by",
}

EN_PROMPT = "What is"

# ── Helpers ──────────────────────────────────────────────────────────────────

async def gen(text, voice, out_path):
    if not FORCE and os.path.exists(out_path):
        print(f"  skip (exists) {out_path}")
        return
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    communicate = edge_tts.Communicate(text, voice)
    await communicate.save(out_path)
    print(f"  wrote {out_path}")

async def main():
    tasks = []

    print("Queuing Czech equation voice clips...")
    tasks.append(gen(CZ_PROMPT, CZ_VOICE, f"{VOICE_ROOT}/cs-CZ/eq_prompt.mp3"))
    for n, word in CZ_NUMBERS.items():
        tasks.append(gen(word, CZ_VOICE, f"{VOICE_ROOT}/cs-CZ/num_{n}.mp3"))
    for key, word in CZ_OPS.items():
        tasks.append(gen(word, CZ_VOICE, f"{VOICE_ROOT}/cs-CZ/{key}.mp3"))

    print("Queuing English equation voice clips...")
    tasks.append(gen(EN_PROMPT, EN_VOICE, f"{VOICE_ROOT}/en-US/eq_prompt.mp3"))
    for n, word in EN_NUMBERS.items():
        tasks.append(gen(word, EN_VOICE, f"{VOICE_ROOT}/en-US/num_{n}.mp3"))
    for key, word in EN_OPS.items():
        tasks.append(gen(word, EN_VOICE, f"{VOICE_ROOT}/en-US/{key}.mp3"))

    print(f"\nGenerating {len(tasks)} clips (parallel)...")
    await asyncio.gather(*tasks)
    print("\nDone! Refresh Unity (Assets > Refresh) to import the new clips.")
    print("Total clips: 2 x (56 numbers + 4 operators + 1 prompt) = 122 MP3s")

# ── Entry point ───────────────────────────────────────────────────────────────

if __name__ == "__main__":
    asyncio.run(main())
