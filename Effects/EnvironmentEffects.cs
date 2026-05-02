using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Effects
{
    public class EnvironmentEffects : MonoBehaviour
    {
        public static EnvironmentEffects Instance { get; private set; }

        [Header("Wind")]
        [SerializeField] private ParticleSystem windParticles;

        [Header("Laser")]
        [SerializeField] private GameObject laserBeamPrefab;
        [SerializeField] private Transform laserContainer;

        [Header("Black Hole")]
        [SerializeField] private GameObject blackHolePrefab;

        private Vector2 activeWindForce;
        private bool windActive;
        private bool zeroGravityActive;
        private bool blackHoleActive;
        private Vector2 blackHoleCenter;
        private float blackHolePullStrength = 2f;

        public bool IsWindActive => windActive;
        public Vector2 WindForce => activeWindForce;
        public bool IsZeroGravityActive => zeroGravityActive;
        public bool IsBlackHoleActive => blackHoleActive;
        public Vector2 BlackHoleCenter => blackHoleCenter;
        public float BlackHolePullStrength => blackHolePullStrength;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ActivateWind(Vector2 force, float duration)
        {
            StartCoroutine(WindRoutine(force, duration));
        }

        public void ActivateZeroGravity(float duration)
        {
            StartCoroutine(ZeroGravityRoutine(duration));
        }

        public void ActivateBlackHole(Vector2 center, float strength, float duration)
        {
            StartCoroutine(BlackHoleRoutine(center, strength, duration));
        }

        public void SpawnLaserBeam(float yPosition, float duration, float warningTime = 1f)
        {
            StartCoroutine(LaserBeamRoutine(yPosition, duration, warningTime));
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            windActive = false;
            zeroGravityActive = false;
            blackHoleActive = false;
            activeWindForce = Vector2.zero;

            if (windParticles != null) windParticles.Stop();
        }

        private IEnumerator WindRoutine(Vector2 force, float duration)
        {
            windActive = true;
            activeWindForce = force;

            if (windParticles != null)
            {
                var main = windParticles.main;
                main.startSpeed = force.magnitude * 2f;
                windParticles.Play();
            }

            yield return new WaitForSeconds(duration);

            windActive = false;
            activeWindForce = Vector2.zero;
            if (windParticles != null) windParticles.Stop();
        }

        private IEnumerator ZeroGravityRoutine(float duration)
        {
            zeroGravityActive = true;

            var animals = FindObjectsOfType<AnimalMovement>();
            var savedSpeeds = new Dictionary<AnimalMovement, float>();
            foreach (var a in animals)
            {
                savedSpeeds[a] = a.speed;
                a.speed = 0f;
            }

            yield return new WaitForSeconds(duration);

            foreach (var kvp in savedSpeeds)
            {
                if (kvp.Key != null) kvp.Key.speed = kvp.Value;
            }

            zeroGravityActive = false;
        }

        private IEnumerator BlackHoleRoutine(Vector2 center, float strength, float duration)
        {
            blackHoleActive = true;
            blackHoleCenter = center;
            blackHolePullStrength = strength;

            GameObject bhVisual = null;
            if (blackHolePrefab != null)
            {
                bhVisual = Instantiate(blackHolePrefab,
                    new Vector3(center.x, center.y, 0f), Quaternion.identity);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var animals = FindObjectsOfType<Animal>();
                foreach (var a in animals)
                {
                    if (a == null) continue;
                    Vector2 dir = center - (Vector2)a.transform.position;
                    float dist = dir.magnitude;
                    if (dist > 0.5f)
                    {
                        float pullForce = strength / (dist * dist) * Time.deltaTime;
                        a.transform.position += (Vector3)(dir.normalized * pullForce);
                    }
                }
                yield return null;
            }

            blackHoleActive = false;
            if (bhVisual != null) Destroy(bhVisual);
        }

        private IEnumerator LaserBeamRoutine(float yPosition, float duration, float warningTime)
        {
            if (laserBeamPrefab == null) yield break;

            Transform parent = laserContainer != null ? laserContainer : transform;
            GameObject laser = Instantiate(laserBeamPrefab,
                new Vector3(0f, yPosition, 0f), Quaternion.identity, parent);

            SpriteRenderer sr = laser.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color warnColor = new Color(1f, 0f, 0f, 0.3f);
                sr.color = warnColor;
            }

            yield return new WaitForSeconds(warningTime);

            if (sr != null)
                sr.color = Color.red;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var animals = FindObjectsOfType<Animal>();
                foreach (var a in animals)
                {
                    if (a == null) continue;
                    if (Mathf.Abs(a.transform.position.y - yPosition) < 0.3f)
                        Destroy(a.gameObject);
                }
                yield return null;
            }

            if (laser != null) Destroy(laser);
        }
    }
}
