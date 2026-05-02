using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class TitaniumArmorHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.TitaniumArmor;

        [SerializeField] private int requiredTaps = 5;
        [SerializeField] private float fallSpeed = 1.2f;
        [SerializeField] private int pointValue = 120;

        private int currentTaps;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            currentTaps = 0;
        }

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
            currentTaps++;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float t = (float)currentTaps / requiredTaps;
                sr.color = Color.Lerp(new Color(0.7f, 0.7f, 0.8f), Color.white, t);
            }

            float shake = Random.Range(-0.1f, 0.1f);
            transform.position += new Vector3(shake, shake * 0.5f, 0);

            if (currentTaps >= requiredTaps)
            {
                context?.GameManager?.OnCorrectTap(1, pointValue);
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);
                Deactivate();
            }
        }
    }
}
