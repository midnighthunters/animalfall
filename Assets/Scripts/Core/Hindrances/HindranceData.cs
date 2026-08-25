// HindranceData ScriptableObject: data for one hindrance type
using UnityEngine;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Core.Hindrances
{
    [CreateAssetMenu(fileName = "HindranceData", menuName = "AnimalFall/Hindrance Definition")]
    public class HindranceData : ScriptableObject
    {
        [Tooltip("Hindrance type this data describes.")]
        public HindranceType hindranceType;

        [Tooltip("Prefab to spawn via ObjectPooler.")]
        public GameObject prefab;

        [Tooltip("Icon sprite (loaded via ImageLibrary).")]
        public Sprite icon;

        [Tooltip("Minimum level this hindrance can appear.")]
        public int unlockLevel;

        [Tooltip("Display name shown in tutorial toast.")]
        public string displayName;

        [Tooltip("One-line effect description for tutorial toast.")]
        [TextArea(1, 3)] public string effectDescription;

        [Header("Selection")]
        public HindranceCategory category;
        [Min(0f)] public float baseWeight = 1f;
        [Range(0, 3)] public int difficultyTier;
        [Min(0.1f)] public float minDuration = 4f;
        [Min(0.1f)] public float maxDuration = 8f;
        [Min(1)] public int maxSimultaneous = 1;
        [Min(0f)] public float cooldown = 12f;
        public HindranceCompatibilityTag compatibilityTags;
        public HindranceCompatibilityTag exclusionTags;
        public HindranceInputMode inputMode;
        public HindranceTargetScope targetScope;
        public bool normalLevelEligible = true;
        public bool megaLevelEligible;
        public bool debugShowcaseOnly;

        [Header("Tutorial and presentation")]
        [Tooltip("Short first-encounter instruction, preferably under eight words.")]
        public string tutorialInstruction;
        public Sprite[] stateSprites;
        public AudioClip activationSfx;
        public AudioClip successSfx;
        public AudioClip blockedSfx;
        public AudioClip completionSfx;

        [Header("Mechanic tuning")]
        [Min(0f)] public float telegraphDuration = 0.75f;
        [Min(0f)] public float interactionWindow = 4f;
        [Min(0)] public int requiredInteractions = 1;
        public float primaryValue = 1f;
        public float secondaryValue;

        private void OnValidate()
        {
            maxDuration = Mathf.Max(minDuration, maxDuration);
            maxSimultaneous = Mathf.Max(1, maxSimultaneous);
            requiredInteractions = Mathf.Max(1, requiredInteractions);
        }
    }
}
