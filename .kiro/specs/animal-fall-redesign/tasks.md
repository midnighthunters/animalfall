# Implementation Plan: Animal Fall Redesign

## Overview

Ground-up redesign of Animal Fall as a 50-level tap-to-save mobile game. Implementation follows
the dependency order: Data ScriptableObjects → Core infrastructure → Animal system → Hindrance system
→ Managers → Effects → MegaLevel → UI → Level generation → Bootstrap/Services → Property-based tests.
All code uses C#, namespace `AnimalFall.<Layer>`, DOTween for animations, and ObjectPooler for all
runtime GameObjects. Visual assets are sourced exclusively from the AnimalBlast project.

---

## Tasks

- [ ] 1. Data Layer — ScriptableObject Definitions
  - [ ] 1.1 Rewrite `LevelData` ScriptableObject
    - Create `Assets/Scripts/Data/LevelData.cs` in namespace `AnimalFall.Data`
    - Add all `[SerializeField]` fields with `[Tooltip]` and `[Range]` bounds per design spec
    - Include `HindranceConfig` nested serializable class with `type`, `weight`, `initialDelay`
    - Add public read-only property accessors for every field
    - _Requirements: 4.1, 4.2, 6.1, 6.4, 6.5, 24.1, 24.2_

  - [ ] 1.2 Create `GoalData` ScriptableObject
    - Create `Assets/Scripts/Data/Goals/GoalData.cs` in namespace `AnimalFall.Data`
    - Add `SpeciesTarget` nested struct with `species` and `count` fields
    - Implement `TotalCount` property using a `for` loop (no LINQ)
    - _Requirements: 4.5, 7.7 (P7 invariant support)_

  - [ ] 1.3 Update `AnimalData` ScriptableObject fields
    - Update `Assets/Scripts/Data/Animals/AnimalData.cs`
    - Add `movementPattern`, `zigzagAmplitude`, `zigzagFrequency`, `lifetime`, `shieldHP` fields
    - Add `[Tooltip]` and `[Range]` to all designer-facing fields
    - _Requirements: 3.4, 24.2_

  - [ ] 1.4 Create `VillainData` ScriptableObject
    - Create `Assets/Scripts/Data/MegaLevel/VillainData.cs` in namespace `AnimalFall.Data`
    - Add `villainName`, `portrait`, `hpPhases`, `animalsPerPhase[]`, `projectileFrequencyPerPhase[]`, `projectilePrefab`
    - _Requirements: 10.1, 10.2, 4.6_

  - [ ] 1.5 Create `ChapterConfig` ScriptableObject
    - Create `Assets/Scripts/Data/Levels/ChapterConfig.cs` in namespace `AnimalFall.Data`
    - Add `chapterIndex`, `chapterName`, `backgroundColor`, `backgroundSprite`, `firstLevel`, `lastLevel`, `focusSpecies[]`
    - _Requirements: 5.1, 5.3_

  - [ ] 1.6 Create `PowerUpData` ScriptableObject
    - Create `Assets/Scripts/Data/PowerUps/PowerUpData.cs` in namespace `AnimalFall.Data`
    - Define `PowerUpType` enum: `SlowTime, Magnet, MultiTap, AutoTap, FreezeAll`
    - Add `powerUpType`, `icon`, `cooldown`, `duration`, `radius`, `charges` fields with `[Tooltip]`/`[Range]`
    - _Requirements: 14.1, 14.7_

  - [ ] 1.7 Rewrite `LevelDatabase` ScriptableObject skeleton
    - Create `Assets/Scripts/Data/Levels/LevelDatabase.cs` in namespace `AnimalFall.Data`
    - Add `_levels` array, `TotalLevels` property, `GetLevel(int)` with bounds check and `Debug.LogError`
    - Add `#if UNITY_EDITOR` stub for `GenerateAndSave50Levels()` context menu (implementation in Task 9)
    - _Requirements: 4.1, 4.3, 4.4_

- [ ] 2. Core Infrastructure
  - [ ] 2.1 Rewrite `AnimalEnums` — add 7 new species and full type/pattern enums
    - Rewrite `Assets/Scripts/Core/Animals/AnimalEnums.cs` in namespace `AnimalFall.Core.Animals`
    - `AnimalSpecies`: `None, Chicken, Dog, Cow, Cat, Monkey, Pig, Rabbit, Penguin, Owl, Mouse, Zebra, Duck` (13 values, 12 playable)
    - `AnimalType`: `Normal, Decoy, Bomb, Shielded, Golden, Special, Paired, Ghost, Bubble, IceCube, Shrinking, Rainbow, FakeAnimal, CursedSkull, ThiefBird`
    - `MovementPattern`: `Static, Drift, ZigZag, SineWave, Bounce, Teleport, FloatUp, HeavyFall, Erratic`
    - `TapResult`: `Correct, Wrong, BombExploded, ShieldBroken, Golden, Rainbow, FakeCollected, IceCubeFrozen, PairedWaiting, CursedSkullDestroyed, GhostMissed, BubblePopped`
    - _Requirements: 3.1, 24.4_

  - [ ] 2.2 Create `ObjectPooler` — zero-GC pool manager
    - Create `Assets/Scripts/Core/ObjectPooler.cs` in namespace `AnimalFall.Core`
    - Implement `Dictionary<int, Stack<GameObject>> _pools` keyed by prefab `InstanceID`
    - Implement `HashSet<int> _activeObjects` for O(1) double-return guard
    - `CreatePool(prefab, initialSize, parent)` — called at level load only
    - `SpawnFromPool(prefab, pos, rot, parent)` — expands pool if empty with `Debug.LogWarning`
    - `ReturnToPool(obj)` — double-return is a no-op + `Debug.LogWarning`
    - `ReturnAllActive(prefab)` — called on scene unload
    - `ActiveCount(prefab)` — returns active object count for prefab
    - `ResetObject(obj)` — resets `localScale`, `color`, calls `DOTween.Kill`, `StopAllCoroutines`, sets inactive
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 23.3_

  - [ ] 2.3 Rewrite `ImageLibrary` — sprite cache loading from AnimalBlast assets
    - Rewrite `Assets/Scripts/Utils/ImageLibrary.cs` in namespace `AnimalFall.Utils`
    - Static `Sprite[]` of length 12 for animal sprites; static `Dictionary<HindranceType, Sprite>` for hindrances
    - `LoadAll()` public method: calls `LoadAnimalSprites()`, `LoadHindranceSprites()`, `LoadUISprites()`
    - `LoadAnimalSprites()`: load 12 sprites from `icons/animals/` paths; assign placeholder + `Debug.LogError` on miss
    - `LoadHindranceSprites()`: load all 20 hindrance sprites using path map from design; log missing
    - `LoadUISprites()`: load panel, panel2, red_buttons, levelbutton1/2, clock, cooldown ring sprites
    - `GetAnimalSprite(AnimalSpecies)`, `GetHindranceSprite(HindranceType)`, all panel/UI accessors — no `Resources.Load` in accessors
    - _Requirements: 3.2, 3.3, 7.5, 20.6, 24.5_

- [ ] 3. Animal System
  - [ ] 3.1 Rewrite `AnimalMovement` — cached bounds, local env variable, pool return
    - Rewrite `Assets/Scripts/Core/Animals/AnimalMovement.cs` in namespace `AnimalFall.Core.Animals`
    - `Awake()`: cache `_cachedScreenWidth`, `_cachedScreenHeight`; call `RecalcBounds()` once
    - `Update()`: store `EnvironmentEffects.Instance` in one local variable at the top; no repeated `.Instance` access
    - Dirty-flag bounds check: re-call `RecalcBounds()` only when `Screen.width` or `Screen.height` changes
    - Apply `IsZeroGravityActive` (float to 0), `WindForce`, `BlackHolePull`, `IsMirrorModeActive` (negate X) from local var
    - `BubbleShield` animal: apply positive Y velocity when `animal.IsBubble == true`
    - Off-screen bottom: call `ObjectPooler.Instance.ReturnToPool(gameObject)` — not `Destroy`
    - `Configure(AnimalData, LevelData)`: reset movement state; do NOT re-calc bounds
    - `FreezeAll` power-up: `enabled = false` for duration
    - _Requirements: 2.3, 2.4, 2.6, 8.6, 14.6, 20.5_

  - [ ] 3.2 Rewrite `Animal` MonoBehaviour — pool setup, tap handling, lifetime coroutine
    - Rewrite `Assets/Scripts/Core/Animals/Animal.cs` in namespace `AnimalFall.Core.Animals`
    - `[RequireComponent(SpriteRenderer, Collider2D, AnimalMovement)]`
    - `SetupForPool(AnimalData, LevelData)`: stop any prior `_lifetimeCoroutine`, reset `_isReturned`, all hindrance state, assign sprite via `ImageLibrary.GetAnimalSprite`, start new `_lifetimeCoroutine`
    - `LifetimeCoroutine(float)`: yield pre-allocated `WaitForSeconds`; on expire call `ReturnToPool()`
    - `HandleTap()` → `TapResult`: respect `_isReturned`/`IsCollected` guard; handle `AnimalType` logic per Req 8 (Shielded flash, IceCube SFX, Helmet layers)
    - `OnCollected()`: kill existing tweens; squash `(1.3, 0.7, 1)` → `(1, 1, 1)` via DOTween OutElastic 0.1s; `ReturnToPool` on complete
    - Shielded flash: DOTween yoyo yellow outline on non-final-shield tap; stop on final shield tap
    - `ReturnToPool()`: double-return guard, stop coroutine, `DOTween.Kill`, call `ObjectPooler.Instance.ReturnToPool`
    - Public state: `Data`, `IsCollected`, `IsPaired`, `PairedPartner`, `HelmetLayers`, `IsIceFrozen`, `IsBubble`, `GhostAlpha`, `PairedTimer`
    - _Requirements: 1.3, 1.7, 2.2, 2.5, 2.7, 2.9, 3.3, 8.5, 8.6, 8.7, 8.8_

  - [ ] 3.3 Rewrite `Spawner` — ObjectPooler, pre-allocated array, cached WaitForSeconds
    - Rewrite `Assets/Scripts/Core/Animals/Spawner.cs` in namespace `AnimalFall.Core.Animals`
    - `Setup(LevelData)`: allocate `_cachedPool[spawnPool.Length]` once; log `Debug.LogError` and return if length is 0
    - `StartSpawning()`: allocate `_spawnWait = new WaitForSeconds(level.SpawnInterval)` once
    - `SpawnLoop()` coroutine: check `ObjectPooler.ActiveCount < maxOnScreen`; call `SpawnOne()`; `yield _spawnWait` — no new allocation inside loop
    - `SpawnOne()`: call `ObjectPooler.Instance.SpawnFromPool`; call `animal.SetupForPool`; DOTween entrance `(0.1,0.1,1)→(1,1,1)` over 0.25s `Ease.OutBack`
    - `ChooseAnimalData()`: `for` loop only on `_cachedPool`; no `Array.Find`, `FindAll`, `.Where`, `.Select`, `.ToList`; log `Debug.LogError` if uninitialized
    - `StopSpawning()`: stop coroutine, set flag
    - _Requirements: 1.2, 1.8, 2.1, 3.4, 3.5, 20.3_

- [ ] 4. Hindrance System — Interfaces, Base, Factory, and Context
  - [ ] 4.1 Create `IHindrance` interface, `HindranceContext` struct, and `HindranceBase`
    - Create `Assets/Scripts/Core/Hindrances/IHindrance.cs`: `Activate(HindranceContext)`, `Deactivate()`, `Type` property
    - Create `Assets/Scripts/Core/Hindrances/HindranceContext.cs` struct: fields for `GameManager`, `HindranceManager`, `EnvironmentEffects`, `ScreenEffects`, `AudioManager`, `LivesManager`, `InputManager`
    - Rewrite `Assets/Scripts/Core/Hindrances/HindranceBase.cs`: `abstract` MonoBehaviour implementing `IHindrance`
    - `Activate()`: sets `_ctx`, `_isActive = true`, calls `OnActivate()`
    - `Deactivate()`: guard `!_isActive`; set false; call `OnDeactivate()`; `DOTween.Kill`; `ObjectPooler.ReturnToPool` — never `Destroy`
    - `protected abstract OnActivate()` and `OnDeactivate()`
    - _Requirements: 7.2, 7.4_

  - [ ] 4.2 Rewrite `HindranceFactory`
    - Rewrite `Assets/Scripts/Core/Hindrances/HindranceFactory.cs`
    - `CreateAtRandomScreenTop(HindranceData, Transform)`: use `Camera.main.ViewportToWorldPoint` for random top position
    - Call `ObjectPooler.Instance.SpawnFromPool` — never `Instantiate`
    - Null-check prefab: log `Debug.LogWarning` and return null
    - Null-check `IHindrance` on spawned object: log `Debug.LogError`, return to pool, return null
    - _Requirements: 7.3_

  - [ ] 4.3 Implement Penalty hindrances (4 types)
    - `BombHindrance.cs` (`Penalties/`): falls via translate; tapped → `GameEvents.OnBombTapped?.Invoke()`; calls `Deactivate()`; missed → `Deactivate()`
    - `AlarmClockHindrance.cs`: `OnActivate` sets `SpawnIntervalMultiplier = 0.6f`, starts 5s coroutine; re-activation resets timer (no stacking); `OnDeactivate` restores multiplier
    - `PoisonVialHindrance.cs`: tapped → `_ctx.LivesManager.UseLife()`, `AudioManager.PlaySFX(WrongTap)`, `Deactivate()`
    - `ThiefBirdHindrance.cs`: query `HindranceManager.GetRandomActiveAnimal()`; if null → `Deactivate()`; else DOTween stolen animal X off-screen 1.5s; `OnComplete` → `ObjectPooler.ReturnToPool(stolen)`; then `Deactivate()` self
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [ ] 4.4 Implement TapModifier hindrances (4 types)
    - `KnightHelmetHindrance.cs` (`TapModifiers/`): set `animal.HelmetLayers = 3`; attach helmet overlay child sprite; each tap decrements + scale-bounce DOTween `(1.15, 0.85, 1)→(1,1,1)`; at 0 → normal collection; `OnDeactivate` removes overlay if animal still on screen
    - `BubbleShieldHindrance.cs`: set `animal.IsBubble = true`; first tap → `IsBubble = false`, resume fall, `Deactivate()`
    - `IceCubeHindrance.cs`: set `animal.IsIceFrozen = true`, ice overlay; plain tap → `SfxType.ShieldHit`, no other effect; swipe ≥80px ≤0.4s → `IsIceFrozen = false`, DOTween shake, remove overlay, `Deactivate()`
    - `GhostAnimalHindrance.cs`: DOTween alpha 1.0→0.2 over 0.5s on animal `_sr`; `OnDeactivate` is no-op (ObjectPooler.ResetObject restores alpha on return)
    - _Requirements: 8.5, 8.6, 8.7, 8.8_

  - [ ] 4.5 Implement ScreenBlocker hindrances (4 types)
    - `InkSquidHindrance.cs` (`ScreenBlockers/`): `_ctx.ScreenEffects.ShowInkOverlay(4f)`; `OnDeactivate` fades out 1s, return overlay to pool
    - `StormCloudHindrance.cs`: `_ctx.ScreenEffects.ShowStormGradient(6f)`; `OnDeactivate` hides + returns gradient
    - `FlashbangHindrance.cs`: `_ctx.ScreenEffects.FlashWhite()`; spawn `Explosion_1_Zap.prefab` via `EffectsController`; `OnDeactivate` is no-op (one-shot)
    - `FallingLeavesHindrance.cs`: spawn exactly 20 pooled leaf objects; each DOTween drift 5s then `ReturnToPool`; `OnDeactivate` returns any still-active leaves immediately
    - _Requirements: 8.9, 8.10, 8.11, 8.12_

  - [ ] 4.6 Implement EnvironmentMod hindrances (4 types)
    - `WindGustHindrance.cs` (`EnvironmentMods/`): set `EnvironmentEffects.WindForce` to random 2D vector magnitude 1.5–3.0; `OnDeactivate` → `WindForce = Vector2.zero`
    - `ZeroGravityHindrance.cs`: set `IsZeroGravityActive = true`; 4s coroutine; on end set false, `Deactivate()`; `OnDeactivate` guard-sets false
    - `BlackHoleHindrance.cs`: set random on-screen `BlackHoleCenter`, `IsBlackHoleActive = true`, show sprite; `AnimalMovement.Update` handles pull (1.5 units/s²) and consume-at-0.5-units via `GameEvents.OnAnimalMissed`; `OnDeactivate` → `IsBlackHoleActive = false`
    - `TornadoHindrance.cs`: DOTween translate tornado sprite horizontally; `Update()` while active: overlap all animals, apply 2.0 units/s force away from center; `OnDeactivate` returns tornado to pool
    - _Requirements: 8.13, 8.14, 8.15, 8.16_

  - [ ] 4.7 Implement Advanced hindrances (4 types)
    - `MagnetTrapHindrance.cs` (`Advanced/`): random offset `Vector2` 0.3–0.8 magnitude; `InputManager.SetMagnetOffset(_offset)`; `OnDeactivate` → `SetMagnetOffset(Vector2.zero)`
    - `MirrorModeHindrance.cs`: `InputManager.SetMirrorMode(true)`, `EnvironmentEffects.IsMirrorModeActive = true`; 8s coroutine; `OnDeactivate` restores both
    - `CursedSkullHindrance.cs`: falls like normal; tapped → `GameManager.AddTime(+2f)` capped at original `timeLimit`, `Deactivate()`; missed bottom → `GameManager.AddTime(-5f)` clamped to 0, `Deactivate()`
    - `PairedAnimalHindrance.cs`: spawn 2 animals via `ObjectPooler`; set `IsPaired`, `PairedPartner` on both; 2s window coroutine; both tapped → normal collection; one tapped → `GameManager.OnWrongTap()`, return untapped to pool; neither → return both, no penalty
    - _Requirements: 8.17, 8.18, 8.19, 8.20, 8.21_

- [ ] 5. Checkpoint — Data, Core, and Hindrance foundation complete
  - Ensure all scripts in Tasks 1–4 compile without errors (validate with Unity console)
  - Ensure `ObjectPooler`, `ImageLibrary`, and all `HindranceBase` subclasses are accessible from the `AnimalFall.*` namespaces
  - Ask the user if questions arise before proceeding.

- [ ] 6. Managers Layer
  - [ ] 6.1 Create `GameEvents` static event bus
    - Create `Assets/Scripts/Managers/GameEvents.cs` in namespace `AnimalFall.Managers`
    - Define all `static System.Action` events: `OnAnimalCollected(AnimalType)`, `OnWrongTap`, `OnBombTapped`, `OnAnimalMissed`, `OnLevelStarted`, `OnLevelWon`, `OnLevelFailed`, `OnTimerWarning`
    - `OnComboChanged(int, float)`, `OnScoreChanged(int)`, `OnHindranceActivated(HindranceType)`, `OnHindranceDeactivated(HindranceType)`
    - `OnVillainPhaseChanged(int, int)`, `OnScreenTapped(Vector2)`, `OnSwipeDetected(Vector2)`, `OnStarsCalculated(int, int, float, float)`
    - All invocations use null-conditional `?.Invoke()` — silent no-op when no subscribers
    - _Requirements: 21.1, 21.6, 21.7_

  - [ ] 6.2 Rewrite `ScoreManager`
    - Rewrite `Assets/Scripts/Managers/ScoreManager.cs` in namespace `AnimalFall.Managers`
    - `ResetScore()`, `AddPoints(int)` — fires `GameEvents.OnScoreChanged`
    - `SetComboMultiplier(float)`, `GetScore()`
    - `CalculateStars(int rescued, int target, float timeRemaining, float totalTime)` — sole authoritative method; 3-star, 2-star, 1-star, 0-star logic per Req 11.1
    - _Requirements: 11.1, 11.4_

  - [ ] 6.3 Rewrite `ComboManager` from stub
    - Rewrite `Assets/Scripts/Managers/ComboManager.cs` in namespace `AnimalFall.Managers`
    - Static `readonly` arrays: `PITCH_STEPS = {0.95f, 1.0f, 1.05f, 1.1f, 1.15f}`, `COMBO_THRESHOLDS = {3, 6, 10, 15}`, `COMBO_MULTIPLIERS = {1.5f, 2.0f, 3.0f, 5.0f}`
    - `OnCorrect()`: increment `_combo`, update `_pitchIndex = Min(_combo-1, 4)`, compute multiplier, call `_scoreManager.SetComboMultiplier`, call `_audioManager.PlaySFX(Collect, pitch)`, fire `GameEvents.OnComboChanged`, check `_combo == 10` for gold border flash
    - `ResetCombo()`: reset all fields, fire `GameEvents.OnComboChanged(0, 1.0f)`
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 18.5_

  - [ ] 6.4 Rewrite `AudioManager` — 12 pooled sources, SfxType enum, pitch modulation
    - Rewrite `Assets/Scripts/Managers/AudioManager.cs` in namespace `AnimalFall.Managers`
    - Define `SfxType` enum: `Collect, WrongTap, Explosion, ComboUp, MegaCombo, LevelWin, LevelLose, HindranceActivate, ShieldHit, PowerUpActivate, TimerWarning`
    - Pool of exactly 12 `AudioSource` children — no `AudioSource` on individual animals
    - `PlaySFX(SfxType, float pitch = 1f)`: borrow first idle source; interrupt oldest-playing if all 12 busy; null clip → silent skip
    - `MegaCombo` flow: `AudioMixer.TransitionToSnapshot("Duck", 0.5f)`, restore "Normal" after clip length
    - Subscribe in `OnEnable`/unsubscribe in `OnDisable`: `OnAnimalCollected`, `OnWrongTap`, `OnBombTapped`, `OnLevelWon`, `OnLevelFailed`
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 21.3_

  - [ ] 6.5 Rewrite `HindranceManager`
    - Rewrite `Assets/Scripts/Managers/HindranceManager.cs` in namespace `AnimalFall.Managers`
    - Pre-allocated `List<IHindrance>` with capacity `maxActive`; `float[]` cumulative weights table
    - `InitForLevel(LevelData)`: clear list, `BuildWeightTable()` with unlock-level filter + `Debug.LogWarning` for skipped entries, allocate `_cachedWait`
    - `HindranceSpawnLoop()`: initial delay wait, then loop — if `_activeHindrances.Count < _maxActive` call `TrySpawnHindrance()`; `yield _cachedWait` — no new allocation inside loop
    - `TrySpawnHindrance()`: weighted random via `for` loop on cumulative weights (no LINQ); call `HindranceFactory`; `hindrance.Activate(BuildContext())`; add to list; fire `GameEvents.OnHindranceActivated`
    - `OnHindranceDeactivated(IHindrance)`: remove from list, fire `GameEvents.OnHindranceDeactivated`
    - `GetRandomActiveAnimal()`, `GetActiveMagnetOffset()`, `SetMirrorMode(bool)`, `SetSpawnIntervalMultiplier(float)`
    - `GetActiveHindrances()` → returns `_activeHindrances` read-only reference (for P2 test)
    - _Requirements: 7.6, 7.7, 9.2, 20.4, 21.4_

  - [ ] 6.6 Rewrite `InputManager` with `GestureDetector`
    - Rewrite `Assets/Scripts/Managers/InputManager.cs` in namespace `AnimalFall.Managers`
    - `Update()`: process `TouchPhase.Began` → `ProcessTouchBegan`; `TouchPhase.Ended` → `ProcessTouchEnded`; `#if UNITY_EDITOR` mouse fallback
    - `ProcessTouchBegan(Vector2 screenPos)`: `Camera.main.ScreenToWorldPoint`; apply mirror negate X; apply magnet offset; `Physics2D.OverlapPoint`; cache `_pendingAnimal`, `_pendingTapWorld`
    - `ProcessTouchEnded(Touch)`: classify via `GestureDetector`; if swipe → `GameEvents.OnSwipeDetected` only; else → `HandleTap` + `GameEvents.OnScreenTapped`
    - `Camera.main == null` → `Debug.LogWarning` and return
    - `SetMagnetOffset(Vector2)`, `SetMirrorMode(bool)` called by hindrance system
    - Create `Assets/Scripts/Utils/GestureDetector.cs`: swipe = dist ≥ 80px AND dur ≤ 0.4s
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6_

  - [ ] 6.7 Rewrite `LevelManager` — DontDestroyOnLoad, scene loading, pool pre-warm
    - Rewrite `Assets/Scripts/Managers/LevelManager.cs` in namespace `AnimalFall.Managers`
    - `DontDestroyOnLoad` in `Awake` — only MonoBehaviour to do so
    - `LoadGameSceneForLevel(int index)`: validate 0–49 with `Debug.LogError` on out-of-range; retrieve `LevelDatabase.GetLevel(index)`; call `PrewarmPoolsForLevel(level)`; load `GameScene`
    - `PrewarmPoolsForLevel(LevelData)`: pool animal prefabs (`maxOnScreen + 2`), hindrance prefabs (`maxActive + 1`), VFX prefabs (10/3/3), floating text (10); null prefab entries → `Debug.LogWarning` and skip
    - Call `ImageLibrary.LoadAll()` inside `PrewarmPoolsForLevel`
    - `LevelSuccess()`: unlock next level, save to `SaveService`
    - `LevelFailed()`: trigger lives deduction
    - _Requirements: 1.6, 4.4, 23.2, 20.6_

  - [ ] 6.8 Rewrite `GameManager` — state machine, level flow, no DontDestroyOnLoad
    - Rewrite `Assets/Scripts/Managers/GameManager.cs` in namespace `AnimalFall.Managers`
    - State machine: `Idle → ShowingIntro → Countdown → Running → Ended`
    - `DontDestroyOnLoad` REMOVED — fresh each scene load
    - `StartLevel(LevelData)`: guard against double-call (`Debug.LogError`, return); call `Reset()` on ScoreManager, ComboManager, PowerUpManager, HindranceManager; tween camera background via `DOTween.Kill(_camera)` then `_camera.DOColor(chapterColor, 0.5f)`; show intro overlay, begin countdown via `CountdownController`
    - `OnWrongTap()`, `AddTime(float)`, `OnMegaLevelComplete()`, `EndLevel(bool won)`
    - Fires `GameEvents.OnLevelStarted`, `OnLevelWon`, `OnLevelFailed`, `OnTimerWarning` (at <10s) — never calls UI directly after `Setup()`
    - `CalculateStars` delegated to `ScoreManager.CalculateStars()`
    - Chapter background tween: kill prior tween first; MegaLevel: call `MegaLevelController.InitMegaLevel(level)`
    - All refs wired via `[SerializeField]` Inspector — no `FindObjectOfType`
    - _Requirements: 5.2, 5.5, 10.1, 11.1, 16.1, 23.1, 23.4, 23.5, 23.6_

  - [ ] 6.9 Rewrite `PowerUpManager` from stub — 5 power-up implementations
    - Rewrite `Assets/Scripts/Managers/PowerUpManager.cs` in namespace `AnimalFall.Managers`
    - `SlowTimePowerUp.Activate()`: if active, kill + restart; `Time.timeScale = 0.5f`; coroutine restores 1.0f after duration
    - `MagnetPowerUp.Activate()`: query active animals from `ObjectPooler`; DOTween each to screen center 1.5s; `OnComplete` → `animal.OnCollected()` (skip already-collected)
    - `MultiTapPowerUp.Activate()`: set `_charges = data.charges`; subscribe to `GameEvents.OnScreenTapped`; on tap `Physics2D.OverlapCircleAll` (pre-alloc buffer), collect all in radius; decrement charges; unsubscribe at 0
    - `AutoTapPowerUp.Activate()`: coroutine every 0.4s; query random active animal; call `OnCollected()` if found; skip tick if no animals
    - `FreezeAllPowerUp.Activate()`: disable all `AnimalMovement.enabled`; animals remain tappable; re-enable after duration
    - Cooldown ring UI updates wired via `GameEvents`
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_

  - [ ] 6.10 Rewrite `LivesManager` — lives cap, regen timer, offline catch-up
    - Rewrite `Assets/Scripts/Managers/LivesManager.cs` in namespace `AnimalFall.Managers`
    - `const MAX_LIVES = 5`, `const REGEN_MINUTES = 30`
    - `HasLives()` → `_currentLives > 0`
    - `UseLife()`: guard at 0; decrement; if `< MAX_LIVES && !_timerRunning` start regen; call `SaveService.SaveLives`
    - `ComputeOfflineLives(int startLives, double offlineMinutes)` → `Min(5, startLives + FloorToInt(offlineMinutes / 30))`
    - `Awake` / `OnAppResume`: compute UTC delta; add earned lives clamped to 5; advance `_nextLifeUTC` by earned × 30min
    - While `_currentLives == MAX_LIVES`: pause timer, do NOT update `_nextLifeUTC`
    - _Requirements: 19.3, 19.4, 19.5, 19.6, 19.7, 19.8, 19.9_

- [ ] 7. Effects Layer
  - [ ] 7.1 Rewrite `EffectsController` — GameEvents subscriber, VFX spawning
    - Rewrite `Assets/Scripts/Effects/EffectsController.cs` in namespace `AnimalFall.Effects`
    - `OnEnable()`: subscribe `GameEvents.OnAnimalCollected → SpawnCollectEffect`, `OnBombTapped → SpawnExplosionEffect`, `OnAnimalMissed → SpawnMissFlash`
    - `OnDisable()`: unsubscribe all
    - `SpawnCollectEffect(AnimalType, Vector3)`: `ObjectPooler.SpawnFromPool(VFXRefs.BattleEffectWhite, worldPos)` + coroutine to `ReturnToPool` after particle system duration
    - `SpawnExplosionEffect(Vector3)`: `ObjectPooler.SpawnFromPool(VFXRefs.ExplosionBam, worldPos)`
    - `SpawnMissFlash()`: spawn pooled 48-unit red bottom strip; DOTween alpha 0.8→0 over 0.3s; `ReturnToPool` on complete
    - Define `VFXRefs` static holder for the 3 prefab references (`BattleEffectWhite`, `ExplosionBam`, `ExplosionZap`)
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5_

  - [ ] 7.2 Rewrite `EnvironmentEffects` — singleton state holder for movement modifiers
    - Rewrite `Assets/Scripts/Effects/EnvironmentEffects.cs` in namespace `AnimalFall.Effects`
    - Singleton `Instance` in `Awake`
    - Properties: `IsZeroGravityActive`, `IsWindActive` (computed from `WindForce.sqrMagnitude`), `WindForce`, `IsBlackHoleActive`, `BlackHoleCenter`, `BlackHolePullStrength`, `IsMirrorModeActive`
    - `ClearAll()`: reset all to defaults — called on level start/end
    - _Requirements: 2.6, 8.13, 8.14, 8.15, 8.18_

  - [ ] 7.3 Rewrite `ScreenEffects` — pooled overlay methods
    - Rewrite `Assets/Scripts/Effects/ScreenEffects.cs` in namespace `AnimalFall.Effects`
    - Singleton `Instance`; `[SerializeField]` overlay prefab references
    - `ShowInkOverlay(float duration)`: spawn from pool, `raycastTarget = false`; DOTween fade out after duration; `ReturnToPool` on complete
    - `ShowStormGradient(float duration)`: spawn lower-screen gradient; return after duration
    - `FlashWhite()`: alpha 0→0.9→0 over 0.8s DOTween; `ReturnToPool` on complete
    - `BorderFlashGold()`: DOTween sequence — fade in 0.1s, hold 0.2s, fade out 0.2s
    - `ClearAll()`: return all active overlays to pool immediately (called on level end)
    - _Requirements: 8.9, 8.10, 8.11, 13.6_

- [ ] 8. MegaLevel System
  - [ ] 8.1 Create `VillainHUD` UI component
    - Create `Assets/Scripts/UI/VillainHUD.cs` in namespace `AnimalFall.UI`
    - `Setup(VillainData)`: assign portrait sprite, set HP bar to 1.0f, call `Show()`
    - Subscribe to `GameEvents.OnVillainPhaseChanged` in `OnEnable`; unsubscribe in `OnDisable`
    - `OnPhaseChanged(int current, int total)`: compute target fill = `1f - ((float)current / total)`; `_hpBar.DOFillAmount(target, 0.3f)`
    - Phase transition: screen flash + villain sprite DOTween punch scale over 0.5s
    - Hidden unless `LevelData.isMegaLevel == true`
    - _Requirements: 10.1, 10.7, 10.8_

  - [ ] 8.2 Rewrite `MegaLevelController`
    - Rewrite `Assets/Scripts/Core/MegaLevel/MegaLevelController.cs` in namespace `AnimalFall.Core`
    - `InitMegaLevel(LevelData)`: set `_villain`, `_currentPhase = 0`, `_currentPhaseCollected = 0`; fire `GameEvents.OnVillainPhaseChanged(0, hpPhases)`; start `ProjectileLoop()`
    - `ProjectileLoop()`: `yield WaitForSeconds(frequency[_currentPhase])` — one cached wait per phase; `SpawnProjectile()`
    - `SpawnProjectile()`: `ObjectPooler.SpawnFromPool(projectilePrefab)`; 0.5s window coroutine — tapped → `DealDeflectDamage()` (reduce HP); not tapped → `GameManager.AddTime(-3f)` clamped to 0
    - `OnAnimalQuotaMet()`: `_currentPhase++`; if ≥ `hpPhases` → `GameManager.OnMegaLevelComplete()`; else fire phase change event + update frequency + `ScreenEffects/VillainHUD`
    - `Cleanup()`: stop coroutines, return active projectiles to pool
    - `Debug.LogError` + fall back to normal flow if `LevelData.Villain == null`
    - _Requirements: 4.6, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

- [ ] 9. Checkpoint — Managers and MegaLevel wired
  - Ensure `GameManager` state machine transitions work end-to-end
  - Ensure `GameEvents` events flow through to `AudioManager`, `EffectsController`, `GameUIManager` subscribers
  - Ensure `HindranceManager` respects `maxHindrancesActive` cap
  - Ask the user if questions arise before proceeding.

- [ ] 10. UI Layer
  - [ ] 10.1 Rewrite `GameUIManager` — StaticCanvas/DynamicCanvas split, GameEvents subscriber
    - Rewrite `Assets/Scripts/UI/GameUIManager.cs` in namespace `AnimalFall.UI`
    - Set up `StaticCanvas` (sort order 0: TopBar, BottomBar, ChapterBackground) and `DynamicCanvas` (sort order 1: timer, score, combo, goal panel, floating text)
    - `OnEnable()`: subscribe `OnScoreChanged`, `OnComboChanged`, `OnAnimalCollected`, `OnLevelWon`, `OnLevelFailed`, `OnTimerWarning`, `OnVillainPhaseChanged`; `OnDisable()`: unsubscribe all
    - No direct calls from `GameManager`/`ScoreManager` after `Setup()`
    - Timer display: `clock.png` icon left of text; `remaining < 10s` → pulse scale DOTween + `SfxType.TimerWarning`
    - `UpdateGoalPanel(AnimalType)`: per-species icons via `ImageLibrary`; DOTween progress bar fill 0.15s
    - `UpdateCombo(int, float)`: punch-scale DOTween (vibrato 5, elasticity 0.5, strength 0.3) over 0.25s
    - `ShowFloatingText(string, Vector2)`: spawn from pool; `Camera.main.WorldToScreenPoint` anchor (fallback: raw pos); DOTween up 80 canvas units + alpha→0 over 1.2s; `ReturnToPool` on complete
    - Hindrance tutorial toast: shown once per type per account via `SaveService.HasSeenHindrance`; 3–5s auto-dismiss
    - _Requirements: 2.8, 12.1, 12.2, 12.4, 12.5, 13.5, 21.2, 9.3_

  - [ ] 10.2 Create `CountdownController`
    - Create `Assets/Scripts/UI/CountdownController.cs` in namespace `AnimalFall.UI`
    - `PlayCountdown(Action onComplete)`: coroutine displaying 3, 2, 1, GO
    - Each beat: set text; DOTween scale 2.5→0.8 `Ease.OutElastic` over 0.7s; `yield 0.7s`
    - Call `onComplete()` after GO animation
    - _Requirements: 16.3_

  - [ ] 10.3 Create `LevelIntroScreen`
    - Create `Assets/Scripts/UI/LevelIntroScreen.cs` in namespace `AnimalFall.UI`
    - `Show(LevelData, Action onDismiss)`: populate level number, chapter name, per-species goal icons (via `ImageLibrary`), time limit, active hindrance icons
    - DOTween entrance `(0,0,1)→(1,1,1)` `Ease.OutBack` 0.3s
    - 2s hold coroutine with `_tapReceived` flag for tap-to-skip
    - On dismiss: DOTween `(1,1,1)→(0,0,1)` `Ease.InBack` 0.2s; call `onDismiss()`
    - Block all player input during intro (except tap-to-skip)
    - _Requirements: 16.1, 16.2, 16.4_

  - [ ] 10.4 Rewrite `ResultsScreenController`
    - Rewrite `Assets/Scripts/UI/ResultsScreenController.cs` in namespace `AnimalFall.UI`
    - `ShowWin(int score, int coins, bool isMegaLevel)`: load `panel.png`/`panel2.png` via `ImageLibrary.GetPanel()`; DOTween entrance; `StarReveal()` — animate each of 3 stars 0→1 `Ease.OutBounce` 0.3s with 0.2s between; save stars via `SaveService` (only if higher or first result); show coin reward + score
    - `ShowLose(int score)`: `red_buttons.png` via `ImageLibrary.GetRedButtons()` on action buttons; DOTween entrance; retry/quit buttons
    - Subscribe to `GameEvents.OnLevelWon`, `OnLevelFailed` in `OnEnable`; unsubscribe in `OnDisable`
    - _Requirements: 11.2, 11.3, 12.6, 12.7, 12.9_

  - [ ] 10.5 Rewrite `JourneyMapController` — 50 scrollable nodes, chapter sections
    - Rewrite `Assets/Scripts/UI/JourneyMapController.cs` in namespace `AnimalFall.UI`
    - `Start()`: instantiate 50 nodes in vertical scroll; group into 5 chapter sections (10 each); chapter headers + panel backgrounds via `ImageLibrary`
    - Node sprites: completed → `GetLevelButton1()`, locked → `GetLevelButton2()` via `ImageLibrary`
    - Star display per node from `SaveService.GetStars(levelIndex)`; no stars if no result; distinct "attempted" icon for 0-star
    - Auto-scroll to first incomplete level centered in viewport
    - Pulse current playable node: DOTween scale 1.0↔1.1 yoyo, loops=-1, 0.5s
    - `OnNodeTapped(int)`: if locked → `DOShakePosition(0.3f, 5f, 10)` + toast 2s; if unlocked → `LevelManager.LoadGameSceneForLevel`; on scene load fail → error toast
    - `OnHigherStarEarned(JourneyMapNode)`: DOScale 1.4 over 0.1s then 1.0 over 0.15s `Ease.OutElastic`
    - _Requirements: 11.5, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8_

- [ ] 11. Level Generation — Editor Tool
  - [ ] 11.1 Create `LevelDatabaseGenerator` editor tool inside `LevelDatabase`
    - Implement `GenerateAndSave50Levels()` inside `LevelDatabase.cs` under `#if UNITY_EDITOR`
    - Compute `timeLimit` and `spawnInterval` from the 4 difficulty bands using `Mathf.Lerp` (Intro i∈[0,9], Rising i∈[10,24], Challenge i∈[25,39], Expert i∈[40,49])
    - MegaLevel bonus: `if ((i+1) % 5 == 0) timeLimit += 15f`
    - Goal count = `Mathf.FloorToInt(timeLimit / spawnInterval * 0.75f)` clamped to linear(10,45,i/49) ± 2
    - `WrongTapTimePenalty(N)` = `1.0 + (3.0/49)*(N-1)` rounded to 2dp
    - `BombTimePenalty(N)` = `3.0 + (5.0/49)*(N-1)` rounded to 2dp
    - Assign `chapterTheme`, `isMegaLevel`, `rewardCoins`; set hindrance configs per unlock schedule
    - Create/overwrite `.asset` files in `Assets/Levels/LevelData/` via `AssetDatabase.CreateAsset` / `EditorUtility.SetDirty`; `AssetDatabase.SaveAssets()` at end
    - Ensure folder `Assets/Levels/LevelData/` is created if missing
    - _Requirements: 4.1, 4.3, 6.1, 6.2, 6.3, 6.4, 6.5, 9.1, 24.6_

  - [ ]* 11.2 Write property test for P6 — LevelDatabase Completeness
    - **Property P6: LevelDatabase Completeness**
    - After calling `GenerateAndSave50Levels()`, assert `LevelDatabase.TotalLevels == 50`
    - For every index 0..49, assert `GetLevel(i)` is non-null and `levelNumber == i + 1`
    - **Validates: Requirements 4.1, 4.4**

  - [ ]* 11.3 Write property test for P7 — Species Goal Sum Invariant
    - **Property P7: Species Goal Sum Invariant**
    - For all 50 `LevelData` assets: `goal.TotalCount` is ≥ 1 and ≤ 50
    - `spawnPool` contains at least one `AnimalData` with `isTargetSpecies == true` for each species in the goal
    - **Validates: Requirements 4.2, 3.4**

  - [ ]* 11.4 Write property test for P8 — Difficulty Parameter Monotonicity
    - **Property P8: Difficulty Parameter Monotonicity**
    - For all consecutive pairs (N, N+1) across all 50 generated levels: `timeLimit[N+1] <= timeLimit[N]` and `spawnInterval[N+1] <= spawnInterval[N]`
    - **Validates: Requirements 6.1, 6.2**

- [ ] 12. Bootstrap and Save Service
  - [ ] 12.1 Create `AppBootstrap` MonoBehaviour
    - Create `Assets/Scripts/AppBootstrap.cs` in namespace `AnimalFall`
    - `Awake()`: `Application.targetFrameRate = 60`
    - Battery watcher: subscribe to `SystemInfo.batteryStatus`; when `batteryLevel <= 0.2f` and `batteryStatus == BatteryStatus.Discharging` → set `targetFrameRate = 30`; restore to 60 otherwise
    - Place as first child under `[Bootstrap]` in GameScene hierarchy
    - _Requirements: 20.1, 20.8_

  - [ ] 12.2 Rewrite `SaveService` — JSON persistence, star rules, hindrance seen flags
    - Rewrite `Assets/Scripts/Services/SaveService.cs` in namespace `AnimalFall.Services`
    - JSON schema stored in `PlayerPrefs` key `"AnimalFall_Save"`: `highestUnlockedLevel`, `starRatings[50]`, `coins`, `lives`, `nextLifeUTC`, `skinUnlocks[]`, `powerUpInventory[5]`, `seenHindranceTypes[20]`
    - `LoadAll()` in `Awake`; `SaveAll()` on level end and `OnApplicationPause(true)` — never only on app close
    - Star save rule: overwrite only if `newStars > existing`; save 0 only if no prior result exists; never go down
    - `GetStars(int)`, `SetStars(int, int)`, `GetHighestUnlockedLevel()`, `SetHighestUnlockedLevel(int)`
    - `GetCoins()`, `AddCoins(int)`, `GetLives()`, `SetLives(int)`, `GetNextLifeUTC()`, `SetNextLifeUTC(long)`
    - `HasSeenHindrance(HindranceType)`, `MarkHindranceSeen(HindranceType)`
    - _Requirements: 11.2, 19.1, 19.2, 19.10_

  - [ ]* 12.3 Write property test for P9 — Lives Regeneration Capped At 5
    - **Property P9: Lives Regeneration Capped At 5**
    - For all `(startLives: 0..5, offlineMinutes: 0..300)`: `LivesManager.ComputeOfflineLives(startLives, offlineMinutes) == Min(5, startLives + Floor(offlineMinutes / 30))`
    - **Validates: Requirements 19.6, 19.8, 19.9**

- [ ] 13. Property-Based Tests for Core Correctness Properties
  - [ ]* 13.1 Write property test for P1 — Pool Round-Trip
    - **Property P1: Pool Round-Trip**
    - For each animal spawn during a level, record `ObjectPooler.ActiveCount(animalPrefab)` before spawn and after collection/miss; assert after == before
    - Test with varying levels and spawn sequences to detect any object leak
    - **Validates: Requirements 1.1, 1.3, 1.4**

  - [ ]* 13.2 Write property test for P2 — Hindrance Count Invariant
    - **Property P2: Hindrance Count Invariant**
    - For all game states during active play, assert `HindranceManager.GetActiveHindrances().Count <= currentLevel.MaxHindrancesActive`
    - Test with rapid hindrance activations and edge-case `maxHindrancesActive` values (1, 5)
    - **Validates: Requirements 7.7, 7.6_

  - [ ]* 13.3 Write example test for P3 — Timer Never Negative
    - **Property P3: Timer Never Negative (Example)**
    - Apply `wrongTapTimePenalty` to a timer at `0.1f`; assert `remainingTime` is clamped to exactly `0f`
    - Assert level failure is triggered — not a negative timer
    - **Validates: Requirement 6.4 (clamping behavior)**

  - [ ]* 13.4 Write property test for P4 — Star Rating Monotonicity
    - **Property P4: Star Rating Monotonicity**
    - For all pairs `(rescued1, rescued2)` where `rescued1 > rescued2` (same target and time): `ScoreManager.CalculateStars(rescued1, target, time, totalTime) >= ScoreManager.CalculateStars(rescued2, target, time, totalTime)`
    - Test across the full target range (1..50) and time-remaining range
    - **Validates: Requirements 11.1, 11.4**

  - [ ]* 13.5 Write example test for P5 — Combo Pitch Sequence
    - **Property P5: Combo Pitch Sequence (Example)**
    - Simulate 5 consecutive correct taps; capture `AudioManager.LastPlayedPitch` after each tap
    - Assert pitch sequence is exactly `[0.95, 1.0, 1.05, 1.1, 1.15]`
    - **Validates: Requirements 13.4, 18.5**

- [ ] 14. Integration and Wiring
  - [ ] 14.1 Wire GameScene hierarchy — place and connect all GameObjects
    - Set up `[Bootstrap]`, `[Persistence]`, `[Managers]`, `[Core]`, `[Effects]`, `[UI — StaticCanvas]`, `[UI — DynamicCanvas]` GameObjects per design hierarchy
    - Assign all `[SerializeField]` cross-references in GameManager (Spawner, HindranceManager, ScoreManager, ComboManager, AudioManager, PowerUpManager, CountdownController, MegaLevelController, Camera)
    - Configure `StaticCanvas` sort order 0 and `DynamicCanvas` sort order 1
    - Pre-configure 12 `AudioSource` children on `AudioManager` GO
    - Place 6 SpawnPoint Transform children under `Spawner`
    - _Requirements: 12.1, 18.1, 23.1_

  - [ ] 14.2 Connect ObjectPooler, VFXRefs, and prefab assignments
    - Assign `_animalPrefab` on `Spawner` and `_animalContainer` Transform
    - Assign hindrance prefabs to a `HindranceRegistry` ScriptableObject (or static map) for factory lookup
    - Assign `VFXRefs.BattleEffectWhite`, `VFXRefs.ExplosionBam`, `VFXRefs.ExplosionZap` prefab references on `EffectsController`
    - Assign `_inkOverlayPrefab`, `_stormGradientPrefab`, `_flashbangPrefab`, `_borderFlashPrefab` on `ScreenEffects`
    - _Requirements: 17.1, 17.2, 17.3, 17.4_

  - [ ]* 14.3 Write integration tests for level start-to-end flow
    - Test `GameManager.StartLevel` → intro → countdown → spawning → `EndLevel(won: true)` sequence
    - Assert `ObjectPooler` has zero active animals after level end
    - Assert `SaveService` is written on level win
    - Assert `GameEvents.OnLevelWon` is fired exactly once
    - _Requirements: 23.3, 23.4, 23.5, 19.2_

- [ ] 15. Final Checkpoint — Ensure all tests pass
  - Validate all 9 property/example tests (P1–P9) pass
  - Validate no compile errors in Unity console
  - Validate `LevelDatabase` contains 50 levels with correct difficulty progression
  - Ask the user if questions arise before considering the feature complete.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP; core implementation will still be complete
- Each task references specific requirements for full traceability
- The implementation order (Data → Core → Animal → Hindrance → Managers → Effects → MegaLevel → UI → Generator → Bootstrap → Tests) ensures no forward dependencies
- All sprite assets must be copied from `c:\AnnusMirabilis\ZEMOLABS\NEMESIS\animal_blast\AnimalBlast\Assets\Resources\` into `Assets/Resources/` maintaining subfolder structure before running `ImageLibrary.LoadAll()`
- `DontDestroyOnLoad` is permitted ONLY on `LevelManager` — all other managers live within `GameScene`
- `GameObject.Instantiate` and `GameObject.Destroy` are banned during gameplay (between `StartLevel` and `EndLevel`); `ObjectPooler` is the sole gateway
- `Resources.Load<Sprite>()` is banned outside `ImageLibrary.LoadAll()` — treat violations as build-breaking errors
- All `System.Action` event subscriptions must have matching unsubscriptions (`OnEnable/OnDisable` or `Start/OnDestroy`)
- DOTween must be called with `DOTween.Kill(gameObject)` before any `ReturnToPool` call to prevent dangling tweens


## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4", "1.5", "1.6", "1.7"] },
    { "id": 1, "tasks": ["2.1", "2.2", "2.3"] },
    { "id": 2, "tasks": ["3.1", "3.2", "3.3", "4.1", "4.2"] },
    { "id": 3, "tasks": ["4.3", "4.4", "4.5", "4.6", "4.7"] },
    { "id": 4, "tasks": ["6.1"] },
    { "id": 5, "tasks": ["6.2", "6.3", "6.4", "6.5", "6.6", "7.1", "7.2", "7.3"] },
    { "id": 6, "tasks": ["6.7", "6.8", "6.9", "6.10", "8.1", "8.2"] },
    { "id": 7, "tasks": ["10.1", "10.2", "10.3", "10.4", "10.5", "12.1", "12.2"] },
    { "id": 8, "tasks": ["11.1"] },
    { "id": 9, "tasks": ["11.2", "11.3", "11.4", "12.3", "13.1", "13.2", "13.3", "13.4", "13.5"] },
    { "id": 10, "tasks": ["14.1", "14.2"] },
    { "id": 11, "tasks": ["14.3"] }
  ]
}
```
