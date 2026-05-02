using UnityEngine;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class ZeroGravityHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.ZeroGravity;

        [SerializeField] private float duration = 2f;
        [SerializeField] private float fallSpeed = 2f;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            float bob = Mathf.Sin(Time.time * 4f) * 0.3f;
            transform.position += new Vector3(bob * Time.deltaTime, 0, 0);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            context?.EnvironmentEffects?.ActivateZeroGravity(duration);
            Deactivate();
        }
    }
}
