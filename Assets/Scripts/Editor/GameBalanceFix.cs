using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using AnimalFall.Data;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;
using AnimalFall.MegaShooter;

/// <summary>
/// One-shot rebalance: creates distinct-species AnimalData, fixes movement tuning,
/// and rebuilds every normal level's spawn pool + ramping hindrances while
/// preserving each level's existing goal.
/// </summary>
public static class GameBalanceFix
{
    const string ANIMAL_DIR = "Assets/Data/Animals";

    // Species that map to a *distinct* sprite in ImageLibrary.
    // Chicken, Dog, Cow(Elephant), Cat(Panda), Monkey, Pig, Penguin
    static readonly AnimalSpecies[] DistinctSpecies =
    {
        AnimalSpecies.Chicken, AnimalSpecies.Dog, AnimalSpecies.Cow,
        AnimalSpecies.Cat, AnimalSpecies.Monkey, AnimalSpecies.Pig,
        AnimalSpecies.Penguin
    };

    static readonly MovementPattern[] FunPatterns =
    {
        MovementPattern.Drift, MovementPattern.ZigZag, MovementPattern.SineWave,
        MovementPattern.Static, MovementPattern.Bounce
    };

    public static void Execute()
    {
        var sb = new StringBuilder();

        // ── 1. Ensure an AnimalData asset exists for every distinct species ──
        var bySpecies = new Dictionary<AnimalSpecies, AnimalData>();
        foreach (var g in AssetDatabase.FindAssets("t:AnimalData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<AnimalData>(AssetDatabase.GUIDToAssetPath(g));
            if (a != null && a.type == AnimalType.Normal && !bySpecies.ContainsKey(a.species))
                bySpecies[a.species] = a;
        }

        for (int i = 0; i < DistinctSpecies.Length; i++)
        {
            var sp = DistinctSpecies[i];
            if (!bySpecies.TryGetValue(sp, out var data) || data == null)
            {
                data = ScriptableObject.CreateInstance<AnimalData>();
                data.species = sp;
                data.type = AnimalType.Normal;
                AssetDatabase.CreateAsset(data, $"{ANIMAL_DIR}/{sp}.asset");
                sb.AppendLine($"Created AnimalData: {sp}");
                bySpecies[sp] = data;
            }
            // Tune movement — snappy, satisfying fall speeds
            data.movementPattern = FunPatterns[i % FunPatterns.Length];
            data.speedMin = 2.2f;
            data.speedMax = 3.4f;
            data.pointValue = 50;
            data.shieldHP = 0;
            data.isTargetSpecies = true;
            data.lifetime = 12f;
            data.zigzagAmplitude = 1.4f;
            data.zigzagFrequency = 1.6f;
            EditorUtility.SetDirty(data);
        }

        // ── 2. Create a Bomb decoy AnimalData (fast, dangerous) ──
        AnimalData bomb = null;
        foreach (var g in AssetDatabase.FindAssets("t:AnimalData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<AnimalData>(AssetDatabase.GUIDToAssetPath(g));
            if (a != null && a.type == AnimalType.Bomb) { bomb = a; break; }
        }
        if (bomb == null)
        {
            bomb = ScriptableObject.CreateInstance<AnimalData>();
            bomb.type = AnimalType.Bomb;
            bomb.species = AnimalSpecies.Chicken; // shares a sprite; visually a decoy
            AssetDatabase.CreateAsset(bomb, $"{ANIMAL_DIR}/Bomb_Decoy.asset");
            sb.AppendLine("Created AnimalData: Bomb_Decoy");
        }
        bomb.movementPattern = MovementPattern.HeavyFall;
        bomb.speedMin = 2.6f;
        bomb.speedMax = 3.8f;
        bomb.pointValue = 0;
        bomb.isTargetSpecies = false;
        bomb.lifetime = 10f;
        EditorUtility.SetDirty(bomb);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 3. Rebuild each normal level ──
        var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Levels/LevelDatabase.asset");
        if (db == null) { Debug.LogError("[GameBalanceFix] No LevelDatabase"); return; }

        int rebuilt = 0;
        var levels = db.Levels;
        for (int idx = 0; idx < levels.Length; idx++)
        {
            var lvl = levels[idx];
            if (lvl == null || lvl.IsMegaLevel || lvl.Mode == LevelMode.MegaShooter) continue;

            int levelNum = lvl.LevelNumber > 0 ? lvl.LevelNumber : idx + 1;

            // Collect goal target species (must be present in pool)
            var required = new HashSet<AnimalSpecies>();
            if (lvl.Goal != null && lvl.Goal.Targets != null)
                foreach (var t in lvl.Goal.Targets)
                    if (t.species != AnimalSpecies.None) required.Add(t.species);

            // Build pool: all required species + extra distractors, scaling with level
            var pool = new List<AnimalData>();
            foreach (var sp in required)
            {
                var d = ResolveSpecies(bySpecies, sp);
                if (d != null && !pool.Contains(d)) pool.Add(d);
            }

            // Add distractor species (more variety at higher levels)
            int extra = Mathf.Clamp(1 + levelNum / 10, 1, 4);
            for (int e = 0; e < extra; e++)
            {
                var sp = DistinctSpecies[(levelNum + e) % DistinctSpecies.Length];
                var d = ResolveSpecies(bySpecies, sp);
                if (d != null && !pool.Contains(d)) pool.Add(d);
            }

            // Add bomb decoy from level 4 onward
            if (levelNum >= 4 && !pool.Contains(bomb)) pool.Add(bomb);

            if (pool.Count == 0 && bySpecies.Count > 0)
                foreach (var kv in bySpecies) { pool.Add(kv.Value); break; }

            // Difficulty ramp
            float t01 = Mathf.Clamp01((levelNum - 1) / 99f);
            int maxOnScreen = Mathf.RoundToInt(Mathf.Lerp(6, 12, t01));
            float interval = Mathf.Lerp(0.75f, 0.4f, t01);
            float timeLimit = Mathf.Lerp(60f, 75f, t01);

            // Hindrances ramp: introduce more types at higher levels
            var hindrances = BuildHindrances(levelNum);

            ApplyLevel(lvl, pool.ToArray(), maxOnScreen, interval, timeLimit, hindrances);
            rebuilt++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.AppendLine($"Rebuilt {rebuilt} normal levels.");
        Debug.Log("[GameBalanceFix] " + sb.ToString());
    }

    static AnimalData ResolveSpecies(Dictionary<AnimalSpecies, AnimalData> map, AnimalSpecies sp)
    {
        if (map.TryGetValue(sp, out var d) && d != null) return d;
        // fall back to a distinct species that shares this sprite family
        return map.Count > 0 ? System.Linq.Enumerable.First(map.Values) : null;
    }

    static HindranceConfig[] BuildHindrances(int levelNum)
    {
        var list = new List<HindranceConfig>();
        // Simple, well-understood hindrances that layer in gradually.
        void Add(HindranceType t, float w, float delay) =>
            list.Add(new HindranceConfig { type = t, weight = w, initialDelay = delay });

        if (levelNum >= 3)  Add(HindranceType.FallingLeaves, 1.0f, 6f);
        if (levelNum >= 5)  Add(HindranceType.WindGust,      1.0f, 8f);
        if (levelNum >= 7)  Add(HindranceType.IceCube,       0.8f, 8f);
        if (levelNum >= 9)  Add(HindranceType.BubbleShield,  0.8f, 10f);
        if (levelNum >= 12) Add(HindranceType.StormCloud,    0.7f, 10f);
        if (levelNum >= 15) Add(HindranceType.KnightHelmet,  0.7f, 12f);
        if (levelNum >= 18) Add(HindranceType.ZeroGravity,   0.6f, 14f);
        if (levelNum >= 22) Add(HindranceType.GhostAnimal,   0.6f, 12f);
        if (levelNum >= 28) Add(HindranceType.Tornado,       0.5f, 15f);
        return list.ToArray();
    }

    static void ApplyLevel(LevelData lvl, AnimalData[] pool, int maxOnScreen,
                           float interval, float timeLimit, HindranceConfig[] hindrances)
    {
        var so = new SerializedObject(lvl);
        so.FindProperty("_spawnPool").arraySize = pool.Length;
        var poolProp = so.FindProperty("_spawnPool");
        for (int i = 0; i < pool.Length; i++)
            poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];

        so.FindProperty("_maxOnScreen").intValue = maxOnScreen;
        so.FindProperty("_spawnInterval").floatValue = interval;
        so.FindProperty("_spawnVariance").floatValue = 0.15f;
        so.FindProperty("_timeLimit").floatValue = timeLimit;

        // Hindrances
        var hProp = so.FindProperty("_hindrances");
        hProp.arraySize = hindrances.Length;
        for (int i = 0; i < hindrances.Length; i++)
        {
            var el = hProp.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("type").enumValueIndex = (int)hindrances[i].type;
            el.FindPropertyRelative("weight").floatValue = hindrances[i].weight;
            el.FindPropertyRelative("initialDelay").floatValue = hindrances[i].initialDelay;
        }
        so.FindProperty("_maxHindrancesActive").intValue = Mathf.Clamp(1 + hindrances.Length / 4, 1, 3);
        so.FindProperty("_hindranceSpawnInterval").floatValue = 7f;
        so.FindProperty("_hindranceInitialDelay").floatValue = 5f;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(lvl);
    }
}
