using System.Collections.Generic;

namespace AnimalFall.Core.Animals
{
    public static class ActiveAnimalRegistry
    {
        private static readonly List<Animal> Animals = new List<Animal>(24);

        public static IReadOnlyList<Animal> All => Animals;

        public static void Register(Animal animal)
        {
            if (animal != null && !Animals.Contains(animal)) Animals.Add(animal);
        }

        public static void Unregister(Animal animal) => Animals.Remove(animal);

        public static Animal GetEligible(int seedOffset = 0)
        {
            int count = Animals.Count;
            if (count == 0) return null;
            int start = UnityEngine.Random.Range(0, count);
            for (int i = 0; i < count; i++)
            {
                Animal animal = Animals[(start + i + seedOffset) % count];
                if (animal != null && animal.gameObject.activeInHierarchy && !animal.IsCollected && !animal.HasExclusiveOwner)
                    return animal;
            }
            return null;
        }

        public static Animal GetEligibleSpecies(AnimalSpecies species, int seedOffset = 0)
        {
            int count = Animals.Count;
            if (count == 0) return null;

            int start = UnityEngine.Random.Range(0, count);
            for (int i = 0; i < count; i++)
            {
                Animal animal = Animals[(start + i + seedOffset) % count];
                if (animal != null &&
                    animal.gameObject.activeInHierarchy &&
                    !animal.IsCollected &&
                    !animal.HasExclusiveOwner &&
                    animal.Data != null &&
                    animal.Data.species == species)
                {
                    return animal;
                }
            }

            return null;
        }


        public static void Clear() => Animals.Clear();
    }
}
