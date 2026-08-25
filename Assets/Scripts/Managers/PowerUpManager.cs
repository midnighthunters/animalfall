// Task 6.9 — PowerUpManager: 5 power-up implementations
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Data;

namespace AnimalFall.Managers
{
    public class PowerUpManager : MonoBehaviour
    {
        [SerializeField] private PowerUpData[] _powerUps;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[30];

        // Active state
        private bool     _slowTimeActive;
        private bool     _multiTapActive;
        private int      _multiTapCharges;
        private bool     _autoTapActive;
        private bool     _freezeAllActive;
        private Coroutine _slowTimeCo, _autoTapCo, _freezeCo;

        public void Reset()
        {
            _slowTimeActive  = false;
            _multiTapActive  = false;
            _autoTapActive   = false;
            _freezeAllActive = false;
            if (_slowTimeCo  != null) { StopCoroutine(_slowTimeCo);  _slowTimeCo  = null; Time.timeScale = 1f; }
            if (_autoTapCo   != null) { StopCoroutine(_autoTapCo);   _autoTapCo   = null; }
            if (_freezeCo    != null) { StopCoroutine(_freezeCo);    _freezeCo    = null; ReenableMovement(); }
        }

        // ── SlowTime ─────────────────────────────────────────────────────────

        public void ActivateSlowTime(PowerUpData data)
        {
            if (_slowTimeCo != null) StopCoroutine(_slowTimeCo);
            _slowTimeCo = StartCoroutine(SlowTimeCoroutine(data.duration));
            GameEvents.OnSfxRequested?.Invoke(SfxType.PowerUpActivate);
        }

        private IEnumerator SlowTimeCoroutine(float duration)
        {
            Time.timeScale = 0.5f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _slowTimeCo = null;
        }

        // ── Magnet ───────────────────────────────────────────────────────────

        public void ActivateMagnet(PowerUpData data)
        {
            var animals = FindObjectsOfType<Animal>();
            float centerX = 0f, centerY = 0f;
            if (Camera.main != null)
            {
                var wp = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));
                centerX = wp.x; centerY = wp.y;
            }
            Vector3 center = new Vector3(centerX, centerY, 0f);

            for (int i = 0; i < animals.Length; i++)
            {
                var a = animals[i];
                if (a.IsCollected) continue;
                a.transform.DOMove(center, 1.5f).SetEase(Ease.InQuad).SetId(a.gameObject)
                    .OnComplete(() => { if (!a.IsCollected) a.OnCollected(); });
            }
            GameEvents.OnSfxRequested?.Invoke(SfxType.PowerUpActivate);
        }

        // ── MultiTap ─────────────────────────────────────────────────────────

        public void ActivateMultiTap(PowerUpData data)
        {
            _multiTapCharges = data.charges;
            _multiTapActive  = true;
            GameEvents.OnScreenTapped += OnMultiTap;
            GameEvents.OnSfxRequested?.Invoke(SfxType.PowerUpActivate);
        }

        private void OnMultiTap(Vector2 worldPos)
        {
            if (!_multiTapActive) return;
            int count = Physics2D.OverlapCircleNonAlloc(worldPos, 2f, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var a = _overlapBuffer[i]?.GetComponent<Animal>();
                if (a != null && !a.IsCollected) a.OnCollected();
            }
            _multiTapCharges--;
            if (_multiTapCharges <= 0)
            {
                _multiTapActive = false;
                GameEvents.OnScreenTapped -= OnMultiTap;
            }
        }

        // ── AutoTap ──────────────────────────────────────────────────────────

        public void ActivateAutoTap(PowerUpData data)
        {
            if (_autoTapCo != null) StopCoroutine(_autoTapCo);
            _autoTapCo = StartCoroutine(AutoTapCoroutine(data.duration));
            GameEvents.OnSfxRequested?.Invoke(SfxType.PowerUpActivate);
        }

        private IEnumerator AutoTapCoroutine(float duration)
        {
            float end = Time.time + duration;
            var wait  = new WaitForSeconds(0.4f);
            while (Time.time < end)
            {
                yield return wait;
                var animals = FindObjectsOfType<Animal>();
                for (int i = 0; i < animals.Length; i++)
                {
                    if (!animals[i].IsCollected) { animals[i].OnCollected(); break; }
                }
            }
            _autoTapCo = null;
        }

        // ── FreezeAll ─────────────────────────────────────────────────────────

        public void ActivateFreezeAll(PowerUpData data)
        {
            var movements = FindObjectsOfType<AnimalMovement>();
            for (int i = 0; i < movements.Length; i++) movements[i].enabled = false;
            if (_freezeCo != null) StopCoroutine(_freezeCo);
            _freezeCo = StartCoroutine(FreezeCoroutine(data.duration));
            GameEvents.OnSfxRequested?.Invoke(SfxType.PowerUpActivate);
        }

        private IEnumerator FreezeCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            ReenableMovement();
            _freezeCo = null;
        }

        private void ReenableMovement()
        {
            var movements = FindObjectsOfType<AnimalMovement>();
            for (int i = 0; i < movements.Length; i++) movements[i].enabled = true;
        }
    }
}
