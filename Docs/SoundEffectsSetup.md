# Sound Effects Setup

## Step 1 — Open jsfxr

Go to **sfxr.me** in your browser.

---

## Step 2 — Generate each sound

For each sound below, click the suggested preset button on the left sidebar, tweak if you like, then export.

| # | Inspector Field | File Name | Preset to click | Tweaks |
|---|---|---|---|---|
| 1 | `correctSound` | `correct.wav` | **Powerup** | Lower pitch slightly, feels like an ascending chime |
| 2 | `wrongSound` | `wrong.wav` | **Hit/Hurt** | Set waveform to **Sine**, lower volume — makes a soft boing |
| 3 | `levelCompleteSound` | `level_complete.wav` | **Powerup** | Increase sustain, raise pitch — feels celebratory |
| 4 | `minigameUnlockSound` | `minigame_unlock.wav` | **Pickup/Coin** | Slightly longer decay |
| 5 | `coinSound` | `coin.wav` | **Pickup/Coin** | Leave as default |
| 6 | `buttonClickSound` | `button_click.wav` | **Blip/Select** | Lower pitch, short attack |

To export each one: click **Export WAV** (top right of the page). Browser will download a `.wav` file — rename it to the file name in the table above.

---

## Step 3 — Place the files

Create this folder structure under the project root if it doesn't exist:

```
Assets/
└── Resources/
    └── Audio/
        └── SFX/
            ├── correct.wav
            ├── wrong.wav
            ├── level_complete.wav
            ├── minigame_unlock.wav
            ├── coin.wav
            └── button_click.wav
```

Full paths on disk:

```
C:\Claude\VacuumVilleAcademy\Assets\Resources\Audio\SFX\correct.wav
C:\Claude\VacuumVilleAcademy\Assets\Resources\Audio\SFX\wrong.wav
C:\Claude\VacuumVilleAcademy\Assets\Resources\Audio\SFX\level_complete.wav
C:\Claude\VacuumVilleAcademy\Assets\Resources\Audio\SFX\minigame_unlock.wav
C:\Claude\VacuumVilleAcademy\Assets\Resources\Audio\SFX\coin.wav
C:\Claude\VacuumVilleAcademy\Assets\Resources\Audio\SFX\button_click.wav
```

---

## Step 4 — Assign in Unity Inspector

1. Open Unity
2. Find the **AudioManager** GameObject in your scene (or prefab)
3. In the Inspector, locate the **Defaults** section
4. Drag each file from the Project window into the matching field:

| Inspector field | File |
|---|---|
| Correct Sound | `correct.wav` |
| Wrong Sound | `wrong.wav` |
| Level Complete Sound | `level_complete.wav` |
| Minigame Unlock Sound | `minigame_unlock.wav` |
| Coin Sound | `coin.wav` |
| Button Click Sound | `button_click.wav` |

---

## Step 5 — Voice lines (optional, later)

When you're ready for localized voice lines, the expected folder structure is:

```
Assets/Resources/Audio/Voice/cs-CZ/   <- Czech
Assets/Resources/Audio/Voice/en-US/   <- English
```

Files are loaded by key at runtime via `AudioManager.PlayVoice("key")`, so file names must match the key passed in code.
