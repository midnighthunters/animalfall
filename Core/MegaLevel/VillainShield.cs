using UnityEngine;

namespace AnimalFall.Core.MegaLevel
{
    public class VillainShield : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer shieldRenderer;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private Color shieldColor = new Color(0.3f, 0.6f, 1f, 0.4f);
        [SerializeField] private Color vulnerableColor = new Color(1f, 0.3f, 0.3f, 0.2f);

        private Villain villain;
        private bool showing;

        private void Awake()
        {
            villain = GetComponentInParent<Villain>();
            if (shieldRenderer == null)
                shieldRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (villain == null || shieldRenderer == null) return;

            bool shouldShow = !villain.IsVulnerable && !villain.IsDefeated;

            if (shouldShow != showing)
            {
                showing = shouldShow;
                shieldRenderer.enabled = showing;
            }

            if (!showing) return;

            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 0.3f);
            Color c = villain.IsVulnerable ? vulnerableColor : shieldColor;
            c.a += pulse;
            shieldRenderer.color = c;

            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed * 2f) * 0.05f;
            transform.localScale = Vector3.one * scale;
        }
    }
}
