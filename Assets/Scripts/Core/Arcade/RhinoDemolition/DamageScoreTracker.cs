using UnityEngine;
using AnimalFall.Core.Arcade.Shared;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    public class DamageScoreTracker : MonoBehaviour
    {
        public static DamageScoreTracker Instance { get; private set; }

        public float TotalDamageScore { get; private set; }
        public float RequiredDamageScore { get; private set; }
        public bool IsTargetMet => TotalDamageScore >= RequiredDamageScore;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Configure(float required)
        {
            RequiredDamageScore = required;
            TotalDamageScore = 0;
        }

        public void RegisterBlockDestruction(float velocity, float mass)
        {
            float score = DamageCalculator.CalculateDamageScore(velocity, mass);
            TotalDamageScore += score;
        }

        public void AddDamageScore(float score)
        {
            TotalDamageScore += score;
        }

        public void Reset()
        {
            TotalDamageScore = 0;
        }
    }
}
