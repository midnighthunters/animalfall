// Task 6.5 — HindranceManager: weighted spawn loop, active cap, context builder
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;
using AnimalFall.Effects;

namespace AnimalFall.Managers
{
    public class HindranceManager : MonoBehaviour
    {
        [SerializeField] private Transform _hindranceContainer;
        [SerializeField] private HindranceRegistry _registry;

        [SerializeField] private GameManager     _gameManager;
        [SerializeField] private EnvironmentEffects _envEffects;
        [SerializeField] private ScreenEffects   _screenEffects;
        [SerializeField] private AudioManager    _audioManager;
        [SerializeField] private LivesManager    _livesManager;
        [SerializeField] private InputManager    _inputManager;

        private readonly List<IHindrance> _activeHindrances = new List<IHindrance>(5);
        private float[]  _cumulativeWeights;
        private HindranceConfig[] _configs;
        private int      _maxActive;
        private float _spawnInterval;
        private float    _spawnIntervalMultiplier = 1f;
        private float _levelStartTime;
        private HindranceType _lastType;
        private readonly Dictionary<HindranceType, float> _cooldownUntil = new Dictionary<HindranceType, float>();
        private readonly Dictionary<HindranceType, int> _activePerType = new Dictionary<HindranceType, int>();
        private readonly Dictionary<object, float> _intervalMultipliers = new Dictionary<object, float>();
        private HindranceCompatibilityTag _activeTags;
        private readonly List<int> _eligibleIndices = new List<int>(32);
        private readonly List<float> _eligibleWeights = new List<float>(32);
        private System.Random _deterministicRandom;

        // ── Public API ────────────────────────────────────────────────────────

        public void InitForLevel(LevelData level)
        {
            StopAllCoroutines();
            StopHindrances();

            if (level == null || (level.IsMegaLevel && !level.AllowNormalHindrancesInMegaLevel))
            {
                _configs = Array.Empty<HindranceConfig>();
                return;
            }

            _configs   = level.Hindrances;
            _maxActive = level.MaxHindrancesActive;
            _spawnInterval = Mathf.Max(0.1f, level.HindranceSpawnInterval);
            _spawnIntervalMultiplier = 1f;
            _levelStartTime = Time.time;
            _lastType = HindranceType.None;
            _cooldownUntil.Clear();
            _activePerType.Clear();
            _intervalMultipliers.Clear();
            _activeTags = HindranceCompatibilityTag.None;

            BuildWeightTable(level.LevelNumber);

            StartCoroutine(HindranceSpawnLoop(level.HindranceInitialDelay));
        }

        public void StopHindrances()
        {
            StopAllCoroutines();
            for (int i = _activeHindrances.Count - 1; i >= 0; i--)
                _activeHindrances[i]?.Deactivate();
            _activeHindrances.Clear();
            _activePerType.Clear();
            _intervalMultipliers.Clear();
            _activeTags = HindranceCompatibilityTag.None;
        }

        /// <summary>Returns a random active (on-screen) animal for hindrances to target.</summary>
        public Animal GetRandomActiveAnimal()
        {
            return ActiveAnimalRegistry.GetEligible();
        }

        public void OnHindranceDeactivated(IHindrance h)
        {
            _activeHindrances.Remove(h);
            if (_activePerType.TryGetValue(h.Type, out int count))
            {
                if (count <= 1) _activePerType.Remove(h.Type);
                else _activePerType[h.Type] = count - 1;
            }
            RebuildActiveTags();
            GameEvents.OnHindranceDeactivated?.Invoke(h.Type);
        }

        public void SetSpawnIntervalMultiplier(float m) => _spawnIntervalMultiplier = Mathf.Max(0.1f, m);

        public HindranceEffectToken AddSpawnIntervalMultiplier(object owner, float multiplier)
        {
            if (owner == null) return new HindranceEffectToken(null);
            _intervalMultipliers[owner] = Mathf.Max(0.1f, multiplier);
            RecalculateIntervalMultiplier();
            return new HindranceEffectToken(() =>
            {
                _intervalMultipliers.Remove(owner);
                RecalculateIntervalMultiplier();
            });
        }

        public void SetDeterministicSeed(int seed) => _deterministicRandom = new System.Random(seed);

        public void SetMirrorMode(bool on) => _inputManager?.SetMirrorMode(on);

        public Vector2 GetActiveMagnetOffset()
            => _inputManager != null ? _inputManager.MagnetOffset : Vector2.zero;

        /// <summary>Read-only list for tests (P2).</summary>
        public IReadOnlyList<IHindrance> GetActiveHindrances() => _activeHindrances;

        // ── Internal ──────────────────────────────────────────────────────────

        private void BuildWeightTable(int levelNumber)
        {
            if (_configs == null || _configs.Length == 0) { _cumulativeWeights = new float[0]; return; }

            _cumulativeWeights = new float[_configs.Length];
            float running = 0f;
            for (int i = 0; i < _configs.Length; i++)
            {
                if (_configs[i].weight <= 0f)
                {
                    Debug.LogWarning($"[HindranceManager] Config[{i}] has weight <= 0, skipping.");
                    _cumulativeWeights[i] = running;
                    continue;
                }
                running += _configs[i].weight;
                _cumulativeWeights[i] = running;
            }
        }

        private IEnumerator HindranceSpawnLoop(float initialDelay)
        {
            float nextSpawn = Time.time + Mathf.Max(0f, initialDelay);
            while (true)
            {
                if (Time.time >= nextSpawn && _activeHindrances.Count < _maxActive)
                {
                    TrySpawnHindrance();
                    nextSpawn = Time.time + _spawnInterval * _spawnIntervalMultiplier;
                }
                yield return null;
            }
        }

        private void TrySpawnHindrance()
        {
            if (_configs == null || _configs.Length == 0 || _cumulativeWeights.Length == 0) return;

            _eligibleIndices.Clear();
            _eligibleWeights.Clear();
            float total = 0f;
            for (int i = 0; i < _configs.Length; i++)
            {
                HindranceConfig candidateConfig = _configs[i];
                // IceCube and BubbleShield are level-wide animal rules handled during spawn.
                if (candidateConfig != null &&
                    (candidateConfig.type == HindranceType.IceCube || candidateConfig.type == HindranceType.BubbleShield || candidateConfig.type == HindranceType.DogHelmet))
                    continue;

                // Avoid immediate repeats when there are alternatives, but allow a
                // single-hindrance level (such as Level 14) to run on its cadence.
                if (candidateConfig == null || candidateConfig.weight <= 0f ||
                    (_configs.Length > 1 && candidateConfig.type == _lastType)) continue;
                if (Time.time - _levelStartTime < candidateConfig.initialDelay) continue;
                if (_cooldownUntil.TryGetValue(candidateConfig.type, out float until) && Time.time < until) continue;
                if (!_registry.TryGetData(candidateConfig.type, out HindranceData candidate) || candidate == null || candidate.prefab == null) continue;
                if (!candidate.normalLevelEligible || candidate.debugShowcaseOnly) continue;
                if ((candidate.exclusionTags & _activeTags) != 0 || (candidate.compatibilityTags & _activeTags) != 0) continue;
                int activeCount = _activePerType.TryGetValue(candidateConfig.type, out int c) ? c : 0;
                if (activeCount >= candidate.maxSimultaneous) continue;
                total += candidateConfig.weight * Mathf.Max(0.01f, candidate.baseWeight);
                _eligibleIndices.Add(i);
                _eligibleWeights.Add(total);
            }
            if (total <= 0f) return;

            float rand = _deterministicRandom != null
                ? (float)(_deterministicRandom.NextDouble() * total)
                : UnityEngine.Random.Range(0f, total);
            int selectedIdx = _eligibleIndices[0];
            for (int i = 0; i < _eligibleWeights.Count; i++)
            {
                if (rand <= _eligibleWeights[i]) { selectedIdx = _eligibleIndices[i]; break; }
            }

            var config = _configs[selectedIdx];
            if (_registry == null) return;

            var data = _registry.GetData(config.type);
            if (data == null) return;

            var hindrance = HindranceFactory.CreateAtRandomScreenTop(data, _hindranceContainer);
            if (hindrance == null) return;

            hindrance.Activate(BuildContext());
            _activeHindrances.Add(hindrance);
            _lastType = hindrance.Type;
            _activePerType[hindrance.Type] = _activePerType.TryGetValue(hindrance.Type, out int active) ? active + 1 : 1;
            _activeTags |= data.compatibilityTags;
            _cooldownUntil[hindrance.Type] = Time.time + Mathf.Max(0f, data.cooldown);
            GameEvents.OnHindranceActivated?.Invoke(hindrance.Type);
        }

        private void RecalculateIntervalMultiplier()
        {
            float value = 1f;
            foreach (float multiplier in _intervalMultipliers.Values) value *= multiplier;
            _spawnIntervalMultiplier = Mathf.Clamp(value, 0.2f, 4f);
        }

        private void RebuildActiveTags()
        {
            _activeTags = HindranceCompatibilityTag.None;
            for (int i = 0; i < _activeHindrances.Count; i++)
            {
                IHindrance h = _activeHindrances[i];
                HindranceData data = h != null ? _registry.GetData(h.Type) : null;
                if (data != null) _activeTags |= data.compatibilityTags;
            }
        }

        private HindranceContext BuildContext() => new HindranceContext
        {
            GameManager       = _gameManager,
            HindranceManager  = this,
            EnvironmentEffects = _envEffects,
            ScreenEffects     = _screenEffects,
            AudioManager      = _audioManager,
            LivesManager      = _livesManager,
            InputManager      = _inputManager
        };
    }
}
