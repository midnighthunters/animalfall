using UnityEngine;
using System.Collections.Generic;

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
        private Rigidbody2D _body;
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
        private readonly HashSet<Object> _hitTargets = new HashSet<Object>();
        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        private readonly RaycastHit2D[] _castBuffer = new RaycastHit2D[16];
        private ContactFilter2D _overlapFilter;

        public MegaFaction Faction => _faction;
        public bool IsReflectable => _data != null && _data.reflectable && _reflectableOverride;
        public Vector2 Direction => _direction;
        public float Damage => _damage;
        public float Speed => _speed;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _body = GetComponent<Rigidbody2D>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.useFullKinematicContacts = true;
            _overlapFilter = new ContactFilter2D { useTriggers = true };
            _overlapFilter.SetLayerMask(Physics2D.AllLayers);
            GameObject glow = new GameObject("ProjectileGlow");
            glow.transform.SetParent(transform, false);
            glow.transform.localScale = Vector3.one * 1.25f;
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
            if (faction == MegaFaction.Enemy) _direction = MegaShooterGameManager.ForceDownward(_direction);
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
            _hitTargets.Clear();
            _renderer.sprite = data.sprite;
            _renderer.color = Color.white;
            _glowRenderer.sprite = data.sprite;
            Color glowColor = faction == MegaFaction.Player ? data.playerColor : data.enemyColor;
            glowColor.a = 0.28f;
            _glowRenderer.color = glowColor;
            _collider.radius = Mathf.Max(0.24f, data.colliderRadius * 1.5f);
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
            Vector2 previousPosition = transform.position;

            if (_data.motion == MegaProjectileMotion.Homing && _homingTarget != null)
            {
                Vector2 desired = ((Vector2)_homingTarget.position - (Vector2)transform.position).normalized;
                if (_faction == MegaFaction.Enemy) desired = MegaShooterGameManager.ForceDownward(desired);
                _direction = Vector2.Lerp(_direction, desired, Mathf.Clamp01(_data.homingStrength * dt)).normalized;
                if (_faction == MegaFaction.Enemy) _direction = MegaShooterGameManager.ForceDownward(_direction);
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
            else if (_data.motion == MegaProjectileMotion.StationaryMine)
            {
                _direction = Vector2.down;
            }

            transform.position += (Vector3)(_direction * (_speed * dt));

            // Continuously update rotation to follow velocity vector
            if (_direction.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // Continuous swept collision prevents tunneling at high speeds for both player and enemies
            if (ResolveSweptPlayerHit(previousPosition, transform.position)) return;
            if (ResolveSweptEnemyHit(previousPosition, transform.position)) return;
            ResolveOverlappingHit();
            if (!gameObject.activeSelf) return;
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
            => ResolveHit(other);

        // Kinematic bodies report trigger callbacks only on fixed physics steps.
        // Checking the projectile's overlap after its movement prevents a fast
        // shot from visually crossing an enemy without registering its damage.
        private void ResolveOverlappingHit()
        {
            int overlapCount = Physics2D.OverlapCircle(
                transform.position, Mathf.Max(0.08f, _collider.radius), _overlapFilter, _overlapBuffer);
            for (int i = 0; i < overlapCount; i++)
            {
                Collider2D overlap = _overlapBuffer[i];
                if (overlap == null || overlap == _collider) continue;
                if (ResolveHit(overlap)) return;
            }
        }

        private bool ResolveSweptEnemyHit(Vector2 from, Vector2 to)
        {
            if (_faction != MegaFaction.Player || _game == null) return false;
            Vector2 motion = to - from;
            float dist = motion.magnitude;
            if (dist < 0.0001f) return false;
            int hitCount = Physics2D.CircleCast(
                from, Mathf.Max(0.06f, _collider.radius * 0.45f), motion / dist, _overlapFilter, _castBuffer, dist);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _castBuffer[i].collider;
                if (col == null || col == _collider) continue;
                if (ResolveHit(col)) return true;
            }
            return false;
        }

        private bool ResolveSweptPlayerHit(Vector2 from, Vector2 to)
        {
            if (_faction != MegaFaction.Enemy || _game == null || _game.Player == null) return false;
            Vector2 segment = to - from;
            float segmentLengthSqr = segment.sqrMagnitude;
            float t = segmentLengthSqr > 0.000001f
                ? Mathf.Clamp01(Vector2.Dot((Vector2)_game.Player.transform.position - from, segment) / segmentLengthSqr)
                : 0f;
            Vector2 closest = from + segment * t;
            float projectileRadius = _collider.radius * Mathf.Max(
                Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            float hitRadius = Mathf.Max(0.04f, projectileRadius) + _game.Player.HitboxRadius;
            if (((Vector2)_game.Player.transform.position - closest).sqrMagnitude > hitRadius * hitRadius) return false;
            return HitPlayer(_game.Player);
        }

        private bool ResolveHit(Collider2D other)
        {
            if (other == null) return false;
            if (_faction == MegaFaction.Player)
            {
                MegaEnemyController enemy = other.GetComponent<MegaEnemyController>();
                if (enemy != null && HitEnemy(enemy)) return true;
                MegaBossController boss = other.GetComponent<MegaBossController>();
                if (boss != null && HitBoss(boss)) return true;
            }
            else
            {
                SuperAnimalController player = other.GetComponent<SuperAnimalController>();
                if (player != null && HitPlayer(player)) return true;
            }
            return false;
        }

        private bool HitEnemy(MegaEnemyController enemy)
        {
            if (!_hitTargets.Add(enemy) || !enemy.TakeDamage(_damage)) return false;
            SpawnImpact();
            ConsumePierce();
            return true;
        }

        private bool HitBoss(MegaBossController boss)
        {
            if (!_hitTargets.Add(boss) || !boss.TakeDamage(_damage)) return false;
            SpawnImpact();
            ConsumePierce();
            return true;
        }

        private bool HitPlayer(SuperAnimalController player)
        {
            if (!_hitTargets.Add(player)) return false;
            _nearMissResolved = true;
            player.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_damage)));
            SpawnImpact();
            Despawn();
            return true;
        }

        public bool Reflect(Vector2 direction)
        {
            if (!IsReflectable || _faction != MegaFaction.Enemy) return false;
            if (_registered) _game?.ChangeProjectileFaction(this, MegaFaction.Enemy, MegaFaction.Player);
            _faction = MegaFaction.Player;
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _homingTarget = _game?.NearestEnemyTransform;
            _damage = Mathf.Max(_damage, 20f);
            _renderer.color = Color.white;
            Color glowColor = _data != null ? _data.playerColor : new Color(0.2f, 0.9f, 1f, 1f);
            glowColor.a = 0.28f;
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
