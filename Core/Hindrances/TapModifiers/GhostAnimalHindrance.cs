using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class GhostAnimalHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.GhostAnimal;

        [SerializeField] private float fallSpeed = 1.5f;
        [SerializeField] private float visibleDuration = 1.5f;
        [SerializeField] private float invisibleDuration = 0.8f;
        [SerializeField] private int pointValue = 60;

        private SpriteRenderer sr;
        private Collider2D col;
        private float cycleTimer;
        private bool visible = true;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            cycleTimer = visibleDuration;
            visible = true;
        }

        private void Update()
        {
            if (!IsActive) return;

            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            cycleTimer -= Time.deltaTime;
            if (cycleTimer <= 0f)
            {
                visible = !visible;
                cycleTimer = visible ? visibleDuration : invisibleDuration;

                if (sr != null)
                    sr.color = visible ? Color.white : new Color(1, 1, 1, 0.1f);
                if (col != null)
                    col.enabled = visible;
            }

            if (!visible && sr != null)
            {
                float flicker = Mathf.PingPong(Time.time * 5f, 0.15f);
                sr.color = new Color(1, 1, 1, flicker);
            }

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive || !visible) return;

            context?.GameManager?.OnCorrectTap(1, pointValue);
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
            Deactivate();
        }
    }
}
