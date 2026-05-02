using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    [CreateAssetMenu(fileName = "NewCanyonLayout", menuName = "AnimalFall/Canyon Layout")]
    public class CanyonLayout : ScriptableObject
    {
        [System.Serializable]
        public struct WallSegment
        {
            public Vector2 position;
            public Vector2 size;
            public float rotation;
        }

        [System.Serializable]
        public struct BumperEntry
        {
            public Vector2 position;
            public float radius;
            public float bounciness;
        }

        [System.Serializable]
        public struct ScarabEntry
        {
            public Vector2 position;
        }

        [System.Serializable]
        public struct RockEntry
        {
            public Vector2 position;
            public Vector2 size;
            public float hp;
        }

        public WallSegment[] walls;
        public BumperEntry[] bumpers;
        public ScarabEntry[] scarabs;
        public RockEntry[] breakableRocks;
        public Vector2 exitPitPosition = new Vector2(0, -6f);
        public Vector2 exitPitSize = new Vector2(3f, 0.5f);
    }
}
