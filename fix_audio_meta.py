"""
Fix Unity audio .meta files:
  compressionFormat: 1 (ADPCM)  -> 0 (PCM, uncompressed - best for short SFX)
  preloadAudioData:  0 (off)    -> 1 (on  - data ready immediately on load)
  3D:                1           -> 0 (force 2D - no spatial rolloff)
"""
import os, re

root = "Assets/Resources/Audio"
fixed = 0

for dirpath, dirnames, filenames in os.walk(root):
    for fname in filenames:
        if not fname.endswith(".meta"):
            continue
        path = os.path.join(dirpath, fname)
        with open(path, "r", encoding="utf-8") as f:
            text = f.read()

        original = text
        text = re.sub(r"compressionFormat:\s*\d+", "compressionFormat: 0", text)
        text = re.sub(r"preloadAudioData:\s*\d+",  "preloadAudioData: 1",  text)
        text = re.sub(r"3D:\s*\d+",                "3D: 0",                text)

        if text != original:
            with open(path, "w", encoding="utf-8") as f:
                f.write(text)
            print(f"  fixed {path}")
            fixed += 1
        else:
            print(f"  skip  {path} (no changes)")

print(f"\nDone — {fixed} file(s) updated.")
