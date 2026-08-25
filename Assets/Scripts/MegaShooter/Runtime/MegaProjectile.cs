using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class MegaProjectile : MonoBehaviour, IMegaPoolable
    {
        private ProjectileData _data;
        private MegaShooterGameManager _game;
        private MegaFaction _faction;
        private SpriteRenderer _renderer;
        private SpriteRenderer _glowRenderer;
        private CircleCollider2D _collider;
        private Vector2 _direction;
        private Transform _homingTarget;
        private float _damage;
        private float _speed;
        private float _life;
        private float _age;
        private int _pierceRemaining;
        private bool _enteredNearRadius;
        private bool _nearMissResolved;
        private bool _registered;
        private bool _reflectableOverride = true;

        public MegaFaction Faction => _faction;
        public bool IsReflectable => _data != null && _data.reflectable && _reflectableOverride;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            GameObject glow = new GameObject("ProjectileGlow");
            glow.transform.SetParent(transform, false);
            glow.transform.localScale = Vector3.one * 1.65f;
            _glowRenderer = glow.AddComponent<SpriteRenderer>();
            _glowRenderer.sortingOrder = _renderer.sortingOrder - 1;
        }

        public void Configure(ProjectileData data, MegaFaction faction, Vector2 direction, float damage,
            float speedMultiplier, int pierce, Transform homingTarget, MegaShooterGameManager game,
            bool reflectableOverride = true)
        {
            _data = data;
            _faction = faction;
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            _damage = Mathf.Max(0f, damage);
            _speed = Mathf.Max(0.1f, data.speed * speedMultiplier);
            _life = Mathf.Max(0.1f, data.lifetime);
            _pierceRemaining = Mathf.Max(0, pierce);
            _homingTarget = homingTarget;
            _game = game;
            _reflectableOverride = reflectableOverride;
            _age = 0f;
            _enteredNearRadius = false;
            _nearMissResolved = false;
            _renderer.sprite = data.sprite;
            _renderer.color = faction == MegaFaction.Player ? data.playerColor : data.enemyColor;
            _glowRenderer.sprite = data.sprite;
            Color glowColor = _renderer.color;
            glowColor.a = 0.22f;
            _glowRenderer.color = glowColor;
            _collider.radius = data.colliderRadius;
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _registered = true;
            _game?.RegisterProjectile(this);
        }

        private void Update()
        {
            if (_data == null || _game == null || _game.IsCombatFrozen) return;
            float scale = _faction == MegaFaction.Enemy ? _game.HostileTimeScale : 1f;
            float dt = Time.deltaTime * scale;
            _age += dt;

            if (_data.motion == MegaProjectileMotion.Homing && _homingTarget != null)
            {
                Vector2 desired = ((Vector2)_homingTarget.position - (Vector2)transform.position).normalized;
                _direction = Vector2.Lerp(_direction, desired, Mathf.Clamp01(_data.homingStrength * dt)).normalized;
            }
            else if (_data.motion == MegaProjectileMotion.Sine)
            {
                Vector2 perpendicular = new Vector2(-_direction.y, _direction.x);
                transform.position += (Vector3)(perpendicular * (Mathf.Sin(_age * _data.sineFrequency) * _data.sineAmplitude * dt));
            }
            else if (_data.motion == MegaProjectileMotion.Returning && _age > _life * 0.5f)
            {
                _direction = Vector2.down;
            }

            if (_data.motion != MegaProjectileMotion.StationaryMine)
                transform.position += (Vector3)(_direction * (_speed * dt));

            TrackNearMiss();
            if (_age >= _life || !_game.IsInsideDespawnBounds(transform.position))
                Despawn();
        }

        private void TrackNearMiss()
        {
            if (_faction != MegaFaction.Enemy || _nearMissResolved || _game.Player == null) return;
            float distance = Vector2.Distance(transform.position, _game.Player.transform.position);
            float inner = _game.Player.HitboxRadius;
            float outer = inner + _game.NearMissOuterRadius;
            if (distance <= outer && distance > inner) _enteredNearRadius = true;
            else if (_enteredNearRadius && distance > outer)
            {
                _nearMissResolved = true;
                _game.RegisterNearMiss(transform.position);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_faction == MegaFaction.Player)
            {
                MegaEnemyController enemy = other.GetComponent<MegaEnemyController>();
                if (enemy != null && enemy.TakeDamage(_damage)) { SpawnImpact(); ConsumePierce(); return; }
                MegaBossController boss = other.GetComponent<MegaBossController>();
                if (boss != null && boss.TakeDamage(_damage)) { SpawnImpact(); ConsumePierce(); }
            }
            else
            {
                SuperAnimalController player = other.GetComponent<SuperAnimalController>();
                if (player != null)
                {
                    _nearMissResolved = true;
                    player.TakeDamage(Mathf.CeilToInt(_damage));
                    SpawnImpact();
                    Despawn();
                }
            }
        }

        public bool Reflect(Vector2 direction)
        {
            if (!IsReflectable || _faction != MegaFaction.Enemy) return false;
            if (_registered) _game?.ChangeProjectileFaction(this, MegaFaction.Enemy, MegaFaction.Player);
            _faction = MegaFaction.Player;
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            _homingTarget = _game?.NearestEnemyTransform;
            _damage = Mathf.Max(_damage, 20f);
            _renderer.color = _data.playerColor;
            Color glowColor = _data.playerColor;
            glowColor.a = 0.22f;
            _glowRenderer.color = glowColor;
            _nearMissResolved = true;
            return true;
        }

        private void SpawnImpact()
        {
            GameObject effect = _data != null && _data.impactVFX != null ? _data.impactVFX : _game?.VFXProfile?.hitSparkPrefab;
            Color color = _faction == MegaFaction.Player
                ? new Color(0.25f, 0.95f, 1f, 0.95f)
                : new Color(1f, 0.08f, 0.05f, 0.95f);
            _game?.SpawnEffect(effect, transform.position, color, 0.42f, 0.2f);
        }

        private void ConsumePierce()
        {
            if (_pierceRemaining > 0) _pierceRemaining--;
            else Despawn();
        }

        private void Despawn()
        {
            if (gameObject.activeSelf) MegaObjectPools.Instance?.Despawn(gameObject);
        }

        public void OnMegaSpawned() { }

        public void OnMegaDespawned()
        {
            if (_registered) _game?.UnregisterProjectile(this, _faction);
            _registered = false;
            _data = null;
            _game = null;
            _homingTarget = null;
        }
    }
}
