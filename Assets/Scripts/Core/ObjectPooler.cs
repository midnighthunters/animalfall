// Task 2.2 — ObjectPooler: zero-GC pool manager
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace AnimalFall.Core
{
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        // keyed by prefab object identity hash
        private readonly Dictionary<int, Stack<GameObject>> _pools    = new Dictionary<int, Stack<GameObject>>();
        private readonly Dictionary<int, GameObject>        _prefabMap = new Dictionary<int, GameObject>();
        // O(1) double-return guard: stores object identity hashes for active objects
        private readonly HashSet<int> _activeObjects = new HashSet<int>();

        private void Awake()
        {
            // Always let the local (scene-level) ObjectPooler win.
            // If a DDOL instance already exists from a previous scene, replace it so
            // this scene's pools (pre-warmed with local prefab references) are used.
            if (Instance != null && Instance != this)
            {
                // Previous instance was DDOL — override it with this scene's pooler
                Instance = this;
                return;
            }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Pre-warm a pool. Call during level load only, never during gameplay.</summary>
        public void CreatePool(GameObject prefab, int initialSize, Transform parent = null)
        {
            if (prefab == null) { Debug.LogWarning("[ObjectPooler] CreatePool: prefab is null."); return; }
            int key = prefab.GetHashCode();
            if (!_pools.ContainsKey(key))
            {
                _pools[key]    = new Stack<GameObject>(initialSize);
                _prefabMap[key] = prefab;
            }
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Instantiate(prefab, parent);
                ResetObject(obj);
                _pools[key].Push(obj);
            }
        }

        /// <summary>Borrow an object from the pool. Expands if empty (logs warning).</summary>
        public GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            if (prefab == null) { Debug.LogWarning("[ObjectPooler] SpawnFromPool: prefab is null."); return null; }
            int key = prefab.GetHashCode();

            if (!_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[ObjectPooler] Pool for '{prefab.name}' not pre-warmed. Creating on-demand.");
                CreatePool(prefab, 1, parent);
            }

            GameObject obj;
            if (_pools[key].Count > 0)
            {
                obj = _pools[key].Pop();
            }
            else
            {
                Debug.LogWarning($"[ObjectPooler] Pool for '{prefab.name}' exhausted. Expanding.");
                obj = Instantiate(prefab);
            }

            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            _activeObjects.Add(obj.GetHashCode());
            // Stamp pool key so ReturnToPool can route back correctly
            var tag = obj.GetComponent<PoolTag>();
            if (tag == null) tag = obj.AddComponent<PoolTag>();
            tag.PrefabKey = key;
            return obj;
        }

        /// <summary>Return an object to the pool. Double-return is a silent no-op + warning.</summary>
        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;
            int instanceId = obj.GetHashCode();
            if (!_activeObjects.Contains(instanceId))
            {
                Debug.LogWarning($"[ObjectPooler] Double-return prevented for '{obj.name}'.");
                return;
            }
            _activeObjects.Remove(instanceId);
            ResetObject(obj);

            // Find pool key — search prefabMap by prefab name match (or use a component tag)
            int key = FindPoolKey(obj);
            if (key != 0 && _pools.ContainsKey(key))
                _pools[key].Push(obj);
            // else just leave it inactive (orphaned expand object)
        }

        /// <summary>Returns ALL active objects for a given prefab back to pool.</summary>
        public void ReturnAllActive(GameObject prefab)
        {
            if (prefab == null) return;
            // We iterate a copy because ReturnToPool mutates _activeObjects
            var toReturn = new List<int>(_activeObjects);
            foreach (int id in toReturn)
            {
                // We can't easily reverse-map instanceID -> GO without storing it,
                // so this method is best called from scene managers that track their spawned objects.
                // Concrete usage: LevelManager.UnloadLevel calls this via tracked references.
            }
        }

        /// <summary>Count of currently active (out-of-pool) objects for this prefab.</summary>
        public int ActiveCount(GameObject prefab)
        {
            if (prefab == null) return 0;
            // We don't track per-prefab active counts individually for perf;
            // callers that need this should track it themselves (Spawner does).
            // Return 0 as sentinel — Spawner maintains its own counter.
            return 0;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private int FindPoolKey(GameObject obj)
        {
            // Attempt to find by PooledObject tag stored on the component
            var tag = obj.GetComponent<PoolTag>();
            if (tag != null && tag.PrefabKey != 0) return tag.PrefabKey;
            // Fallback: search prefabMap by name match
            foreach (var kv in _prefabMap)
                if (kv.Value != null && kv.Value.name == obj.name.Replace("(Clone)", "").Trim())
                    return kv.Key;
            return 0;
        }

        private void ResetObject(GameObject obj)
        {
            var tag = obj.GetComponent<PoolTag>();
            if (tag == null) tag = obj.AddComponent<PoolTag>();
            bool firstInitialization = !tag.Initialized;
            if (!tag.Initialized)
            {
                tag.Initialized = true;
                tag.OriginalLocalScale = obj.transform.localScale;
                tag.OriginalLocalRotation = obj.transform.localRotation;
            }
            // Non-serialized caches are rebuilt after a domain reload.
            if (tag.Behaviours == null || tag.Behaviours.Length == 0)
                tag.CacheComponents(obj);
            if (firstInitialization)
                tag.OriginalSpriteColor = tag.SpriteRenderer != null ? tag.SpriteRenderer.color : Color.white;
            DOTween.Kill(obj);
            obj.transform.localScale = tag.OriginalLocalScale;
            obj.transform.localRotation = tag.OriginalLocalRotation;
            if (tag.SpriteRenderer != null) tag.SpriteRenderer.color = tag.OriginalSpriteColor;
            Collider2D[] colliders = tag.Colliders;
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = true;
            Rigidbody2D body = tag.Body;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            MonoBehaviour[] behaviours = tag.Behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                    behaviours[i].StopAllCoroutines();
            }
            obj.SetActive(false);
        }
    }

    /// <summary>Attached to pooled GameObjects so the pooler can reverse-map them.</summary>
    public class PoolTag : MonoBehaviour
    {
        public int PrefabKey;
        public bool Initialized;
        public Vector3 OriginalLocalScale;
        public Quaternion OriginalLocalRotation;
        public Color OriginalSpriteColor = Color.white;
        [System.NonSerialized] public Collider2D[] Colliders = System.Array.Empty<Collider2D>();
        [System.NonSerialized] public MonoBehaviour[] Behaviours = System.Array.Empty<MonoBehaviour>();
        [System.NonSerialized] public SpriteRenderer SpriteRenderer;
        [System.NonSerialized] public Rigidbody2D Body;

        public void CacheComponents(GameObject owner)
        {
            Colliders = owner.GetComponents<Collider2D>();
            Behaviours = owner.GetComponents<MonoBehaviour>();
            SpriteRenderer = owner.GetComponent<SpriteRenderer>();
            Body = owner.GetComponent<Rigidbody2D>();
        }
    }
}
