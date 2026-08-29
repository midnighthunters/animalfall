using UnityEngine;

namespace AnimalFall.MegaShooter
{
    /// <summary>
    /// Identifies the prefab source for objects managed by the mega-level pool.
    /// This MonoBehaviour intentionally lives in its own same-named file so Unity
    /// can persist the component on generated prefabs without a missing-script entry.
    /// </summary>
    public sealed class MegaPoolMember : MonoBehaviour
    {
        [System.NonSerialized] public GameObject SourcePrefab;
        [System.NonSerialized] private IMegaPoolable[] _poolables;

        public IMegaPoolable[] Poolables
        {
            get
            {
                if (_poolables == null) CachePoolables();
                return _poolables;
            }
        }

        public void CachePoolables()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is IMegaPoolable) count++;

            _poolables = new IMegaPoolable[count];
            int write = 0;
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is IMegaPoolable poolable) _poolables[write++] = poolable;
        }
    }
}
