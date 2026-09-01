using UnityEngine;

namespace AnimalFall
{
    /// <summary>
    /// Configuration data for a booster item.
    /// Defines initial count, VFX references, and activation parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "BoosterData", menuName = "AnimalFall/Booster Data", order = 3)]
    public class BoosterData : ScriptableObject
    {
        [Header("Booster Info")]
        [Tooltip("Type of booster (Bomb, Rainbow, or Rocket)")]
        public BoosterType boosterType = BoosterType.Bomb;

        [Tooltip("Display name for UI")]
        public string displayName = "Bomb";

        [Tooltip("Initial count when level starts")]
        public int initialCount = 3;

        [Header("Visual Effects")]
        [Tooltip("VFX prefab spawned when booster is activated")]
        public GameObject activationVFX;

        [Tooltip("Icon sprite for the booster button")]
        public Sprite icon;

        [Header("Booster-Specific Settings")]
        [Tooltip("For Rocket: width of the vertical lane")]
        public float rocketLaneWidth = 1.5f;

        [Tooltip("Cascade delay between affecting multiple animals")]
        public float cascadeDelay = 0.03f;
    }
}
