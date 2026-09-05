using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnimalFall.Core.Arcade;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.Services;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaShooterGameManager : MonoBehaviour
    {
#if UNITY_EDITOR
        // Editor-only play-mode audit hooks. They are never set by shipped gameplay.
        public static MegaLevelData RuntimeTestLevelOverride;
#endif
        public static bool RuntimeTestFastStart;

        [Header("Scene References")]
        public Camera worldCamera;
        public MegaObjectPools pools;
        public MegaWaveDirector waveDirector;
        public MegaShooterInput shooterInput;
        public MegaHUD hud;
        public MegaCameraEffects cameraEffects;
        public MegaStarfield starfield;
        public MegaDebugOverlay debugOverlay;
        public Transform playerContainer;
        public Transform enemyContainer;
        public Transform projectileContainer;
        public Transform pickupContainer;

        [Header("Generated Generic Prefabs")]
        public GameObject pickupPrefab;
        public Sprite healthPickupSprite;
        public Sprite counterPickupSprite;
        public ProjectileData defaultEnemyProjectile;

        [Header("Debug")]
        [Tooltip("Used only when opening MegaShooterScene directly without LevelManager selection.")]
        public MegaLevelData debugLevel;
        public bool showDebugOverlay;

        private readonly List<MegaEnemyController> _enemies = new List<MegaEnemyController>(20);
        private readonly List<MegaEnemyController> _enemyScratch = new List<MegaEnemyController>(20);
        private readonly List<MegaProjectile> _hostileProjectiles = new List<MegaProjectile>(120);
        private readonly List<MegaProjectile> _projectileScratch = new List<MegaProjectile>(120);
        private MegaLevelData _level;
        private LevelData _levelAsset;
        private SaveService _save;
        private SuperAnimalData _selectedAnimal;
        private int _selectionIndex;
        private int _score;
        private float _elapsed;
        private float _hostileTimeScale = 1f;
        private int _hostileScaleVersion;
        private int _effectFrame = -1;
        private int _effectsThisFrame;
        private float _nextOrdinaryEnemyVolley;
        private float _ordinaryVolleyInterval = 0.8f;
        private float _earlyMegaEase;
        private bool _ended;
        private MegaShooterState _stateBeforePause;
        private System.Random _random;

        public MegaShooterState State { get; private set; } = MegaShooterState.Intro;
        public SuperAnimalController Player { get; private set; }
        public MegaBossController Boss { get; private set; }
        public MegaHUD Hud => hud;
        public MegaCameraEffects CameraEffects => cameraEffects;
        public MegaVFXProfile VFXProfile => _level != null ? _level.vfxProfile : null;
        public ProjectileData DefaultEnemyProjectile => defaultEnemyProjectile;
        public float NearMissOuterRadius => 0.48f;
        public float HostileTimeScale => _hostileTimeScale;
        public bool IsCombatFrozen => State == MegaShooterState.Intro || State == MegaShooterState.Countdown || State == MegaShooterState.WaveTransition || State == MegaShooterState.BossWarning || State == MegaShooterState.Won || State == MegaShooterState.Lost || State == MegaShooterState.Paused;
        // The hero only auto-fires when there is actually something to shoot at:
        // an active wave with living army ships, or a boss that is still alive.
        // This stops the endless firing during spawn gaps and after the sky is clear.
        public bool IsPlayerAutoFireActive =>
            (State == MegaShooterState.Wave && ActiveEnemyCount > 0) ||
            (State == MegaShooterState.Boss && Boss != null);

        // Relief applied to the opening mega missions (0 = full difficulty).
        // Slower enemy fire, slower bullets, gentler swarms and spawn pacing.
        public float HostileFireIntervalScale => 1f + _earlyMegaEase * 1.15f;
        public float HostileProjectileSpeedScale => 1f - _earlyMegaEase * 0.28f;
        public float SpawnCadenceScale => 1f + _earlyMegaEase * 0.9f;
        public int EffectiveMaxActiveEnemies(int levelCap, int waveCap)
            => Mathf.Max(2, Mathf.RoundToInt(Mathf.Min(levelCap, waveCap) * (1f - _earlyMegaEase * 0.45f)));
        public bool IsWaveRunning => State == MegaShooterState.Wave;
        public bool CanAdvanceCombat => !_ended && State != MegaShooterState.Lost && State != MegaShooterState.Won;
        public int ActiveEnemyCount => _enemies.Count;
        public int ActiveHostileProjectiles => _hostileProjectiles.Count;
        public int PoolMisses => pools != null ? pools.PoolMisses : 0;
        public int ActiveSeed { get; private set; }
        public string CurrentWaveDisplay => waveDirector != null && _level != null ? $"{waveDirector.CurrentWaveIndex + 1}/{_level.waves.Length}" : "-";
        public string BossPhaseDisplay => Boss != null ? (Boss.PhaseIndex + 1).ToString() : "-";
        public float PlayerDps => Player != null ? Player.GetComponent<AutoWeaponController>()?.EstimatedDps ?? 0f : 0f;
        public Transform NearestEnemyTransform => GetNearestEnemyTransform();

        public bool RuntimeVerifyBossDamage()
        {
            if (!Debug.isDebugBuild || _level == null || _level.boss == null || _level.boss.prefab == null || pools == null) return false;
            MegaShooterState previousState = State;
            State = MegaShooterState.Boss;
            GameObject go = pools.Spawn(_level.boss.prefab, Vector3.zero, Quaternion.identity, enemyContainer);
            MegaBossController probe = go.GetComponent<MegaBossController>();
            probe?.ConfigureForRuntimeVerification(_level.boss, _level, this);
            if (probe == null) { State = previousState; return false; }
            float before = probe.HealthNormalized;
            float fillBefore = hud != null && hud.bossHealthFill != null ? hud.bossHealthFill.fillAmount : -1f;
            float damage = Mathf.Max(1f, _level.boss.baseHitPoints * _level.bossOverrides.healthMultiplier * 0.1f);
            bool accepted = probe.TakeDamage(damage);
            float after = probe.HealthNormalized;
            float fillAfter = hud != null && hud.bossHealthFill != null ? hud.bossHealthFill.fillAmount : -1f;
            bool passed = accepted && after < before && (fillBefore < 0f || fillAfter < fillBefore);
            Debug.Log($"[MegaShooter Runtime Verify] bossDamage={passed}, health={before:F3}->{after:F3}, hud={fillBefore:F3}->{fillAfter:F3}");
            MegaObjectPools.Instance?.Despawn(go);
            Boss = null;
            State = previousState;
            return passed;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            LevelManager levelManager = LevelManager.Instance;
            _levelAsset = levelManager != null ? levelManager.CurrentLevel : null;
            MegaLevelData editorOverride = null;
#if UNITY_EDITOR
            editorOverride = RuntimeTestLevelOverride;
            if (editorOverride != null) debugLevel = editorOverride;
#endif
            _level = editorOverride != null ? editorOverride
                : _levelAsset != null && _levelAsset.IsConfiguredMegaShooter ? _levelAsset.MegaShooterData : debugLevel;
            if (_level == null)
            {
                Debug.LogError("[MegaShooter] No configured MegaLevelData selected.");
                enabled = false;
                return;
            }

            _save = levelManager != null ? levelManager.Save : null;
            if (_save == null) _save = FindFirstObjectByType<SaveService>();
            if (levelManager != null && levelManager.Save == null && _save != null) levelManager.Init(_save);
            _save?.EnsureCapacity(levelManager != null ? Mathf.Max(100, levelManager.TotalLevels) : 100);
            LivesManager lives = FindFirstObjectByType<LivesManager>();
            lives?.Init(_save);

            ActiveSeed = _level.randomizeSeed ? System.Environment.TickCount : _level.deterministicSeed;
            _random = new System.Random(ActiveSeed);
            // The first mega missions (sequence 1-3, i.e. game levels 5/10/15) get the
            // strongest relief; it tapers to zero by sequence 6 so later levels are untouched.
            int megaSequence = Mathf.Max(1, _level.megaSequenceIndex);
            _earlyMegaEase = Mathf.Clamp01((6f - megaSequence) / 6f);
            _nextOrdinaryEnemyVolley = 0f;
            _ordinaryVolleyInterval = 0.8f * HostileFireIntervalScale;
            Camera.main?.gameObject.SetActive(true);
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera != null)
            {
                worldCamera.backgroundColor = _level.backgroundColor;
                worldCamera.orthographic = true;
                worldCamera.orthographicSize = _level.cameraBounds.height * 0.5f;
            }

            hud.Bind(this);
            cameraEffects?.Configure(_level.vfxProfile);
            starfield?.Configure(_level);
            debugOverlay?.Configure(this);
            // Mega levels are intentionally gameplay-only while active: no debug panel
            // or diagnostic text should cover the combat lane, even in development builds.
            if (debugOverlay != null) debugOverlay.enabled = false;
            PrewarmPools();
            SetupSelection();
        }

        private void ConfigureMissionPreview()
        {
            EnemyShipData first = null;
            EnemyShipData second = null;
            for (int w = 0; _level.waves != null && w < _level.waves.Length; w++)
            {
                EnemySpawnGroup[] groups = _level.waves[w].spawnGroups;
                for (int g = 0; groups != null && g < groups.Length; g++)
                {
                    EnemyShipData candidate = groups[g]?.enemy;
                    if (candidate == null) continue;
                    if (first == null) first = candidate;
                    else if (candidate != first) { second = candidate; break; }
                }
                if (second != null) break;
            }
            if (second == null) second = first;
            hud.SetMissionPreview(first, second, _level.boss);
        }

        private void Update()
        {
            if (!_ended && State != MegaShooterState.Intro && State != MegaShooterState.Countdown && State != MegaShooterState.Paused)
                _elapsed += Time.deltaTime;
        }

        private void SetupSelection()
        {
            State = MegaShooterState.Intro;
            SuperAnimalData[] roster = _level.allowedAnimals;
            if (roster == null || roster.Length == 0)
            {
                Debug.LogError($"[MegaShooter] {_level.name} has no allowed Super Animals.");
                return;
            }

            // Mega levels now begin just like normal levels: choose the saved animal when
            // possible, otherwise use the level's featured animal, then immediately count in.
            if (_level.featuredAnimal != null)
                _save?.UnlockSuperAnimal(_level.featuredAnimal.stableId);

            _selectedAnimal = _level.featuredAnimal;
            if (!IsAnimalUnlocked(_selectedAnimal) || _selectedAnimal.playerPrefab == null)
            {
                int start = Mathf.Max(0, _level.megaSequenceIndex - 1) % roster.Length;
                _selectedAnimal = null;
                for (int offset = 0; offset < roster.Length; offset++)
                {
                    SuperAnimalData candidate = roster[(start + offset) % roster.Length];
                    if (candidate != null && IsAnimalUnlocked(candidate) && candidate.playerPrefab != null)
                    {
                        _selectedAnimal = candidate;
                        break;
                    }
                }
            }

            if (_selectedAnimal == null || _selectedAnimal.playerPrefab == null)
            {
                Debug.LogError($"[MegaShooter] {_level.name} has no playable Super Animal.");
                return;
            }

            _save?.SetSelectedSuperAnimalId(_selectedAnimal.stableId);
            hud.HideSelection();
            SpawnPlayer();
#if UNITY_EDITOR
            if (RuntimeTestFastStart)
            {
                waveDirector.Configure(this, _level);
                waveDirector.Begin();
                return;
            }
#endif
            StartCoroutine(CountdownRoutine());
        }

        public void SelectAnimal(int direction)
        {
            if (State != MegaShooterState.Intro || _level.allowedAnimals == null || _level.allowedAnimals.Length == 0) return;
            _selectionIndex = (_selectionIndex + direction + _level.allowedAnimals.Length) % _level.allowedAnimals.Length;
            UpdateSelectionUI();
        }

        private void UpdateSelectionUI()
        {
            _selectedAnimal = _level.allowedAnimals[Mathf.Clamp(_selectionIndex, 0, _level.allowedAnimals.Length - 1)];
            hud.SetSelection(_selectedAnimal, IsAnimalUnlocked(_selectedAnimal));
        }

        private bool IsAnimalUnlocked(SuperAnimalData animal)
            => animal != null && (_save == null || _save.IsSuperAnimalUnlocked(animal.stableId));

        public void ConfirmAnimalSelection()
        {
            if (State != MegaShooterState.Intro || !IsAnimalUnlocked(_selectedAnimal) || _selectedAnimal.playerPrefab == null) return;
            _save?.SetSelectedSuperAnimalId(_selectedAnimal.stableId);
            hud.HideSelection();
            SpawnPlayer();
            StartCoroutine(CountdownRoutine());
        }

        private void SpawnPlayer()
        {
            Vector2 start = new Vector2(_level.playerMovementBounds.center.x, _level.playerMovementBounds.yMin + 1f);
            GameObject go = pools.Spawn(_selectedAnimal.playerPrefab, start, Quaternion.identity, playerContainer);
            Player = go.GetComponent<SuperAnimalController>();
            Player.Configure(_selectedAnimal, _level, this);
            shooterInput.Configure(Player, worldCamera);
            hud.SetAnimalPortrait(_selectedAnimal.portrait);
        }

        private IEnumerator CountdownRoutine()
        {
            State = MegaShooterState.Countdown;
            string[] values = { "3", "2", "1", "GO!" };
            for (int i = 0; i < values.Length; i++)
            {
                hud.ShowCountdown(values[i]);
                yield return new WaitForSeconds(0.65f);
            }
            hud.HideCountdown();
            waveDirector.Configure(this, _level);
            waveDirector.Begin();
        }

        public void EnterWave(int index, int total, MegaWaveData wave)
        {
            State = MegaShooterState.Wave;
            hud.SetWave(index + 1, total);
            if (!string.IsNullOrWhiteSpace(wave.warningBanner)) hud.ShowBanner(wave.warningBanner);
        }

        public void EnterWaveTransition() => State = MegaShooterState.WaveTransition;

        public void AllWavesCompleted()
        {
            if (_ended) return;
            if (_level.boss == null)
            {
                StartCoroutine(VictoryRoutine());
                return;
            }
            StartCoroutine(BossWarningRoutine());
        }

        private IEnumerator BossWarningRoutine()
        {
            State = MegaShooterState.BossWarning;
            ClearOrReflectHostileProjectiles(false);
            hud.ShowBanner(_level.bossWarningText, 2.2f);
            cameraEffects?.Shake(0.2f, 0.12f);
            yield return new WaitForSeconds(2.25f);
            if (_level.boss == null || _level.boss.prefab == null)
            {
                Debug.LogError("[MegaShooter] Boss configuration is missing.");
                FailLevel();
                yield break;
            }
            // Spawn the boss inside the top edge; its entrance animation must remain
            // visible instead of approaching from outside the camera.
            GameObject go = pools.Spawn(_level.boss.prefab,
                new Vector3(0f, _level.cameraBounds.yMax - 1.25f, 0f), Quaternion.identity, enemyContainer);
            go.GetComponent<MegaBossController>()?.Configure(_level.boss, _level, this);
        }

        public void BossCombatStarted(MegaBossController boss, string bossName)
        {
            Boss = boss;
            State = MegaShooterState.Boss;
            hud.ShowBoss(bossName);
        }

        public void BossDefeated(MegaBossController boss)
        {
            if (_ended || Boss != boss) return;
            StartCoroutine(VictoryRoutine());
        }

        private IEnumerator VictoryRoutine()
        {
            State = MegaShooterState.Won;
            cameraEffects?.Shake(0.4f, 0.28f);
            cameraEffects?.Flash(0.65f, 0.35f);
            ClearOrReflectHostileProjectiles(false);
            yield return new WaitForSeconds(1.1f);
            CompleteLevel();
        }

        public void CompleteLevel()
        {
            if (_ended) return;
            _ended = true;
            State = MegaShooterState.Won;
            int stars = _elapsed <= _level.parTime ? 3 : _elapsed <= _level.parTime * 1.35f ? 2 : 1;
            int levelIndex = _levelAsset != null ? _levelAsset.LevelNumber - 1 : _level.gameLevelNumber - 1;
            _save?.RecordMegaResult(levelIndex, _score, stars, _elapsed);
            _save?.AddCoins(_level.coinReward);
            ArcadeTokenService.Instance?.AddTokens(_level.arcadeTokenReward);
            LevelManager.Instance?.LevelSuccess(levelIndex);
            GameEvents.OnLevelWon?.Invoke();
            hud.ShowResult(true, _score, stars, _level.coinReward);
            StopCombat();
            if (FindFirstObjectByType<AnimalFall.UI.VictoryOverlay>() == null)
            {
                StartCoroutine(AutoExitRoutine());
            }
        }

        private IEnumerator AutoExitRoutine()
        {
            yield return new WaitForSeconds(5.5f);
            if (State == MegaShooterState.Won)
            {
                Quit();
            }
        }

        public void PlayerDefeated()
        {
            if (_ended) return;
            StartCoroutine(FailureRoutine());
        }

        private IEnumerator FailureRoutine()
        {
            State = MegaShooterState.Lost;
            cameraEffects?.Shake(0.25f, 0.2f);
            yield return new WaitForSeconds(0.65f);
            FailLevel();
        }

        private void FailLevel()
        {
            if (_ended) return;
            _ended = true;
            State = MegaShooterState.Lost;
            (LivesManager.Instance ?? FindFirstObjectByType<LivesManager>())?.UseLife();
            GameEvents.OnLevelFailed?.Invoke();
            hud.ShowResult(false, _score, 0, 0);
            StopCombat();
        }

        private void StopCombat()
        {
            waveDirector?.StopDirector();
            _hostileTimeScale = 1f;
            DespawnAllEnemies();
            ClearOrReflectHostileProjectiles(false);
        }

        public void TogglePause()
        {
            if (_ended) return;
            if (State == MegaShooterState.Paused)
            {
                State = _stateBeforePause;
                Time.timeScale = 1f;
                hud.ShowPause(false);
            }
            else
            {
                _stateBeforePause = State;
                State = MegaShooterState.Paused;
                Time.timeScale = 0f;
                hud.ShowPause(true);
            }
        }

        public void ActivateCounter() => Player?.Counter?.TryActivate();

        public void Retry()
        {
            Time.timeScale = 1f;
            pools?.DespawnAll();
            if (LevelManager.Instance != null) LevelManager.Instance.RetryCurrentLevel();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Quit()
        {
            Time.timeScale = 1f;
            if (State != MegaShooterState.Won && State != MegaShooterState.Lost)
            {
                GameEvents.OnLevelFailed?.Invoke();
            }
            pools?.DespawnAll();
            if (LevelManager.Instance != null) LevelManager.Instance.ReturnToMainScene();
            else SceneManager.LoadScene("MainScene");
        }

        public MegaProjectile SpawnProjectile(ProjectileData data, MegaFaction faction, Vector2 position,
            Vector2 direction, float damage, float speedMultiplier, int pierce, Transform homingTarget,
            bool reflectableOverride = true)
        {
            if (data == null || data.prefab == null || pools == null) return null;
            if (faction == MegaFaction.Enemy && _hostileProjectiles.Count >= _level.maximumHostileProjectiles) return null;
            if (faction == MegaFaction.Enemy)
            {
                direction = ForceDownward(direction);
            }
            damage = Mathf.Max(0.1f, damage);
            GameObject go = pools.Spawn(data.prefab, position, Quaternion.identity, projectileContainer);
            MegaProjectile projectile = go.GetComponent<MegaProjectile>();
            projectile?.Configure(data, faction, direction, damage, speedMultiplier, pierce, homingTarget, this, reflectableOverride);
            return projectile;
        }

        /// <summary>Limits the combined fire rate of ordinary enemies in mega waves.</summary>
        public bool TryBeginOrdinaryEnemyVolley()
        {
            if (IsCombatFrozen || Time.time < _nextOrdinaryEnemyVolley) return false;
            _nextOrdinaryEnemyVolley = Time.time + _ordinaryVolleyInterval;
            return true;
        }

        /// <summary>Hostile shots in mega levels always travel toward the player lane.</summary>
        public static Vector2 ForceDownward(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return Vector2.down;
            direction.Normalize();
            if (direction.y > -0.12f) direction.y = -0.12f;
            return direction.normalized;
        }

        public void SpawnEffect(GameObject prefab, Vector2 position, Color color, float scale = 1f, float duration = 0.45f)
        {
            if (prefab == null || pools == null) return;
            if (_effectFrame != Time.frameCount)
            {
                _effectFrame = Time.frameCount;
                _effectsThisFrame = 0;
            }
            bool transient = duration <= 0.3f;
            if (transient && _effectsThisFrame >= 8) return;
            _effectsThisFrame++;
            if (transient)
            {
                scale = Mathf.Min(scale, 0.72f);
                duration = Mathf.Min(duration, 0.22f);
            }
            GameObject effect = pools.Spawn(prefab, position, Quaternion.identity, projectileContainer);
            effect.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            MegaTimedPoolEffect timed = effect.GetComponent<MegaTimedPoolEffect>();
            if (timed != null) timed.Configure(color, duration, scale);
        }

        public void RegisterProjectile(MegaProjectile projectile)
        {
            if (projectile != null && projectile.Faction == MegaFaction.Enemy && !_hostileProjectiles.Contains(projectile))
                _hostileProjectiles.Add(projectile);
        }

        public void UnregisterProjectile(MegaProjectile projectile, MegaFaction faction)
        {
            if (faction == MegaFaction.Enemy) _hostileProjectiles.Remove(projectile);
        }

        public void ChangeProjectileFaction(MegaProjectile projectile, MegaFaction from, MegaFaction to)
        {
            if (from == MegaFaction.Enemy) _hostileProjectiles.Remove(projectile);
            if (to == MegaFaction.Enemy && !_hostileProjectiles.Contains(projectile)) _hostileProjectiles.Add(projectile);
        }

        public void RegisterEnemy(MegaEnemyController enemy)
        {
            if (enemy != null && !_enemies.Contains(enemy)) _enemies.Add(enemy);
        }

        public void UnregisterEnemy(MegaEnemyController enemy) => _enemies.Remove(enemy);
        public void RegisterBoss(MegaBossController boss) => Boss = boss;
        public void UnregisterBoss(MegaBossController boss) { if (Boss == boss) Boss = null; }

        public void DespawnAllEnemies()
        {
            _enemyScratch.Clear();
            _enemyScratch.AddRange(_enemies);
            for (int i = 0; i < _enemyScratch.Count; i++) _enemyScratch[i]?.ForceDespawn();
            _enemyScratch.Clear();
        }

        public void ClearOrReflectHostileProjectiles(bool reflect)
        {
            _projectileScratch.Clear();
            _projectileScratch.AddRange(_hostileProjectiles);
            for (int i = 0; i < _projectileScratch.Count; i++)
            {
                MegaProjectile projectile = _projectileScratch[i];
                if (projectile == null) continue;
                if (!reflect || !projectile.Reflect(Vector2.up)) pools.Despawn(projectile.gameObject);
            }
            _projectileScratch.Clear();
        }

        public void RegisterNearMiss(Vector2 position)
        {
            if (Player == null) return;
            float multiplier = Player.Data != null ? Player.Data.passive.nearMissChargeMultiplier : 1f;
            Player.Counter?.AddCharge(12f * multiplier);
            AddScore(_level.nearMissScore);
        }

        public void FireCounterBurst(Vector2 origin, int count, float damage)
        {
            ProjectileData projectile = _selectedAnimal?.primaryWeapon?.projectile;
            if (projectile == null) return;
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? -80f + i * 160f / (count - 1) : 0f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                SpawnProjectile(projectile, MegaFaction.Player, origin, direction, damage, 1.25f, 2, NearestEnemyTransform);
            }
        }

        public void DamageEnemiesInRadius(Vector2 center, float radius, float damage, bool includeBoss)
        {
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                MegaEnemyController enemy = _enemies[i];
                if (enemy != null && Vector2.Distance(center, enemy.transform.position) <= radius) enemy.TakeDamage(damage);
            }
            if (includeBoss && Boss != null) Boss.TakeDamage(damage);
        }

        public void SetHostileTimeScale(float scale, float duration)
        {
            _hostileScaleVersion++;
            StartCoroutine(HostileScaleRoutine(Mathf.Clamp(scale, 0.1f, 1f), duration, _hostileScaleVersion));
        }

        private IEnumerator HostileScaleRoutine(float scale, float duration, int version)
        {
            _hostileTimeScale = scale;
            yield return new WaitForSeconds(duration);
            if (version == _hostileScaleVersion) _hostileTimeScale = 1f;
        }

        public void TryDropPickup(Vector2 position, float chance, MegaPickupType type)
        {
            if (pickupPrefab == null || NextRandom01() > chance) return;
            GameObject go = pools.Spawn(pickupPrefab, position, Quaternion.identity, pickupContainer);
            Sprite sprite = type == MegaPickupType.Health ? healthPickupSprite : counterPickupSprite;
            go.GetComponent<MegaPickupController>()?.Configure(type, sprite, this);
        }

        public void AddScore(int amount)
        {
            _score = Mathf.Max(0, _score + Mathf.Max(0, amount));
            hud.SetScore(_score);
        }

        public bool IsInsideDespawnBounds(Vector2 position)
        {
            Rect b = _level.cameraBounds;
            return position.x >= b.xMin - 2f && position.x <= b.xMax + 2f && position.y >= b.yMin - 2f && position.y <= b.yMax + 2f;
        }

        public bool IsInsideEnemyBounds(Vector2 position)
        {
            Rect b = _level.cameraBounds;
            return position.x >= b.xMin - 3f && position.x <= b.xMax + 3f && position.y >= b.yMin - 3f && position.y <= b.yMax + 3f;
        }

        public float NextRandom01() => _random != null ? (float)_random.NextDouble() : Random.value;

        private Transform GetNearestEnemyTransform()
        {
            Transform best = Boss != null ? Boss.transform : null;
            float bestSqr = best != null && Player != null ? (best.position - Player.transform.position).sqrMagnitude : float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                MegaEnemyController enemy = _enemies[i];
                if (enemy == null) continue;
                float sqr = Player != null ? (enemy.transform.position - Player.transform.position).sqrMagnitude : enemy.transform.position.sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = enemy.transform; }
            }
            return best;
        }

        private void PrewarmPools()
        {
            if (pools == null) return;
            SuperAnimalData[] animals = _level.allowedAnimals;
            for (int i = 0; animals != null && i < animals.Length; i++)
            {
                AddPrefab(animals[i]?.playerPrefab, 1);
                AddPrefab(animals[i]?.primaryWeapon?.projectile?.prefab, 40);
            }
            for (int w = 0; w < _level.waves.Length; w++)
            {
                EnemySpawnGroup[] groups = _level.waves[w].spawnGroups;
                for (int g = 0; groups != null && g < groups.Length; g++)
                {
                    AddPrefab(groups[g]?.enemy?.prefab, _level.maximumActiveEnemies);
                    AddPrefab(groups[g]?.enemy?.projectile?.prefab, _level.maximumHostileProjectiles);
                }
            }
            AddPrefab(_level.boss?.prefab, 1);
            BossPhaseData[] bossPhases = _level.boss?.phases;
            for (int p = 0; bossPhases != null && p < bossPhases.Length; p++)
            {
                BossAttackPattern[] attacks = bossPhases[p]?.attacks;
                for (int a = 0; attacks != null && a < attacks.Length; a++)
                    AddPrefab(attacks[a]?.projectile?.prefab, _level.maximumHostileProjectiles);
            }
            AddPrefab(defaultEnemyProjectile?.prefab, _level.maximumHostileProjectiles);
            AddPrefab(pickupPrefab, 5);
            MegaVFXProfile vfx = _level.vfxProfile;
            if (vfx != null)
            {
                AddPrefab(vfx.hitSparkPrefab, 18);
                AddPrefab(vfx.playerMuzzlePrefab, 12);
                AddPrefab(vfx.enemyMuzzlePrefab, 12);
                AddPrefab(vfx.bossMuzzlePrefab, 8);
                AddPrefab(vfx.explosionPrefab, 10);
                AddPrefab(vfx.eliteExplosionPrefab, 4);
                AddPrefab(vfx.warningPrefab, 4);
                AddPrefab(vfx.nearMissPrefab, 8);
                AddPrefab(vfx.counterReadyPrefab, 2);
                AddPrefab(vfx.bossDeathPrefab, 3);
            }
        }

        private void AddPrefab(GameObject prefab, int count)
        {
            if (prefab == null) return;
            pools.Prewarm(prefab, Mathf.Max(1, count));
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            waveDirector?.StopDirector();
            pools?.DespawnAll();
        }
    }
}
