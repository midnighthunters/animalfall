using System;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "MegaBoss", menuName = "AnimalFall/Mega Shooter/Boss Ship")]
    public sealed class BossShipData : ScriptableObject
    {
        public string stableId;
        public string displayName;
        public MegaVillainArchetype archetype;
        public GameObject prefab;
        public Sprite sprite;
        public Sprite weaponIcon;
        [Range(0.2f, 1f)] public float visualScale = 0.55f;
        public Vector2 colliderSize = new Vector2(2.2f, 1.4f);
        [Min(1f)] public float baseHitPoints = 1200f;
        public Rect movementBounds = new Rect(-3.4f, 2.5f, 6.8f, 2.5f);
        [Min(0)] public int score = 5000;
        [Min(0.1f)] public float entranceDuration = 2f;
        public BossPhaseData[] phases = Array.Empty<BossPhaseData>();
        public GameObject entranceVFX;
        public GameObject deathVFX;
        public AudioClip entranceAudio;
        public AudioClip deathAudio;
    }
}
