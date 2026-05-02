using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Penalties
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class BombHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.Bomb;

        [SerializeField] private float timePenalty = 5f;
        [SerializeField] private float fallSpeed = 2f;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (context?.GameManager != null)
            {
                context.GameManager.AddTime(-timePenalty);
                context.AudioManager?.PlaySFX(AudioManager.SfxType.Explosion);
            }

            EffectsController.Instance?.SpawnExplosionEffect(transform.position);
            Deactivate();
        }
    }
}
