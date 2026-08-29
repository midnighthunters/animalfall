// Task 7.3 — ScreenEffects: pooled overlay methods
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;

namespace AnimalFall.Effects
{
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        [SerializeField] private GameObject _inkOverlayPrefab;
        [SerializeField] private GameObject _stormGradientPrefab;
        [SerializeField] private GameObject _flashbangPrefab;
        [SerializeField] private GameObject _borderFlashPrefab;

        private readonly List<GameObject> _activeOverlays = new List<GameObject>(4);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void ShowInkOverlay(float duration)
        {
            if (_inkOverlayPrefab == null) return;
            var go = SpawnOverlay(_inkOverlayPrefab);
            if (go == null) return;

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = Color.white;

            // Disable raycast blocking
            var img = go.GetComponent<UnityEngine.UI.Graphic>();
            if (img != null) img.raycastTarget = false;

            StartCoroutine(FadeOutAndReturn(go, duration, 1f));
        }

        public void ShowStormGradient(float duration)
        {
            if (_stormGradientPrefab == null) return;
            var go = SpawnOverlay(_stormGradientPrefab);
            if (go == null) return;
            StartCoroutine(FadeOutAndReturn(go, duration, 0.5f));
        }

        public void FlashWhite()
        {
            if (_flashbangPrefab == null) return;
            var go = SpawnOverlay(_flashbangPrefab);
            if (go == null) return;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { ReturnOverlay(go); return; }

            sr.color = new Color(1f, 1f, 1f, 0f);
            var seq = DOTween.Sequence().SetId(go);
            seq.Append(sr.DOFade(0.9f, 0.1f));
            seq.Append(sr.DOFade(0f, 0.7f));
            seq.OnComplete(() => ReturnOverlay(go));
        }

        public void BorderFlashGold()
        {
            if (_borderFlashPrefab == null) return;
            var go = SpawnOverlay(_borderFlashPrefab);
            if (go == null) return;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { ReturnOverlay(go); return; }

            var seq = DOTween.Sequence().SetId(go);
            seq.Append(sr.DOFade(0.8f, 0.1f));
            seq.AppendInterval(0.2f);
            seq.Append(sr.DOFade(0f, 0.2f));
            seq.OnComplete(() => ReturnOverlay(go));
        }

        public void ClearAll()
        {
            for (int i = _activeOverlays.Count - 1; i >= 0; i--)
            {
                if (_activeOverlays[i] != null)
                {
                    DOTween.Kill(_activeOverlays[i]);
                    ReturnOverlay(_activeOverlays[i]);
                }
            }
            _activeOverlays.Clear();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private GameObject SpawnOverlay(GameObject prefab)
        {
            var go = ObjectPooler.Instance?.SpawnFromPool(prefab, Vector3.zero, Quaternion.identity, transform);
            if (go != null) _activeOverlays.Add(go);
            return go;
        }

        private void ReturnOverlay(GameObject go)
        {
            _activeOverlays.Remove(go);
            ObjectPooler.Instance?.ReturnToPool(go);
        }

        private IEnumerator FadeOutAndReturn(GameObject go, float holdDuration, float fadeTime)
        {
            yield return new WaitForSeconds(holdDuration);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                DOTween.Kill(go);
                sr.DOFade(0f, fadeTime).SetId(go).OnComplete(() => ReturnOverlay(go));
            }
            else { ReturnOverlay(go); }
        }
    }
}
