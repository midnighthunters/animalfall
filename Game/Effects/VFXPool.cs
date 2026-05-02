// ============================================================
//  VFXPool.cs  –  Animal Fall
//  Generic object pool for particle-effect GameObjects.
//  Replaces all Instantiate / Destroy calls in EffectsController.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single-prefab pool.  Request a particle GO, play it, return it
/// automatically after its particle system finishes (or after a
/// configurable max lifetime).
/// </summary>
public class VFXPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int        initialSize    = 8;
    [SerializeField] private float      maxLifetime    = 4f;

    private Queue<GameObject> _free = new();
    private Transform         _container;

    private void Awake()
    {
        _container = new GameObject($"{prefab?.name ?? "null"}_Pool").transform;
        _container.SetParent(transform);
        for (int i = 0; i < initialSize; i++)
            _free.Enqueue(CreateInstance());
    }

    // ── Public ────────────────────────────────────────────────
    public void Spawn(Vector3 worldPos, Quaternion rotation = default)
    {
        if (prefab == null) return;
        GameObject go = _free.Count > 0 ? _free.Dequeue() : CreateInstance();
        go.transform.SetPositionAndRotation(worldPos, rotation);
        go.SetActive(true);

        // Play all particle systems on this GO
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Clear();
            ps.Play();
        }

        StartCoroutine(ReturnAfterDelay(go));
    }

    // ── Private ───────────────────────────────────────────────
    private GameObject CreateInstance()
    {
        var go = Instantiate(prefab, _container);
        go.SetActive(false);
        return go;
    }

    private System.Collections.IEnumerator ReturnAfterDelay(GameObject go)
    {
        yield return new WaitForSeconds(maxLifetime);
        Return(go);
    }

    private void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        _free.Enqueue(go);
    }
}

// ============================================================
//  VFXPoolRegistry.cs  –  Animal Fall
//  Central registry so any system can request a pool by key
//  without needing direct Inspector references.
// ============================================================
public class VFXPoolRegistry : MonoBehaviour
{
    public static VFXPoolRegistry Instance { get; private set; }

    [System.Serializable]
    public struct PoolEntry
    {
        public string  key;
        public VFXPool pool;
    }

    [SerializeField] private PoolEntry[] pools;

    private Dictionary<string, VFXPool> _map = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var entry in pools)
            if (!string.IsNullOrEmpty(entry.key) && entry.pool != null)
                _map[entry.key] = entry.pool;

        EventBus.Publish(new OnPoolWarmed());
    }

    // ── API ───────────────────────────────────────────────────
    /// <summary>Spawn an effect by key.  No-op if key not found.</summary>
    public void Spawn(string key, Vector3 worldPos, Quaternion rotation = default)
    {
        if (_map.TryGetValue(key, out VFXPool pool))
            pool.Spawn(worldPos, rotation);
        else
            Debug.LogWarning($"[VFXPoolRegistry] Key '{key}' not registered.");
    }

    // ── Convenience keys ──────────────────────────────────────
    public const string Collect    = "collect";
    public const string Explosion  = "explosion";
    public const string ShieldBreak= "shieldBreak";
    public const string GoalPop    = "goalPop";
    public const string LevelWin   = "levelWin";
    public const string CoinFly    = "coinFly";
}
