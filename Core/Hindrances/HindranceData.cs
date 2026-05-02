using UnityEngine;

namespace AnimalFall.Core.Hindrances
{
    [CreateAssetMenu(fileName = "NewHindranceData", menuName = "AnimalFall/Hindrance Data")]
    public class HindranceData : ScriptableObject
    {
        [Header("Identity")]
        public HindranceType type;
        public string displayName;
        public string description;
        public Sprite icon;
        public HindranceCategory category;

        [Header("Progression")]
        public int minLevel = 1;
        public float spawnWeight = 1f;

        [Header("Behavior")]
        public float duration = 5f;
        public float value = 1f;
        public float cooldown = 3f;

        [Header("Prefab")]
        public GameObject prefab;
    }
}
