// Task 1.2 — GoalData ScriptableObject
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Data
{
    [CreateAssetMenu(fileName = "Goal_XX", menuName = "AnimalFall/Goal Data")]
    public class GoalData : ScriptableObject
    {
        [System.Serializable]
        public struct SpeciesTarget
        {
            [Tooltip("Target animal species.")]
            public AnimalSpecies species;
            [Tooltip("Number of this species to rescue.")]
            [Range(1, 50)] public int count;
        }

        [Tooltip("Per-species rescue targets. All listed species must appear in the level spawnPool.")]
        [SerializeField] private SpeciesTarget[] _targets;

        public SpeciesTarget[] Targets => _targets;

        /// <summary>Total rescue count across all species. No LINQ — for loop only.</summary>
        public int TotalCount
        {
            get
            {
                int total = 0;
                if (_targets == null) return 0;
                for (int i = 0; i < _targets.Length; i++) total += _targets[i].count;
                return total;
            }
        }
    }
}
