using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.MegaLevel
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class VillainProjectile : MonoBehaviour
    {
        private float speed;
        private int damage;
        private Vector3 direction;
        private bool deflected;

        public void Initialize(float projectileSpeed, int projectileDamage)
        {
            speed = projectileSpeed;
            damage = projectileDamage;
            deflected = false;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 target = cam.ViewportToWorldPoint(
                    new Vector3(Random.Range(0.1f, 0.9f), Random.Range(0f, 0.3f), 10f));
                target.z = 0f;
                direction = (target - transform.position).normalized;
            }
            else
            {
                direction = Vector3.down;
            }
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;

            transform.Rotate(0, 0, Time.deltaTime * 180f);

            if (transform.position.y < -7f || transform.position.y > 7f ||
                Mathf.Abs(transform.position.x) > 7f)
            {
                Destroy(gameObject);
            }
        }

        private void OnMouseDown()
        {
            if (deflected) return;
            Deflect();
        }

        private void Deflect()
        {
            deflected = true;
            direction = Vector3.up + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);
            direction = direction.normalized;
            speed *= 1.5f;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.cyan;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (deflected)
            {
                Villain villain = other.GetComponent<Villain>();
                if (villain != null && !villain.IsDefeated)
                {
                    villain.TakeDamage(damage);
                    AudioManager.Instance?.PlaySFX(AudioManager.SfxType.Explosion);
                    Destroy(gameObject);
                }
            }
            else
            {
                if (other.GetComponent<Villain>() == null)
                {
                    GameManager.Instance?.AddTime(-2f);
                    AudioManager.Instance?.PlaySFX(AudioManager.SfxType.Explosion);
                    Destroy(gameObject);
                }
            }
        }
    }
}
