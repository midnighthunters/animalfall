using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class KnightHelmetHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.KnightHelmet;

        [SerializeField] private int requiredTaps = 3;
        [SerializeField] private float fallSpeed = 1.5f;
        [SerializeField] private int pointValue = 75;

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
                sr.color = Color.Lerp(Color.white, Color.grey, t);
            }

            Vector3 origScale = transform.localScale;
            transform.localScale = origScale * 1.1f;
            LeanTweenHelper.DelayedAction(this, 0.1f,
                () => { if (this != null) transform.localScale = origScale; });

            if (currentTaps >= requiredTaps)
            {
                context?.GameManager?.OnCorrectTap(1, pointValue);
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);
                Deactivate();
            }
            else
            {
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);
            }
        }
    }

    internal static class LeanTweenHelper
    {
        public static void DelayedAction(MonoBehaviour host, float delay, System.Action action)
        {
            host.StartCoroutine(DelayCoroutine(delay, action));
        }

        private static System.Collections.IEnumerator DelayCoroutine(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
