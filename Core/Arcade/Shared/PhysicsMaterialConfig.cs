using UnityEngine;

namespace AnimalFall.Core.Arcade.Shared
{
    [CreateAssetMenu(fileName = "NewPhysicsMaterialConfig", menuName = "AnimalFall/Physics Material Config")]
    public class PhysicsMaterialConfig : ScriptableObject
    {
        public BlockMaterial materialType;
        public float bounciness = 0.5f;
        public float friction = 0.4f;

        public PhysicsMaterial2D CreateMaterial()
        {
            var mat = new PhysicsMaterial2D(materialType.ToString())
            {
                bounciness = bounciness,
                friction = friction
            };
            return mat;
        }

        public static PhysicsMaterial2D CreateDefault(BlockMaterial type)
        {
            float bounce, fric;
            switch (type)
            {
                case BlockMaterial.Glass: bounce = 0.1f; fric = 0.2f; break;
                case BlockMaterial.Wood:  bounce = 0.3f; fric = 0.5f; break;
                case BlockMaterial.Stone: bounce = 0.3f; fric = 0.5f; break;
                case BlockMaterial.Metal: bounce = 0.6f; fric = 0.3f; break;
                case BlockMaterial.TNT:   bounce = 0.2f; fric = 0.4f; break;
                default:                  bounce = 0.3f; fric = 0.4f; break;
            }

            return new PhysicsMaterial2D(type.ToString())
            {
                bounciness = bounce,
                friction = fric
            };
        }
    }
}
