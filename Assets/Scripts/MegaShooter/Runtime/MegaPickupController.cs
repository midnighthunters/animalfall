using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class MegaPickupController : MonoBehaviour, IMegaPoolable
    {
        private MegaPickupType _type;
        private MegaShooterGameManager _game;
        private SpriteRenderer _renderer;
        private float _age;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Configure(MegaPickupType type, Sprite sprite, MegaShooterGameManager game)
        {
            _type = type;
            _game = game;
            _renderer.sprite = sprite;
            _renderer.color = type == MegaPickupType.Health ? new Color(0.4f, 1f, 0.55f) : new Color(0.35f, 0.9f, 1f);
            _age = 0f;
        }

        private void Update()
        {
            if (_game == null || _game.IsCombatFrozen) return;
            _age += Time.deltaTime;
            transform.position += Vector3.down * (1.8f * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, _age * 90f);
            if (_age > 8f) MegaObjectPools.Instance?.Despawn(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SuperAnimalController player = other.GetComponent<SuperAnimalController>();
            if (player == null) return;
            if (_type == MegaPickupType.Health) player.Heal(1);
            else if (_type == MegaPickupType.CounterEnergy) player.Counter?.AddCharge(35f);
            else if (_type == MegaPickupType.Shield) player.GrantInvulnerability(3f);
            else player.Counter?.AddCharge(20f);
            MegaObjectPools.Instance?.Despawn(gameObject);
        }

        public void OnMegaSpawned() { }
        public void OnMegaDespawned() { _game = null; }
    }
}
