using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Penalties
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class FakeAnimalHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.FakeAnimal;

        [SerializeField] private float fallSpeed = 1.5f;
        [SerializeField] private int pointDeduction = 100;

        private float driftX;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            driftX = Random.Range(-0.3f, 0.3f);
        }

        private void Update()
        {
            if (!IsActive) return;

            Vector3 movement = (Vector3.down + Vector3.right * driftX) * fallSpeed * Time.deltaTime;
            transform.Translate(movement);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (context?.ScoreManager != null)
                context.ScoreManager.AddPoints(-pointDeduction);

            context?.AudioManager?.PlaySFX(AudioManager.SfxType.WrongTap);
            Deactivate();
        }
    }
}
