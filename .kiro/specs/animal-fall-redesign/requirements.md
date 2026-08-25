# Requirements Document

## Introduction

A complete ground-up redesign of the Animal Fall mobile game as a polished 50-level tap-to-save experience. Animals fall from the top of the screen and the player must tap them before they reach the bottom. Each level has a countdown timer and a species-based rescue target. As levels progress, hindrances drawn from the Animal Blast hindrance library are layered in. All visual assets (animal sprites, hindrance icons, UI panels, VFX prefabs) are sourced exclusively from the Animal Blast project at `c:\AnnusMirabilis\ZEMOLABS\NEMESIS\animal_blast\AnimalBlast`.

### Issues Found in Current Animal Fall Game

**Architecture & Code Issues:**
1. **No object pooling** — `Spawner.SpawnOne()` calls `Instantiate` every spawn; `Animal.OnCollected()` and `Animal.Update()` call `Destroy(gameObject)` — continuous GC allocation.
2. **No hindrance implementation** — `HindranceBase.Deactivate()` calls `Destroy(gameObject)`. All 30 hindrance classes are stubs with no real behaviour.
3. **`HindranceFactory.CreateAtRandomScreenTop()`** is referenced but never defined — factory implementation is missing.
4. **LINQ in hot path** — `ChooseAnimalData()` calls `System.Array.Find`, `System.Array.FindAll` on every spawn interval.
5. **`AnimalMovement.RecalcBounds()`** is called every `Update()` frame — should be cached.
6. **`Animal.Update()`** checks lifetime every frame — should be a coroutine.
7. **Goal system broken** — `Goal` is a plain class, not a ScriptableObject; cannot be designer-configured per level.
8. **Only 6 levels exist** — `Generate50Levels()` creates in-memory objects not saved as assets.
9. **Only 6 animal species** — missing Pig, Rabbit, Penguin, Owl, Mouse, Zebra, Duck.
10. **Scene race condition** — `LevelManager` uses `DontDestroyOnLoad`; `GameManager` does not.
11. **Star calculation broken** — hardcoded thresholds (250, 500) unrelated to level difficulty.
12. **Floating text spawns at `Vector3.zero`** — should use the tapped animal's world position.
13. **No DOTween** — raw coroutines only; animation rules violated.
14. **`ComboManager` and `PowerUpManager`** are empty stubs.
15. **Single canvas** — violates StaticCanvas + DynamicCanvas separation rule.
16. **No sprite atlas** — sprites loaded individually causing multiple draw calls.

**Design & Flow Issues:**
1. No proper difficulty curve — flat linear scaling with no chapter theming.
2. No level intro screen — objectives not shown before gameplay.
3. No meaningful star system connected to levels.
4. No hindrance sprite assets in Animal Fall project.
5. No world/chapter theming across 50 levels.
6. Journey Map nodes are unstyled.

---

## Glossary

- **AnimalFall_System**: The redesigned Animal Fall game system as a whole.
- **Spawner**: Spawns animals from the top of the screen.
- **Animal**: A falling entity the player taps to save. Has species, type, movement pattern, and point value.
- **AnimalType**: Normal, Golden, Bomb, Shielded, Decoy, Ghost, Rainbow, Paired, Shrinking, IceCube, FakeAnimal, CursedSkull, ThiefBird.
- **AnimalSpecies**: Chicken, Dog, Cow, Cat, Monkey, Pig, Rabbit, Penguin, Owl, Mouse, Zebra, Duck.
- **MovementPattern**: Static, Drift, ZigZag, SineWave, Bounce, Teleport, FloatUp, HeavyFall, Erratic.
- **LevelData**: ScriptableObject defining all parameters for a single level.
- **LevelDatabase**: ScriptableObject containing the ordered array of all 50 LevelData assets.
- **Goal**: Per-species rescue target embedded in LevelData.
- **ObjectPooler**: Singleton pool manager. `Instantiate` and `Destroy` are banned during gameplay.
- **HindranceManager**: Activates, tracks, and cleans up hindrance instances for a given level.
- **IHindrance**: Interface all hindrance MonoBehaviours implement — `Activate(context)`, `Deactivate()`.
- **HindranceContext**: Struct passed to `IHindrance.Activate()` carrying references to active managers.
- **GameManager**: Owns the gameplay loop — starts level, tracks time, collected count, calls EndLevel.
- **ScoreManager**: Tracks score with combo multiplier.
- **ComboManager**: Tracks consecutive correct taps and computes multipliers.
- **GameUIManager**: Drives all HUD elements and in-game popups using DOTween.
- **LevelManager**: Handles scene-level progression, PlayerPrefs persistence, and level unlocking.
- **ImageLibrary**: Static sprite cache. All sprites must be retrieved via `ImageLibrary`.
- **Chapter**: Themed grouping of 10 levels (5 chapters × 10 levels = 50 total).
- **MegaLevel**: Boss-fight level occurring every 5 levels (L5, L10, …, L50).
- **Villain**: Boss entity with HP, shield phases, and projectile attacks in MegaLevels.
- **PowerUp**: In-level ability (SlowTime, Magnet, MultiTap, AutoTap, FreezeAll).
- **Star Rating**: 0–3 star evaluation awarded at level end.
- **DOTween**: DG.Tweening animation library — all gameplay animations MUST use DOTween.
- **AnimalBlast_Path**: `c:\AnnusMirabilis\ZEMOLABS\NEMESIS\animal_blast\AnimalBlast`.

---

## Requirements

### Requirement 1: Object Pooling — Zero GC During Gameplay

**User Story:** As a player, I want the game to run smoothly at 60 fps without stutters, so that tapping animals feels responsive and satisfying.

#### Acceptance Criteria

1. THE `ObjectPooler` SHALL manage all runtime GameObjects — `GameObject.Instantiate` and `GameObject.Destroy` are forbidden during active gameplay (between `GameManager.StartLevel` and `GameManager.EndLevel`).
2. WHEN the `Spawner` spawns an animal, THE `Spawner` SHALL call `ObjectPooler.SpawnFromPool(animalPrefab, spawnPoint, Quaternion.identity, animalContainer)` — not `Instantiate`.
3. WHEN an animal is collected, THE `Animal` SHALL call `ObjectPooler.ReturnToPool(gameObject)` — with a double-return guard so a second call in the same frame is a no-op and logs a warning.
4. WHEN an animal misses the screen or its lifetime expires, THE `Animal` SHALL call `ObjectPooler.ReturnToPool(gameObject)` — also guarded against double-return.
5. WHEN a hindrance is deactivated, THE `HindranceBase.Deactivate()` SHALL call `ObjectPooler.ReturnToPool(gameObject)` — not `Destroy(gameObject)`.
6. WHEN a level is loaded, THE `LevelManager` SHALL pre-warm the `ObjectPooler` for every animal prefab and hindrance prefab listed in `LevelData`, using the configured pool count; if a prefab entry is null or count is zero, it SHALL log a `Debug.LogWarning` and skip that entry.
7. WHEN a pooled object is returned, THE `ObjectPooler` SHALL reset `transform.localScale = Vector3.one`, reset sprite color to `Color.white` (RGB + alpha), call `DOTween.Kill(gameObject)`, stop all Coroutines, and clear component state (cubeType, gridIndex, canTapped equivalents).
8. THE `Spawner.ChooseAnimalData()` SHALL operate on a pre-allocated fixed-size array (sized to `LevelData.spawnPool.Length` at level load) using `for` loops — no `System.Array.Find`, `System.Array.FindAll`, `.Where`, `.Select`, or `.ToList()` calls; if the array is uninitialized, it SHALL log `Debug.LogError` and return null.

### Requirement 2: Animal Movement & Visual Polish

**User Story:** As a player, I want animals to fall with satisfying animations and clear visual feedback when I tap them, so that saving animals feels rewarding.

#### Acceptance Criteria

1. WHEN an animal is spawned, THE `Spawner` SHALL animate its scale from `(0.1, 0.1, 1)` to `(1, 1, 1)` over 0.25s with OutBack easing as the entrance animation.
2. WHEN an animal lands a correct tap, THE `Animal` SHALL play a squash-and-stretch animation (`(1.3, 0.7, 1)` → `(1, 1, 1)` over 0.1s with OutElastic easing) before returning to pool; if the entrance animation is still playing when the tap is registered it SHALL be killed first.
3. WHEN an animal falls below the screen bottom, THE `AnimalMovement` SHALL call `ObjectPooler.ReturnToPool(gameObject)` — it SHALL NOT call `Destroy(gameObject)`.
4. THE `AnimalMovement` bounds cache SHALL be computed once during `Awake` and recomputed only when the device orientation changes or the screen aspect ratio changes — it SHALL NOT be recomputed every `Update()` frame.
5. WHEN an animal has `AnimalType.Shielded` and `currentShield > 0`, THE `Animal` SHALL display a yellow outline flash animation (DOTween yoyo, 2 loops, 0.08s per loop) on each tap that does not deplete the final shield; on the tap that depletes the final shield, the flash SHALL play once then stop.
6. THE `AnimalMovement` SHALL read wind, zero-gravity, and black-hole forces from `EnvironmentEffects.Instance` into one local variable at the top of each `Update()` frame — no repeated `.Instance` accesses or null-checks within a single Update call.
7. WHEN an animal is retrieved from pool, `SetupForPool` SHALL stop any running lifetime coroutine on that object before starting a new one, ensuring no two lifetime coroutines run concurrently on the same Animal instance; when lifetime expires, the coroutine SHALL call `ObjectPooler.ReturnToPool`.
8. WHEN floating score text is shown on a correct tap, THE `GameUIManager` SHALL set the text's world-space anchor to `Camera.main.WorldToScreenPoint(animal.transform.position)`; if `Camera.main` is null, it SHALL fall back to the animal's raw world position.
9. WHEN a new tap animation is triggered on an animal while a previous tap animation is still playing, THE previous animation SHALL be killed and the new animation SHALL start immediately.

### Requirement 3: 12 Animal Species Using Animal Blast Sprites

**User Story:** As a player, I want to see a variety of adorable animals with distinct looks across 50 levels, so that the game stays visually fresh.

#### Acceptance Criteria

1. THE `AnimalSpecies` enum SHALL contain exactly 12 values: `Chicken, Dog, Cow, Cat, Monkey, Pig, Rabbit, Penguin, Owl, Mouse, Zebra, Duck`.
2. THE `ImageLibrary` SHALL eagerly load and cache a `Sprite` for each of the 12 species during level load from AnimalBlast_Path `Assets/Resources/icons/animals/`; if a sprite file is absent, `ImageLibrary` SHALL assign a placeholder sprite and log `Debug.LogError` identifying the missing path.
3. WHEN `Animal.SetupForPool(AnimalData data, LevelData level)` is called, THE `Animal` SHALL assign `sr.sprite = ImageLibrary.GetAnimalSprite(data.species)`; if `GetAnimalSprite` returns null, the Animal SHALL assign the placeholder sprite and log a `Debug.LogWarning` — no `Resources.Load<Sprite>()` call is permitted in Animal or its callers.
4. THE `LevelData.spawnPool` field SHALL be a `[SerializeField]` array of `AnimalData` with `[Tooltip]`, bounded to 1–12 entries in the Inspector (matching the enum size); the Spawner SHALL reject a pool with 0 entries by logging `Debug.LogError` and halting all spawn attempts for the remainder of that level session.
5. WHERE a level's `spawnPool` array is empty or null at runtime, THE `Spawner` SHALL log `Debug.LogError` and halt all further spawn attempts for the remainder of that level session rather than throwing a `NullReferenceException`.

### Requirement 4: 50-Level Data-Driven Level System

**User Story:** As a designer, I want all 50 levels defined in ScriptableObjects with full designer control, so that I can tune difficulty, species, and hindrances without touching code.

#### Acceptance Criteria

1. THE `LevelDatabase` SHALL contain an ordered array of exactly 50 `LevelData` ScriptableObject assets, named `Level_01` through `Level_50`, stored in `Assets/Levels/LevelData/`.
2. THE `LevelData` ScriptableObject SHALL expose these designer-configurable fields with `[Tooltip]` attributes and concrete value bounds:
   - `int levelNumber` (1–50) — Level index
   - `string chapterTheme` — Chapter theme name
   - `float timeLimit` (10–120s) — Countdown in seconds
   - `GoalData goal` — Per-species rescue targets
   - `AnimalData[] spawnPool` (1–12 entries) — Animals eligible to spawn
   - `float spawnInterval` (0.1–2.0s) — Base seconds between spawns
   - `float spawnVariance` (0–0.5s) — ± randomness on spawn interval
   - `int maxOnScreen` (1–20) — Max simultaneous animals
   - `HindranceConfig[] hindrances` — Hindrance entries with type, weight, initial delay
   - `float hindranceSpawnInterval` (1–30s) — Seconds between hindrance activations
   - `int maxHindrancesActive` (1–5) — Max simultaneous active hindrances
   - `bool isMegaLevel` — Is this a boss level
   - `int rewardCoins` (0–500) — Coins awarded on win
   - `float wrongTapTimePenalty` (0–10s) — Seconds lost per wrong tap
   - `float bombTimePenalty` (0–15s) — Seconds lost when bomb is tapped
3. WHEN `LevelDatabase.[ContextMenu("Generate & Save 50 Levels")]` is invoked in the Editor, THE `LevelDatabase` SHALL create and save all 50 `LevelData` assets as `.asset` files on disk using `AssetDatabase.CreateAsset`; if an asset already exists at the target path, it SHALL overwrite it; this method SHALL only execute inside `#if UNITY_EDITOR`.
4. WHEN `LevelManager.LoadGameSceneForLevel(int levelIndex)` is called with a 0-based index (0–49), THE `LevelManager` SHALL retrieve `LevelDatabase.GetLevel(levelIndex)` and pass it to `GameManager.StartLevel`; if `levelIndex` is out of range, THE `LevelManager` SHALL log `Debug.LogError` and abort the load.
5. THE `GoalData` type SHALL be a `ScriptableObject` (not a plain `[Serializable]` class) so it can be shared across multiple levels and edited independently in the Inspector.
6. WHEN `LevelData.isMegaLevel == false`, THE `villain` field SHALL be null or ignored; WHEN `LevelData.isMegaLevel == true`, THE `villain` field SHALL reference a non-null `VillainData` ScriptableObject or THE `MegaLevelController` SHALL log `Debug.LogError` and fall back to normal level flow.

### Requirement 5: Five Chapter Themes with Distinct Visual Identity

**User Story:** As a player, I want each 10-level chapter to feel visually distinct and thematically cohesive, so that progressing through the game feels like a journey.

#### Acceptance Criteria

1. THE AnimalFall_System SHALL define exactly 5 chapters:
   - Chapter 1 (L1–10): **Sunny Meadow** — Camera.backgroundColor = `#F5E87A`; focus species: Chicken, Dog, Pig
   - Chapter 2 (L11–20): **Tropical Jungle** — Camera.backgroundColor = `#2E7D32`; focus species: Monkey, Cat, Rabbit
   - Chapter 3 (L21–30): **Snowy Arctic** — Camera.backgroundColor = `#B3E5FC`; focus species: Penguin, Owl, Zebra
   - Chapter 4 (L31–40): **Mystic Forest** — Camera.backgroundColor = `#4A148C`; focus species: Mouse, Duck, Cow
   - Chapter 5 (L41–50): **Storm Peaks** — Camera.backgroundColor = `#0D1B2A`; all 12 species active
2. WHEN a level from a given chapter is loaded, THE `GameManager` SHALL tween the camera's background color to the chapter's defined hex value over 0.5s using DOTween — if the tween is already running from a previous load it SHALL be killed first.
3. THE `LevelData` SHALL include a `[SerializeField] Sprite chapterBackground` field; the sprite SHALL follow the naming convention `bg_chapter<N>.png` (e.g., `bg_chapter1.png`) sourced from AnimalBlast_Path `Assets/Resources/panels/`; if the sprite is missing, THE `GameManager` SHALL use a solid-color background matching the chapter hex instead.
4. WHEN the player completes all 10 levels in a chapter, THE `LevelManager` SHALL display a chapter-complete popup animated from scale `(0,0,1)` to `(1,1,1)` with `Ease.OutBack` over 0.4s.
5. WHERE a chapter background sprite is missing or null at runtime, THE `GameManager` SHALL fall back to the chapter's solid hex background color and log `Debug.LogWarning`.

### Requirement 6: Proper 50-Level Difficulty Curve

**User Story:** As a player, I want each level to feel appropriately challenging and progressively harder, so that I stay engaged across all 50 levels.

#### Acceptance Criteria

1. THE 50 levels SHALL follow a staged difficulty progression with four bands, each parameter linearly interpolated between its band start and end values based on the level's 0-based position within the band:
   - Levels 1–10 (Intro): `timeLimit` 60s→45s, `maxOnScreen` 5→8, `spawnInterval` 0.9s→0.7s, 0–1 hindrance types active
   - Levels 11–25 (Rising): `timeLimit` 45s→35s, `maxOnScreen` 8→11, `spawnInterval` 0.7s→0.5s, 1–3 hindrance types active
   - Levels 26–40 (Challenge): `timeLimit` 35s→28s, `maxOnScreen` 11→13, `spawnInterval` 0.5s→0.35s, 3–5 hindrance types active
   - Levels 41–50 (Expert): `timeLimit` 28s→20s, `maxOnScreen` 13→15, `spawnInterval` 0.35s→0.25s, 5–7 hindrance types active
2. THE rescue target (total Goal count) for each level SHALL be `floor(timeLimit / spawnInterval) * 0.75`; where this formula deviates by more than 2 animals from a smooth linear scale (10 animals at Level 1 to 45 at Level 50), the linear scale value SHALL take precedence.
3. WHEN a MegaLevel is active (every 5th level), THE `timeLimit` for that level SHALL equal the standard formula value from Criterion 1 plus 15 additional seconds.
4. THE `wrongTapTimePenalty` for level N (1-based) SHALL be `1.0 + (3.0 / 49) * (N - 1)` seconds, rounded to 2 decimal places, increasing linearly from 1.0s at Level 1 to 4.0s at Level 50.
5. THE `bombTimePenalty` for level N (1-based) SHALL be `3.0 + (5.0 / 49) * (N - 1)` seconds, rounded to 2 decimal places, increasing linearly from 3.0s at Level 1 to 8.0s at Level 50.

### Requirement 7: Hindrance System — 20 Hindrance Types Fully Implemented

**User Story:** As a player, I want multiple hindrances per level that make rescuing animals challenging in different ways, so that each level feels unique.

#### Acceptance Criteria

1. THE AnimalFall_System SHALL implement exactly 20 distinct hindrance types in 5 categories:
   - **Penalties**: Bomb, AlarmClock, PoisonVial, ThiefBird
   - **TapModifiers**: KnightHelmet, BubbleShield, IceCube, GhostAnimal
   - **ScreenBlockers**: InkSquid, StormCloud, Flashbang, FallingLeaves
   - **EnvironmentMods**: WindGust, ZeroGravity, BlackHole, Tornado
   - **Advanced**: MagnetTrap, MirrorMode, CursedSkull, PairedAnimal
2. EACH hindrance SHALL implement `IHindrance`: `Activate(HindranceContext)` SHALL start the hindrance's observable effect (state change, visual, audio); `Deactivate()` SHALL fully reverse all effects and call `ObjectPooler.ReturnToPool(gameObject)`.
3. THE `HindranceFactory.CreateAtRandomScreenTop(HindranceData data, Transform parent)` SHALL call `ObjectPooler.SpawnFromPool` — no `Instantiate` call inside the factory.
4. WHEN a hindrance is deactivated, THE `HindranceBase.Deactivate()` SHALL call `ObjectPooler.ReturnToPool(gameObject)` — not `Destroy(gameObject)`.
5. THE hindrance sprites SHALL be sourced from AnimalBlast_Path `Assets/Resources/icons/hindrances/` and cached via `ImageLibrary`.
6. THE `LevelData.HindranceConfig[]` array SHALL include a `float weight` (> 0) per entry; the `HindranceManager` SHALL use weighted-random selection — heavier entries spawn more frequently.
7. IF `maxHindrancesActive` simultaneous hindrances are already active, THEN THE `HindranceManager` SHALL skip (not queue) the next spawn attempt and wait until the next `hindranceSpawnInterval` tick.
8. WHEN a level starts, THE `HindranceManager` SHALL wait `hindranceInitialDelay` seconds (configurable 2–15s per `LevelData`) before spawning the first hindrance.
9. THE `LevelData.maxHindrancesActive` field SHALL be a designer-configurable `[SerializeField]` integer in the range 1–5, with a `[Tooltip]` attribute.

### Requirement 8: Hindrance Behaviours — Per-Type Specification

**User Story:** As a player, I want each hindrance to behave in a clearly understandable and distinct way, so that I can learn the patterns and adapt my tapping strategy.

#### Acceptance Criteria

**Penalties:**
1. WHEN `Bomb` is tapped, THE `GameManager` SHALL deduct `bombTimePenalty` seconds from `remainingTime` (clamped to 0) and THE `EffectsController` SHALL spawn the Explosion VFX at the Bomb's world position.
2. WHEN `AlarmClock` activates, THE `HindranceManager` SHALL multiply the active `spawnInterval` by 0.6 for 5 seconds then restore it; if a second AlarmClock activates while one is already active, the 5s timer SHALL reset (not stack the multiplier).
3. WHEN `PoisonVial` is tapped, THE `LivesManager` SHALL deduct 1 life and THE `AudioManager` SHALL play `SfxType.WrongTap`.
4. WHEN `ThiefBird` is active, THE `ThiefBirdHindrance` SHALL select one random on-screen animal, tween it horizontally off-screen over 1.5s, and call `ObjectPooler.ReturnToPool` when off-screen; if no animals are currently on screen, THE `ThiefBird` SHALL deactivate without stealing.

**TapModifiers:**
5. WHEN `KnightHelmet` wraps an animal, THE `Animal` SHALL require 3 taps to rescue; each tap SHALL decrement a `helmetLayers` counter (3→2→1→0) and play a scale-bounce DOTween animation on the animal; the animal is collected on the tap that brings `helmetLayers` to 0.
6. WHEN `BubbleShield` wraps an animal, THE `Animal` SHALL move upward (positive Y velocity) instead of falling until the first tap pops the bubble; subsequent taps collect normally.
7. WHEN `IceCube` wraps an animal, THE `Animal` SHALL require a swipe gesture (≥ 80 screen-pixels of drag) to melt the ice; a plain tap SHALL play `SfxType.ShieldHit` and have no other effect; after melting, the next tap collects normally.
8. WHEN `GhostAnimal` activates on an animal, THE `AnimalMovement` SHALL tween the animal's sprite alpha from 1.0 to 0.2 over 0.5s via DOTween; the ghost alpha SHALL persist until the animal is collected or returns to pool — it SHALL NOT be reset mid-fall.

**ScreenBlockers:**
9. WHEN `InkSquid` activates, THE `ScreenEffects` SHALL display a pooled ink-splatter overlay covering 40% of the screen area for 4 seconds, then fade it out over 1s via DOTween alpha tween; the overlay SHALL not intercept tap events.
10. WHEN `StormCloud` activates, THE `ScreenEffects` SHALL overlay a dark gradient covering the lower 60% of the screen for 6 seconds.
11. WHEN `Flashbang` activates, THE `ScreenEffects` SHALL flash a full-screen white overlay to alpha 0.9 then fade to 0 over 0.8s via DOTween.
12. WHEN `FallingLeaves` activates, THE `EnvironmentEffects` SHALL spawn exactly 20 pooled leaf particles that drift across the screen for 5 seconds; each leaf SHALL be returned to pool after its drift completes.

**EnvironmentMods:**
13. WHEN `WindGust` activates, THE `EnvironmentEffects.WindForce` SHALL be set to a random horizontal vector with magnitude between 1.5 and 3.0 units/s; `AnimalMovement` SHALL apply this as a per-frame translation offset; when the WindGust deactivates, `WindForce` SHALL return to `Vector2.zero`.
14. WHEN `ZeroGravity` activates, THE `EnvironmentEffects.IsZeroGravityActive` SHALL be `true` for 4 seconds; all animal fall velocities SHALL be reduced to 0 during this period; when ZeroGravity deactivates, normal fall velocities SHALL resume.
15. WHEN `BlackHole` activates, THE `EnvironmentEffects.BlackHoleCenter` SHALL be set to a random on-screen world position; animals within 4 world units SHALL be pulled toward `BlackHoleCenter` at 1.5 world units/s² per frame; an animal that reaches within 0.5 units of the center SHALL be returned to pool (counted as missed).
16. WHEN `Tornado` activates, THE `TornadoHindrance` SHALL move horizontally across the screen; any animal whose collider overlaps the tornado's collider SHALL receive a horizontal force of 2.0 world units/s away from the tornado's center.

**Advanced:**
17. WHEN `MagnetTrap` activates, THE `InputManager` SHALL offset every world tap position by a random `Vector2` with magnitude between 0.3 and 0.8 world units in a random direction; the offset SHALL be re-randomized once when MagnetTrap activates and remain constant until it deactivates.
18. WHEN `MirrorMode` activates, THE `AnimalMovement` SHALL negate the X component of all movement vectors and spawn X-positions for 8 seconds; when MirrorMode deactivates, THE `AnimalMovement` SHALL restore original X axis behaviour.
19. WHEN `CursedSkull` falls and reaches the screen bottom without being tapped, THE `GameManager` SHALL deduct 5 seconds from `remainingTime` (clamped to 0).
20. WHEN `CursedSkull` is tapped before reaching the screen bottom, THE `GameManager` SHALL add 2 seconds to `remainingTime` (capped at the level's original `timeLimit`).
21. WHEN `PairedAnimal` spawns two animals simultaneously, BOTH animals SHALL be tapped within 2 seconds of each other; IF only one is tapped within the 2-second window, THEN THE `GameManager` SHALL apply `wrongTapTimePenalty` and return the un-tapped paired animal to pool; IF neither is tapped within 2 seconds, THEN both SHALL return to pool with no penalty.

### Requirement 9: Hindrance Unlock Progression by Level

**User Story:** As a player, I want hindrances to be introduced gradually so I learn the game mechanics before being overwhelmed.

#### Acceptance Criteria

1. THE hindrance unlock schedule SHALL be: Level 3: Bomb, FallingLeaves; Level 5: AlarmClock, WindGust; Level 7: KnightHelmet, InkSquid; Level 10: PoisonVial, GhostAnimal; Level 12: BubbleShield, StormCloud; Level 15: Flashbang, ZeroGravity; Level 18: IceCube, ThiefBird; Level 20: Tornado, BlackHole; Level 23: PairedAnimal, MirrorMode; Level 26: MagnetTrap, CursedSkull; a hindrance type is considered unlocked at the listed level and all subsequent levels.
2. IF a `LevelData.HindranceConfig[]` entry references a hindrance type whose unlock level is greater than the current level number, THEN THE `HindranceManager` SHALL exclude that entry from spawning and log a `Debug.LogWarning` treating the entry as absent.
3. WHEN a hindrance type appears in a level's `HindranceConfig[]` for the first time in the player's account progression, THE `GameUIManager` SHALL display a tutorial toast notification naming the hindrance and its one-line effect description, keeping the toast visible for 3–5 seconds before auto-dismissing; this notification SHALL appear at most once per hindrance type per account, persisted via `SaveService`.
4. EACH level SHALL specify its exact enabled hindrance set via `LevelData.HindranceConfig[]`; the `HindranceManager` SHALL only spawn hindrances explicitly listed in the current level's config.

### Requirement 10: MegaLevel Boss Fights (Every 5 Levels)

**User Story:** As a player, I want boss fights every 5 levels that feel epic and different from normal rounds.

#### Acceptance Criteria

1. WHEN `LevelData.isMegaLevel == true`, THE `GameManager` SHALL initialize `MegaLevelController` and THE `GameUIManager` SHALL display the `VillainHUD` showing the Villain's HP bar before spawning begins.
2. THE Villain SHALL have 3 HP phases with projectile frequencies: Phase 1 (full health) = 1 projectile every 8s; Phase 2 (50% health) = 1 projectile every 5s; Phase 3 (25% health) = 1 projectile every 3s.
3. WHEN the player collects a per-phase animal quota, THE `MegaLevelController` SHALL reduce Villain HP by 1 phase and enter the next phase's projectile frequency.
4. WHEN the Villain fires a projectile and the player taps it within 0.5s of spawn, THE `MegaLevelController` SHALL deal 1 HP of deflection damage to the Villain.
5. WHEN the Villain fires a projectile and the player does NOT tap it within 0.5s, THE `GameManager` SHALL deduct 3 seconds from `remainingTime` (clamped to 0).
6. WHEN `GameManager.OnMegaLevelComplete()` is called, THE win flow SHALL be triggered and THE `MegaLevelController` SHALL deactivate the Villain.
7. WHEN a Villain HP phase changes, THE `VillainHUD` HP bar SHALL animate to the new value over 0.3s.
8. WHEN the Villain transitions to a new phase, THE `GameUIManager` SHALL display a phase transition visual cue (screen flash + villain scale punch) lasting 0.5s.

### Requirement 11: Star Rating & Score System

**User Story:** As a player, I want to be rated on my performance in each level with 0–3 stars.

#### Acceptance Criteria

1. THE star rating SHALL be calculated at level end by `ScoreManager.CalculateStars(int rescued, int target, float timeRemaining, float totalTime)`: 3 stars = rescued ≥ 100% of target AND timeRemaining ≥ totalTime * 0.3; 2 stars = rescued ≥ 100% of target (any remaining time); 1 star = rescued ≥ 75% of target; 0 stars = rescued < 75% of target.
2. THE star ratings SHALL be persisted per-level in `SaveService`; replaying a level SHALL only overwrite the saved star count if the new count is higher; a new 0-star result SHALL be saved only if no prior result exists for that level.
3. WHEN the level-complete popup displays, THE `ResultsScreenController` SHALL animate each of the 3 star icons from scale 0 to 1 with `Ease.OutBounce` over 0.3s per star, with 0.2s between each star's animation.
4. THE `ScoreManager.CalculateStars` SHALL be the single method responsible for star calculation — thresholds SHALL NOT be hardcoded in `GameManager` or any other class.
5. THE Journey Map SHALL display a star icon for each completed level: if no result exists, the node shows no stars; if a 0-star result exists, the node shows a distinct "attempted" state.

### Requirement 12: HUD and UI Redesign Using Animal Blast Assets

**User Story:** As a player, I want a polished, clear HUD that shows the timer, progress, and score using the Animal Blast visual language.

#### Acceptance Criteria

1. THE `GameScene` Canvas SHALL be split into two separate Canvas components with distinct sort orders: `StaticCanvas` (sort order 0, background/borders, never rebuilds during gameplay) and `DynamicCanvas` (sort order 1, timer/score/combo, rebuilds independently).
2. THE `DynamicCanvas` timer display SHALL render the `clock.png` sprite from AnimalBlast_Path `Assets/Resources/icons/clock.png` as a decorative icon to the left of the timer text.
3. WHEN remaining time falls below 10 seconds, THE `GameUIManager` SHALL tween the timer text color to red over 0.3s and continuously pulse the timer scale between 1.0 and 1.15 (yoyo, infinite loops) until the level ends.
4. WHEN the goal progress value changes, THE `GameUIManager` SHALL animate the progress bar fill to the new fill amount over 0.15s rather than assigning it instantly.
5. THE goal counter display SHALL show one species icon (sourced via `ImageLibrary`) followed by `current/target` count for each tracked species, laid out horizontally.
6. THE level-complete popup SHALL use `panel.png` or `panel2.png` from AnimalBlast_Path `Assets/Resources/panels/` as its background image and animate entrance from scale `(0,0,1)` to `(1,1,1)` with `Ease.OutBack` over 0.3s.
7. THE level-fail popup action buttons SHALL use `red_buttons.png` from AnimalBlast_Path `Assets/Resources/panels/red_buttons.png` as the Image source sprite on each action button.
8. WHEN floating score text is spawned, THE `GameUIManager` SHALL animate it upward by 80 Canvas units and fade its alpha to 0 over 1.2s via DOTween; on animation completion the text object SHALL be returned to pool.
9. ALL popups SHALL animate entrance from scale `(0,0,1)` to `(1,1,1)` with `Ease.OutBack` over 0.3s and exit to scale `(0,0,1)` with `Ease.InBack` over 0.2s.

### Requirement 13: Combo System with Pitch Modulation

**User Story:** As a player, I want to feel a dopamine rush when I tap animals consecutively without missing.

#### Acceptance Criteria

1. WHEN the player successfully taps a 3rd consecutive animal without a miss, THE `ComboManager` SHALL set the multiplier to 1.5x and increment the combo display.
2. THE combo multiplier thresholds SHALL be: 3 consecutive taps = 1.5x; 6 = 2.0x; 10 = 3.0x; 15+ = 5.0x; the multiplier is updated on each correct tap that crosses a threshold.
3. WHEN a wrong tap or an animal miss occurs, THE `ComboManager` SHALL reset the combo counter to 0 and the multiplier to 1.0x.
4. THE `AudioManager` SFX pitch SHALL advance one step per correct tap through the array `[0.95, 1.0, 1.05, 1.1, 1.15]` (index 0 at tap 1, clamped at index 4 for taps beyond 5); a miss or wrong tap SHALL reset the pitch index to 0.
5. WHEN the combo counter increments, THE `GameUIManager` SHALL apply a DOTween punch-scale animation (vibrato 5, elasticity 0.5, strength 0.3) to the combo text over 0.25s.
6. WHEN the combo reaches 10 consecutive taps, THE `AudioManager` SHALL play `SfxType.MegaCombo` and THE `ScreenEffects` SHALL briefly flash the screen border gold over 0.5s (fade in 0.1s, hold 0.2s, fade out 0.2s).

### Requirement 14: Power-Ups (5 Types)

**User Story:** As a player, I want to use power-ups to get out of difficult situations.

#### Acceptance Criteria

1. THE AnimalFall_System SHALL implement 5 power-up types: `SlowTime`, `Magnet`, `MultiTap`, `AutoTap`, `FreezeAll`, each with a unique activation effect and cooldown.
2. WHEN `SlowTime` is activated, THE game time scale SHALL drop to 0.5x for 4 seconds then return to 1.0x; if re-activated while already active, the 4s duration SHALL reset (not stack below 0.5x).
3. WHEN `Magnet` is activated, all on-screen animals SHALL be moved to the screen center over 1.5s and collected upon arrival; animals that are already being collected SHALL not be affected.
4. WHEN `MultiTap` is activated, THE next 3 taps SHALL each collect all animals within a 2-world-unit radius; a tap on empty space SHALL consume one charge; remaining charges SHALL expire at level end.
5. WHEN `AutoTap` is activated, THE system SHALL automatically collect one random on-screen animal every 0.4s for 5 seconds; if no animals are on screen at a collection tick, that tick SHALL be skipped (not add time).
6. WHEN `FreezeAll` is activated, all active `AnimalMovement` components SHALL be disabled for 3 seconds, then re-enabled; animals SHALL remain tappable (and collectible) while frozen.
7. WHEN a power-up is on cooldown, THE power-up slot UI SHALL display a cooldown ring (sourced from AnimalBlast_Path `Assets/Resources/icons/boosters/`) that depletes as the cooldown expires; each power-up SHALL have a designer-configurable cooldown duration stored in its `PowerUpData` ScriptableObject.

### Requirement 15: Journey Map — Scrollable Level Selection

**User Story:** As a player, I want a beautiful scrollable map showing all 50 levels with my progress.

#### Acceptance Criteria

1. THE `JourneyMapController` SHALL display 50 level nodes in a scrollable vertical layout, grouped into 5 chapter sections of 10 nodes each.
2. IF a level node's level index is ≤ the highest completed level, THEN its sprite SHALL be `levelbutton1.png` from AnimalBlast_Path; IF a level node's level index is > the highest completed level AND > the highest unlocked level, THEN its sprite SHALL be `levelbutton2.png`.
3. THE "current playable level" is defined as the first level with no completed result; WHEN the Journey Map opens, THE scroll view SHALL auto-scroll so the current playable level node is visible in the center of the viewport.
4. THE current playable level node SHALL continuously pulse its scale between 1.0 and 1.1 over 0.5s (yoyo, infinite loops) to draw attention.
5. WHEN the player taps a locked level node, THE `JourneyMapController` SHALL apply a `DOShakePosition(0.3f, 5f, 10)` to the node and show a toast "Complete previous levels first!" for 2s.
6. WHEN the player taps an unlocked level node, THE `JourneyMapController` SHALL call `LevelManager.LoadGameSceneForLevel(levelIndex)`; if the scene load fails, THE `JourneyMapController` SHALL display an error toast "Unable to load level. Please try again."
7. THE chapter section headers and decorative backgrounds SHALL use images from AnimalBlast_Path `Assets/Resources/panels/`.
8. WHEN a level is replayed and a higher star count is earned, THE `JourneyMapNode` SHALL animate a star count update via DOTween scale bounce (scale to 1.4 over 0.1s, back to 1.0 over 0.15s with OutElastic easing).

### Requirement 16: Level Intro Screen with Objectives

**User Story:** As a player, I want to see the level objectives before the countdown begins.

#### Acceptance Criteria

1. WHEN `GameManager.StartLevel(LevelData level)` is called, THE `GameManager` SHALL suspend animal spawning and display the level intro overlay for 2 seconds before the 3-2-1 countdown; during the intro all player input SHALL be blocked except a tap to skip; the overlay SHALL show: level number, chapter name, per-species rescue targets with icons, timer duration, and active hindrance icons.
2. WHEN the intro panel appears, THE `GameUIManager` SHALL animate it from scale `(0,0,1)` to `(1,1,1)` with `Ease.OutBack` over 0.3s; when the intro dismisses, THE panel SHALL animate to scale `(0,0,1)` with `Ease.InBack` over 0.2s.
3. WHEN the 3-2-1-GO countdown plays, each beat SHALL animate its number from scale 2.5 down to 0.8 with `Ease.OutElastic` over 0.7s.
4. WHEN the player taps the screen during the intro 2-second hold period, THE `GameManager` SHALL skip the remaining hold time and begin the 3-2-1 countdown immediately.

### Requirement 17: VFX — Animal Blast VFX Prefabs

**User Story:** As a player, I want satisfying visual feedback when I save animals or trigger effects.

#### Acceptance Criteria

1. WHEN an animal is correctly tapped and collected, THE `EffectsController` SHALL call `ObjectPooler.SpawnFromPool` to instantiate `Battle_Effect_White.prefab` (sourced from AnimalBlast_Path `Assets/Resources/VFX/`) at the animal's world position with identity rotation; the VFX object SHALL be returned to pool after its particle system completes.
2. WHEN a Bomb animal is tapped, THE `EffectsController` SHALL spawn `Explosion_1_Bam.prefab` (sourced from AnimalBlast_Path `Assets/Resources/VFX/`) at the Bomb's world position via `ObjectPooler.SpawnFromPool`.
3. WHEN an `AlarmClock` or `Flashbang` hindrance activates, THE `EffectsController` SHALL spawn `Explosion_1_Zap.prefab` (sourced from AnimalBlast_Path `Assets/Resources/VFX/`) at the hindrance's world position via `ObjectPooler.SpawnFromPool`.
4. ALL VFX prefabs SHALL be pre-warmed in the `ObjectPooler` during level load — no `Instantiate` or `Destroy` calls for VFX during active gameplay.
5. WHEN an animal misses (falls below the screen bottom), THE `EffectsController` SHALL display a pooled bottom-edge flash overlay (full screen width, 48 units tall, red, alpha 0.8) that fades to alpha 0 over 0.3s via DOTween; the overlay object SHALL be returned to pool after the fade completes.

### Requirement 18: Audio System with Pooled Audio Sources

**User Story:** As a player, I want responsive audio feedback for all taps, misses, combos, and hindrance events.

#### Acceptance Criteria

1. THE `AudioManager` SHALL maintain a pool of exactly 12 `AudioSource` components — no `AudioSource` SHALL be attached to individual Animal GameObjects.
2. THE `AudioManager.PlaySFX(SfxType type, float pitch = 1f)` SHALL borrow the first available pooled source, assign the clip for `type` and the given pitch, play it, then return the source to the pool when the clip finishes; if all 12 sources are in use, THE oldest playing source SHALL be interrupted and borrowed; if the clip for `type` is null, THE call SHALL be silently skipped (no exception).
3. THE `SfxType` enum SHALL define: `Collect, WrongTap, Explosion, ComboUp, MegaCombo, LevelWin, LevelLose, HindranceActivate, ShieldHit, PowerUpActivate, TimerWarning`.
4. WHEN a `MegaCombo` event fires, THE `AudioManager` SHALL blend the Audio Mixer to a "duck" snapshot (BGM at 0.3) over 0.5s, then restore to the "normal" snapshot (BGM at 1.0) over 1s after the MegaCombo SFX finishes.
5. WHEN `ComboManager` fires a correct-tap event at combo step N, THE `ComboManager` SHALL call `AudioManager.PlaySFX(SfxType.Collect, pitchSteps[min(N-1, 4)])` where `pitchSteps = [0.95, 1.0, 1.05, 1.1, 1.15]`.

### Requirement 19: Save, Persistence & Lives System

**User Story:** As a player, I want my progress and lives to persist between sessions.

#### Acceptance Criteria

1. THE `SaveService` SHALL persist player progress: highest unlocked level (int) via `PlayerPrefs`; per-level star ratings as a JSON int array (indices 0–49, values 0–3) stored in `PlayerPrefs`.
2. THE `SaveService` SHALL save the player's state at level end and on app pause — not only at app close.
3. THE `LivesManager` SHALL enforce a maximum of 5 lives at all times; the initial lives value on a fresh install SHALL be 5.
4. WHEN lives reach 0, THE `LivesManager.HasLives()` SHALL return `false`; THE `GameManager` SHALL check `HasLives()` before starting a new level and abort with an "Out of lives" UI state if false.
5. WHEN a level fails, THE `LivesManager` SHALL deduct exactly 1 life; if lives are already 0 when the deduction is attempted, the count SHALL remain 0.
6. WHILE lives < 5, THE `LivesManager` SHALL regenerate 1 life every 30 minutes of real time.
7. THE regeneration countdown SHALL be stored as a UTC timestamp (the time when the next life will be granted) persisted in `SaveService` across app sessions.
8. WHEN the player opens the app after being offline, THE `LivesManager` SHALL compute `floor((currentUTC - storedNextLifeUTC) / 30 minutes)` to determine lives earned while offline, add them capped at 5, and advance `storedNextLifeUTC` by `earnedLives * 30 minutes`.
9. WHILE lives == 5, THE `LivesManager` SHALL not decrement the regeneration timer and SHALL not update `storedNextLifeUTC` — the timer only runs while lives < 5.
10. THE `SaveService` SHALL store coin balance (int), skin unlocks (bool[] indexed by skin ID), and power-up inventory counts (int[] indexed by PowerUpType) as part of the same JSON save structure.

### Requirement 20: Performance Targets

**User Story:** As a developer, I want the game to maintain 60fps with zero GC allocation during active gameplay.

#### Acceptance Criteria

1. THE AnimalFall_System SHALL set `Application.targetFrameRate = 60` in a bootstrap MonoBehaviour's `Awake` method.
2. WHILE active gameplay is running (from first animal spawn through the level-end trigger or app pause, whichever comes first), THE profiler `GC Alloc` column SHALL read 0 B per frame — verified by a 60-second Profiler recording during level play.
3. THE `Spawner.SpawnLoop()` coroutine body SHALL not contain `new WaitForSeconds(…)` — the `WaitForSeconds` instance SHALL be created once in `Start()` and reused each loop iteration.
4. THE `HindranceManager.HindranceSpawnLoop()` coroutine body SHALL not contain `new WaitForSeconds(…)` — a cached instance SHALL be used.
5. THE `AnimalMovement.Update()` method SHALL store `EnvironmentEffects.Instance` in a local variable at the top of the method body and use that local variable for all subsequent accesses within the same Update call.
6. THE `ImageLibrary` sprite cache SHALL be fully populated before the first `Spawner.SpawnLoop()` iteration begins — no `Resources.Load` calls SHALL occur after level spawning starts.
7. ALL `System.Action` event subscriptions SHALL use the symmetric lifecycle pattern: subscribe in `OnEnable` (or `Start`) and unsubscribe in the matching `OnDisable` (or `OnDestroy`).
8. WHILE the device battery level is ≤ 20% and the device is discharging, THE AnimalFall_System SHALL set `Application.targetFrameRate = 30` to reduce battery consumption.

### Requirement 21: Event Bus — Decoupled C# Events

**User Story:** As a developer, I want all game-state communication to go through a decoupled event bus.

#### Acceptance Criteria

1. THE `GameEvents` static class SHALL define `System.Action` events: `OnAnimalCollected(AnimalType type)`, `OnWrongTap`, `OnBombTapped`, `OnLevelStarted`, `OnLevelWon`, `OnLevelFailed`, `OnTimerWarning`, `OnComboChanged(int combo, float multiplier)`, `OnHindranceActivated(HindranceType type)`, `OnHindranceDeactivated(HindranceType type)`.
2. THE `GameUIManager` SHALL subscribe to `GameEvents` events in `OnEnable` and unsubscribe in `OnDisable`; it SHALL NOT accept direct method calls from `GameManager` or `ScoreManager`.
3. THE `AudioManager` SHALL subscribe to audio-relevant `GameEvents` events (`OnAnimalCollected`, `OnWrongTap`, `OnBombTapped`, `OnLevelWon`, `OnLevelFailed`) in `OnEnable` and unsubscribe in `OnDisable`.
4. EACH component subscribing to `GameEvents` SHALL unsubscribe in `OnDisable` or `OnDestroy`; a component SHALL NOT subscribe without a corresponding unsubscribe.
5. THE designer-facing `UnityEvent` fields (`onLevelStart`, `onLevelWin`, `onLevelFail`) in `GameManager` SHALL be kept for Inspector hooks only; all code-to-code communication SHALL use `System.Action` events from `GameEvents`.
6. `GameEvents.OnAnimalCollected` SHALL carry the `AnimalType` as a parameter so subscribers can distinguish species, type, and combo state.
7. IF a `GameEvents` static event is invoked and no subscribers are registered, THE invocation SHALL be a silent no-op — no exception SHALL be thrown.

### Requirement 22: Input System — Gesture Detection

**User Story:** As a player, I want tap and swipe inputs to be detected accurately regardless of screen size.

#### Acceptance Criteria

1. THE `InputManager` SHALL convert each touch/click position to world space via `Camera.main.ScreenToWorldPoint` and query `Physics2D.OverlapPoint(worldPos)` to detect animal hits; if `Camera.main` is null, input SHALL be silently ignored with a logged warning.
2. WHEN a touch enters the `Began` phase, THE `InputManager` SHALL fire `GameEvents.OnScreenTapped(Vector2 worldPosition)` exactly once — it SHALL NOT fire on the `Held` or `Ended` phase.
3. WHILE `MagnetTrap` hindrance is active, THE `InputManager` SHALL add `HindranceManager.GetActiveMagnetOffset()` to the computed world tap position before firing `GameEvents.OnScreenTapped`.
4. WHILE `MirrorMode` hindrance is active, THE `InputManager` SHALL negate the X component of the computed world tap position before firing `GameEvents.OnScreenTapped`.
5. THE `GestureDetector` SHALL register a swipe when a touch moves ≥ 80 screen-pixels within a single touch (from `Began` to `Ended`) in ≤ 0.4 seconds, and SHALL fire `GameEvents.OnSwipeDetected(Vector2 direction)`.
6. WHEN a swipe gesture is detected, THE `GestureDetector` SHALL fire `GameEvents.OnSwipeDetected` instead of (not in addition to) `GameEvents.OnScreenTapped` for that touch — a single gesture SHALL trigger at most one event type.

### Requirement 23: Scene Architecture — GameScene Isolation

**User Story:** As a developer, I want the game scene to be fully self-contained with no stale object references between levels.

#### Acceptance Criteria

1. THE `GameManager` MonoBehaviour (and all its child GameObjects) SHALL NOT call `DontDestroyOnLoad`; they SHALL be created fresh each time `GameScene` is loaded and destroyed when `GameScene` is unloaded.
2. THE `LevelManager` SHALL be the only MonoBehaviour with `DontDestroyOnLoad`; no other MonoBehaviour in the project SHALL call `DontDestroyOnLoad`.
3. WHEN `GameScene` is unloaded, THE `ObjectPooler` SHALL call `ReturnToPool` on all currently active animal and hindrance GameObjects before the scene is destroyed, to prevent pool leaks between sessions.
4. WHEN `GameManager.StartLevel(LevelData level)` is called, THE `GameManager` SHALL call `Reset()` (or equivalent) on `ScoreManager`, `ComboManager`, `PowerUpManager`, and `HindranceManager` in sequence before starting any coroutines.
5. WHEN a level ends (win or fail), THE `Spawner.StopSpawning()` SHALL be called before the results popup is shown.
6. IF `GameManager.StartLevel` is called while a level is already running, THEN THE `GameManager` SHALL log `Debug.LogError("StartLevel called on active level")` and return without starting a second level.

### Requirement 24: Code Style & Architecture Standards

**User Story:** As a developer, I want the codebase to follow consistent naming conventions and architecture rules.

#### Acceptance Criteria

1. ALL private fields SHALL use `_camelCase` naming; ALL public properties and methods SHALL use `PascalCase`.
2. ALL `[SerializeField]` private fields exposed to the Inspector SHALL have a `[Tooltip("…")]` attribute with a non-empty, meaningful description (not just the field name restated).
3. ALL MonoBehaviour methods exceeding 50 lines SHALL be refactored into single-responsibility private helper methods.
4. THE namespace for all Animal Fall scripts SHALL follow `AnimalFall.<Layer>` sub-namespaces: `AnimalFall.Core` (animals, pooling), `AnimalFall.Managers` (game, score, combo), `AnimalFall.UI` (HUD, popups), `AnimalFall.Hindrances`, `AnimalFall.Data` (ScriptableObjects).
5. THE `ImageLibrary` class SHALL reside at `Assets/Scripts/Utils/ImageLibrary.cs`; all sprite access from game code SHALL call `ImageLibrary` methods — direct `Resources.Load<Sprite>()` calls outside `ImageLibrary` SHALL be treated as a build-breaking error.
6. ALL Editor-only code (level generation, debug context menus, custom inspectors) SHALL be inside `#if UNITY_EDITOR` compile guards or in scripts residing in a folder named `Editor`; violating this rule breaks non-editor builds.

---

## Correctness Properties

### P1 — Pool Round-Trip (PBT)
FOR ALL animals spawned during a level, `ObjectPooler.ActiveCount(animalPrefab)` AFTER the animal is collected or misses SHALL equal the count BEFORE it was spawned.
*Rationale: Verifies no objects leak out of the pool.*

### P2 — Hindrance Count Invariant (PBT)
FOR ALL game states during active play, `HindranceManager.GetActiveHindrances().Count` SHALL never exceed `currentLevel.maxHindrancesActive`.
*Rationale: Invariant — active hindrance count is always ≤ configured cap.*

### P3 — Timer Never Negative (Example)
WHEN `wrongTapTimePenalty` is applied to a timer at `0.1f` seconds, `remainingTime` SHALL be clamped to `0f` and level failure SHALL be triggered — not a negative timer.

### P4 — Star Rating Monotonicity (PBT)
FOR ALL pairs `(rescued1, rescued2)` where `rescued1 > rescued2` (same target, same time), `CalculateStars(rescued1, target, time, totalTime) >= CalculateStars(rescued2, target, time, totalTime)`.
*Rationale: Rescuing more animals should never produce fewer stars.*

### P5 — Combo Pitch Sequence (Example)
GIVEN 5 consecutive correct taps, the pitch values played SHALL be `[0.95, 1.0, 1.05, 1.1, 1.15]` in order, verifiable via `AudioManager.LastPlayedPitch` per tap.

### P6 — LevelDatabase Completeness (Example)
`LevelDatabase.TotalLevels` SHALL equal exactly 50, and every index `0..49` SHALL return a non-null `LevelData` with `levelNumber == index + 1`.

### P7 — Species Goal Sum Invariant (PBT)
FOR ALL `LevelData` assets, `levelData.goal.TotalCount` SHALL be ≥ 1 and ≤ 50, and the `spawnPool` SHALL contain at least one `AnimalData` with `isTargetSpecies == true` for each species referenced in the goal.

### P8 — Difficulty Parameter Monotonicity (PBT)
FOR ALL consecutive level pairs `(N, N+1)`, the `timeLimit` of level N+1 SHALL be ≤ `timeLimit` of level N, and the `spawnInterval` of level N+1 SHALL be ≤ `spawnInterval` of level N — difficulty never decreases between consecutive levels.

### P9 — Lives Regeneration Capped At 5 (PBT)
FOR ALL offline durations D (in minutes), `livesAfterOffline = min(5, livesBeforeOffline + floor(D / 30))` SHALL hold exactly.
