using System;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    /// <summary>Allocation-free snapshot used by automated Play Mode smoke checks and the debug overlay.</summary>
    public readonly struct MegaRuntimeAuditSnapshot
    {
        public readonly MegaShooterState State;
        public readonly int ActiveEnemies;
        public readonly int HostileProjectiles;
        public readonly int PoolMisses;
        public readonly bool HasPlayer;
        public readonly bool HasBoss;
        public readonly float TimeScale;

        public MegaRuntimeAuditSnapshot(MegaShooterGameManager manager)
        {
            State = manager != null ? manager.State : MegaShooterState.Intro;
            ActiveEnemies = manager != null ? manager.ActiveEnemyCount : -1;
            HostileProjectiles = manager != null ? manager.ActiveHostileProjectiles : -1;
            PoolMisses = manager != null ? manager.PoolMisses : -1;
            HasPlayer = manager != null && manager.Player != null;
            HasBoss = manager != null && manager.Boss != null;
            TimeScale = Time.timeScale;
        }

        public bool WithinMobileBudgets(int enemyCap, int projectileCap)
            => ActiveEnemies >= 0 && ActiveEnemies <= enemyCap && HostileProjectiles >= 0 && HostileProjectiles <= projectileCap;

        public override string ToString()
            => $"state={State}, player={HasPlayer}, boss={HasBoss}, enemies={ActiveEnemies}, hostileProjectiles={HostileProjectiles}, poolMisses={PoolMisses}, timeScale={TimeScale:0.##}";
    }

    public static class MegaShooterRuntimeAudit
    {
        public static MegaRuntimeAuditSnapshot Capture(MegaShooterGameManager manager)
            => new MegaRuntimeAuditSnapshot(manager);

        public static bool ValidateSceneWiring(MegaShooterGameManager manager, out string error)
        {
            if (manager == null) { error = "MegaShooterGameManager is missing."; return false; }
            if (manager.worldCamera == null || manager.pools == null || manager.waveDirector == null || manager.shooterInput == null)
            { error = "Camera, pool, wave director, or input reference is missing."; return false; }
            if (manager.hud == null || manager.starfield == null || manager.cameraEffects == null)
            { error = "HUD, starfield, or camera-effects reference is missing."; return false; }
            if (manager.playerContainer == null || manager.enemyContainer == null || manager.projectileContainer == null || manager.pickupContainer == null)
            { error = "One or more runtime containers are missing."; return false; }
            if (manager.debugLevel == null || manager.defaultEnemyProjectile == null)
            { error = "Direct-play debug level or default enemy projectile is missing."; return false; }
            error = string.Empty;
            return true;
        }
    }
}
