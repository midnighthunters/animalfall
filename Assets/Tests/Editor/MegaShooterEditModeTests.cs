#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.MegaShooter;
using AnimalFall.MegaShooter.Editor;

namespace AnimalFall.Tests.Editor
{
    public sealed class MegaShooterEditModeTests
    {
        private MegaLevelData[] _levels;
        private LevelDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _levels = MegaShooterValidator.LoadMegaLevels();
            _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Levels/LevelDatabase.asset");
        }

        [Test]
        public void ExactlyTwentyMegaLevelsCoverMultiplesOfFive()
        {
            Assert.That(_levels.Length, Is.EqualTo(20));
            CollectionAssert.AreEqual(Enumerable.Range(1, 20).Select(i => i * 5), _levels.Select(level => level.gameLevelNumber));
            CollectionAssert.AreEqual(Enumerable.Range(1, 20), _levels.Select(level => level.megaSequenceIndex));
        }

        [Test]
        public void MegaLevelsHaveCompleteEncounterAndDifficultyData()
        {
            foreach (MegaLevelData level in _levels)
            {
                Assert.That(level.IsValidMegaNumber, Is.True, level.name);
                Assert.That(level.waves, Is.Not.Null.And.Not.Empty, level.name);
                Assert.That(level.waves.All(wave => wave != null && wave.spawnGroups != null && wave.spawnGroups.Length > 0), Is.True, level.name);
                Assert.That(level.boss != null, Is.EqualTo(level.gameLevelNumber % 10 == 0), level.name);
                Assert.That(level.enemyHealthMultiplier, Is.GreaterThan(0f), level.name);
                Assert.That(level.enemyProjectileSpeedMultiplier, Is.GreaterThan(0f), level.name);
                Assert.That(level.ordinaryEnemyFireInterval, Is.GreaterThanOrEqualTo(.85f), level.name);
                Assert.That(level.maximumHostileProjectiles, Is.InRange(1, 120), level.name);
            }
        }

        [Test]
        public void DatabaseIsDynamicAndOnlyMultiplesOfFiveUseShooterMode()
        {
            Assert.That(_database, Is.Not.Null);
            Assert.That(_database.TotalLevels, Is.GreaterThanOrEqualTo(100));
            Assert.That(_database.GetLevelOrNull(-1), Is.Null);
            Assert.That(_database.GetLevelOrNull(_database.TotalLevels), Is.Null);
            for (int index = 0; index < _database.TotalLevels; index++)
            {
                LevelData level = _database.GetLevelOrNull(index);
                if ((index + 1) % 5 == 0)
                    Assert.That(level != null && level.IsConfiguredMegaShooter, Is.True, $"Level {index + 1}");
                else if (level != null)
                    Assert.That(level.Mode == LevelMode.Normal && level.MegaShooterData == null, Is.True, $"Level {index + 1}");
            }
        }

        [Test]
        public void SuperAnimalUnlocksAndStableIdsAreValid()
        {
            SuperAnimalData[] animals = LoadAssets<SuperAnimalData>(MegaShooterGenerator.DataRoot + "/Animals");
            Assert.That(animals.Length, Is.EqualTo(10));
            Assert.That(animals.Select(a => a.stableId).Distinct().Count(), Is.EqualTo(10));
            CollectionAssert.AreEqual(new[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 }, animals.OrderBy(a => a.unlockGameLevel).Select(a => a.unlockGameLevel));
            Assert.That(animals.All(a => a.unlockGameLevel == a.unlockMegaIndex * 5), Is.True);
        }

        [Test]
        public void EveryMegaLevelIncludesEveryHeroFromTheHeroSpriteSheet()
        {
            SuperAnimalData[] animals = LoadAssets<SuperAnimalData>(MegaShooterGenerator.DataRoot + "/Animals");
            Sprite[] heroSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/megalevel/heroes.png")
                .OfType<Sprite>().ToArray();

            Assert.That(animals.Length, Is.EqualTo(heroSprites.Length), "Every sliced hero needs a Super Animal asset.");
            CollectionAssert.AreEquivalent(heroSprites, animals.Select(animal => animal.shipSprite).ToArray());
            foreach (MegaLevelData level in _levels)
                CollectionAssert.AreEquivalent(animals, level.allowedAnimals, level.name);
        }

        [Test]
        public void EveryHeroWeaponUsesItsMatchingProjectileSprite()
        {
            Sprite[] projectileSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/icons/projectile.png")
                .OfType<Sprite>()
                .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)))
                .ToArray();
            SuperAnimalData[] roster = _levels[0].allowedAnimals;

            Assert.That(projectileSprites, Has.Length.EqualTo(10));
            Assert.That(roster, Has.Length.EqualTo(projectileSprites.Length));
            for (int index = 0; index < roster.Length; index++)
            {
                SuperAnimalData hero = roster[index];
                Assert.That(hero, Is.Not.Null, $"Hero slot {index}");
                Assert.That(hero.primaryWeapon, Is.Not.Null, hero.stableId);
                Assert.That(hero.primaryWeapon.projectile, Is.Not.Null, hero.stableId);
                Assert.That(hero.primaryWeapon.projectile.sprite, Is.EqualTo(projectileSprites[index]), hero.stableId);
            }
        }

        [Test]
        public void BossThresholdsAreStrictlyDescendingAndReferencesAreComplete()
        {
            foreach (MegaLevelData level in _levels)
            {
                if (level.boss == null) continue;
                Assert.That(level.boss.prefab, Is.Not.Null, level.name);
                Assert.That(level.boss.sprite, Is.Not.Null, level.name);
                Assert.That(level.boss.phases.Length, Is.GreaterThanOrEqualTo(2), level.name);
                for (int i = 1; i < level.boss.phases.Length; i++)
                    Assert.That(level.boss.phases[i].healthThreshold, Is.LessThan(level.boss.phases[i - 1].healthThreshold), level.name);
                Assert.That(level.allowedAnimals.All(a => a != null && a.playerPrefab != null && a.primaryWeapon != null && a.primaryWeapon.projectile != null), Is.True, level.name);
                Assert.That(level.waves.SelectMany(w => w.spawnGroups).All(g => g != null && g.enemy != null && g.enemy.prefab != null && g.enemy.projectile != null && g.enemy.sprite != null), Is.True, level.name);
            }
        }

        [Test]
        public void ArmyAndBossCadenceMatchesTenVillainChapters()
        {
            string[] expected =
            {
                "Venom Emperor", "Admiral Inkstorm", "Ironhorn", "Captain Chomper", "General Smash",
                "Emperor Sting", "Croc Commander", "Doom Puffer", "Queen Webula", "Cosmic Draconis"
            };
            for (int chapter = 0; chapter < expected.Length; chapter++)
            {
                MegaLevelData armyLevel = _levels[chapter * 2];
                MegaLevelData bossLevel = _levels[chapter * 2 + 1];
                Assert.That(armyLevel.boss, Is.Null, armyLevel.name);
                Assert.That(bossLevel.boss, Is.Not.Null, bossLevel.name);
                Assert.That(armyLevel.displayTitle, Does.Contain(expected[chapter]));
                Assert.That(bossLevel.boss.displayName, Does.Contain(expected[chapter]));
                CollectionAssert.AreEquivalent(
                    armyLevel.waves.SelectMany(w => w.spawnGroups).Select(g => g.enemy.sprite).Distinct().ToArray(),
                    bossLevel.waves.SelectMany(w => w.spawnGroups).Select(g => g.enemy.sprite).Distinct().ToArray());
                Assert.That(armyLevel.waves.SelectMany(w => w.spawnGroups)
                    .All(g => AssetDatabase.GetAssetPath(g.enemy.sprite).Contains("megalevelhindrances/army/")), Is.True);
                Assert.That(AssetDatabase.GetAssetPath(bossLevel.boss.sprite), Does.Contain("megalevelhindrances/villains/"));
            }
        }

        [Test]
        public void VillainsUseAuthoredAttackProjectilesAndDistinctMuzzleVfx()
        {
            MegaVFXProfile vfx = _levels[0].vfxProfile;
            Assert.That(vfx.playerMuzzlePrefab, Is.Not.Null);
            Assert.That(vfx.enemyMuzzlePrefab, Is.Not.Null);
            Assert.That(vfx.bossMuzzlePrefab, Is.Not.Null);
            Assert.That(vfx.playerMuzzlePrefab, Is.Not.EqualTo(vfx.enemyMuzzlePrefab));
            Assert.That(vfx.enemyMuzzlePrefab, Is.Not.EqualTo(vfx.bossMuzzlePrefab));
            foreach (MegaLevelData level in _levels.Where(level => level.boss != null))
                Assert.That(level.boss.phases.SelectMany(phase => phase.attacks)
                    .All(attack => attack.projectile != null && attack.projectile.sprite != null), Is.True, level.name);
        }

        [Test]
        public void VillainFamiliesUseTheirMatchingVillainProjectileSprites()
        {
            string[] familyIds =
            {
                "venom_emperor", "admiral_inkstorm", "ironhorn", "captain_chomper", "general_smash",
                "emperor_sting", "croc_commander", "doom_puffer", "queen_webula", "cosmic_draconis"
            };
            Sprite[] villainSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/megalevel/villain_weapon.png")
                .OfType<Sprite>()
                .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)))
                .ToArray();
            ProjectileData[] projectiles = LoadAssets<ProjectileData>(MegaVillainRosterGenerator.VillainDataRoot + "/Projectiles");

            Assert.That(villainSprites, Has.Length.EqualTo(familyIds.Length));
            Assert.That(projectiles, Has.Length.EqualTo(familyIds.Length * 3));
            for (int family = 0; family < familyIds.Length; family++)
            {
                ProjectileData[] familyProjectiles = projectiles
                    .Where(projectile => projectile.stableId.StartsWith(familyIds[family] + "_"))
                    .ToArray();
                Assert.That(familyProjectiles, Has.Length.EqualTo(3), familyIds[family]);
                Assert.That(familyProjectiles.All(projectile => projectile.sprite == villainSprites[family]), Is.True, familyIds[family]);
            }
        }

        [Test]
        public void HostileDirectionsAreAlwaysDownward()
        {
            Vector2[] samples = { Vector2.up, Vector2.right, Vector2.left, new Vector2(.2f, .9f), Vector2.zero };
            foreach (Vector2 sample in samples)
            {
                Vector2 constrained = MegaShooterGameManager.ForceDownward(sample);
                Assert.That(constrained.y, Is.LessThan(-0.1f));
                Assert.That(constrained.sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
            }
        }

        [Test]
        public void MegaCombatUsesRequestedHitCounts()
        {
            Assert.That(SuperAnimalController.VillainHitsToDefeat, Is.EqualTo(3));
            Assert.That(MegaEnemyController.HitsToDefeat, Is.EqualTo(1));
        }

        [Test]
        public void MegaPrefabsHaveNoMissingScriptsAndGameplaySizesArePositive()
        {
            string[] prefabPaths =
            {
                "Assets/MegaShooter/Prefabs/MegaPlayer.prefab",
                "Assets/MegaShooter/Prefabs/MegaEnemy.prefab",
                "Assets/MegaShooter/Prefabs/MegaBoss.prefab",
                "Assets/MegaShooter/Prefabs/MegaProjectile.prefab",
                "Assets/MegaShooter/Prefabs/MegaPickup.prefab",
                "Assets/MegaShooter/Prefabs/MegaEffect.prefab",
                "Assets/MegaShooter/Prefabs/MegaPlayerMuzzle.prefab",
                "Assets/MegaShooter/Prefabs/MegaEnemyMuzzle.prefab",
                "Assets/MegaShooter/Prefabs/MegaBossMuzzle.prefab"
            };
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), Is.EqualTo(0), path);
                Assert.That(prefab.transform.localScale.x, Is.GreaterThan(0f), path);
            }
            foreach (MegaLevelData level in _levels)
            {
                foreach (EnemySpawnGroup group in level.waves.SelectMany(w => w.spawnGroups))
                {
                    Assert.That(group.enemy.speed, Is.GreaterThan(0f), level.name);
                    Assert.That(group.enemy.colliderSize.x, Is.GreaterThan(0f), level.name);
                    Assert.That(group.enemy.colliderSize.y, Is.GreaterThan(0f), level.name);
                    Assert.That(group.enemy.projectile.damage, Is.GreaterThan(0f), level.name);
                }
            }
        }

        [Test]
        public void LevelRoutingSelectsExistingAndDedicatedScenes()
        {
            GameObject go = new GameObject("LevelManagerRoutingTest");
            try
            {
                LevelManager manager = go.AddComponent<LevelManager>();
                Assert.That(manager.GetSceneNameForLevel(_database.GetLevelOrNull(0)), Is.EqualTo("GameScene"));
                Assert.That(manager.GetSceneNameForLevel(_database.GetLevelOrNull(4)), Is.EqualTo("MegaShooterScene"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void GeneratorIsIdempotentForNormalAssetsAndDatabaseReferences()
        {
            LevelData[] beforeReferences = _database.Levels.ToArray();
            var normalJson = new Dictionary<LevelData, string>();
            for (int i = 0; i < beforeReferences.Length; i++)
                if ((i + 1) % 5 != 0 && beforeReferences[i] != null)
                    normalJson.Add(beforeReferences[i], EditorJsonUtility.ToJson(beforeReferences[i]));

            MegaShooterGenerator.GenerateOrUpdateMegaLevelsOnly();

            CollectionAssert.AreEqual(beforeReferences, _database.Levels);
            foreach (KeyValuePair<LevelData, string> item in normalJson)
                Assert.That(EditorJsonUtility.ToJson(item.Key), Is.EqualTo(item.Value), item.Key.name);
        }

        [Test]
        public void FullValidatorPasses()
        {
            Assert.That(MegaShooterValidator.ValidateAll(false), Is.True);
        }

        private static T[] LoadAssets<T>(string folder) where T : Object
            => AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(asset => asset != null).ToArray();
    }
}
#endif
