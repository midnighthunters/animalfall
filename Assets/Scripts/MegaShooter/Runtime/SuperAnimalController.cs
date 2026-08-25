using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class SuperAnimalController : MonoBehaviour, IMegaPoolable
    {
        private SuperAnimalData _data;
        private MegaLevelData _level;
        private MegaShooterGameManager _game;
        private SpriteRenderer _renderer;
        private CircleCollider2D _collider;
        private AutoWeaponController _weapon;
        private MegaCounterController _counter;
        private Vector2 _desiredPosition;
        private int _health;
        private float _invulnerableUntil;

        public int Health => _health;
        public int MaxHealth { get; private set; }
        public float HitboxRadius => _data != null ? _data.hitboxRadius : 0.22f;
        public SuperAnimalData Data => _data;
        public MegaCounterController Counter => _counter;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
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
            MaxHealth = Mathf.Max(1, level.startingHealth + data.passive.bonusHealth);
            _health = MaxHealth;
            _desiredPosition = transform.position;
            _invulnerableUntil = Time.time + 1f;
            _weapon.Configure(data.primaryWeapon, data, level.playerPowerMultiplier, game);
            _counter.Configure(data.counter, data, level, game, this);
            _game.Hud?.SetHealth(_health, MaxHealth);
        }

        private void Update()
        {
            if (_data == null || _game == null || _game.IsCombatFrozen) return;
            float speed = _data.movementSpeed * _data.passive.movementMultiplier * _level.movementSpeedMultiplier;
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
            float reduction = _data != null ? _data.passive.frontalDamageReduction : 0f;
            int applied = Mathf.Max(1, Mathf.CeilToInt(amount * (1f - reduction)));
            _health = Mathf.Max(0, _health - applied);
            _invulnerableUntil = Time.time + _level.invulnerabilityDuration;
            _counter.OnPlayerDamaged();
            _game.Hud?.SetHealth(_health, MaxHealth);
            _game.CameraEffects?.Shake(0.1f, 0.12f);
            if (_health <= 0) _game.PlayerDefeated();
            return true;
        }

        public void Heal(int amount)
        {
            _health = Mathf.Min(MaxHealth, _health + Mathf.Max(0, amount));
            _game?.Hud?.SetHealth(_health, MaxHealth);
        }

        public void GrantInvulnerability(float duration)
            => _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + Mathf.Max(0f, duration));

        public void OnMegaSpawned() { }

        public void OnMegaDespawned()
        {
            _weapon?.StopWeapon();
            _data = null;
            _level = null;
            _game = null;
        }
    }
}
