using System.Collections;
using UnityEngine;

namespace AnimalFall.Effects
{
    public class EffectsController : MonoBehaviour
    {
        public static EffectsController Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private GameObject collectSparkPrefab;
        [SerializeField] private GameObject coinRainPrefab;

        [Header("Camera Shake")]
        [SerializeField] private float shakeIntensity = 0.3f;
        [SerializeField] private float shakeDuration = 0.2f;

        private Camera mainCam;
        private Vector3 originalCamPos;
        private bool shaking;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            mainCam = Camera.main;
            if (mainCam != null) originalCamPos = mainCam.transform.position;
        }

        public void SpawnExplosionEffect(Vector3 position)
        {
            if (explosionPrefab != null)
            {
                var obj = Instantiate(explosionPrefab, position, Quaternion.identity);
                Destroy(obj, 1.5f);
            }
        }

        public void SpawnCollectSpark(Vector3 position)
        {
            if (collectSparkPrefab != null)
            {
                var obj = Instantiate(collectSparkPrefab, position, Quaternion.identity);
                Destroy(obj, 1f);
            }
        }

        public void SpawnCoinRain(Vector3 position, float duration = 2f)
        {
            if (coinRainPrefab != null)
            {
                var obj = Instantiate(coinRainPrefab, position, Quaternion.identity);
                Destroy(obj, duration + 1f);
            }
        }

        public void ShakeCamera()
        {
            if (!shaking && mainCam != null)
                StartCoroutine(CameraShakeRoutine());
        }

        private IEnumerator CameraShakeRoutine()
        {
            shaking = true;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-shakeIntensity, shakeIntensity);
                float y = Random.Range(-shakeIntensity, shakeIntensity);
                mainCam.transform.position = originalCamPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCam.transform.position = originalCamPos;
            shaking = false;
        }
    }
}
