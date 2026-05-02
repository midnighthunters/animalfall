using UnityEngine;

namespace AnimalFall.Core.Skins
{
    [CreateAssetMenu(fileName = "NewSkinData", menuName = "AnimalFall/Skin Data")]
    public class SkinData : ScriptableObject
    {
        [Header("Identity")]
        public string skinId;
        public string displayName;
        public string description;
        public Sprite previewSprite;

        [Header("Category")]
        public SkinCategory category;
        public SkinRarity rarity;

        [Header("Cost")]
        public int costCoins = 200;
        public bool isPremium;

        [Header("Visuals")]
        public Sprite animalSprite;
        public Color tintColor = Color.white;
        public RuntimeAnimatorController animatorOverride;

        [Header("Species")]
        public Animals.AnimalSpecies targetSpecies = Animals.AnimalSpecies.None;
    }
}
