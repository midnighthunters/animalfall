// CountdownController — 3-2-1-GO sequence before the timer starts
using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using AnimalFall.Managers;

namespace AnimalFall.UI
{
    public class CountdownController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private static readonly string[] Beats = { "3", "2", "1", "GO!" };
        private static readonly Color[] BeatColors =
        {
            new Color(1f, 0.95f, 0.4f),
            new Color(1f, 0.75f, 0.25f),
            new Color(1f, 0.45f, 0.3f),
            new Color(0.35f, 1f, 0.55f),
        };

        public void PlayCountdown(Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(CountdownCoroutine(onComplete));
        }

        private IEnumerator CountdownCoroutine(Action onComplete)
        {
            if (AnimalFall.Automation.LevelPlaythroughRunner.FastPlayMode)
            {
                if (_root != null) _root.SetActive(false);
                onComplete?.Invoke();
                yield break;
            }

            if (_root != null) _root.SetActive(true);
            if (_canvasGroup == null && _root != null)
                _canvasGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }

            for (int i = 0; i < Beats.Length; i++)
            {
                if (_countdownText != null)
                {
                    DOTween.Kill(_countdownText.transform);
                    _countdownText.text = Beats[i];
                    var c = BeatColors[i];
                    c.a = 1f;
                    _countdownText.color = c;
                    _countdownText.transform.localScale = Vector3.one * 2.4f;
                    _countdownText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetId(_countdownText.transform);
                }

                GameEvents.OnSfxRequested?.Invoke(i == Beats.Length - 1 ? SfxType.ComboUp : SfxType.TimerWarning);
                yield return new WaitForSecondsRealtime(0.7f);
            }

            if (_canvasGroup != null)
            {
                float t = 0f;
                while (t < 0.2f)
                {
                    t += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.2f);
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }

            if (_root != null) _root.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
