using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.Advanced
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class MagnetTrapHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.MagnetTrap;

        [SerializeField] private float fallSpeed = 1.5f;
        [SerializeField] private float offsetAmount = 0.8f;
        [SerializeField] private float effectDuration = 5f;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
            transform.localScale = Vector3.one * pulse;

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (context?.InputManager != null)
                StartCoroutine(ApplyMagnetEffect());
            else
                Deactivate();
        }

        private IEnumerator ApplyMagnetEffect()
        {
            var input = context.InputManager;
            input.TapOffset = new Vector2(
                Random.Range(-offsetAmount, offsetAmount),
                Random.Range(-offsetAmount, offsetAmount));

            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;

            yield return new WaitForSeconds(effectDuration);

            if (input != null)
                input.TapOffset = Vector2.zero;

            Deactivate();
        }
    }
}
