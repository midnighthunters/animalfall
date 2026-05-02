using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.Penalties
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class ThiefBirdHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.ThiefBird;

        [SerializeField] private float horizontalSpeed = 4f;
        [SerializeField] private float stealRadius = 1.5f;

        private int direction;
        private float yPosition;
        private bool hasStolen;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            direction = Random.value > 0.5f ? 1 : -1;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float startX = direction > 0
                    ? cam.ViewportToWorldPoint(new Vector3(-0.1f, 0.5f, 10f)).x
                    : cam.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, 10f)).x;
                yPosition = cam.ViewportToWorldPoint(
                    new Vector3(0, Random.Range(0.3f, 0.8f), 10f)).y;
                transform.position = new Vector3(startX, yPosition, 0f);
            }

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -direction;
            transform.localScale = scale;
        }

        private void Update()
        {
            if (!IsActive) return;

            transform.Translate(Vector3.right * direction * horizontalSpeed * Time.deltaTime,
                Space.World);

            if (!hasStolen)
                TryStealAnimal();

            Camera cam = Camera.main;
            if (cam != null)
            {
                float edge = direction > 0
                    ? cam.ViewportToWorldPoint(new Vector3(1.2f, 0, 10f)).x
                    : cam.ViewportToWorldPoint(new Vector3(-0.2f, 0, 10f)).x;

                if ((direction > 0 && transform.position.x > edge) ||
                    (direction < 0 && transform.position.x < edge))
                    Deactivate();
            }
        }

        private void TryStealAnimal()
        {
            var animals = FindObjectsOfType<Animal>();
            foreach (var a in animals)
            {
                if (a == null || a.data == null || !a.data.isTargetSpecies) continue;

                float dist = Vector2.Distance(transform.position, a.transform.position);
                if (dist <= stealRadius)
                {
                    hasStolen = true;
                    Destroy(a.gameObject);
                    break;
                }
            }
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            Deactivate();
        }
    }
}
