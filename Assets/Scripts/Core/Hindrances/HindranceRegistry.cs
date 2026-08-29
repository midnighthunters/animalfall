// HindranceRegistry ScriptableObject: maps HindranceType → HindranceData + prefab
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Hindrances
{
    [CreateAssetMenu(fileName = "HindranceRegistry", menuName = "AnimalFall/Hindrance Registry")]
    public class HindranceRegistry : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public HindranceType type;
            public HindranceData data;
        }

        [SerializeField] private Entry[] _entries;

        private readonly Dictionary<HindranceType, HindranceData> _byType =
            new Dictionary<HindranceType, HindranceData>(56);
        private bool _built;

        public IReadOnlyList<Entry> Entries => _entries;

        private void OnEnable() => Rebuild();

        public void Rebuild()
        {
            _byType.Clear();
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry entry = _entries[i];
                    if (entry == null || entry.type == HindranceType.None || entry.data == null) continue;
                    if (!_byType.ContainsKey(entry.type)) _byType.Add(entry.type, entry.data);
                }
            }
            _built = true;
        }

        public HindranceData GetData(HindranceType type)
        {
            if (!_built) Rebuild();
            return _byType.TryGetValue(type, out HindranceData data) ? data : null;
        }

        public bool TryGetData(HindranceType type, out HindranceData data)
        {
            if (!_built) Rebuild();
            return _byType.TryGetValue(type, out data);
        }

        public List<string> ValidateRegistry(bool requireAll = true)
        {
            Rebuild();
            var issues = new List<string>();
            var seen = new HashSet<HindranceType>();
            if (_entries == null) issues.Add("Registry entries array is null.");
            else for (int i = 0; i < _entries.Length; i++)
            {
                Entry e = _entries[i];
                if (e == null || e.data == null) { issues.Add($"Entry {i} has no definition."); continue; }
                if (!seen.Add(e.type)) issues.Add($"Duplicate type: {e.type}.");
                if (e.data.hindranceType != e.type) issues.Add($"{e.type}: definition type mismatch.");
                if (e.data.prefab == null) issues.Add($"{e.type}: prefab missing.");
                else if (e.data.prefab.GetComponent<IHindrance>() == null) issues.Add($"{e.type}: prefab lacks IHindrance.");
                if (e.data.icon == null) issues.Add($"{e.type}: icon missing.");
                if (e.data.baseWeight < 0f || e.data.maxSimultaneous < 1 || e.data.maxDuration < e.data.minDuration)
                    issues.Add($"{e.type}: invalid selection tuning.");
            }
            if (requireAll)
            {
                for (int id = 1; id <= (int)HindranceType.FrogSnatcher; id++)
                    if (!seen.Contains((HindranceType)id)) issues.Add($"Missing type {(HindranceType)id} ({id}).");
            }
            return issues;
        }

#if UNITY_EDITOR
        public void EditorSetEntries(Entry[] entries)
        {
            _entries = entries;
            Rebuild();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
