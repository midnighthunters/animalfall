namespace AnimalFall.Core.Arcade.Shared
{
    public static class DamageCalculator
    {
        public static float Calculate(float relativeVelocityMag, float impactorMass, BlockMaterial material)
        {
            float multiplier = GetMaterialMultiplier(material);
            return relativeVelocityMag * impactorMass * multiplier;
        }

        public static float GetMaterialMultiplier(BlockMaterial material)
        {
            switch (material)
            {
                case BlockMaterial.Glass: return 1.5f;
                case BlockMaterial.Wood:  return 1.0f;
                case BlockMaterial.Stone: return 0.7f;
                case BlockMaterial.Metal: return 0.5f;
                case BlockMaterial.TNT:   return 2.0f;
                default:                  return 1.0f;
            }
        }

        public static float GetMaterialHP(BlockMaterial material)
        {
            switch (material)
            {
                case BlockMaterial.Glass: return 20f;
                case BlockMaterial.Wood:  return 50f;
                case BlockMaterial.Stone: return 100f;
                case BlockMaterial.Metal: return 150f;
                case BlockMaterial.TNT:   return 30f;
                default:                  return 50f;
            }
        }

        public static float CalculateDamageScore(float velocity, float mass)
        {
            return velocity * mass * 10f;
        }
    }
}
