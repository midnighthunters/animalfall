using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.Advanced
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class MirrorModeHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.MirrorMode;

        [SerializeField] private float fallSpeed = 2f;
        [SerializeField] private float mirrorDuration = 3f;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            float flip = Mathf.PingPong(Time.time * 2f, 1f) > 0.5f ? 1f : -1f;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * flip;
            transform.localScale = scale;

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (context?.InputManager != null)
                StartCoroutine(ApplyMirrorMode());
            else
                Deactivate();
        }

        private IEnumerator ApplyMirrorMode()
        {
            var input = context.InputManager;
            input.IsMirrorModeActive = true;

            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;

            yield return new WaitForSeconds(mirrorDuration);

            if (input != null)
                input.IsMirrorModeActive = false;

            Deactivate();
        }
    }
}
