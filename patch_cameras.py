"""
For every scene camera that has an AudioListener component, patch the scene
YAML to disable the AudioListener (set m_Enabled: 0).
AudioManager now carries a permanent DontDestroyOnLoad AudioListener, so
scene cameras don't need one.
"""
import os, re, glob

scenes = glob.glob("Assets/Scenes/*.unity")
patched = 0

for scene_path in scenes:
    with open(scene_path, "r", encoding="utf-8") as f:
        text = f.read()

    # AudioListener component in Unity YAML is type !u!81
    # Pattern: find AudioListener blocks and set m_Enabled: 0
    # We only disable it if it's currently enabled (m_Enabled: 1)
    new_text = re.sub(
        r'(--- !u!81 &\d+\nAudioListener:\n(?:.*\n)*?  m_Enabled:) 1',
        r'\1 0',
        text
    )

    if new_text != text:
        with open(scene_path, "w", encoding="utf-8") as f:
            f.write(new_text)
        print(f"  patched {scene_path}")
        patched += 1
    else:
        print(f"  skip    {scene_path} (no enabled AudioListener found)")

print(f"\nDone — {patched} scene(s) patched.")
