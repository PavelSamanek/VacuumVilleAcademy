# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Akademie Robotů / Robot Academy** — An Android educational game for children (ages 6–9) learning basic math. Characters are branded robotic vacuum cleaners (Roomba, Xiaomi, Roborock). Primary language: **Czech**, optional: **English**.

## Unity Setup

- Engine: Unity 2023 LTS, 2D mode, Android Build Support
- Target API: Android 7.0+ (API 24+), portrait only
- Required Unity packages: TextMeshPro, 2D Animation, Addressables (optional), Input System (legacy + new)

## Architecture

### Core Flow
`GameManager` (singleton, DontDestroyOnLoad) owns the state machine. All scene transitions go through `GameManager.TransitionTo(GameState)`. Never call `SceneManager.LoadScene` directly from other scripts.

### Data Layer
- `LevelDefinition` — ScriptableObject per level, stored in `Assets/Resources/Levels/`
- `CharacterDefinition` — ScriptableObject per character, stored in `Assets/Resources/Characters/`
- `PlayerProgress` — serialized to JSON via `SaveSystem` at `Application.persistentDataPath/player_progress.json`
- `LevelRegistry.cs` — static table mirroring all level data (used for tests/tooling without Unity editor)

### Math System
`MathTaskGenerator.Generate(topic, accuracy)` — stateless, generates `MathProblem` objects. Pass `TopicAccuracy` from `PlayerProgress` to get adaptive difficulty. Never generate problems outside this class.

### Localization
`LocalizationManager.Instance.Get(key)` / `.Get(key, args)` / `.GetPlural(key, count)`
JSON string tables live in `Assets/Resources/Localization/{cs-CZ|en-US}/strings.json` and `plurals.json`.
Czech plural rules: 1 = one, 2–4 = few, 5+ = many (all handled automatically).

### Scenes Required
Each scene name must match exactly:
- `Home`, `LevelSelect`, `MathTask`, `LevelIntro`, `CharacterSelect`
- `ParentDashboard`, `Settings`, `Achievements`
- `Minigame_SockSortSweep`, `Minigame_CrumbCollectCountdown`, `Minigame_CushionCannonCatch`
- `Minigame_DrainDefense`, `Minigame_BoxTowerBuilder`, `Minigame_StreamerUntangleSprint`
- `Minigame_FlowerBedFrenzy`, `Minigame_AtticBinBlitz`, `Minigame_SequenceSprinkler`
- `Minigame_GrandHallRestoration`, `Minigame_ScramblerShutdown`

## Namespaces

| Namespace | Contents |
|---|---|
| `VacuumVille.Core` | GameManager, AudioManager, LocalizationManager, SaveSystem |
| `VacuumVille.Data` | All ScriptableObjects, enums, MathProblem, PlayerProgress |
| `VacuumVille.Math` | MathTaskGenerator |
| `VacuumVille.UI` | All screen controllers |
| `VacuumVille.Minigames` | BaseMinigame + all 11 minigame classes |

## Key Conventions

- **No timers on math task screens** — timed pressure during learning is prohibited per design spec. Only minigames use timers.
- **Wrong answer feedback**: orange shake (`#FF9100`), never red. Sound: gentle boing, never a buzzer.
- **Correct feedback**: green burst (`#69F0AE`), ascending chime.
- All `BaseMinigame` subclasses call `CompleteEarly()` or let `FinishMinigame()` trigger via timer. Always call `GameManager.Instance.CompleteMinigame(score)` — never navigate away manually.
- Minimum tap target: 48×48dp. Answer tiles: 80×80dp minimum.
- All UI text must be routed through `LocalizationManager` — no hardcoded strings.

## Localization Keys Pattern

- Level names: `level_{N}_name` (1-indexed)
- Room names: `room_{roomkey}`
- Question prompts: `q_{topic}`
- Character names: `char_{id}_name`, `char_{id}_catchphrase`
- Minigame instructions: `minigame_instruction_{classname_lowercase}`
- Re-teach panels: `reteach_{MathTopic enum value}`
- Shape names: `shape_{shapekey}`

## Adding a New Level

1. Create `LevelDefinition` ScriptableObject in `Assets/Resources/Levels/`
2. Add entry to `LevelRegistry.cs`
3. Add localization keys to both `cs-CZ/strings.json` and `en-US/strings.json`
4. Create minigame scene named `Minigame_{MinigameType}` with a script extending `BaseMinigame`
5. Add `MinigameType` enum value to `MathTopic.cs`
