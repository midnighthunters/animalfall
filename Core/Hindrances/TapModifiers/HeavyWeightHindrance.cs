using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class HeavyWeightHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.HeavyWeight;

        [SerializeField] private float baseSpeed = 1.5f;
        [SerializeField] private float speedMultiplier = 3f;
        [SerializeField] private int pointValue = 55;

        private float actualSpeed;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            actualSpeed = baseSpeed * speedMultiplier;

            transform.localScale = Vector3.one * 1.3f;
        }

        private void Update()
        {
            if (!IsActive) return;

            transform.Translate(Vector3.down * actualSpeed * Time.deltaTime);

            float wobble = Mathf.Sin(Time.time * 8f) * 2f;
            transform.rotation = Quaternion.Euler(0, 0, wobble);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            context?.GameManager?.OnCorrectTap(1, pointValue);
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
            Deactivate();
        }
    }
}
