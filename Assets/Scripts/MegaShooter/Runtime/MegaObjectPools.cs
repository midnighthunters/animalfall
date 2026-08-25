using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public interface IMegaPoolable
    {
        void OnMegaSpawned();
        void OnMegaDespawned();
    }

    public sealed class MegaPoolMember : MonoBehaviour
    {
        [System.NonSerialized] public GameObject SourcePrefab;
    }

    public sealed class MegaObjectPools : MonoBehaviour
    {
        public static MegaObjectPools Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _available = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<int, GameObject> _active = new Dictionary<int, GameObject>();
        private readonly List<GameObject> _cleanup = new List<GameObject>(160);
        private bool _isDestroying;

        public int ActiveCount => _active.Count;
        public int PoolMisses { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            _isDestroying = true;
            if (Instance == this) Instance = null;
        }

        public void Prewarm(GameObject prefab, int count, Transform parent = null)
        {
            if (prefab == null || count <= 0) return;
            if (!_available.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>(count);
                _available.Add(prefab, queue);
            }

            while (queue.Count < count)
                queue.Enqueue(Create(prefab, parent));
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            if (!_available.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>(4);
                _available.Add(prefab, queue);
            }

            GameObject instance;
            if (queue.Count > 0)
            {
                instance = queue.Dequeue();
            }
            else
            {
                PoolMisses++;
                instance = Create(prefab, parent);
            }

            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            _active[instance.GetInstanceID()] = instance;
            Notify(instance, true);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            int id = instance.GetInstanceID();
            if (!_active.Remove(id)) return;

            Notify(instance, false);
            instance.SetActive(false);
            // Scene teardown order is not deterministic. Never reparent into a pool whose
            // Transform is already being destroyed; the scene will reclaim the instance.
            if (_isDestroying) return;
            instance.transform.SetParent(transform, false);
            MegaPoolMember member = instance.GetComponent<MegaPoolMember>();
            if (member != null && member.SourcePrefab != null && _available.TryGetValue(member.SourcePrefab, out Queue<GameObject> queue))
                queue.Enqueue(instance);
        }

        public void DespawnAll()
        {
            _cleanup.Clear();
            foreach (KeyValuePair<int, GameObject> pair in _active)
                if (pair.Value != null) _cleanup.Add(pair.Value);
            for (int i = 0; i < _cleanup.Count; i++) Despawn(_cleanup[i]);
            _cleanup.Clear();
        }

        private GameObject Create(GameObject prefab, Transform parent)
        {
            GameObject instance = Instantiate(prefab, parent != null ? parent : transform);
            MegaPoolMember member = instance.GetComponent<MegaPoolMember>();
            if (member == null) member = instance.AddComponent<MegaPoolMember>();
            member.SourcePrefab = prefab;
            instance.SetActive(false);
            return instance;
        }

        private static void Notify(GameObject instance, bool spawned)
        {
            MonoBehaviour[] behaviours = instance.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IMegaPoolable poolable) continue;
                if (spawned) poolable.OnMegaSpawned();
                else poolable.OnMegaDespawned();
            }
        }
    }
}
