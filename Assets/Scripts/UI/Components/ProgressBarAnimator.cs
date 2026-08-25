using UnityEngine;
using UnityEngine.UI;

namespace AnimalFall.UI.Components
{
    public class ProgressBarAnimator : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private float lerpSpeed = 5f;
        [SerializeField] private Gradient colorGradient;

        private float targetFill;

        private void Awake()
        {
            if (colorGradient == null || colorGradient.colorKeys.Length == 0)
            {
                colorGradient = new Gradient();
                colorGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(Color.red, 0f),
                        new GradientColorKey(Color.yellow, 0.5f),
                        new GradientColorKey(Color.green, 1f)
                    },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
        }

        public void SetTarget(float value)
        {
            targetFill = Mathf.Clamp01(value);
        }

        private void Update()
        {
            if (fillImage == null) return;
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
            fillImage.color = colorGradient.Evaluate(fillImage.fillAmount);
        }
    }
}
