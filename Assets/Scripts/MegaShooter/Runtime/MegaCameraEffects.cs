using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaCameraEffects : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Image _flashOverlay;
        private MegaVFXProfile _profile;
        private Vector3 _basePosition;
        private Coroutine _shake;
        private Coroutine _flash;

        public void Configure(MegaVFXProfile profile)
        {
            _profile = profile;
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) _basePosition = _camera.transform.position;
        }

        public void Shake(float duration, float amplitude)
        {
            float scale = _profile != null ? _profile.masterShakeScale : 1f;
            if (_profile != null && _profile.reducedShake) scale *= 0.25f;
            if (scale <= 0f || _camera == null) return;
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeRoutine(duration, amplitude * scale));
        }

        public void Flash(float alpha, float duration)
            => Flash(Color.white, alpha, duration);

        public void Flash(Color flashColor, float alpha, float duration)
        {
            float scale = _profile != null ? _profile.masterFlashScale : 1f;
            if (_profile != null && _profile.reducedFlash) scale *= 0.2f;
            if (_flashOverlay == null || scale <= 0f) return;
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(flashColor, alpha * scale, duration));
        }

        private IEnumerator ShakeRoutine(float duration, float amplitude)
        {
            float end = Time.unscaledTime + duration;
            while (Time.unscaledTime < end)
            {
                _camera.transform.position = _basePosition + (Vector3)(Random.insideUnitCircle * amplitude);
                yield return null;
            }
            _camera.transform.position = _basePosition;
            _shake = null;
        }

        private IEnumerator FlashRoutine(Color flashColor, float alpha, float duration)
        {
            _flashOverlay.gameObject.SetActive(true);
            Color color = flashColor;
            float end = Time.unscaledTime + duration;
            while (Time.unscaledTime < end)
            {
                float remaining = Mathf.Clamp01((end - Time.unscaledTime) / Mathf.Max(0.01f, duration));
                color.a = alpha * remaining;
                _flashOverlay.color = color;
                yield return null;
            }
            color.a = 0f;
            _flashOverlay.color = color;
            _flashOverlay.gameObject.SetActive(false);
            _flash = null;
        }

        private void OnDisable()
        {
            if (_camera != null) _camera.transform.position = _basePosition;
        }
    }
}
