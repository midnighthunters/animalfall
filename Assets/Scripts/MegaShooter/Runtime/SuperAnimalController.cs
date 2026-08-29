using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class SuperAnimalController : MonoBehaviour, IMegaPoolable
    {
        public const int VillainHitsToDefeat = 3;

        private SuperAnimalData _data;
        private MegaLevelData _level;
        private MegaShooterGameManager _game;
        private SpriteRenderer _renderer;
        private CircleCollider2D _collider;
        private Rigidbody2D _body;
        private AutoWeaponController _weapon;
        private MegaCounterController _counter;
        private Vector2 _desiredPosition;
        private int _health;
        private int _villainHitsTaken;
        private float _invulnerableUntil;
        private float _movementModifier = 1f;
        private float _movementModifierUntil;
        private float _hitTintRemaining;
        private Color _baseColor = Color.white;

        public int Health => _health;
        public int VillainHitsTaken => _villainHitsTaken;
        public int MaxHealth { get; private set; }
        public float HitboxRadius => _data != null ? _data.hitboxRadius : 0.22f;
        public SuperAnimalData Data => _data;
        public MegaCounterController Counter => _counter;

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
            _weapon = GetComponent<AutoWeaponController>();
            _counter = GetComponent<MegaCounterController>();
        }

        public void Configure(SuperAnimalData data, MegaLevelData level, MegaShooterGameManager game)
        {
            _data = data;
            _level = level;
            _game = game;
            _renderer.sprite = data.shipSprite;
            _collider.radius = data.hitboxRadius;
            _baseColor = Color.white;
            _renderer.color = _baseColor;
            // Mega-level survivability is hit based: every accepted villain hit
            // removes one shield, and the third hit ends the level.
            MaxHealth = VillainHitsToDefeat;
            _health = MaxHealth;
            _villainHitsTaken = 0;
            _desiredPosition = transform.position;
            _invulnerableUntil = Time.time + 1.35f;
            _hitTintRemaining = 0f;
            _movementModifier = 1f;
            _movementModifierUntil = 0f;
            _weapon.Configure(data.primaryWeapon, data, level.playerPowerMultiplier, game);
            _counter.Configure(data.counter, data, level, game, this);
            _game.Hud?.SetHealth(_health, MaxHealth);
        }

        private void Update()
        {
            UpdateHitTint(Time.deltaTime);
            if (_data == null || _game == null || _game.IsCombatFrozen) return;
            if (_movementModifierUntil > 0f && Time.time >= _movementModifierUntil)
            {
                _movementModifier = 1f;
                _movementModifierUntil = 0f;
            }
            float speed = _data.movementSpeed * _data.passive.movementMultiplier * _level.movementSpeedMultiplier * _movementModifier;
            Vector3 previous = transform.position;
            transform.position = Vector2.MoveTowards(transform.position, _desiredPosition, speed * Time.deltaTime);
            float tilt = Mathf.Clamp((transform.position.x - previous.x) * 20f, -12f, 12f);
            transform.rotation = Quaternion.Euler(0f, 0f, -tilt);
        }

        public void SetDesiredPosition(Vector2 worldPosition)
        {
            if (_level == null) return;
            Rect bounds = _level.playerMovementBounds;
            _desiredPosition = new Vector2(
                Mathf.Clamp(worldPosition.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(worldPosition.y, bounds.yMin, bounds.yMax));
        }

        public bool TakeDamage(int amount)
        {
            if (_health <= 0 || Time.time < _invulnerableUntil || _game == null || _game.IsCombatFrozen) return false;
            _villainHitsTaken++;
            _health = Mathf.Max(0, MaxHealth - _villainHitsTaken);
            _invulnerableUntil = Time.time + _level.invulnerabilityDuration * 1.25f;
            _hitTintRemaining = 0.28f;
            _renderer.color = new Color(1f, 0.12f, 0.12f, 1f);
            _counter.OnPlayerDamaged();
            _game.Hud?.SetHealth(_health, MaxHealth);
            _game.CameraEffects?.Shake(0.1f, 0.12f);
            if (_health <= 0) _game.PlayerDefeated();
            return true;
        }

        private void UpdateHitTint(float dt)
        {
            if (_renderer == null || _hitTintRemaining <= 0f) return;
            _hitTintRemaining -= dt;
            float t = 1f - Mathf.Clamp01(_hitTintRemaining / 0.28f);
            _renderer.color = Color.Lerp(new Color(1f, 0.12f, 0.12f, 1f), _baseColor, t);
            if (_hitTintRemaining <= 0f) _renderer.color = _baseColor;
        }

        public void Heal(int amount)
        {
            // The three-hit rule is cumulative for the level, so pickups cannot
            // erase a registered hit. Health pickups instead provide brief cover.
            if (amount > 0) GrantInvulnerability(0.75f * amount);
        }

        public void GrantInvulnerability(float duration)
            => _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + Mathf.Max(0f, duration));

        public void ApplyMovementModifier(float multiplier, float duration)
        {
            if (duration <= 0f) return;
            _movementModifier = Mathf.Min(_movementModifier, Mathf.Clamp(multiplier, 0.15f, 1f));
            _movementModifierUntil = Mathf.Max(_movementModifierUntil, Time.time + duration);
        }

        public void OnMegaSpawned() { }

        public void OnMegaDespawned()
        {
            _weapon?.StopWeapon();
            _data = null;
            _level = null;
            _game = null;
            _villainHitsTaken = 0;
            _movementModifier = 1f;
            _movementModifierUntil = 0f;
            _hitTintRemaining = 0f;
            if (_renderer != null) _renderer.color = Color.white;
        }
    }
}
