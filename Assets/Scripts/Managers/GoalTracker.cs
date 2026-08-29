// GoalTracker — per-species remaining counts; fires win when all targets hit 0
using System;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Data;

namespace AnimalFall.Managers
{
    public class GoalTracker : MonoBehaviour
    {
        public static GoalTracker Instance { get; private set; }

        /// <summary>species → remaining count needed.</summary>
        private readonly Dictionary<AnimalSpecies, int> _remaining = new Dictionary<AnimalSpecies, int>();
        private readonly Dictionary<AnimalSpecies, int> _targets   = new Dictionary<AnimalSpecies, int>();
        private bool _initialized;
        private bool _completed;

        public event Action<AnimalSpecies, int, int> OnGoalProgress; // species, remaining, target
        public event Action OnAllGoalsComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnEnable()  => GameEvents.OnAnimalCollected += HandleCollected;
        private void OnDisable() => GameEvents.OnAnimalCollected -= HandleCollected;

        public void Setup(GoalData goal)
        {
            _remaining.Clear();
            _targets.Clear();
            _completed = false;
            _initialized = false;

            if (goal == null || goal.Targets == null || goal.Targets.Length == 0)
            {
                Debug.LogWarning("[GoalTracker] No goal data — win condition disabled.");
                return;
            }

            for (int i = 0; i < goal.Targets.Length; i++)
            {
                var t = goal.Targets[i];
                if (t.species == AnimalSpecies.None || t.count <= 0) continue;
                // Goals are keyed by species. Accumulate repeated authored entries
                // instead of replacing an earlier target and ending the level too soon.
                int current = _remaining.TryGetValue(t.species, out int value) ? value : 0;
                _remaining[t.species] = current + t.count;
                _targets[t.species] = current + t.count;
            }

            _initialized = _remaining.Count > 0;
            foreach (var target in _remaining)
                OnGoalProgress?.Invoke(target.Key, target.Value, _targets[target.Key]);
        }

        public IReadOnlyDictionary<AnimalSpecies, int> Remaining => _remaining;
        public IReadOnlyDictionary<AnimalSpecies, int> Targets   => _targets;
        public bool IsComplete => _completed;

        public int GetRemaining(AnimalSpecies species)
            => _remaining.TryGetValue(species, out int v) ? v : 0;

        public int GetTarget(AnimalSpecies species)
            => _targets.TryGetValue(species, out int v) ? v : 0;

        public bool IsTargetSpecies(AnimalSpecies species)
            => _targets.ContainsKey(species);

        public bool TrySwapFirstUnfinishedGoal(Spawner spawner)
        {
            if (!_initialized || _completed || spawner == null) return false;
            AnimalSpecies from = AnimalSpecies.None;
            foreach (var pair in _remaining)
                if (pair.Value > 0) { from = pair.Key; break; }
            if (from == AnimalSpecies.None) return false;

            AnimalSpecies to = AnimalSpecies.None;
            for (int id = 1; id <= (int)AnimalSpecies.Raccoon; id++)
            {
                AnimalSpecies candidate = (AnimalSpecies)id;
                if (candidate != from && spawner.ContainsSpecies(candidate)) { to = candidate; break; }
            }
            if (to == AnimalSpecies.None) return false;

            int remaining = _remaining[from];
            int collected = Mathf.Max(0, _targets[from] - remaining);
            _remaining.Remove(from); _targets.Remove(from);
            _remaining[to] = remaining;
            _targets[to] = remaining + collected;
            OnGoalProgress?.Invoke(to, remaining, _targets[to]);
            return true;
        }

        private void HandleCollected(AnimalSpecies species, AnimalType type, Vector3 _)
        {
            if (!_initialized || _completed) return;
            if (!_remaining.ContainsKey(species)) return;

            int left = Mathf.Max(0, _remaining[species] - 1);
            _remaining[species] = left;
            OnGoalProgress?.Invoke(species, left, _targets[species]);

            if (AllDone())
            {
                _completed = true;
                OnAllGoalsComplete?.Invoke();
            }
        }

        private bool AllDone()
        {
            foreach (var kv in _remaining)
                if (kv.Value > 0) return false;
            return true;
        }
    }
}
