// Task 1.3 — AnimalData ScriptableObject with all required fields
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Data
{
    [CreateAssetMenu(fileName = "AnimalData", menuName = "AnimalFall/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        [Tooltip("Species enum value — drives sprite lookup via ImageLibrary.")]
        public AnimalSpecies species;

        [Tooltip("Type controls special tap behaviour.")]
        public AnimalType type;

        [Tooltip("Movement pattern for this animal.")]
        public MovementPattern movementPattern;

        [Tooltip("Minimum fall speed (world units/s).")]
        [Range(0.5f, 10f)] public float speedMin = 1.5f;

        [Tooltip("Maximum fall speed (world units/s).")]
        [Range(0.5f, 10f)] public float speedMax = 3f;

        [Tooltip("Point value on correct tap.")]
        public int pointValue = 50;

        [Tooltip("Shield HP for Shielded type; 0 for others.")]
        [Range(0, 5)] public int shieldHP;

        [Tooltip("True if this species counts toward the level goal.")]
        public bool isTargetSpecies;

        [Tooltip("Seconds before the animal auto-returns to pool (lifetime).")]
        [Range(1f, 30f)] public float lifetime = 8f;

        [Tooltip("ZigZag/SineWave amplitude in world units.")]
        public float zigzagAmplitude = 0.5f;

        [Tooltip("ZigZag/SineWave frequency.")]
        public float zigzagFrequency = 2f;

        // Legacy: kept for backward compatibility during migration
        public bool requiresDoubleTap;
    }
}
