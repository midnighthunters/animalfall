using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class MegaEnemyController : MonoBehaviour, IMegaPoolable
    {
        private EnemyShipData _data;
        private EnemySpawnGroup _group;
        private MegaWaveData _wave;
        private MegaLevelData _level;
        private MegaShooterGameManager _game;
        private MegaWaveDirector _director;
        private SpriteRenderer _renderer;
        private BoxCollider2D _collider;
        private LineRenderer _warningLine;
        private Vector3 _spawnPosition;
        private float _health;
        private float _speed;
        private float _fireTimer;
        private float _age;
        private float _telegraphRemaining;
        private float _hitGlowRemaining;
        private Color _baseColor;
        private bool _registered;
        private bool _telegraphing;
        private bool _elite;

        public bool IsPriority => _group != null && (_group.priorityTarget || (_data != null && _data.priorityTarget));
        public float Health => _health;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            _warningLine = GetComponentInChildren<LineRenderer>(true);
        }

        public void Configure(EnemyShipData data, EnemySpawnGroup group, MegaWaveData wave, MegaLevelData level,
            MegaShooterGameManager game, MegaWaveDirector director, bool elite)
        {
            _data = data;
            _group = group;
            _wave = wave;
            _level = level;
            _game = game;
            _director = director;
            _elite = elite;
            _renderer.sprite = data.sprite;
            _baseColor = elite ? new Color(1f, 0.72f, 0.25f, 1f) : Color.white;
            _renderer.color = _baseColor;
            _collider.size = data.colliderSize;
            _health = data.hitPoints * level.enemyHealthMultiplier * wave.healthMultiplier * (elite ? 1.8f : 1f);
            float overrideSpeed = group.movementSpeedOverride > 0f ? group.movementSpeedOverride : data.speed;
            _speed = overrideSpeed * wave.speedMultiplier;
            _fireTimer = Mathf.Max(0f, data.initialFireDelay);
            _age = 0f;
            _telegraphRemaining = 0f;
            _telegraphing = false;
            _hitGlowRemaining = 0f;
            _spawnPosition = transform.position;
            _registered = true;
            _director.EnemySpawned(this);
            _game.RegisterEnemy(this);
            if (_warningLine != null) _warningLine.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_data == null || _game == null || _game.IsCombatFrozen) return;
            float dt = Time.deltaTime * _game.HostileTimeScale;
            UpdateHitGlow(Time.deltaTime);
            _age += dt;
            Move(dt);
            UpdateWeapon(dt);

            if (!_game.IsInsideEnemyBounds(transform.position))
                Despawn(false);
        }

        private void Move(float dt)
        {
            MegaMovementPattern pattern = _data.movementPattern;
            float x = transform.position.x;
            float y = transform.position.y;
            switch (pattern)
            {
                case MegaMovementPattern.Sine:
                    x = _spawnPosition.x + Mathf.Sin(_age * 2.2f) * 1.25f;
                    y -= _speed * dt;
                    break;
                case MegaMovementPattern.ZigZag:
                    x = _spawnPosition.x + Mathf.PingPong(_age * _speed, 2.6f) - 1.3f;
                    y -= _speed * 0.55f * dt;
                    break;
                case MegaMovementPattern.Hover:
                case MegaMovementPattern.Orbit:
                    x = _spawnPosition.x + Mathf.Sin(_age * 1.4f) * 1.4f;
                    y = Mathf.Max(_spawnPosition.y - 2.2f, y - _speed * dt);
                    break;
                case MegaMovementPattern.Rammer:
                case MegaMovementPattern.Dive:
                    y -= _speed * (_age > Mathf.Max(0.85f, _data.telegraphTime) ? 2.4f : 0.35f) * dt;
                    break;
                case MegaMovementPattern.SideSweep:
                    x += Mathf.Sign(_spawnPosition.x == 0f ? 1f : -_spawnPosition.x) * _speed * dt;
                    y -= _speed * 0.2f * dt;
                    break;
                case MegaMovementPattern.Stationary:
                    break;
                default:
                    y -= _speed * dt;
                    break;
            }
            transform.position = new Vector3(x, y, 0f);
        }

        private void UpdateWeapon(float dt)
        {
            if (_data.projectile == null) return;
            if (_telegraphing)
            {
                _telegraphRemaining -= dt;
                UpdateWarningLine();
                if (_telegraphRemaining <= 0f)
                {
                    _telegraphing = false;
                    if (_warningLine != null) _warningLine.gameObject.SetActive(false);
                    FireNow();
                }
                return;
            }

            _fireTimer -= dt;
            if (_fireTimer > 0f) return;
            MegaWeaponPattern pattern = EffectiveWeaponPattern;
            if (pattern == MegaWeaponPattern.Laser || pattern == MegaWeaponPattern.Sniper)
            {
                _telegraphing = true;
                _telegraphRemaining = Mathf.Max(0.85f, _data.telegraphTime);
                if (_warningLine != null) _warningLine.gameObject.SetActive(true);
                UpdateWarningLine();
            }
            else FireNow();
        }

        private MegaWeaponPattern EffectiveWeaponPattern
            => _group != null && _group.firePatternOverride != MegaWeaponPattern.None
                ? _group.firePatternOverride
                : _data.weaponPattern;

        private void FireNow()
        {
            MegaWeaponPattern pattern = EffectiveWeaponPattern;
            int count = pattern == MegaWeaponPattern.Radial ? 8 : pattern == MegaWeaponPattern.Burst ? 3 : pattern == MegaWeaponPattern.FixedSpread ? 3 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction;
                if (pattern == MegaWeaponPattern.Radial)
                    direction = Quaternion.Euler(0f, 0f, i * (360f / count)) * Vector2.down;
                else if (pattern == MegaWeaponPattern.FixedSpread || pattern == MegaWeaponPattern.Burst)
                    direction = Quaternion.Euler(0f, 0f, -24f + i * (48f / Mathf.Max(1, count - 1))) * Vector2.down;
                else
                    direction = _game.Player != null ? ((Vector2)_game.Player.transform.position - (Vector2)transform.position).normalized : Vector2.down;

                _game.SpawnProjectile(_data.projectile, MegaFaction.Enemy, transform.position, direction,
                    _data.projectile.damage * _level.enemyDamageMultiplier,
                    _level.enemyProjectileSpeedMultiplier, 0,
                    pattern == MegaWeaponPattern.AimedSingle ? _game.Player?.transform : null);
            }
            _fireTimer = Mathf.Max(_level.ordinaryEnemyFireInterval,
                _data.fireInterval * _level.enemyFireIntervalMultiplier * _wave.fireRateMultiplier);
        }

        private void UpdateWarningLine()
        {
            if (_warningLine == null) return;
            Vector3 target = _game.Player != null ? _game.Player.transform.position : transform.position + Vector3.down * 10f;
            _warningLine.positionCount = 2;
            _warningLine.SetPosition(0, transform.position);
            _warningLine.SetPosition(1, target);
        }

        public bool TakeDamage(float amount)
        {
            if (_data == null || amount <= 0f) return false;
            _health -= amount;
            _hitGlowRemaining = 0.14f;
            _renderer.color = new Color(1f, 0.08f, 0.08f, 1f);
            _game.SpawnEffect(_game.VFXProfile?.hitSparkPrefab, transform.position,
                new Color(1f, 0.08f, 0.04f, 0.95f), _elite ? 0.75f : 0.55f, 0.25f);
            if (_health <= 0f) Despawn(true);
            return true;
        }

        private void UpdateHitGlow(float dt)
        {
            if (_hitGlowRemaining <= 0f) return;
            _hitGlowRemaining -= dt;
            float t = 1f - Mathf.Clamp01(_hitGlowRemaining / 0.14f);
            _renderer.color = Color.Lerp(new Color(1f, 0.08f, 0.08f, 1f), _baseColor, t);
            if (_hitGlowRemaining <= 0f) _renderer.color = _baseColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SuperAnimalController player = other.GetComponent<SuperAnimalController>();
            if (player == null || _data == null) return;
            player.TakeDamage(Mathf.CeilToInt(_data.contactDamage * _level.enemyDamageMultiplier));
            if (_data.movementPattern == MegaMovementPattern.Rammer || _data.movementPattern == MegaMovementPattern.Dive)
                Despawn(false);
        }

        public void ForceDespawn() => Despawn(false);

        private void Despawn(bool defeated)
        {
            if (!gameObject.activeSelf) return;
            if (defeated)
            {
                GameObject effect = _elite ? _game.VFXProfile?.eliteExplosionPrefab : _game.VFXProfile?.explosionPrefab;
                _game.SpawnEffect(effect, transform.position,
                    _elite ? new Color(1f, 0.35f, 0.05f, 1f) : new Color(1f, 0.1f, 0.04f, 1f),
                    _elite ? 1.2f : 0.85f, _elite ? 0.65f : 0.48f);
                _game.CameraEffects?.Shake(0.08f, _elite ? 0.08f : 0.035f);
                _game.AddScore(Mathf.RoundToInt(_data.score * _wave.scoreMultiplier * (_elite ? 2f : 1f)));
                _game.TryDropPickup(transform.position, _data.pickupChance, _group.pickupDropOverride);
            }
            Unregister(defeated);
            MegaObjectPools.Instance?.Despawn(gameObject);
        }

        private void Unregister(bool defeated)
        {
            if (!_registered) return;
            _registered = false;
            _director?.EnemyRemoved(this, defeated, IsPriority);
            _game?.UnregisterEnemy(this);
        }

        public void OnMegaSpawned() { }

        public void OnMegaDespawned()
        {
            Unregister(false);
            if (_warningLine != null) _warningLine.gameObject.SetActive(false);
            _renderer.color = Color.white;
            _data = null;
            _group = null;
            _wave = null;
            _level = null;
            _game = null;
            _director = null;
        }
    }
}
