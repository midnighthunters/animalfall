using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public AnimalData[] spawnPool; // assign normal animals, decoys, bombs too
    public GameObject animalPrefab; // generic animal prefab (will set data on instantiate)
    private LevelData level;
    private bool spawning = false;
    [Header("Settings")]
    public Transform animalContainer;

    private List<GameObject> spawned = new List<GameObject>();

    public void Setup(LevelData lv)
    {
        level = lv;
        Debug.LogFormat("[Spawner] Setup called. level = {0}", level != null ? level.name : "NULL");
        if (level == null)
        {
            Debug.LogWarning("[Spawner] LevelData is null in Setup!");
        }
    }

    public void StartSpawning()
    {
        if (level == null)
        {
            Debug.LogWarning("[Spawner] StartSpawning called but level == null. Did you call Setup(level)?");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] No spawnPoints assigned! Assign some Transforms to spawnPoints in the inspector.");
            return;
        }

        if (spawnPool == null || spawnPool.Length == 0)
        {
            Debug.LogWarning("[Spawner] spawnPool is empty! Assign AnimalData assets to spawnPool in inspector.");
            return;
        }

        if (animalPrefab == null)
        {
            Debug.LogError("[Spawner] animalPrefab is null! Assign the Animal prefab in inspector.");
            return;
        }

        spawning = true;
        Debug.LogFormat("[Spawner] Starting spawn loop. spawnInterval={0}, spawnVariance={1}, maxOnScreen={2}",
            level.spawnInterval, level.spawnVariance, level.maxOnScreen);
        StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        spawning = false;
        StopAllCoroutines();
        Debug.Log("[Spawner] Stopped spawning and stopped all coroutines.");
        // optionally clear current animals
    }

    IEnumerator SpawnLoop()
    {
        Debug.Log("[Spawner] SpawnLoop started.");
        while (spawning)
        {
            // cap on screen
            spawned.RemoveAll(x => x == null);
            if (spawned.Count < level.maxOnScreen)
            {
                SpawnOne();
            }

            float interval = level.spawnInterval + Random.Range(-level.spawnVariance, level.spawnVariance);
            yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
        }
        Debug.Log("[Spawner] SpawnLoop ended (spawning flag false).");
    }

    void SpawnOne()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] SpawnOne aborted: spawnPoints is empty.");
            return;
        }

        AnimalData data = ChooseAnimalData();
        if (data == null)
        {
            Debug.LogWarning("[Spawner] ChooseAnimalData returned null. Check spawnPool contents and level flags.");
            return;
        }

        var spIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[spIndex];
        if (spawnPoint == null)
        {
            Debug.LogWarningFormat("[Spawner] spawnPoint at index {0} is null.", spIndex);
            return;
        }

        Transform parent = animalContainer != null ? animalContainer : transform;
        GameObject obj = Instantiate(animalPrefab, spawnPoint.position, Quaternion.identity, parent);
        if (obj == null)
        {
            Debug.LogError("[Spawner] Failed to Instantiate animalPrefab.");
            return;
        }

        Animal animal = obj.GetComponent<Animal>();
        if (animal == null)
        {
            Debug.LogError("[Spawner] Instantiated prefab does not contain Animal component!");
            Destroy(obj);
            return;
        }

        animal.Setup(data, level);
        spawned.Add(obj);

        Debug.LogFormat("[Spawner] Spawned '{0}' at spawnPoint[{1}] pos={2}. On-screen now: {3}",
            data.displayName ?? data.name, spIndex, spawnPoint.position, spawned.Count);
    }

    AnimalData ChooseAnimalData()
    {
        if (spawnPool == null || spawnPool.Length == 0)
        {
            Debug.LogWarning("[Spawner] spawnPool is null or empty in ChooseAnimalData.");
            return null;
        }

        float r = Random.value;
        if (level != null && level.enableBombs && r < 0.25f)
        {
            var d = System.Array.Find(spawnPool, x => x.type == AnimalType.Bomb);
            if (d != null) return d;
        }
        if (level != null && level.enableShielded && r < 0.15f)
        {
            var d = System.Array.Find(spawnPool, x => x.type == AnimalType.Shielded);
            if (d != null) return d;
        }
        if (level != null && level.enableDecoys && r < 0.2f)
        {
            var d = System.Array.Find(spawnPool, x => x.type == AnimalType.Decoy);
            if (d != null) return d;
        }
        var goldChance = 0.02f;
        if (Random.value < goldChance)
        {
            var g = System.Array.Find(spawnPool, x => x.type == AnimalType.Golden);
            if (g != null) return g;
        }
        var normal = System.Array.FindAll(spawnPool, x => x.type == AnimalType.Normal || x.type == AnimalType.Special);
        if (normal.Length == 0)
        {
            Debug.Log("[Spawner] No Normal/Special animals found in spawnPool. Picking random entry.");
            return spawnPool[Random.Range(0, spawnPool.Length)];
        }
        var chosen = normal[Random.Range(0, normal.Length)];
        return chosen;
    }
}
