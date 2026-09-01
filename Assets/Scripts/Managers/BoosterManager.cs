using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using AnimalFall.Core.Animals;
using AnimalFall.Data;
using AnimalFall.Managers;

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

        [Header("Booster Sprites")]
        [SerializeField] private Sprite _bombSprite;
        [SerializeField] private Sprite _rainbowSprite;
        [SerializeField] private Sprite _rocketSprite;

        [Header("Settings")]
        [SerializeField] private float _rocketLaneWidth = 1.5f;
        [SerializeField] private Color _selectionHighlight = new Color(1f, 1f, 0.5f, 1f);
        [SerializeField] private float _rainbowRotationSpeed = 720f;
        [SerializeField] private float _rainbowPullDuration = 1.2f;
        [SerializeField] private float _rocketSpeed = 15f;
        [SerializeField] private float _bombAppearDelay = 0.05f;

        // Current state
        private BoosterType _selectedBooster = BoosterType.None;
        private bool _waitingForTarget;
        private Color _originalColor;
        private Transform _vfxContainer;

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
            
            // Create VFX container for organization
            if (_vfxContainer == null)
            {
                GameObject container = new GameObject("BoosterVFX_Runtime");
                _vfxContainer = container.transform;
                _vfxContainer.SetParent(transform);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnScreenTapped -= OnScreenTapped;
            
            // Clean up VFX container
            if (_vfxContainer != null)
            {
                Destroy(_vfxContainer.gameObject);
                _vfxContainer = null;
            }
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

            // Freeze animals when booster is selected
            FreezeAllAnimals();

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

            // Unfreeze animals
            UnfreezeAllAnimals();

            OnBoosterDeselected?.Invoke();
        }

        private void FreezeAllAnimals()
        {
            var movements = FindObjectsOfType<AnimalMovement>();
            foreach (var movement in movements)
            {
                movement.enabled = false;
            }
        }

        private void UnfreezeAllAnimals()
        {
            var movements = FindObjectsOfType<AnimalMovement>();
            foreach (var movement in movements)
            {
                movement.enabled = true;
            }
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
        // BOMB: Bomb sprites appear on each animal, then explode
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

            var animals = new List<Animal>(ActiveAnimalRegistry.All);
            
            if (animals.Count == 0)
            {
                Debug.Log("[BoosterManager] Bomb: No animals on screen");
                UnfreezeAllAnimals();
                _selectedBooster = BoosterType.None;
                _waitingForTarget = false;
                OnBoosterDeselected?.Invoke();
                return;
            }

            StartCoroutine(BombSequence(animals));
        }

        private IEnumerator BombSequence(List<Animal> animals)
        {
            List<GameObject> bombSprites = new List<GameObject>();

            // Phase 1: Spawn bomb sprites on each animal with stagger
            for (int i = 0; i < animals.Count; i++)
            {
                if (animals[i] == null || animals[i].IsCollected) continue;

                Vector3 pos = animals[i].transform.position;
                GameObject bombObj = CreateSpriteObject(_bombSprite, pos, 0.8f);
                bombSprites.Add(bombObj);

                // Animate bomb appearing (scale up with bounce)
                bombObj.transform.localScale = Vector3.zero;
                bombObj.transform.DOScale(0.8f, 0.3f).SetEase(Ease.OutBack);

                // Add slight delay between bombs appearing
                if (_bombAppearDelay > 0)
                    yield return new WaitForSeconds(_bombAppearDelay);
            }

            // Wait a moment for dramatic effect
            yield return new WaitForSeconds(0.3f);

            // Phase 2: Explode all bombs simultaneously
            GameEvents.OnSfxRequested?.Invoke(SfxType.Explosion);

            for (int i = 0; i < bombSprites.Count; i++)
            {
                if (bombSprites[i] == null) continue;

                Vector3 pos = bombSprites[i].transform.position;

                // Bomb shake before explosion
                bombSprites[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 10, 1f);

                // Spawn explosion VFX
                if (_bombExplosionVFX != null)
                {
                    InstantiateVFX(_bombExplosionVFX, pos, 2f);
                }

                // Destroy animal
                if (i < animals.Count && animals[i] != null && !animals[i].IsCollected)
                {
                    animals[i].PlayPopAndReturn(false);
                }

                // Remove bomb sprite
                Destroy(bombSprites[i], 0.2f);
            }

            Debug.Log($"[BoosterManager] Bomb activated! Destroyed {animals.Count} animals");

            // Cleanup and unfreeze
            yield return new WaitForSeconds(0.3f);
            UnfreezeAllAnimals();
            _selectedBooster = BoosterType.None;
            _waitingForTarget = false;
            OnBoosterDeselected?.Invoke();
        }

        // ──────────────────────────────────────────────────────────────────
        // RAINBOW: Sprite moves to center, rotates, animals pulled in
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
            List<Animal> matchingAnimals = new List<Animal>();

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;
                if (animal.Data == null || animal.Data.species != targetSpecies) continue;
                matchingAnimals.Add(animal);
            }

            if (matchingAnimals.Count == 0)
            {
                Debug.Log($"[BoosterManager] Rainbow: No {targetSpecies} animals found on screen");
                UnfreezeAllAnimals();
                _selectedBooster = BoosterType.None;
                _waitingForTarget = false;
                OnBoosterDeselected?.Invoke();
                return;
            }

            StartCoroutine(RainbowSequence(matchingAnimals, targetSpecies));
        }

        private IEnumerator RainbowSequence(List<Animal> animals, AnimalSpecies species)
        {
            // Get screen center
            Vector3 centerPos = Vector3.zero;
            if (Camera.main != null)
            {
                centerPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                centerPos.z = 0f;
            }

            // Create rainbow sprite at center
            GameObject rainbowObj = CreateSpriteObject(_rainbowSprite, centerPos, 0f);
            rainbowObj.transform.localScale = Vector3.zero;

            // Animate rainbow appearing and growing
            rainbowObj.transform.DOScale(2f, 0.5f).SetEase(Ease.OutBack);

            // Start rotating the rainbow sprite
            rainbowObj.transform.DORotate(new Vector3(0, 0, 360), _rainbowRotationSpeed / 360f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);

            yield return new WaitForSeconds(0.3f);

            // Pull all matching animals into the rainbow
            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;

                // Disable animal movement
                var movement = animal.GetComponent<AnimalMovement>();
                if (movement != null) movement.enabled = false;

                // Animate animal moving to center with spiral effect
                Sequence animalSeq = DOTween.Sequence();
                animalSeq.Append(animal.transform.DOMove(centerPos, _rainbowPullDuration).SetEase(Ease.InQuad));
                animalSeq.Join(animal.transform.DOScale(0.3f, _rainbowPullDuration).SetEase(Ease.InQuad));
                animalSeq.Join(animal.transform.DORotate(new Vector3(0, 0, 720), _rainbowPullDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
                
                // Spawn sparkle VFX at animal position
                if (_rainbowCollectionVFX != null)
                {
                    InstantiateVFX(_rainbowCollectionVFX, animal.transform.position, 2f);
                }
            }

            // Wait for pull animation to complete
            yield return new WaitForSeconds(_rainbowPullDuration);

            // Collect all animals
            foreach (var animal in animals)
            {
                if (animal != null && !animal.IsCollected)
                {
                    animal.OnCollected();
                }
            }

            GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);

            // Rainbow sprite shrinks and disappears
            rainbowObj.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            Destroy(rainbowObj, 0.4f);

            Debug.Log($"[BoosterManager] Rainbow activated! Collected {animals.Count} {species} animals");

            yield return new WaitForSeconds(0.3f);

            // Cleanup and unfreeze remaining animals
            UnfreezeAllAnimals();
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
        // ROCKET: Rocket travels up, destroying animals in its path
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

            StartCoroutine(RocketSequence(targetX));
        }

        private IEnumerator RocketSequence(float targetX)
        {
            if (Camera.main == null) yield break;

            // Get bottom and top positions
            Vector3 bottomPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, -0.1f, 10f));
            bottomPos.x = targetX;
            bottomPos.z = 0f;

            Vector3 topPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1.2f, 10f));
            topPos.x = targetX;
            topPos.z = 0f;

            // Create rocket sprite at bottom
            GameObject rocketObj = CreateSpriteObject(_rocketSprite, bottomPos, 1f);
            
            // Spawn trail VFX
            GameObject trailVFX = null;
            if (_rocketLaunchVFX != null)
            {
                trailVFX = InstantiateVFX(_rocketLaunchVFX, bottomPos, travelTime + 0.5f);
                if (trailVFX != null)
                    trailVFX.transform.SetParent(rocketObj.transform);
            }

            // Calculate travel time
            float distance = Vector3.Distance(bottomPos, topPos);
            float travelTime = distance / _rocketSpeed;

            // Collect animals in the lane
            var animals = new List<Animal>(ActiveAnimalRegistry.All);
            List<Animal> hitAnimals = new List<Animal>();
            float halfWidth = _rocketLaneWidth * 0.5f;

            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;
                
                float animalX = animal.transform.position.x;
                if (Mathf.Abs(animalX - targetX) <= halfWidth)
                {
                    hitAnimals.Add(animal);
                }
            }

            // Sort by Y position (bottom to top)
            hitAnimals.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

            // Animate rocket moving up
            float elapsedTime = 0f;
            int currentHitIndex = 0;

            while (elapsedTime < travelTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / travelTime;
                
                rocketObj.transform.position = Vector3.Lerp(bottomPos, topPos, t);

                // Check if we've reached any animals
                while (currentHitIndex < hitAnimals.Count)
                {
                    Animal animal = hitAnimals[currentHitIndex];
                    if (animal == null || animal.IsCollected)
                    {
                        currentHitIndex++;
                        continue;
                    }

                    // Check if rocket has reached this animal
                    if (rocketObj.transform.position.y >= animal.transform.position.y - 0.5f)
                    {
                        // Spawn explosion at animal
                        if (_bombExplosionVFX != null)
                        {
                            InstantiateVFX(_bombExplosionVFX, animal.transform.position, 1.5f);
                        }

                        // Destroy animal
                        animal.PlayPopAndReturn(false);
                        currentHitIndex++;
                    }
                    else
                    {
                        break; // Haven't reached this animal yet
                    }
                }

                yield return null;
            }

            // Clean up remaining animals in case any were missed
            foreach (var animal in hitAnimals)
            {
                if (animal != null && !animal.IsCollected)
                {
                    animal.PlayPopAndReturn(false);
                }
            }

            if (hitAnimals.Count > 0)
            {
                GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);
                Debug.Log($"[BoosterManager] Rocket activated at X={targetX:F2}! Destroyed {hitAnimals.Count} animals");
            }
            else
            {
                Debug.Log($"[BoosterManager] Rocket: No animals in lane at X={targetX:F2}");
            }

            // Destroy rocket
            Destroy(rocketObj, 0.2f);

            yield return new WaitForSeconds(0.3f);

            // Cleanup and unfreeze
            UnfreezeAllAnimals();
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

        /// <summary>
        /// Create a temporary sprite object for visual effects
        /// </summary>
        private GameObject CreateSpriteObject(Sprite sprite, Vector3 position, float scale)
        {
            if (sprite == null) return new GameObject("NullSprite");

            GameObject obj = new GameObject("BoosterSprite");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * scale;

            // Parent to VFX container for organization
            if (_vfxContainer != null)
                obj.transform.SetParent(_vfxContainer);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 100; // High sorting order to appear on top

            return obj;
        }

        /// <summary>
        /// Instantiate VFX with proper parenting and cleanup
        /// </summary>
        private GameObject InstantiateVFX(GameObject prefab, Vector3 position, float lifetime = 2f)
        {
            if (prefab == null) return null;

            GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
            
            // Parent to VFX container for organization
            if (_vfxContainer != null)
                vfx.transform.SetParent(_vfxContainer);

            // Auto-destroy after lifetime
            if (lifetime > 0)
                Destroy(vfx, lifetime);

            return vfx;
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
