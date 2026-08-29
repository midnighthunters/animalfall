#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using AnimalFall.MegaShooter;

namespace AnimalFall.Tests.Editor
{
    public sealed class MegaShooterRuntimeFivePassTests
    {
        // This project has no dedicated PlayMode test assembly. Keep the five-pass
        // audit available without letting the EditMode runner invoke scene loading.
        [UnityTest, Explicit] public IEnumerator MegaRuntimePass01() { yield return RunPass(1); }
        [UnityTest, Explicit] public IEnumerator MegaRuntimePass02() { yield return RunPass(2); }
        [UnityTest, Explicit] public IEnumerator MegaRuntimePass03() { yield return RunPass(3); }
        [UnityTest, Explicit] public IEnumerator MegaRuntimePass04() { yield return RunPass(4); }
        [UnityTest, Explicit] public IEnumerator MegaRuntimePass05() { yield return RunPass(5); }

        private IEnumerator RunPass(int pass)
        {
            MegaLevelData[] levels = AssetDatabase.FindAssets("t:MegaLevelData", new[] { "Assets/MegaShooter/Data/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MegaLevelData>)
                .Where(level => level != null)
                .OrderBy(level => level.gameLevelNumber)
                .ToArray();
            Assert.That(levels, Has.Length.EqualTo(20));
            FieldInfo invulnerability = typeof(SuperAnimalController).GetField(
                "_invulnerableUntil", BindingFlags.Instance | BindingFlags.NonPublic);

            try
            {
                MegaShooterGameManager.RuntimeTestFastStart = true;
                for (int index = 0; index < levels.Length; index++)
                {
                    MegaLevelData expected = levels[index];
                    MegaShooterGameManager.RuntimeTestLevelOverride = expected;
                    yield return SceneManager.LoadSceneAsync("MegaShooterScene", LoadSceneMode.Single);

                    MegaShooterGameManager manager = null;
                    float findDeadline = Time.realtimeSinceStartup + 4f;
                    while (manager == null && Time.realtimeSinceStartup < findDeadline)
                    {
                        manager = Object.FindAnyObjectByType<MegaShooterGameManager>();
                        yield return null;
                    }
                    Assert.That(manager, Is.Not.Null, $"pass {pass}, level {expected.gameLevelNumber}: manager");
                    Assert.That(manager.debugLevel, Is.EqualTo(expected), $"pass {pass}, level {expected.gameLevelNumber}: data");

                    float waveDeadline = Time.realtimeSinceStartup + 4f;
                    while (manager.State != MegaShooterState.Wave && Time.realtimeSinceStartup < waveDeadline)
                        yield return null;
                    Assert.That(manager.State, Is.EqualTo(MegaShooterState.Wave), $"pass {pass}, level {expected.gameLevelNumber}: wave started");
                    Assert.That(manager.Player, Is.Not.Null, $"pass {pass}, level {expected.gameLevelNumber}: player");
                    Assert.That(manager.Player.MaxHealth, Is.EqualTo(SuperAnimalController.VillainHitsToDefeat),
                        $"pass {pass}, level {expected.gameLevelNumber}: player survives exactly three villain hits");
                    Assert.That(manager.debugOverlay == null || !manager.debugOverlay.enabled,
                        Is.True, $"pass {pass}, level {expected.gameLevelNumber}: debug overlay hidden");
                    Assert.That(manager.hud == null || manager.hud.transform.Cast<Transform>().All(child => !child.gameObject.activeSelf),
                        Is.True, $"pass {pass}, level {expected.gameLevelNumber}: gameplay UI hidden");

                    AutoWeaponController weapon = manager.Player.GetComponent<AutoWeaponController>();
                    if (weapon != null) weapon.enabled = false;

                    MegaEnemyController enemy = null;
                    float enemyDeadline = Time.realtimeSinceStartup + 4f;
                    while (enemy == null && Time.realtimeSinceStartup < enemyDeadline)
                    {
                        enemy = Object.FindObjectsByType<MegaEnemyController>(FindObjectsSortMode.None)
                            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy);
                        yield return null;
                    }
                    Assert.That(enemy, Is.Not.Null, $"pass {pass}, level {expected.gameLevelNumber}: enemy spawned");
                    Assert.That(expected.cameraBounds.Contains(enemy.transform.position), Is.True,
                        $"pass {pass}, level {expected.gameLevelNumber}: enemy begins inside the visible arena");
                    Vector3 enemyBefore = enemy.transform.position;
                    yield return new WaitForSeconds(0.12f);
                    if (enemy != null && enemy.gameObject.activeInHierarchy)
                        Assert.That(enemy.transform.position.y, Is.LessThan(enemyBefore.y),
                            $"pass {pass}, level {expected.gameLevelNumber}: enemy moves down");

                    ProjectileData playerProjectile = manager.Player.Data.primaryWeapon.projectile;
                    MegaProjectile playerShot = manager.SpawnProjectile(playerProjectile, MegaFaction.Player,
                        enemy.transform.position + Vector3.down * 0.6f, Vector2.up,
                        manager.Player.Data.primaryWeapon.damage, 1f, 0, null);
                    Assert.That(playerShot, Is.Not.Null, $"pass {pass}, level {expected.gameLevelNumber}: player shot spawned");
                    yield return new WaitForSeconds(0.25f);
                    Assert.That(enemy == null || !enemy.gameObject.activeInHierarchy,
                        Is.True, $"pass {pass}, level {expected.gameLevelNumber}: one player shot eliminates army enemy");

                    Assert.That(invulnerability, Is.Not.Null);
                    invulnerability.SetValue(manager.Player, Time.time - 1f);
                    EnemySpawnGroup group = expected.waves.SelectMany(wave => wave.spawnGroups)
                        .First(candidate => candidate != null && candidate.enemy != null && candidate.enemy.projectile != null);
                    float healthBefore = manager.Player.Health;
                    MegaProjectile shot = manager.SpawnProjectile(group.enemy.projectile, MegaFaction.Enemy,
                        manager.Player.transform.position + Vector3.up * 0.45f, Vector2.up,
                        group.enemy.projectile.damage, 1f, 0, null);
                    Assert.That(shot, Is.Not.Null, $"pass {pass}, level {expected.gameLevelNumber}: hostile shot spawned");
                    Assert.That(shot.Direction.y, Is.LessThan(-0.1f), $"pass {pass}, level {expected.gameLevelNumber}: shot down");
                    Assert.That(shot.Damage, Is.GreaterThan(0f), $"pass {pass}, level {expected.gameLevelNumber}: shot damage");
                    yield return new WaitForSeconds(0.22f);
                    Assert.That(manager.Player.Health, Is.EqualTo(healthBefore - 1),
                        $"pass {pass}, level {expected.gameLevelNumber}: each accepted hostile hit removes one shield");

                    manager.DespawnAllEnemies();
                    manager.ClearOrReflectHostileProjectiles(false);
                    manager.waveDirector.StopDirector();
                    manager.AllWavesCompleted();
                    if (expected.boss == null)
                    {
                        Assert.That(manager.State, Is.EqualTo(MegaShooterState.Won),
                            $"pass {pass}, level {expected.gameLevelNumber}: army-only victory");
                        Assert.That(manager.Boss, Is.Null, $"pass {pass}, level {expected.gameLevelNumber}: no boss");
                    }
                    else
                    {
                        Assert.That(manager.State, Is.EqualTo(MegaShooterState.BossWarning),
                            $"pass {pass}, level {expected.gameLevelNumber}: boss warning");
                        yield return new WaitForSeconds(2.35f);
                        Assert.That(manager.Boss, Is.Not.Null,
                            $"pass {pass}, level {expected.gameLevelNumber}: boss spawned");
                        Assert.That(manager.debugLevel.boss.archetype, Is.Not.EqualTo(MegaVillainArchetype.None),
                            $"pass {pass}, level {expected.gameLevelNumber}: boss archetype");
                    }
                }
            }
            finally
            {
                MegaShooterGameManager.RuntimeTestLevelOverride = null;
                MegaShooterGameManager.RuntimeTestFastStart = false;
            }
        }
    }
}
#endif
