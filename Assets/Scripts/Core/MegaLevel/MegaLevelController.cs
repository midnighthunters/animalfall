// Task 8.2 — MegaLevelController
using System.Collections;
using UnityEngine;
using AnimalFall.Core;
using AnimalFall.Data;
using AnimalFall.Effects;
using AnimalFall.Managers;

namespace AnimalFall.Core.MegaLevel
{
    public class MegaLevelController : MonoBehaviour
    {
        [SerializeField] private Transform _projectileContainer;

        private VillainData _villain;
        private int         _currentPhase;
        private int         _currentPhaseCollected;
        private Coroutine   _projectileLoop;
        private WaitForSeconds[] _phaseWaits;

        public void InitMegaLevel(LevelData level)
        {
            if (level.Villain == null)
            {
                Debug.LogError($"[MegaLevelController] LevelData.Villain is null. Falling back to normal flow.");
                return;
            }

            _villain              = level.Villain;
            _currentPhase         = 0;
            _currentPhaseCollected = 0;

            // Pre-allocate one WaitForSeconds per phase
            _phaseWaits = new WaitForSeconds[_villain.projectileFrequencyPerPhase.Length];
            for (int i = 0; i < _phaseWaits.Length; i++)
                _phaseWaits[i] = new WaitForSeconds(_villain.projectileFrequencyPerPhase[i]);

            GameEvents.OnVillainPhaseChanged?.Invoke(0, _villain.hpPhases);

            if (_projectileLoop != null) StopCoroutine(_projectileLoop);
            _projectileLoop = StartCoroutine(ProjectileLoop());
        }

        public void OnAnimalCollected()
        {
            if (_villain == null) return;
            _currentPhaseCollected++;

            int quota = _villain.animalsPerPhase != null && _currentPhase < _villain.animalsPerPhase.Length
                ? _villain.animalsPerPhase[_currentPhase]
                : 5;

            if (_currentPhaseCollected >= quota)
                OnAnimalQuotaMet();
        }

        public void Cleanup()
        {
            if (_projectileLoop != null) { StopCoroutine(_projectileLoop); _projectileLoop = null; }
        }

        private IEnumerator ProjectileLoop()
        {
            while (true)
            {
                int phaseIdx = Mathf.Clamp(_currentPhase, 0, _phaseWaits.Length - 1);
                yield return _phaseWaits[phaseIdx];
                SpawnProjectile();
            }
        }

        private void SpawnProjectile()
        {
            if (_villain.projectilePrefab == null) return;

            float x = Camera.main != null
                ? Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0.2f, 0.8f), 1.1f, Mathf.Abs(Camera.main.transform.position.z))).x
                : 0f;

            var go = ObjectPooler.Instance?.SpawnFromPool(
                _villain.projectilePrefab,
                new Vector3(x, 6f, 0f),
                Quaternion.identity,
                _projectileContainer);

            if (go == null) return;
            StartCoroutine(ProjectileWindow(go));
        }

        private IEnumerator ProjectileWindow(GameObject go)
        {
            bool tapped = false;

            // Listen for tap on this projectile for 0.5s
            void OnTap(Vector2 _)
            {
                if (go == null) return;
                // Simple proximity check
                if (Camera.main != null)
                {
                    Vector2 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    if (Vector2.Distance(wp, go.transform.position) < 1f)
                    {
                        tapped = true;
                        DealDeflectDamage();
                    }
                }
            }

            GameEvents.OnScreenTapped += OnTap;
            yield return new WaitForSeconds(0.5f);
            GameEvents.OnScreenTapped -= OnTap;

            if (!tapped)
                GameManager.Instance?.AddTime(-3f);

            if (go != null) ObjectPooler.Instance?.ReturnToPool(go);
        }

        private void DealDeflectDamage()
        {
            // Deflect damage = advance phase by 1
            OnAnimalQuotaMet();
        }

        private void OnAnimalQuotaMet()
        {
            _currentPhase++;
            _currentPhaseCollected = 0;

            if (_currentPhase >= _villain.hpPhases)
            {
                Cleanup();
                GameManager.Instance?.OnMegaLevelComplete();
                return;
            }

            GameEvents.OnVillainPhaseChanged?.Invoke(_currentPhase, _villain.hpPhases);
            ScreenEffects.Instance?.FlashWhite();

            // Update projectile loop timing (restart with new wait)
            if (_projectileLoop != null) StopCoroutine(_projectileLoop);
            _projectileLoop = StartCoroutine(ProjectileLoop());
        }
    }
}
