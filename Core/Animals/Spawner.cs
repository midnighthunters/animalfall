using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Levels;

namespace AnimalFall.Core.Animals
{
    public class Spawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private AnimalData[] spawnPool;
        [SerializeField] private GameObject animalPrefab;
        [SerializeField] private Transform animalContainer;

        private LevelData level;
        private bool spawning;
        private readonly List<GameObject> spawned = new List<GameObject>();

        public void Setup(LevelData levelData)
        {
            level = levelData;
            if (level == null)
                Debug.LogWarning("[Spawner] LevelData is null in Setup.");
        }

        public void StartSpawning()
        {
            if (level == null || spawnPoints == null || spawnPoints.Length == 0 ||
                spawnPool == null || spawnPool.Length == 0 || animalPrefab == null)
            {
                Debug.LogWarning("[Spawner] Cannot start spawning — missing references.");
                return;
            }

            spawning = true;
            StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            spawning = false;
            StopAllCoroutines();
        }

        private IEnumerator SpawnLoop()
        {
            while (spawning)
            {
                spawned.RemoveAll(x => x == null);
                if (spawned.Count < level.maxOnScreen)
                    SpawnOne();

                float interval = level.spawnInterval + Random.Range(-level.spawnVariance, level.spawnVariance);
                yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
            }
        }

        private void SpawnOne()
        {
            AnimalData data = ChooseAnimalData();
            if (data == null) return;

            int spIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[spIndex];
            if (spawnPoint == null) return;

            Transform parent = animalContainer != null ? animalContainer : transform;
            GameObject obj = Instantiate(animalPrefab, spawnPoint.position, Quaternion.identity, parent);
            if (obj == null) return;

            Animal animal = obj.GetComponent<Animal>();
            if (animal == null)
            {
                Destroy(obj);
                return;
            }

            animal.Setup(data, level);
            spawned.Add(obj);
        }

        private AnimalData ChooseAnimalData()
        {
            float r = Random.value;

            if (level.enableBombs && r < 0.25f)
            {
                var d = System.Array.Find(spawnPool, x => x.type == AnimalType.Bomb);
                if (d != null) return d;
            }

            if (level.enableShielded && r < 0.15f)
            {
                var d = System.Array.Find(spawnPool, x => x.type == AnimalType.Shielded);
                if (d != null) return d;
            }

            if (level.enableDecoys && r < 0.2f)
            {
                var d = System.Array.Find(spawnPool, x => x.type == AnimalType.Decoy);
                if (d != null) return d;
            }

            if (Random.value < 0.02f)
            {
                var g = System.Array.Find(spawnPool, x => x.type == AnimalType.Golden);
                if (g != null) return g;
            }

            var normal = System.Array.FindAll(spawnPool, x =>
                x.type == AnimalType.Normal || x.type == AnimalType.Special);

            if (normal.Length == 0)
                return spawnPool[Random.Range(0, spawnPool.Length)];

            return normal[Random.Range(0, normal.Length)];
        }
    }
}
