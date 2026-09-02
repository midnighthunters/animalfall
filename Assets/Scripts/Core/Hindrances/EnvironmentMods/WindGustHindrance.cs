// A strong, rightward gust with dedicated wind-streak VFX.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class WindGustHindrance : HindranceBase
    {
        private const float GustDuration = 5.5f;
        private const int StreakCount = 26;

        private HindranceEffectToken _token;
        private readonly List<GameObject> _visualStreaks = new List<GameObject>(StreakCount);
        private Material _windMaterial;

        public override HindranceType Type => HindranceType.WindGust;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;

            // Keep the gameplay direction unambiguous: every gust carries animals right.
            float horizontalStrength = Random.Range(6.2f, 7.4f);
            var wind = new Vector2(horizontalStrength, Random.Range(-0.18f, 0.18f));
            _token = _ctx.EnvironmentEffects?.AddWind(this, wind);
            StartCoroutine(PlayWindVisuals());
            StartCoroutine(EndAfter(GustDuration));
        }

        protected override void OnDeactivate()
        {
            _token?.Dispose();
            _token = null;

            for (int i = _visualStreaks.Count - 1; i >= 0; i--)
            {
                GameObject streak = _visualStreaks[i];
                if (streak == null) continue;
                DOTween.Kill(streak);
                Destroy(streak);
            }
            _visualStreaks.Clear();

            if (_windMaterial != null)
            {
                Destroy(_windMaterial);
                _windMaterial = null;
            }
        }

        private IEnumerator EndAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Deactivate();
        }

        private IEnumerator PlayWindVisuals()
        {
            if (Camera.main == null || !EnsureWindMaterial()) yield break;

            float z = Mathf.Abs(Camera.main.transform.position.z);
            float left = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, z)).x;
            float right = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0f, z)).x;
            float bottom = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, z)).y;
            float top = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, z)).y;

            for (int i = 0; i < StreakCount && _isActive; i++)
            {
                CreateWindStreak(left, right, bottom, top);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private bool EnsureWindMaterial()
        {
            if (_windMaterial != null) return true;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return false;

            _windMaterial = new Material(shader) { name = "Wind Gust Runtime Material" };
            return true;
        }

        private void CreateWindStreak(float left, float right, float bottom, float top)
        {
            float startX = left - 1.5f;
            float endX = right + 1.5f;
            float y = Random.Range(bottom + 0.35f, top - 0.35f);
            float length = Random.Range(0.65f, 1.45f);

            GameObject streak = new GameObject("Wind Gust VFX");
            streak.transform.SetParent(transform, false);
            streak.transform.position = new Vector3(startX, y, 0f);

            LineRenderer line = streak.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 4;
            line.SetPosition(0, new Vector3(-length * 0.5f, 0.00f, 0f));
            line.SetPosition(1, new Vector3(-length * 0.16f, 0.08f, 0f));
            line.SetPosition(2, new Vector3(length * 0.18f, -0.04f, 0f));
            line.SetPosition(3, new Vector3(length * 0.5f, 0.00f, 0f));
            line.widthMultiplier = Random.Range(0.045f, 0.095f);
            line.numCapVertices = 4;
            line.material = _windMaterial;
            line.sortingOrder = 120;

            Color tint = Random.value < 0.5f
                ? new Color(0.78f, 0.94f, 1f, 0.78f)
                : new Color(1f, 1f, 1f, 0.68f);
            line.startColor = tint;
            line.endColor = new Color(tint.r, tint.g, tint.b, 0f);
            _visualStreaks.Add(streak);

            float duration = Random.Range(0.75f, 1.15f);
            streak.transform.DOMoveX(endX, duration)
                .SetEase(Ease.OutQuad)
                .SetId(streak)
                .OnComplete(() => RemoveWindStreak(streak));
        }

        private void RemoveWindStreak(GameObject streak)
        {
            _visualStreaks.Remove(streak);
            if (streak != null) Destroy(streak);
        }
    }
}
