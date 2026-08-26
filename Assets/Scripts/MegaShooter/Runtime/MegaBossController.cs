using System.Collections;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class MegaBossController : MonoBehaviour, IMegaPoolable
    {
        private BossShipData _data;
        private MegaLevelData _level;
        private MegaShooterGameManager _game;
        private SpriteRenderer _renderer;
        private BoxCollider2D _collider;
        private float _maxHealth;
        private float _health;
        private float _attackTimer;
        private float _age;
        private float _pendingOverflow;
        private int _phaseIndex;
        private bool _transitioning;
        private bool _damageLocked;
        private bool _registered;
        private Coroutine _hitGlowRoutine;
        private Color _phaseColor = Color.white;

        public int PhaseIndex => _phaseIndex;
        public float HealthNormalized => _maxHealth > 0f ? _health / _maxHealth : 0f;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
        }

        public void Configure(BossShipData data, MegaLevelData level, MegaShooterGameManager game)
            => ConfigureInternal(data, level, game, false);

        public void ConfigureForRuntimeVerification(BossShipData data, MegaLevelData level, MegaShooterGameManager game)
            => ConfigureInternal(data, level, game, true);

        private void ConfigureInternal(BossShipData data, MegaLevelData level, MegaShooterGameManager game, bool skipEntrance)
        {
            _data = data;
            _level = level;
            _game = game;
            _renderer.sprite = data.sprite;
            _renderer.color = Color.white;
            _phaseColor = Color.white;
            _collider.size = data.colliderSize;
            _maxHealth = data.baseHitPoints * level.bossOverrides.healthMultiplier;
            _health = _maxHealth;
            _phaseIndex = 0;
            _age = 0f;
            _attackTimer = 1f;
            _pendingOverflow = 0f;
            _transitioning = false;
            _damageLocked = !skipEntrance;
            _registered = true;
            _game.RegisterBoss(this);
            if (skipEntrance)
            {
                transform.position = Vector3.zero;
                _game.Hud?.ShowBoss(_data.displayName);
            }
            else StartCoroutine(EntranceRoutine());
        }

        private IEnumerator EntranceRoutine()
        {
            Vector3 target = new Vector3(0f, _data.movementBounds.yMax - 0.4f, 0f);
            Vector3 start = new Vector3(0f, _level.cameraBounds.yMax + 2f, 0f);
            transform.position = start;
            float duration = Mathf.Max(0.1f, _data.entranceDuration);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            transform.position = target;
            _damageLocked = false;
            _game.BossCombatStarted(this, _data.displayName);
        }

        private void Update()
        {
            if (_data == null || _game == null || _game.IsCombatFrozen || _damageLocked) return;
            float dt = Time.deltaTime * _game.HostileTimeScale;
            _age += dt;
            BossPhaseData phase = CurrentPhase;
            UpdateMovement(phase, dt);

            _attackTimer -= dt;
            if (_attackTimer <= 0f && !_transitioning)
            {
                FireAttack(phase);
                _attackTimer = Mathf.Max(0.25f, phase.attackInterval * _level.bossOverrides.attackIntervalMultiplier);
            }
        }

        private void UpdateMovement(BossPhaseData phase, float dt)
        {
            Rect bounds = _data.movementBounds;
            float x = bounds.center.x;
            float y = bounds.center.y;
            float horizontal = bounds.width * 0.42f;
            float vertical = bounds.height * 0.34f;
            float pace = 0.65f + _phaseIndex * 0.08f;

            switch (phase.movementPattern)
            {
                case MegaMovementPattern.Stationary:
                    break;
                case MegaMovementPattern.SideSweep:
                    x = bounds.xMin + Mathf.PingPong(_age * (1.1f + _phaseIndex * 0.06f), bounds.width);
                    y += Mathf.Sin(_age * 1.6f) * vertical;
                    break;
                case MegaMovementPattern.Orbit:
                    x += Mathf.Cos(_age * pace) * horizontal;
                    y += Mathf.Sin(_age * pace * 1.35f) * vertical;
                    break;
                case MegaMovementPattern.ZigZag:
                    x = bounds.xMin + Mathf.PingPong(_age * (1.35f + _phaseIndex * 0.06f), bounds.width);
                    y += Mathf.PingPong(_age * .7f, vertical * 2f) - vertical;
                    break;
                case MegaMovementPattern.Dive:
                case MegaMovementPattern.Rammer:
                    x += Mathf.Sin(_age * pace) * horizontal;
                    y = bounds.yMax - Mathf.PingPong(_age * .55f, bounds.height * .72f);
                    break;
                case MegaMovementPattern.Sine:
                case MegaMovementPattern.Hover:
                default:
                    x += Mathf.Sin(_age * pace) * horizontal;
                    y += Mathf.Sin(_age * 1.25f) * vertical * .4f;
                    break;
            }

            transform.position = new Vector3(Mathf.Clamp(x, bounds.xMin, bounds.xMax), Mathf.Clamp(y, bounds.yMin, bounds.yMax), 0f);
        }

        private BossPhaseData CurrentPhase
            => _data.phases[Mathf.Clamp(_phaseIndex, 0, _data.phases.Length - 1)];

        private void FireAttack(BossPhaseData phase)
        {
            if (phase.attacks == null || phase.attacks.Length == 0) return;
            float total = 0f;
            for (int i = 0; i < phase.attacks.Length; i++) total += Mathf.Max(0.01f, phase.attacks[i].weight);
            float pick = _game.NextRandom01() * total;
            BossAttackPattern attack = phase.attacks[0];
            for (int i = 0; i < phase.attacks.Length; i++)
            {
                pick -= Mathf.Max(0.01f, phase.attacks[i].weight);
                if (pick <= 0f) { attack = phase.attacks[i]; break; }
            }
            StartCoroutine(AttackRoutine(phase, attack));
        }

        private IEnumerator AttackRoutine(BossPhaseData phase, BossAttackPattern attack)
        {
            _game.Hud?.ShowBanner(attack.attackName);
            yield return new WaitForSeconds(Mathf.Max(0.85f, attack.telegraphTime));

            ProjectileData projectile = _game.DefaultEnemyProjectile;
            if (projectile == null) yield break;
            if (attack.clearsBulletsBeforeAttack) _game.ClearOrReflectHostileProjectiles(false);
            int count = Mathf.Clamp(attack.projectileCount, 1, 24);
            int volleys = attack.pattern == MegaWeaponPattern.Burst ? 3 : 1;
            for (int volley = 0; volley < volleys; volley++)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 direction;
                    if (attack.pattern == MegaWeaponPattern.Radial || attack.pattern == MegaWeaponPattern.Mine)
                        direction = Quaternion.Euler(0f, 0f, i * (360f / count) + volley * 9f) * Vector2.down;
                    else if ((attack.aimed || attack.pattern == MegaWeaponPattern.Sniper || attack.pattern == MegaWeaponPattern.Laser) && _game.Player != null)
                    {
                        Vector2 aimed = ((Vector2)_game.Player.transform.position - (Vector2)transform.position).normalized;
                        float microSpread = count > 1 ? -8f + i * 16f / (count - 1) : 0f;
                        direction = Quaternion.Euler(0f, 0f, microSpread) * aimed;
                    }
                    else
                    {
                        float angle = count > 1 ? -attack.spreadDegrees * 0.5f + i * attack.spreadDegrees / (count - 1) : 0f;
                        direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;
                    }
                    float damage = phase.projectileDamageOverride > 0f ? phase.projectileDamageOverride : projectile.damage;
                    float mechanismSpeed = attack.pattern == MegaWeaponPattern.Mine ? 0.18f
                        : attack.pattern == MegaWeaponPattern.Sniper || attack.pattern == MegaWeaponPattern.Laser ? 1.6f : 1f;
                    _game.SpawnProjectile(projectile, MegaFaction.Enemy, transform.position, direction,
                        damage * _level.enemyDamageMultiplier,
                        _level.enemyProjectileSpeedMultiplier * phase.projectileSpeedMultiplier * _level.bossOverrides.projectileSpeedMultiplier * mechanismSpeed,
                        0, attack.aimed ? _game.Player?.transform : null, attack.reflectable);
                }
                if (volley > 1 && volley < volleys - 1) yield return new WaitForSeconds(0.16f);
            }
        }

        public bool TakeDamage(float amount)
        {
            if (_data == null || amount <= 0f || _damageLocked || _transitioning) return false;
            float target = _health - amount;
            PlayHitFeedback();
            if (_phaseIndex < _data.phases.Length - 1)
            {
                float boundary = _data.phases[_phaseIndex + 1].healthThreshold * _maxHealth;
                if (target <= boundary)
                {
                    _pendingOverflow += boundary - target;
                    _health = boundary;
                    StartCoroutine(TransitionRoutine());
                    _game.Hud?.SetBossHealth(HealthNormalized);
                    return true;
                }
            }

            _health = Mathf.Max(0f, target);
            _game.Hud?.SetBossHealth(HealthNormalized);
            if (_health <= 0f) Defeat();
            return true;
        }

        private IEnumerator TransitionRoutine()
        {
            _transitioning = true;
            _damageLocked = true;
            _phaseIndex++;
            BossPhaseData phase = CurrentPhase;
            _phaseColor = phase.phaseTint;
            _renderer.color = _phaseColor;
            _game.SpawnEffect(phase.transitionVFX != null ? phase.transitionVFX : _game.VFXProfile?.warningPrefab,
                transform.position, new Color(1f, 0.08f, 0.12f, 1f), 2.2f, 0.85f);
            _game.ClearOrReflectHostileProjectiles(false);
            _game.CameraEffects?.Shake(0.22f, phase.cameraShake);
            _game.Hud?.ShowBanner(string.IsNullOrWhiteSpace(phase.warningText) ? $"PHASE {_phaseIndex + 1}" : phase.warningText);
            yield return new WaitForSeconds(Mathf.Max(1f, phase.transitionDuration));
            _damageLocked = false;
            _transitioning = false;
            if (_pendingOverflow > 0f)
            {
                float overflow = _pendingOverflow;
                _pendingOverflow = 0f;
                TakeDamage(overflow);
            }
        }

        private void Defeat()
        {
            if (!_registered) return;
            _game.SpawnEffect(_data.deathVFX != null ? _data.deathVFX : _game.VFXProfile?.bossDeathPrefab,
                transform.position, new Color(1f, 0.08f, 0.03f, 1f), 3.1f, 1.1f);
            _game.CameraEffects?.Shake(0.55f, 0.34f);
            _game.CameraEffects?.Flash(new Color(1f, 0.12f, 0.04f, 1f), 0.72f, 0.42f);
            _game.AddScore(_data.score);
            _game.BossDefeated(this);
            MegaObjectPools.Instance?.Despawn(gameObject);
        }

        private void PlayHitFeedback()
        {
            if (_hitGlowRoutine != null) StopCoroutine(_hitGlowRoutine);
            _hitGlowRoutine = StartCoroutine(HitGlowRoutine());
            _game.SpawnEffect(_game.VFXProfile?.hitSparkPrefab, transform.position,
                new Color(1f, 0.03f, 0.02f, 1f), 1.15f, 0.3f);
            _game.CameraEffects?.Shake(0.06f, 0.035f);
        }

        private IEnumerator HitGlowRoutine()
        {
            const float duration = 0.16f;
            Color red = new Color(1f, 0.02f, 0.02f, 1f);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                _renderer.color = Color.Lerp(red, _phaseColor, elapsed / duration);
                yield return null;
            }
            _renderer.color = _phaseColor;
            _hitGlowRoutine = null;
        }

        public void OnMegaSpawned() { }

        public void OnMegaDespawned()
        {
            StopAllCoroutines();
            if (_registered) _game?.UnregisterBoss(this);
            _registered = false;
            _renderer.color = Color.white;
            _phaseColor = Color.white;
            _data = null;
            _level = null;
            _game = null;
        }
    }
}
