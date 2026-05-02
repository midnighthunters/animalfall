using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.Penalties
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class AlarmClockHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.AlarmClock;

        [SerializeField] private float fallSpeed = 2.2f;
        [SerializeField] private float speedBoostDuration = 3f;
        [SerializeField] private float speedMultiplier = 2f;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            float shake = Mathf.Sin(Time.time * 20f) * 0.05f;
            transform.position += new Vector3(shake, 0, 0);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            StartCoroutine(SpeedUpAnimals());
            IsActive = false;
        }

        private IEnumerator SpeedUpAnimals()
        {
            var animals = FindObjectsOfType<AnimalMovement>();
            var originalSpeeds = new System.Collections.Generic.Dictionary<AnimalMovement, float>();

            foreach (var a in animals)
            {
                originalSpeeds[a] = a.speed;
                a.speed *= speedMultiplier;
            }

            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;

            yield return new WaitForSeconds(speedBoostDuration);

            foreach (var kvp in originalSpeeds)
            {
                if (kvp.Key != null)
                    kvp.Key.speed = kvp.Value;
            }

            Deactivate();
        }
    }
}
