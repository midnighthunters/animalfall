using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class ShrinkingAnimalHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.ShrinkingAnimal;

        [SerializeField] private float fallSpeed = 1.4f;
        [SerializeField] private float shrinkRate = 0.15f;
        [SerializeField] private float minScale = 0.2f;
        [SerializeField] private int pointValue = 80;

        private float currentScale = 1f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            currentScale = 1f;
            transform.localScale = Vector3.one * currentScale;
        }

        private void Update()
        {
            if (!IsActive) return;

            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            currentScale -= shrinkRate * Time.deltaTime;
            currentScale = Mathf.Max(currentScale, minScale);
            transform.localScale = Vector3.one * currentScale;

            if (transform.position.y < -6f || currentScale <= minScale + 0.01f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            int bonus = Mathf.RoundToInt(pointValue * (1f + (1f - currentScale)));
            context?.GameManager?.OnCorrectTap(1, bonus);
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
            Deactivate();
        }
    }
}
