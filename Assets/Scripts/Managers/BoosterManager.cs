using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace AnimalFall
{
    /// <summary>
    /// Manages booster inventory, selection state, and activation logic.
    /// Handles Bomb (destroys all animals), Rainbow (collects one species), and Rocket (clears vertical lane).
    /// </summary>
    public class BoosterManager : MonoBehaviour
    {
        [Header("Booster Counts")]
        [SerializeField] private int _bombCount = 3;
        [SerializeField] private int _rainbowCount = 3;
        [SerializeField] private int _rocketCount = 3;

        [Header("VFX")]
        [SerializeField] private GameObject _bombExplosionVFX;
        [SerializeField] private GameObject _rainbowCollectionVFX;
        [SerializeField] private GameObject _rocketLaunchVFX;

        [Header("Settings")]
        [SerializeField] private float _rocketLaneWidth = 1.5f;
        [SerializeField] private Color _selectionHighlight = new Color(1f, 1f, 0.5f, 1f);

        // Current state
        private BoosterType _selectedBooster = BoosterType.None;
        private bool _waitingForTarget;
        private Color _originalColor;

        // Events
        public event Action<BoosterType, int> OnBoosterCountChanged;
        public event Action<BoosterType> OnBoosterSelected;
        public event Action OnBoosterDeselected;

        public int BombCount => _bombCount;
        public int RainbowCount => _rainbowCount;
        public int RocketCount => _rocketCount;
        public BoosterType SelectedBooster => _selectedBooster;
        public bool IsBoosterSelected => _selectedBooster != BoosterType.None;

        private void OnEnable()
        {
            GameEvents.OnScreenTapped += OnScreenTapped;
        }

        private void OnDisable()
        {
            GameEvents.OnScreenTapped -= OnScreenTapped;
        }

        /// <summary>
        /// Reset booster counts for a new level.
        /// </summary>
        public void ResetBoosters(int bombCount = 3, int rainbowCount = 3, int rocketCount = 3)
        {
            _bombCount = bombCount;
            _rainbowCount = rainbowCount;
            _rocketCount = rocketCount;
            _selectedBooster = BoosterType.None;
            _waitingForTarget = false;

            OnBoosterCountChanged?.Invoke(BoosterType.Bomb, _bombCount);
            OnBoosterCountChanged?.Invoke(BoosterType.Rainbow, _rainbowCount);
            OnBoosterCountChanged?.Invoke(BoosterType.Rocket, _rocketCount);
        }

        /// <summary>
        /// Attempt to select a booster. Returns true if selection succeeded.
        /// </summary>
        public bool SelectBooster(BoosterType type)
        {
            if (type == BoosterType.None) return false;

            // Check if we have any of this booster
            int count = GetBoosterCount(type);
            if (count <= 0)
            {
                Debug.LogWarning($"[BoosterManager] Cannot select {type} - count is {count}");
                return false;
            }

            // If already selected, deselect
            if (_selectedBooster == type)
            {
                DeselectBooster();
                return false;
            }

            // Select the booster
            _selectedBooster = type;
            _waitingForTarget = (type == BoosterType.Rainbow || type == BoosterType.Rocket);

            OnBoosterSelected?.Invoke(type);
            Debug.Log($"[BoosterManager] Selected {type}. Waiting for target: {_waitingForTarget}");

            // If bomb, activate immediately (no target needed)
            if (type == BoosterType.Bomb)
            {
                StartCoroutine(ActivateBombDelayed());
            }

            return true;
        }

        /// <summary>
        /// Deselect the current booster without using it.
        /// </summary>
        public void DeselectBooster()
        {
            if (_selectedBooster == BoosterType.None) return;

            Debug.Log($"[BoosterManager] Deselected {_selectedBooster}");
            _selectedBooster = BoosterType.None;
            _waitingForTarget = false;
            OnBoosterDeselected?.Invoke();
        }

        private IEnumerator ActivateBombDelayed()
        {
            // Small delay for visual feedback
            yield return new WaitForSeconds(0.1f);
            ActivateBomb();
        }

        private void OnScreenTapped(Vector2 worldPos)
        {
            if (!_waitingForTarget) return;

            // Find the animal at the tapped position
            Animal targetAnimal = GetAnimalAtPosition(worldPos);

            if (_selectedBooster == BoosterType.Rainbow)
            {
                if (targetAnimal != null && targetAnimal.Data != null)
                {
                    ActivateRainbow(targetAnimal.Data.species);
                }
                else
                {
                    Debug.Log("[BoosterManager] Rainbow: No animal at tap position");
                    DeselectBooster();
                }
            }
            else if (_selectedBooster == BoosterType.Rocket)
            {
                ActivateRocket(worldPos.x);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // BOMB: Destroy all animals on screen
        // ──────────────────────────────────────────────────────────────────
        private void ActivateBomb()
        {
            if (_bombCount <= 0)
            {
                DeselectBooster();
                return;
            }

            _bombCount--;
            OnBoosterCountChanged?.Invoke(BoosterType.Bomb, _bombCount);

            // Get screen center for VFX
            Vector3 centerPos = Vector3.zero;
            if (Camera.main != null)
            {
                centerPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                centerPos.z = 0f;
            }

            // Spawn explosion VFX
            if (_bombExplosionVFX != null)
            {
                GameObject vfx = Instantiate(_bombExplosionVFX, centerPos, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            // Destroy all animals
            var animals = new List<Animal>(ActiveAnimalRegistry.All);
            int destroyedCount = 0;

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;

                // Add a small delay to each for cascade effect
                float delay = destroyedCount * 0.02f;
                StartCoroutine(DestroyAnimalDelayed(animal, delay));
                destroyedCount++;
            }

            GameEvents.OnSfxRequested?.Invoke(SfxType.Explosion);
            Debug.Log($"[BoosterManager] Bomb activated! Destroyed {destroyedCount} animals");

            _selectedBooster = BoosterType.None;
            _waitingForTarget = false;
            OnBoosterDeselected?.Invoke();
        }

        private IEnumerator DestroyAnimalDelayed(Animal animal, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (animal != null && !animal.IsCollected)
            {
                animal.PlayPopAndReturn(false);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // RAINBOW: Collect all animals of the selected species
        // ──────────────────────────────────────────────────────────────────
        private void ActivateRainbow(AnimalSpecies targetSpecies)
        {
            if (_rainbowCount <= 0)
            {
                DeselectBooster();
                return;
            }

            _rainbowCount--;
            OnBoosterCountChanged?.Invoke(BoosterType.Rainbow, _rainbowCount);

            var animals = new List<Animal>(ActiveAnimalRegistry.All);
            int collectedCount = 0;

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;
                if (animal.Data == null || animal.Data.species != targetSpecies) continue;

                // Add rainbow VFX at each animal position
                if (_rainbowCollectionVFX != null)
                {
                    Vector3 pos = animal.transform.position;
                    GameObject vfx = Instantiate(_rainbowCollectionVFX, pos, Quaternion.identity);
                    Destroy(vfx, 2f);
                }

                // Collect with a small cascading delay
                float delay = collectedCount * 0.05f;
                StartCoroutine(CollectAnimalDelayed(animal, delay));
                collectedCount++;
            }

            if (collectedCount > 0)
            {
                GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);
                Debug.Log($"[BoosterManager] Rainbow activated! Collected {collectedCount} {targetSpecies} animals");
            }
            else
            {
                Debug.Log($"[BoosterManager] Rainbow: No {targetSpecies} animals found on screen");
            }

            _selectedBooster = BoosterType.None;
            _waitingForTarget = false;
            OnBoosterDeselected?.Invoke();
        }

        private IEnumerator CollectAnimalDelayed(Animal animal, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (animal != null && !animal.IsCollected)
            {
                animal.OnCollected();
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // ROCKET: Clear a vertical lane at the target X position
        // ──────────────────────────────────────────────────────────────────
        private void ActivateRocket(float targetX)
        {
            if (_rocketCount <= 0)
            {
                DeselectBooster();
                return;
            }

            _rocketCount--;
            OnBoosterCountChanged?.Invoke(BoosterType.Rocket, _rocketCount);

            // Spawn rocket VFX at bottom, traveling up
            if (_rocketLaunchVFX != null && Camera.main != null)
            {
                Vector3 bottomPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, 10f));
                bottomPos.x = targetX;
                bottomPos.z = 0f;
                GameObject vfx = Instantiate(_rocketLaunchVFX, bottomPos, Quaternion.identity);
                
                // Animate rocket moving up
                Vector3 topPos = bottomPos;
                topPos.y = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, 10f)).y + 2f;
                vfx.transform.DOMove(topPos, 0.8f).SetEase(Ease.InQuad);
                Destroy(vfx, 1.5f);
            }

            // Collect all animals in the vertical lane
            var animals = new List<Animal>(ActiveAnimalRegistry.All);
            int collectedCount = 0;
            float halfWidth = _rocketLaneWidth * 0.5f;

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;
                
                float animalX = animal.transform.position.x;
                if (Mathf.Abs(animalX - targetX) <= halfWidth)
                {
                    float delay = collectedCount * 0.03f;
                    StartCoroutine(CollectAnimalDelayed(animal, delay));
                    collectedCount++;
                }
            }

            if (collectedCount > 0)
            {
                GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);
                Debug.Log($"[BoosterManager] Rocket activated at X={targetX:F2}! Collected {collectedCount} animals");
            }
            else
            {
                Debug.Log($"[BoosterManager] Rocket: No animals in lane at X={targetX:F2}");
            }

            _selectedBooster = BoosterType.None;
            _waitingForTarget = false;
            OnBoosterDeselected?.Invoke();
        }

        // ──────────────────────────────────────────────────────────────────
        // Helper methods
        // ──────────────────────────────────────────────────────────────────
        private int GetBoosterCount(BoosterType type)
        {
            return type switch
            {
                BoosterType.Bomb => _bombCount,
                BoosterType.Rainbow => _rainbowCount,
                BoosterType.Rocket => _rocketCount,
                _ => 0
            };
        }

        private Animal GetAnimalAtPosition(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                Animal animal = hit.GetComponent<Animal>();
                if (animal == null)
                    animal = hit.GetComponentInParent<Animal>();
                return animal;
            }
            return null;
        }
    }

    public enum BoosterType
    {
        None,
        Bomb,
        Rainbow,
        Rocket
    }
}
