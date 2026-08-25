// Task 1.5 — ChapterConfig ScriptableObject
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Data
{
    [CreateAssetMenu(fileName = "ChapterConfig", menuName = "AnimalFall/Chapter Config")]
    public class ChapterConfig : ScriptableObject
    {
        [Tooltip("1-based chapter index (1-5).")]
        [Range(1, 5)] public int chapterIndex;

        [Tooltip("Display name (e.g. Sunny Meadow).")]
        public string chapterName;

        [Tooltip("Camera background color for this chapter.")]
        public Color backgroundColor;

        [Tooltip("Chapter background panel sprite: bg_chapter<N>.png from panels/.")]
        public Sprite backgroundSprite;

        [Tooltip("First level index (1-based, inclusive).")]
        public int firstLevel;

        [Tooltip("Last level index (1-based, inclusive).")]
        public int lastLevel;

        [Tooltip("Focus species for this chapter.")]
        public AnimalSpecies[] focusSpecies;
    }
}
