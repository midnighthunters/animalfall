using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Levels;
using AnimalFall.Core.PowerUps;

namespace AnimalFall.Managers
{
    public class PowerUpManager : MonoBehaviour
    {
        public bool IsPaused { get; private set; }

        private readonly Dictionary<PowerUpType, Coroutine> activeEffects = new Dictionary<PowerUpType, Coroutine>();

        public void InitForLevel(LevelData level)
        {
            CancelAll();
            IsPaused = false;
        }

        public void UsePowerUp(PowerUpData powerUp)
        {
            if (activeEffects.ContainsKey(powerUp.type)) return;

            switch (powerUp.type)
            {
                case PowerUpType.SlowTime:
                    activeEffects[powerUp.type] = StartCoroutine(SlowTimeRoutine(powerUp.duration, powerUp.value));
                    break;
                case PowerUpType.Magnet:
                    activeEffects[powerUp.type] = StartCoroutine(MagnetRoutine(powerUp.duration, powerUp.value));
                    break;
                case PowerUpType.MultiTap:
                    activeEffects[powerUp.type] = StartCoroutine(MultiTapRoutine(powerUp.duration, (int)powerUp.value));
                    break;
                case PowerUpType.AutoTap:
                    activeEffects[powerUp.type] = StartCoroutine(AutoTapRoutine(powerUp.duration, powerUp.value));
                    break;
                case PowerUpType.ShieldBreaker:
                    StartCoroutine(ShieldBreakerRoutine());
                    break;
                case PowerUpType.BombClear:
                    BombClear();
                    break;
                case PowerUpType.ScoreMultiplier:
                    activeEffects[powerUp.type] = StartCoroutine(ScoreMultiplierRoutine(powerUp.duration, powerUp.value));
                    break;
                case PowerUpType.ExtraTime:
                    GameManager.Instance?.AddTime(powerUp.value);
                    break;
                case PowerUpType.FreezeHighlight:
                    activeEffects[powerUp.type] = StartCoroutine(FreezeHighlightRoutine(powerUp.duration));
                    break;
            }
        }

        public void CancelAll()
        {
            foreach (var coroutine in activeEffects.Values)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            activeEffects.Clear();
        }

        private IEnumerator SlowTimeRoutine(float duration, float slowFactor)
        {
            var animals = FindObjectsOfType<AnimalMovement>();
            var originalSpeeds = new Dictionary<AnimalMovement, float>();
            foreach (var a in animals)
            {
                originalSpeeds[a] = a.speed;
                a.speed *= slowFactor;
            }

            yield return new WaitForSeconds(duration);

            foreach (var kvp in originalSpeeds)
            {
                if (kvp.Key != null) kvp.Key.speed = kvp.Value;
            }
            activeEffects.Remove(PowerUpType.SlowTime);
        }

        private IEnumerator MagnetRoutine(float duration, float radius)
        {
            float elapsed = 0f;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            while (elapsed < duration)
            {
                var animals = FindObjectsOfType<Animal>();
                foreach (var a in animals)
                {
                    if (a == null || a.data == null || !a.data.isTargetSpecies) continue;
                    float dist = Vector2.Distance(
                        a.transform.position,
                        Camera.main.ScreenToWorldPoint(screenCenter)
                    );
                    if (dist <= radius)
                        a.HandleTap();
                }

                elapsed += 0.25f;
                yield return new WaitForSeconds(0.25f);
            }
            activeEffects.Remove(PowerUpType.Magnet);
        }

        private IEnumerator MultiTapRoutine(float duration, int multiplicity)
        {
            yield return new WaitForSeconds(duration);
            activeEffects.Remove(PowerUpType.MultiTap);
        }

        private IEnumerator AutoTapRoutine(float duration, float tapsPerSecond)
        {
            AutoTapService auto = AutoTapService.Instance;
            if (auto != null) auto.StartAutoTap(tapsPerSecond);
            yield return new WaitForSeconds(duration);
            if (auto != null) auto.StopAutoTap();
            activeEffects.Remove(PowerUpType.AutoTap);
        }

        private IEnumerator ShieldBreakerRoutine()
        {
            Animal[] animals = FindObjectsOfType<Animal>();
            foreach (var a in animals)
            {
                if (a.data != null &&
                    (a.data.type == AnimalType.Shielded || a.data.requiresDoubleTap))
                {
                    a.data.requiresDoubleTap = false;
                    a.currentShield = 0;
                    break;
                }
            }
            yield return null;
        }

        private void BombClear()
        {
            Animal[] animals = FindObjectsOfType<Animal>();
            foreach (var a in animals)
            {
                if (a.data != null && a.data.type == AnimalType.Bomb)
                    Destroy(a.gameObject);
            }
        }

        private IEnumerator ScoreMultiplierRoutine(float duration, float value)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.SetComboMultiplier(value);
            yield return new WaitForSeconds(duration);
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.SetComboMultiplier(1f);
            activeEffects.Remove(PowerUpType.ScoreMultiplier);
        }

        private IEnumerator FreezeHighlightRoutine(float duration)
        {
            var animals = FindObjectsOfType<Animal>();
            var frozenMovements = new List<AnimalMovement>();

            foreach (var a in animals)
            {
                if (a.data == null) continue;
                if (!a.data.isTargetSpecies)
                {
                    var movement = a.GetComponent<AnimalMovement>();
                    if (movement != null)
                    {
                        frozenMovements.Add(movement);
                        movement.enabled = false;
                    }
                }
            }

            yield return new WaitForSeconds(duration);

            foreach (var m in frozenMovements)
            {
                if (m != null) m.enabled = true;
            }
            activeEffects.Remove(PowerUpType.FreezeHighlight);
        }
    }
}
