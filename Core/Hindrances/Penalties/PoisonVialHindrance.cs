using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Penalties
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class PoisonVialHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.PoisonVial;

        [SerializeField] private float fallSpeed = 1.8f;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
            transform.Rotate(0, 0, Time.deltaTime * 90f);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (context?.LivesManager != null)
                context.LivesManager.UseLife();

            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Explosion);
            Deactivate();
        }
    }
}
