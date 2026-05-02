using UnityEngine;

namespace AnimalFall.Core.Animals
{
    [CreateAssetMenu(fileName = "NewAnimalData", menuName = "AnimalFall/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;
        public Sprite sprite;
        public AnimalType type = AnimalType.Normal;
        public AnimalSpecies species = AnimalSpecies.None;

        [Header("Gameplay")]
        public int pointValue = 50;
        public bool isTargetSpecies = true;
        public bool requiresDoubleTap;
        public int shieldHP = 1;

        [Header("Movement")]
        public float speedMin = 1f;
        public float speedMax = 2f;
        public float lifetime = 12f;

        [Header("Visuals")]
        public GameObject prefab;
        public Color outlineColor = Color.white;
    }
}
