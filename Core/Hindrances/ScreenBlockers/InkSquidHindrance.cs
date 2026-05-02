using UnityEngine;
using AnimalFall.Effects;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class InkSquidHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.InkSquid;

        [SerializeField] private float fallSpeed = 1.8f;
        [SerializeField] private float inkDuration = 3f;

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
            context?.ScreenEffects?.ShowInkSplatter(inkDuration);
            Deactivate();
        }
    }
}
