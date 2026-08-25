using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaCounterController : MonoBehaviour
    {
        private interface ICounterStrategy
        {
            IEnumerator Execute(MegaCounterController context);
        }

        private sealed class BurstStrategy : ICounterStrategy
        {
            private readonly int _shots;
            private readonly bool _clear;
            public BurstStrategy(int shots, bool clear) { _shots = shots; _clear = clear; }
            public IEnumerator Execute(MegaCounterController c)
            {
                if (_clear) c._game.ClearOrReflectHostileProjectiles(false);
                c._game.FireCounterBurst(c.transform.position, _shots, c._config.damage);
                yield return new WaitForSeconds(c._config.duration);
            }
        }

        private sealed class ShieldStrategy : ICounterStrategy
        {
            private readonly bool _reflect;
            public ShieldStrategy(bool reflect) => _reflect = reflect;
            public IEnumerator Execute(MegaCounterController c)
            {
                c._player.GrantInvulnerability(c._config.duration);
                c._game.ClearOrReflectHostileProjectiles(_reflect);
                yield return new WaitForSeconds(c._config.duration);
            }
        }

        private sealed class AreaStrategy : ICounterStrategy
        {
            private readonly float _hostileScale;
            private readonly bool _ram;
            public AreaStrategy(float hostileScale, bool ram = false) { _hostileScale = hostileScale; _ram = ram; }
            public IEnumerator Execute(MegaCounterController c)
            {
                c._player.GrantInvulnerability(c._config.duration);
                c._game.SetHostileTimeScale(_hostileScale, c._config.duration);
                c._game.DamageEnemiesInRadius(c.transform.position, c._config.radius, c._config.damage, _ram);
                if (c._config.clearsProjectiles) c._game.ClearOrReflectHostileProjectiles(c._config.reflectsProjectiles);
                yield return new WaitForSeconds(c._config.duration);
            }
        }

        private static readonly Dictionary<MegaCounterType, ICounterStrategy> Strategies =
            new Dictionary<MegaCounterType, ICounterStrategy>
            {
                { MegaCounterType.SkyBarrage, new BurstStrategy(15, true) },
                { MegaCounterType.MirageCounter, new ShieldStrategy(false) },
                { MegaCounterType.ShellReflector, new ShieldStrategy(true) },
                { MegaCounterType.ClawDash, new AreaStrategy(0.75f, true) },
                { MegaCounterType.GravitySmash, new AreaStrategy(0.35f) },
                { MegaCounterType.TimeEye, new AreaStrategy(0.28f) },
                { MegaCounterType.SpectrumShift, new BurstStrategy(9, true) },
                { MegaCounterType.MeteorRam, new AreaStrategy(0.65f, true) },
                { MegaCounterType.RoyalFan, new BurstStrategy(21, true) },
                { MegaCounterType.SolarRoar, new BurstStrategy(25, true) }
            };

        private MegaCounterData _config;
        private MegaLevelData _level;
        private MegaShooterGameManager _game;
        private SuperAnimalController _player;
        private float _meter;
        private float _cooldownUntil;
        private bool _active;

        public float NormalizedMeter => _config != null ? Mathf.Clamp01(_meter / _config.meterRequirement) : 0f;
        public bool IsReady => _config != null && _meter >= _config.meterRequirement && !_active && Time.time >= _cooldownUntil;

        public void Configure(MegaCounterData config, SuperAnimalData animal, MegaLevelData level,
            MegaShooterGameManager game, SuperAnimalController player)
        {
            _config = config;
            _level = level;
            _game = game;
            _player = player;
            _meter = 0f;
            _active = false;
            _game.Hud?.SetCounter(0f, false);
        }

        public void AddCharge(float amount)
        {
            if (_config == null || _active) return;
            bool wasReady = IsReady;
            _meter = Mathf.Min(_config.meterRequirement, _meter + amount * _level.counterChargeMultiplier);
            _game.Hud?.SetCounter(NormalizedMeter, IsReady);
            if (!wasReady && IsReady) _game.Hud?.PulseCounterReady();
        }

        public void OnPlayerDamaged()
        {
            if (_config == null) return;
            _meter *= 1f - Mathf.Clamp01(_config.meterLossOnHit);
            _game.Hud?.SetCounter(NormalizedMeter, false);
        }

        public void TryActivate()
        {
            if (!IsReady || _game == null || _game.IsCombatFrozen) return;
            StartCoroutine(ActivateRoutine());
        }

        private IEnumerator ActivateRoutine()
        {
            _active = true;
            _meter = 0f;
            _game.AddScore(_level.counterScore);
            _game.Hud?.SetCounter(0f, false);
            _game.CameraEffects?.Shake(0.16f, 0.22f);
            if (Strategies.TryGetValue(_config.type, out ICounterStrategy strategy))
                yield return strategy.Execute(this);
            _cooldownUntil = Time.time + _config.cooldown;
            _active = false;
        }
    }
}
