using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class BouncingBorderHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.BouncingBorder;

        [SerializeField] private float duration = 8f;

        private float bottomY;
        private bool active;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            active = true;

            Camera cam = Camera.main;
            if (cam != null)
                bottomY = cam.ViewportToWorldPoint(new Vector3(0, 0.02f, 10f)).y;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Invoke(nameof(FinishHindrance), duration);
        }

        private void Update()
        {
            if (!active) return;

            var animals = FindObjectsOfType<Animal>();
            foreach (var a in animals)
            {
                if (a == null) continue;
                var movement = a.GetComponent<AnimalMovement>();
                if (movement == null) continue;

                if (a.transform.position.y <= bottomY)
                {
                    Vector3 pos = a.transform.position;
                    pos.y = bottomY + 0.5f;
                    a.transform.position = pos;

                    movement.speed = Mathf.Abs(movement.speed) * -0.6f;
                }

                if (movement.speed < 0 && a.transform.position.y > bottomY + 2f)
                {
                    movement.speed = Mathf.Abs(movement.speed);
                }
            }
        }

        private void FinishHindrance()
        {
            active = false;
            Deactivate();
        }
    }
}
