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

            int count = GetBoosterCount(type);
            if (count <= 0)
            {
                Debug.LogWarning($"[BoosterManager] Cannot select {type} - count is {count}");
                return false;
            }

            if (_selectedBooster == type)
            {
                DeselectBooster();
                return false;
            }

            _selectedBooster = type;
            // Bomb and Rainbow activate from the booster button. Rocket is the only
            // booster that waits for a lane tap.
            _waitingForTarget = type == BoosterType.Rocket;

            FreezeAllAnimals();
            OnBoosterSelected?.Invoke(type);

            if (type == BoosterType.Bomb)
            {
                StartCoroutine(ActivateBombDelayed());
            }
            else if (type == BoosterType.Rainbow)
            {
                ActivateRainbow();
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
            // Rainbow activates directly from its HUD button, so a player never
            // needs a second tap on an animal. Rocket still uses its target tap.
            if (!_waitingForTarget || _selectedBooster != BoosterType.Rocket) return;
            ActivateRocket(worldPos.x);
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

                // A booster pop is still a collection for goal purposes. Use
                // the shared collection path so target progress, score, VFX,
                // and completion checks stay consistent with tapped animals.
                if (i < animals.Count && animals[i] != null && !animals[i].IsCollected)
                {
                    animals[i].OnCollected();
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
private void ActivateRainbow()
        {
            if (_rainbowCount <= 0)
            {
                DeselectBooster();
                return;
            }

            _rainbowCount--;
            OnBoosterCountChanged?.Invoke(BoosterType.Rainbow, _rainbowCount);

            var animals = new List<Animal>();
            foreach (var animal in ActiveAnimalRegistry.All)
            {
                if (animal != null && !animal.IsCollected)
                    animals.Add(animal);
            }

            if (animals.Count == 0)
            {
                Debug.Log("[BoosterManager] Rainbow: No animals on screen");
                UnfreezeAllAnimals();
                _selectedBooster = BoosterType.None;
                _waitingForTarget = false;
                OnBoosterDeselected?.Invoke();
                return;
            }

            StartCoroutine(RainbowSequence(animals));
        }

private IEnumerator RainbowSequence(List<Animal> animals)
        {
            Vector3 centerPos = Vector3.zero;
            if (Camera.main != null)
            {
                centerPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                centerPos.z = 0f;
            }

            GameObject rainbowObj = CreateSpriteObject(_rainbowSprite, centerPos, 0f);
            rainbowObj.transform.localScale = Vector3.zero;

            // A tight, fast center animation makes the booster feel immediate
            // without covering the whole playfield.
            rainbowObj.transform.DOScale(1.35f, 0.18f).SetEase(Ease.OutBack);
            rainbowObj.transform.DORotate(new Vector3(0f, 0f, 360f), 0.25f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);

            if (_rainbowCollectionVFX != null)
                InstantiateVFX(_rainbowCollectionVFX, centerPos, 0.8f);

            yield return new WaitForSeconds(0.12f);

            const float pullDuration = 0.55f;
            foreach (var animal in animals)
            {
                if (animal == null || animal.IsCollected) continue;

                var movement = animal.GetComponent<AnimalMovement>();
                if (movement != null) movement.enabled = false;

                Sequence animalSeq = DOTween.Sequence();
                animalSeq.Append(animal.transform.DOMove(centerPos, pullDuration).SetEase(Ease.InQuad));
                animalSeq.Join(animal.transform.DOScale(0.1f, pullDuration).SetEase(Ease.InQuad));
                animalSeq.Join(animal.transform.DORotate(new Vector3(0f, 0f, 1080f), pullDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));

                if (_rainbowCollectionVFX != null)
                    InstantiateVFX(_rainbowCollectionVFX, animal.transform.position, 0.8f);
            }

            yield return new WaitForSeconds(pullDuration);

            foreach (var animal in animals)
            {
                if (animal != null && !animal.IsCollected)
                    animal.OnCollected();
            }

            GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);
            rainbowObj.transform.DOKill();
            rainbowObj.transform.DOScale(0f, 0.15f).SetEase(Ease.InBack);
            Destroy(rainbowObj, 0.2f);

            Debug.Log($"[BoosterManager] Rainbow activated! Collected {animals.Count} animals");

            yield return new WaitForSeconds(0.15f);
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

            // Consume the target tap immediately so an extra tap cannot launch
            // another rocket while the zigzag sweep is running.
            _waitingForTarget = false;
            _rocketCount--;
            OnBoosterCountChanged?.Invoke(BoosterType.Rocket, _rocketCount);
            StartCoroutine(RocketSequence(targetX));
        }

private IEnumerator RocketSequence(float targetX)
        {
            if (Camera.main == null)
            {
                DeselectBooster();
                yield break;
            }

            Vector3 bottomPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, -0.12f, 10f));
            bottomPos.x = targetX;
            bottomPos.z = 0f;

            Vector3 topPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1.15f, 10f));
            topPos.z = 0f;

            GameObject rocketObj = CreateSpriteObject(_rocketSprite, bottomPos, 0.78f);

            GameObject trailVFX = null;
            if (_rocketLaunchVFX != null)
            {
                trailVFX = InstantiateVFX(_rocketLaunchVFX, bottomPos, 5f);
                if (trailVFX != null)
                    trailVFX.transform.SetParent(rocketObj.transform);
            }

            // Every active animal is a target. Sorting bottom-to-top keeps the
            // sweep readable, while alternating side waypoints creates the zigzag.
            var hitAnimals = new List<Animal>();
            foreach (var animal in ActiveAnimalRegistry.All)
            {
                if (animal != null && !animal.IsCollected)
                    hitAnimals.Add(animal);
            }
            hitAnimals.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

            float leftX = Camera.main.ViewportToWorldPoint(new Vector3(0.14f, 0.5f, 10f)).x;
            float rightX = Camera.main.ViewportToWorldPoint(new Vector3(0.86f, 0.5f, 10f)).x;
            bool steerLeft = rocketObj.transform.position.x > 0f;

            foreach (var animal in hitAnimals)
            {
                if (animal == null || animal.IsCollected) continue;

                Vector3 animalPos = animal.transform.position;
                animalPos.z = 0f;

                // Bend to alternate sides before each impact. This gives a
                // deliberate lightning-bolt path without ever skipping a target.
                float bendY = Mathf.Lerp(rocketObj.transform.position.y, animalPos.y, 0.5f);
                Vector3 bendPos = new Vector3(steerLeft ? leftX : rightX, bendY, 0f);
                steerLeft = !steerLeft;

                if (Vector3.Distance(rocketObj.transform.position, bendPos) > 0.2f)
                    yield return MoveRocketAlongSegment(rocketObj, bendPos);

                yield return MoveRocketAlongSegment(rocketObj, animalPos);

                if (_bombExplosionVFX != null)
                    InstantiateVFX(_bombExplosionVFX, animalPos, 0.8f);

                // Count booster-cleared animals toward their species goal.
                animal.OnCollected();
            }

            yield return MoveRocketAlongSegment(rocketObj, topPos);

            if (hitAnimals.Count > 0)
            {
                GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);
                Debug.Log($"[BoosterManager] Rocket zigzag sweep destroyed {hitAnimals.Count} animals.");
            }
            else
            {
                Debug.Log("[BoosterManager] Rocket: No animals on screen.");
            }

            Destroy(rocketObj, 0.1f);
            yield return new WaitForSeconds(0.15f);

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
    

private IEnumerator MoveRocketAlongSegment(GameObject rocketObj, Vector3 destination)
        {
            Vector3 start = rocketObj.transform.position;
            float distance = Vector3.Distance(start, destination);
            float duration = Mathf.Clamp(distance / Mathf.Max(_rocketSpeed, 0.01f), 0.07f, 0.22f);
            float tilt = Mathf.Clamp((destination.x - start.x) * -12f, -38f, 38f);

            Sequence motion = DOTween.Sequence();
            motion.Join(rocketObj.transform.DOMove(destination, duration).SetEase(Ease.InOutSine));
            motion.Join(rocketObj.transform.DORotate(new Vector3(0f, 0f, tilt), duration * 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));

            yield return motion.WaitForCompletion();
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
