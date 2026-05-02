using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class ZigZagFlyerHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.ZigZagFlyer;

        [SerializeField] private float fallSpeed = 1.6f;
        [SerializeField] private float zigzagAmplitude = 2f;
        [SerializeField] private float zigzagFrequency = 3f;
        [SerializeField] private int pointValue = 65;

        private float startX;
        private float spawnTime;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            startX = transform.position.x;
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (!IsActive) return;

            float elapsed = Time.time - spawnTime;
            float x = startX + Mathf.Sin(elapsed * zigzagFrequency) * zigzagAmplitude;
            float y = transform.position.y - fallSpeed * Time.deltaTime;

            transform.position = new Vector3(x, y, transform.position.z);

            float angle = Mathf.Cos(elapsed * zigzagFrequency) * 15f;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (y < -6f)
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
