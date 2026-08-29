using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class AutoWeaponController : MonoBehaviour
    {
        private WeaponData _weapon;
        private SuperAnimalData _animal;
        private MegaShooterGameManager _game;
        private float _powerScale;
        private float _nextShot;
        private int _volleyIndex;

        public float EstimatedDps => _weapon != null ? _weapon.EstimatedDps * _powerScale : 0f;

        public void Configure(WeaponData weapon, SuperAnimalData animal, float powerScale, MegaShooterGameManager game)
        {
            _weapon = weapon;
            _animal = animal;
            _powerScale = Mathf.Max(0.1f, powerScale);
            _game = game;
            _nextShot = Time.time + 0.15f;
            _volleyIndex = 0;
        }

        private void Update()
        {
            if (_weapon == null || _weapon.projectile == null || _game == null || !_game.IsPlayerAutoFireActive) return;
            if (Time.time < _nextShot) return;
            _nextShot = Time.time + 1f / Mathf.Max(0.05f, _weapon.shotsPerSecond);
            FireVolley();
        }

        private void FireVolley()
        {
            _volleyIndex++;
            _game.SpawnEffect(_weapon.muzzleVFX != null ? _weapon.muzzleVFX : _game.VFXProfile?.playerMuzzlePrefab,
                (Vector2)transform.position + Vector2.up * 0.48f,
                new Color(0.2f, 0.95f, 1f, 0.8f), 0.28f, 0.14f);
            int count = Mathf.Max(1, _weapon.projectileCount);
            float spread = count > 1 ? _weapon.spreadDegrees / (count - 1) : 0f;
            float start = -_weapon.spreadDegrees * 0.5f;
            bool strongHoming = _weapon.stronglyHomingEveryNthVolley > 0 && _volleyIndex % _weapon.stronglyHomingEveryNthVolley == 0;
            for (int i = 0; i < count; i++)
            {
                float angle = start + spread * i;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                Vector2 muzzle = _animal.muzzleOffsets != null && _animal.muzzleOffsets.Length > 0
                    ? _animal.muzzleOffsets[i % _animal.muzzleOffsets.Length]
                    : new Vector2(0f, 0.45f);
                _game.SpawnProjectile(_weapon.projectile, MegaFaction.Player,
                    (Vector2)transform.position + muzzle, direction,
                    _weapon.damage * _powerScale, 1f,
                    _weapon.pierce, strongHoming ? _game.NearestEnemyTransform : null);
            }
        }

        public void StopWeapon()
        {
            _weapon = null;
            _game = null;
        }
    }
}
