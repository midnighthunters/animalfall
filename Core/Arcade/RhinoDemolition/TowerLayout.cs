using UnityEngine;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    [CreateAssetMenu(fileName = "NewTowerLayout", menuName = "AnimalFall/Tower Layout")]
    public class TowerLayout : ScriptableObject
    {
        [System.Serializable]
        public struct BlockEntry
        {
            public Vector2 position;
            public Vector2 scale;
            public BlockMaterial material;
            public float rotation;
        }

        public BlockEntry[] blocks;
        public Vector2 towerOrigin = new Vector2(5f, -3f);
    }
}
