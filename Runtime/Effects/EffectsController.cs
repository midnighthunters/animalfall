using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Effects
{
    public class EffectsController : MonoBehaviour
    {
        public static EffectsController Instance { get; private set; }

        [Header("Animal FX Prefabs")]
        [SerializeField] private GameObject collectEffectPrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private GameObject shieldBreakEffectPrefab;
        [SerializeField] private GameObject goldenCollectEffectPrefab;

        [Header("Camera Shake")]
        [SerializeField] private float shakeDuration = 0.35f;
        [SerializeField] private float shakeMagnitude = 0.15f;

        [Header("Species Colors")]
        [SerializeField] private Color chickenColor = new Color(1f, 0.85f, 0.6f);
        [SerializeField] private Color dogColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color cowColor = new Color(1f, 0.9f, 0.55f);
        [SerializeField] private Color catColor = new Color(0.6f, 1f, 0.6f);
        [SerializeField] private Color monkeyColor = new Color(0.75f, 0.6f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void SpawnCollectEffect(Vector3 position, AnimalSpecies species)
        {
            if (collectEffectPrefab == null) return;
            GameObject fx = Instantiate(collectEffectPrefab, position, Quaternion.identity);
            SetParticleColor(fx, GetColorForSpecies(species));
            Destroy(fx, 3f);
        }

        public void SpawnExplosionEffect(Vector3 position)
        {
            if (explosionEffectPrefab == null) return;
            GameObject fx = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(fx, 3.5f);
        }

        public void SpawnShieldBreakEffect(Vector3 position)
        {
            if (shieldBreakEffectPrefab == null) return;
            GameObject fx = Instantiate(shieldBreakEffectPrefab, position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        public void SpawnGoldenEffect(Vector3 position)
        {
            if (goldenCollectEffectPrefab == null) return;
            GameObject fx = Instantiate(goldenCollectEffectPrefab, position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        public void ShakeCamera()
        {
            StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude));
        }

        public IEnumerator StaggeredEffects(IEnumerable<Transform> targets, float interval = 0.06f)
        {
            foreach (var t in targets)
            {
                if (t == null) continue;
                SpawnCollectEffect(t.position, AnimalSpecies.None);
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator CameraShakeRoutine(float duration, float magnitude)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Transform camTransform = cam.transform;
            Vector3 originalPos = camTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                camTransform.localPosition = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }

            camTransform.localPosition = originalPos;
        }

        private void SetParticleColor(GameObject fx, Color color)
        {
            foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.startColor = color;
            }
        }

        private Color GetColorForSpecies(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Chicken: return chickenColor;
                case AnimalSpecies.Dog:     return dogColor;
                case AnimalSpecies.Cow:     return cowColor;
                case AnimalSpecies.Cat:     return catColor;
                case AnimalSpecies.Monkey:  return monkeyColor;
                default:                    return Color.white;
            }
        }
    }
}
