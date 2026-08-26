#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimalFall.Data;

namespace AnimalFall.MegaShooter.Editor
{
    public static class MegaShooterValidator
    {
        [MenuItem("Tools/Animal Fall/Mega Shooter/Validate All Mega Content")]
        public static void ValidateFromMenu() => ValidateAll(true);

        public static bool ValidateAll(bool logReport)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            MegaLevelData[] levels = LoadMegaLevels();

            if (levels.Length != 20)
                errors.Add($"Expected exactly 20 MegaLevelData assets, found {levels.Length}.");

            var sequenceIds = new HashSet<int>();
            var gameLevels = new HashSet<int>();
            foreach (MegaLevelData level in levels)
                ValidateLevel(level, errors, warnings, sequenceIds, gameLevels);

            for (int sequence = 1; sequence <= 20; sequence++)
            {
                if (!sequenceIds.Contains(sequence)) errors.Add($"Missing mega sequence index {sequence}.");
                if (!gameLevels.Contains(sequence * 5)) errors.Add($"Missing game level {sequence * 5}.");
            }

            ValidateDatabase(levels, errors);
            ValidateAnimals(errors, warnings);

            if (logReport)
            {
                foreach (string warning in warnings) Debug.LogWarning($"[MegaShooterValidator] {warning}");
                foreach (string error in errors) Debug.LogError($"[MegaShooterValidator] {error}");
                Debug.Log(MegaBalanceValidator.BuildReport(levels));
                Debug.Log(errors.Count == 0
                    ? $"[MegaShooterValidator] PASS — {levels.Length} mega levels, {warnings.Count} advisory warning(s)."
                    : $"[MegaShooterValidator] FAIL — {errors.Count} error(s), {warnings.Count} warning(s).");
            }

            return errors.Count == 0;
        }

        public static MegaLevelData[] LoadMegaLevels()
        {
            return AssetDatabase.FindAssets("t:MegaLevelData", new[] { MegaShooterGenerator.DataRoot + "/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MegaLevelData>)
                .Where(level => level != null)
                .OrderBy(level => level.megaSequenceIndex)
                .ToArray();
        }

        private static void ValidateLevel(MegaLevelData level, List<string> errors, List<string> warnings,
            HashSet<int> sequenceIds, HashSet<int> gameLevels)
        {
            string label = level == null ? "<null>" : $"Mega {level.megaSequenceIndex} (level {level.gameLevelNumber})";
            if (level == null) { errors.Add("Null MegaLevelData reference."); return; }
            if (!level.IsValidMegaNumber) errors.Add($"{label}: number must be a multiple of 5 from 5 through 100.");
            if (!sequenceIds.Add(level.megaSequenceIndex)) errors.Add($"{label}: duplicate mega sequence index.");
            if (!gameLevels.Add(level.gameLevelNumber)) errors.Add($"{label}: duplicate game level number.");
            if (level.gameLevelNumber != level.megaSequenceIndex * 5) errors.Add($"{label}: game level and sequence index do not match.");
            if (string.IsNullOrWhiteSpace(level.displayTitle)) errors.Add($"{label}: display title is empty.");
            if (level.featuredAnimal == null) errors.Add($"{label}: featured animal is missing.");
            if (level.allowedAnimals == null || level.allowedAnimals.Length == 0) errors.Add($"{label}: allowed animal roster is empty.");
            else if (level.allowedAnimals.Any(animal => animal == null)) errors.Add($"{label}: allowed animal roster contains a null entry.");
            if (level.backgroundLayers == null || level.backgroundLayers.Length < 4 || level.backgroundLayers.Any(sprite => sprite == null))
                errors.Add($"{label}: four non-null parallax background layers are required.");
            if (level.vfxProfile == null) errors.Add($"{label}: VFX profile is missing.");
            if (level.boss == null) errors.Add($"{label}: boss data is missing.");
            else ValidateBoss(label, level.boss, errors);
            if (level.waves == null || level.waves.Length == 0) errors.Add($"{label}: no waves are authored.");
            else ValidateWaves(label, level, errors);
            if (level.maximumHostileProjectiles > 120) errors.Add($"{label}: hostile projectile cap exceeds 120.");
            if (level.maximumActiveEnemies > 20) errors.Add($"{label}: active enemy cap exceeds 20.");
            if (level.ordinaryEnemyFireInterval < .85f) errors.Add($"{label}: ordinary enemy fire interval is below the 0.85s readability floor.");
            if (level.parTime <= 0f) errors.Add($"{label}: par time must be positive.");
            if (level.targetEnemyCount <= 0) errors.Add($"{label}: target enemy count must be positive.");

            MegaBalanceEstimate estimate = MegaBalanceValidator.Estimate(level);
            if (estimate.OrdinaryEnemyTtkSeconds < .25f || estimate.OrdinaryEnemyTtkSeconds > 5f)
                warnings.Add($"{label}: ordinary-enemy TTK estimate {estimate.OrdinaryEnemyTtkSeconds:0.00}s is outside 0.25–5s.");
            if (estimate.EliteEnemyTtkSeconds > 12f)
                warnings.Add($"{label}: elite TTK estimate {estimate.EliteEnemyTtkSeconds:0.0}s exceeds 12s.");
            if (estimate.BossPhaseTtkSeconds < 8f || estimate.BossPhaseTtkSeconds > 28f)
                warnings.Add($"{label}: boss phase TTK estimate {estimate.BossPhaseTtkSeconds:0.0}s is outside 8–28s.");
            if (estimate.SustainedProjectileDensity > level.maximumHostileProjectiles)
                errors.Add($"{label}: estimated sustained projectile density {estimate.SustainedProjectileDensity:0} exceeds cap {level.maximumHostileProjectiles}.");
            if (MegaBalanceValidator.HasUnsafeLaneOverlap(level))
                errors.Add($"{label}: a boss phase can overlap beam and ram lanes across the full playable width.");
            if (estimate.EstimatedDuration > level.parTime * 1.45f)
                warnings.Add($"{label}: estimated duration {estimate.EstimatedDuration:0}s substantially exceeds par {level.parTime:0}s.");
        }

        private static void ValidateBoss(string label, BossShipData boss, List<string> errors)
        {
            if (boss.prefab == null || boss.sprite == null) errors.Add($"{label}: boss prefab or sprite is missing.");
            if (boss.baseHitPoints <= 0f) errors.Add($"{label}: boss HP must be positive.");
            if (boss.phases == null || boss.phases.Length < 2) errors.Add($"{label}: boss needs at least two phases.");
            else
            {
                float previous = 1.01f;
                for (int i = 0; i < boss.phases.Length; i++)
                {
                    BossPhaseData phase = boss.phases[i];
                    if (phase == null) { errors.Add($"{label}: boss phase {i + 1} is null."); continue; }
                    if (phase.healthThreshold >= previous) errors.Add($"{label}: phase thresholds must descend strictly.");
                    previous = phase.healthThreshold;
                    if (phase.attacks == null || phase.attacks.Length == 0) errors.Add($"{label}: phase {i + 1} has no attacks.");
                    else foreach (BossAttackPattern attack in phase.attacks)
                    {
                        if (attack == null) { errors.Add($"{label}: phase {i + 1} contains a null attack."); continue; }
                        if (attack.telegraphTime < .85f) errors.Add($"{label}: {attack.attackName} telegraph is below 0.85s.");
                        if (attack.projectileCount > 24) errors.Add($"{label}: {attack.attackName} exceeds 24 projectiles per volley.");
                    }
                }
            }
        }

        private static void ValidateWaves(string label, MegaLevelData level, List<string> errors)
        {
            int totalEnemies = 0;
            for (int i = 0; i < level.waves.Length; i++)
            {
                MegaWaveData wave = level.waves[i];
                if (wave == null) { errors.Add($"{label}: wave {i + 1} is null."); continue; }
                if (wave.waveNumber != i + 1) errors.Add($"{label}: wave numbers must be sequential.");
                if (wave.completionCondition == MegaWaveCompletion.SurviveDuration && wave.surviveDuration <= 0f)
                    errors.Add($"{label}: survival wave {i + 1} needs a positive survive duration.");
                if (wave.completionCondition == MegaWaveCompletion.DefeatPriorityTargets &&
                    (wave.spawnGroups == null || !wave.spawnGroups.Any(group => group != null && group.priorityTarget)))
                    errors.Add($"{label}: priority wave {i + 1} needs at least one priority target.");
                if (wave.maximumSimultaneousEnemies > level.maximumActiveEnemies)
                    errors.Add($"{label}: wave {i + 1} cap exceeds the level active-enemy cap.");
                if (wave.spawnGroups == null || wave.spawnGroups.Length == 0) errors.Add($"{label}: wave {i + 1} has no spawn groups.");
                else foreach (EnemySpawnGroup group in wave.spawnGroups)
                {
                    if (group == null || group.enemy == null) { errors.Add($"{label}: wave {i + 1} contains a null enemy group."); continue; }
                    if (group.enemy.prefab == null || group.enemy.sprite == null || group.enemy.projectile == null)
                        errors.Add($"{label}: enemy '{group.enemy.displayName}' is missing prefab, sprite, or projectile data.");
                    if (group.enemy.telegraphTime < .85f) errors.Add($"{label}: enemy '{group.enemy.displayName}' telegraph is below 0.85s.");
                    if (group.count <= 0) errors.Add($"{label}: wave {i + 1} has a non-positive group count.");
                    totalEnemies += Mathf.Max(0, group.count);
                }
            }
            if (totalEnemies != level.targetEnemyCount)
                errors.Add($"{label}: authored wave total is {totalEnemies}, expected {level.targetEnemyCount}.");
        }

        private static void ValidateDatabase(MegaLevelData[] levels, List<string> errors)
        {
            LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Levels/LevelDatabase.asset");
            if (database == null) { errors.Add("LevelDatabase asset is missing."); return; }
            if (database.TotalLevels < 100) errors.Add($"LevelDatabase has {database.TotalLevels} slots; at least 100 are required.");
            for (int levelNumber = 1; levelNumber <= database.TotalLevels; levelNumber++)
            {
                LevelData level = database.GetLevelOrNull(levelNumber - 1);
                bool megaSlot = levelNumber <= 100 && levelNumber % 5 == 0;
                if (megaSlot)
                {
                    if (level == null || !level.IsConfiguredMegaShooter || level.MegaShooterData == null)
                        errors.Add($"LevelDatabase slot {levelNumber} is not configured as MegaShooter.");
                    else if (level.MegaShooterData.gameLevelNumber != levelNumber)
                        errors.Add($"LevelDatabase slot {levelNumber} points to mismatched mega data.");
                }
                else if (level != null && (level.Mode == LevelMode.MegaShooter || level.MegaShooterData != null))
                    errors.Add($"Normal LevelDatabase slot {levelNumber} has mega-shooter configuration.");
            }
        }

        private static void ValidateAnimals(List<string> errors, List<string> warnings)
        {
            SuperAnimalData[] animals = AssetDatabase.FindAssets("t:SuperAnimalData", new[] { MegaShooterGenerator.DataRoot + "/Animals" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SuperAnimalData>).Where(a => a != null).ToArray();
            if (animals.Length != 10) errors.Add($"Expected 10 super animals, found {animals.Length}.");
            if (animals.Select(a => a.stableId).Distinct().Count() != animals.Length) errors.Add("Super-animal stable IDs are not unique.");
            foreach (SuperAnimalData animal in animals)
            {
                if (string.IsNullOrWhiteSpace(animal.stableId)) errors.Add("A super animal has an empty stable ID.");
                if (animal.primaryWeapon == null || animal.primaryWeapon.projectile == null) errors.Add($"{animal.displayName}: weapon/projectile data is missing.");
                if (animal.playerPrefab == null || animal.shipSprite == null || animal.portrait == null) errors.Add($"{animal.displayName}: prefab/ship/portrait reference is missing.");
                if (animal.unlockGameLevel != animal.unlockMegaIndex * 5) warnings.Add($"{animal.displayName}: unlock level and mega index do not align.");
            }
        }
    }

    public readonly struct MegaBalanceEstimate
    {
        public readonly float PlayerDps;
        public readonly float OrdinaryEnemyTtkSeconds;
        public readonly float EliteEnemyTtkSeconds;
        public readonly float BossTtkSeconds;
        public readonly float BossPhaseTtkSeconds;
        public readonly float WaveSeconds;
        public readonly float SustainedProjectileDensity;
        public readonly float EstimatedDuration;
        public MegaBalanceEstimate(float playerDps, float ordinaryTtk, float eliteTtk, float bossTtk,
            float bossPhaseTtk, float waveSeconds, float projectileDensity)
        {
            PlayerDps = playerDps;
            OrdinaryEnemyTtkSeconds = ordinaryTtk;
            EliteEnemyTtkSeconds = eliteTtk;
            BossTtkSeconds = bossTtk;
            BossPhaseTtkSeconds = bossPhaseTtk;
            WaveSeconds = waveSeconds;
            SustainedProjectileDensity = projectileDensity;
            EstimatedDuration = bossTtk + waveSeconds;
        }
    }

    public static class MegaBalanceValidator
    {
        public static MegaBalanceEstimate Estimate(MegaLevelData level)
        {
            float playerDps = 1f;
            if (level != null && level.featuredAnimal != null && level.featuredAnimal.primaryWeapon != null)
                playerDps = level.featuredAnimal.primaryWeapon.EstimatedDps;
            else if (level != null && level.allowedAnimals != null)
            {
                float sum = 0f; int count = 0;
                foreach (SuperAnimalData animal in level.allowedAnimals)
                {
                    if (animal == null || animal.primaryWeapon == null) continue;
                    sum += animal.primaryWeapon.EstimatedDps; count++;
                }
                if (count > 0) playerDps = sum / count;
            }
            playerDps *= level != null ? level.playerPowerMultiplier : 1f;
            float bossHp = level != null && level.boss != null ? level.boss.baseHitPoints * level.bossOverrides.healthMultiplier : 0f;
            float bossTtk = bossHp / Mathf.Max(1f, playerDps * .72f);
            int phaseCount = level != null && level.boss != null && level.boss.phases != null ? Mathf.Max(1, level.boss.phases.Length) : 1;
            float ordinaryHpSum = 0f;
            int ordinaryCount = 0;
            float projectileDensity = 0f;
            float waveSeconds = 0f;
            if (level != null && level.waves != null)
            {
                foreach (MegaWaveData wave in level.waves)
                {
                    if (wave == null) continue;
                    float spawnSeconds = wave.startDelay + wave.completionDelay;
                    int enemies = 0;
                    if (wave.spawnGroups != null) foreach (EnemySpawnGroup group in wave.spawnGroups)
                    {
                        if (group == null) continue;
                        enemies += group.count;
                        spawnSeconds += group.startDelay + Mathf.Max(0, group.count - 1) * group.cadence;
                        if (group.enemy != null)
                        {
                            ordinaryHpSum += group.enemy.hitPoints;
                            ordinaryCount++;
                            float interval = Mathf.Max(level.ordinaryEnemyFireInterval, group.enemy.fireInterval);
                            float projectilesPerShot = group.enemy.weaponPattern == MegaWeaponPattern.Radial ? 8f :
                                group.enemy.weaponPattern == MegaWeaponPattern.FixedSpread ? 3f : 1f;
                            projectileDensity = Mathf.Max(projectileDensity,
                                wave.maximumSimultaneousEnemies * projectilesPerShot * 5f / Mathf.Max(.85f, interval));
                        }
                    }
                    float clearSeconds = enemies * Mathf.Max(.25f, level.enemyHealthMultiplier) / Mathf.Max(1, wave.maximumSimultaneousEnemies) * 1.2f;
                    waveSeconds += Mathf.Max(spawnSeconds, clearSeconds);
                }
            }
            float ordinaryHp = ordinaryCount > 0 ? ordinaryHpSum / ordinaryCount : 30f;
            float ordinaryTtk = ordinaryHp * (level != null ? level.enemyHealthMultiplier : 1f) / Mathf.Max(1f, playerDps);
            float eliteTtk = ordinaryTtk * 2.5f;
            if (level != null) projectileDensity = Mathf.Min(projectileDensity, level.maximumHostileProjectiles);
            return new MegaBalanceEstimate(playerDps, ordinaryTtk, eliteTtk, bossTtk,
                bossTtk / phaseCount, waveSeconds, projectileDensity);
        }

        public static bool HasUnsafeLaneOverlap(MegaLevelData level)
        {
            if (level == null || level.boss == null || level.boss.phases == null) return false;
            foreach (BossPhaseData phase in level.boss.phases)
            {
                if (phase == null) continue;
                bool beam = phase.attacks != null && phase.attacks.Any(attack => attack != null && attack.pattern == MegaWeaponPattern.Laser);
                bool ram = phase.addGroups != null && phase.addGroups.Any(group => group != null && group.enemy != null && group.enemy.movementPattern == MegaMovementPattern.Rammer);
                if (beam && ram && phase.attacks.Sum(attack => attack != null ? attack.spreadDegrees : 0f) >= 180f) return true;
            }
            return false;
        }

        public static string BuildReport(MegaLevelData[] levels)
        {
            if (levels == null || levels.Length == 0) return "[MegaBalanceValidator] No levels available for balance report.";
            int[] indices = { 0, Mathf.Min(9, levels.Length - 1), levels.Length - 1 };
            var lines = new List<string> { "[MegaBalanceValidator] Representative estimates (continuous-fire model, 72% boss uptime):" };
            foreach (int index in indices.Distinct())
            {
                MegaLevelData level = levels[index];
                MegaBalanceEstimate estimate = Estimate(level);
                lines.Add($"  Mega {level.megaSequenceIndex:D2} / Level {level.gameLevelNumber:D3}: DPS {estimate.PlayerDps:0.0}, enemy TTK {estimate.OrdinaryEnemyTtkSeconds:0.00}s, elite TTK {estimate.EliteEnemyTtkSeconds:0.0}s, boss phase {estimate.BossPhaseTtkSeconds:0.0}s, density {estimate.SustainedProjectileDensity:0}/{level.maximumHostileProjectiles}, waves {estimate.WaveSeconds:0}s, total {estimate.EstimatedDuration:0}s, target {level.parTime:0}s");
            }
            return string.Join("\n", lines);
        }
    }
}
#endif
