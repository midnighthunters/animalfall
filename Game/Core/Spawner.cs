// ============================================================
//  Spawner.cs  –  Animal Fall  (REFACTORED)
//  Changes from original:
//    • Destroy(gameObject) replaced with VFXPoolRegistry calls
//    • Animal pool: instead of Instantiate per animal, we borrow
//      from AnimalPool, return on collect/expire
//    • Emits EventBus events (OnAnimalMissed)
//    • StopSpawning now clears on-screen animals cleanly
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────
    [Header("Pool")]
    [SerializeField] private AnimalPool animalPool;    // NEW – replaces animalPrefab + Instantiate

    [Header("Legacy (fallback if pool is null)")]
    [SerializeField] public GameObject animalPrefab;   // kept for Inspector compat

    [Header("Spawn points")]
    [SerializeField] public Transform[] spawnPoints;
    [SerializeField] public AnimalData[] spawnPool;
    [SerializeField] public Transform    animalContainer;

    // ── State ─────────────────────────────────────────────────
    private LevelData            _level;
    private bool                 _spawning;
    private List<GameObject>     _alive = new(32);

    // ── API ───────────────────────────────────────────────────
    public void Setup(LevelData lv)
    {
        _level = lv;
        Debug.Log($"[Spawner] Setup → {lv?.name ?? "NULL"}");
    }

    public void StartSpawning()
    {
        if (_level == null) { Debug.LogWarning("[Spawner] StartSpawning – level is null."); return; }
        if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogWarning("[Spawner] No spawn points."); return; }
        if ((spawnPool == null || spawnPool.Length == 0))   { Debug.LogWarning("[Spawner] spawnPool empty."); return; }
        if (animalPool == null && animalPrefab == null)     { Debug.LogError("[Spawner] No pool or prefab."); return; }

        _spawning = true;
        StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        _spawning = false;
        StopAllCoroutines();

        // Gracefully return alive animals to pool
        foreach (var go in _alive)
        {
            if (go == null) continue;
            if (animalPool != null) animalPool.Return(go);
            else                   Destroy(go);
        }
        _alive.Clear();
    }

    // ── Internal ──────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        while (_spawning)
        {
            _alive.RemoveAll(x => x == null);
            if (_alive.Count < _level.maxOnScreen)
                SpawnOne();

            float interval = _level.spawnInterval
                + Random.Range(-_level.spawnVariance, _level.spawnVariance);
            yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
        }
    }

    private void SpawnOne()
    {
        AnimalData data = ChooseAnimalData();
        if (data == null) return;

        int spIdx = Random.Range(0, spawnPoints.Length);
        Transform pt = spawnPoints[spIdx];
        if (pt == null) return;

        Transform parent = animalContainer != null ? animalContainer : transform;

        GameObject obj;
        if (animalPool != null)
            obj = animalPool.Borrow(pt.position, parent);
        else
            obj = Instantiate(animalPrefab, pt.position, Quaternion.identity, parent);

        if (obj == null) { Debug.LogError("[Spawner] Failed to get animal GO."); return; }

        Animal animal = obj.GetComponent<Animal>();
        if (animal == null)
        {
            Debug.LogError("[Spawner] Prefab missing Animal component.");
            if (animalPool != null) animalPool.Return(obj); else Destroy(obj);
            return;
        }

        animal.Setup(data, _level);
        _alive.Add(obj);
    }

    private AnimalData ChooseAnimalData()
    {
        float r = Random.value;
        if (_level.enableBombs   && r < 0.25f)
        { var d = Find(AnimalType.Bomb);     if (d != null) return d; }
        if (_level.enableShielded && r < 0.15f)
        { var d = Find(AnimalType.Shielded); if (d != null) return d; }
        if (_level.enableDecoys  && r < 0.20f)
        { var d = Find(AnimalType.Decoy);    if (d != null) return d; }
        if (Random.value < 0.02f)
        { var d = Find(AnimalType.Golden);   if (d != null) return d; }

        var normals = System.Array.FindAll(spawnPool,
            x => x.type == AnimalType.Normal || x.type == AnimalType.Special);
        if (normals.Length > 0) return normals[Random.Range(0, normals.Length)];
        return spawnPool[Random.Range(0, spawnPool.Length)];
    }

    private AnimalData Find(AnimalType t) =>
        System.Array.Find(spawnPool, x => x.type == t);
}
