// Animal — pool setup, tap handling, pop animation
using System.Collections;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;
using AnimalFall.Data;
using AnimalFall.Utils;
using AnimalFall.Managers;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Core.Animals
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(AnimalMovement))]
    public class Animal : MonoBehaviour
    {
        /// <summary>Fallback world scale for a portrait ortho camera (~size 5–6).</summary>
        public const float IdealScale = 0.528f;

        /// <summary>Target on-screen size (world units) for an animal's largest dimension.
        /// Every species is normalised to this so no animal looks bigger/smaller than another.</summary>
        public const float TargetWorldSize = 0.896f;

        /// <summary>Highest level number that still counts as an "easy start" level.</summary>
        public const int EarlyLevelMaxNumber = 4;

        /// <summary>Early levels (1..EarlyLevelMaxNumber) render animals larger so they are
        /// easier to see and tap, easing new players in.</summary>
        public const float EarlyLevelSizeMultiplier = 1.4f;

        /// <summary>
        /// Animals in the visual reference occupy about 14.4% of the portrait playfield width.
        /// Deriving the world size from the camera keeps that footprint stable on different phones.
        /// </summary>
        private const float TargetViewportWidth = 0.144f;

        /// <summary>Per-instance normalised scale, computed from the current sprite in SetupForPool.
        /// Movement, pop and pool-reset logic all read this instead of the flat IdealScale.</summary>
        public float CurrentScale { get; private set; } = IdealScale;

        public AnimalData Data         { get; private set; }
        public bool IsCollected        { get; private set; }
        public bool IsPaired           { get; set; }
        public Animal PairedPartner    { get; set; }

        public int   HelmetLayers      { get; set; }
        public bool  IsIceFrozen       { get; set; }
        public bool  IsBubble          { get; set; }
        public bool  IsDogHelmeted     { get; set; }
        public float GhostAlpha        { get; set; } = 1f;
        public float PairedTimer       { get; set; }
        public int   CurrentShield     { get; set; }
        public object ExclusiveOwner   { get; private set; }
        public bool HasExclusiveOwner  => ExclusiveOwner != null;

        private SpriteRenderer _sr;
        private BoxCollider2D  _col;
        private AnimalMovement _movement;
        private Rigidbody2D    _rb;
        private Coroutine      _lifetimeCoroutine;
        private bool           _isReturned;
        private float          _targetWorldSize = TargetWorldSize;

        private static readonly System.Collections.Generic.Dictionary<float, WaitForSeconds>
            _waitCache = new System.Collections.Generic.Dictionary<float, WaitForSeconds>();

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_col == null) _col = GetComponent<BoxCollider2D>();
            if (_col == null) _col = gameObject.AddComponent<BoxCollider2D>();
            _col.isTrigger = true;
            if (_movement == null) _movement = GetComponent<AnimalMovement>();
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        }

        public void SetupForPool(AnimalData data, LevelData level)
        {
            CacheComponents();
            if (_lifetimeCoroutine != null) { StopCoroutine(_lifetimeCoroutine); _lifetimeCoroutine = null; }

            _isReturned   = false;
            IsCollected   = false;
            IsPaired      = false;
            PairedPartner = null;
            HelmetLayers  = 0;
            IsIceFrozen   = false;
            IsBubble      = false;
            IsDogHelmeted = false;
            GhostAlpha    = 1f;
            PairedTimer   = 0f;
            CurrentShield = data.shieldHP;
            ExclusiveOwner = null;
            Data          = data;

            // Ensure colliders participate in Physics2D queries.
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.simulated = true;
                _rb.gravityScale = 0f;
            }

            _targetWorldSize = GetResponsiveTargetWorldSize();

            // Early levels start easy: make animals noticeably bigger so they are
            // simple to see and tap for new players.
            if (level != null && level.LevelNumber >= 1 && level.LevelNumber <= EarlyLevelMaxNumber)
                _targetWorldSize *= EarlyLevelSizeMultiplier;
            // Normal falling-animal levels use a larger 2x touch target; Mega Shooter has its own visuals.
            if (level == null || !level.IsConfiguredMegaShooter)
                _targetWorldSize *= 2f;

            Sprite displaySprite = ImageLibrary.GetAnimalSprite(data.species);

            // Frozen Pig and Bubble Monkey are level-wide rules. If their hindrance
            // is configured for the level, every matching animal (including future
            // pooled spawns) receives the special sprite and two-tap state.
            if (data.species == AnimalSpecies.Pig && LevelHasHindrance(level, HindranceType.IceCube))
            {
                Sprite frozenPig = Resources.Load<Sprite>("icons/hindrances/frozen_pig");
                if (frozenPig != null)
                {
                    displaySprite = frozenPig;
                    IsIceFrozen = true;
                }
                else
                {
                    Debug.LogWarning("[Animal] Missing frozen_pig sprite.");
                }
            }
            else if (data.species == AnimalSpecies.Monkey && LevelHasHindrance(level, HindranceType.BubbleShield))
            {
                Sprite bubbleMonkey = Resources.Load<Sprite>("icons/hindrances/bubble_monkey");
                if (bubbleMonkey != null)
                {
                    displaySprite = bubbleMonkey;
                    IsBubble = true;
                }
                else
                {
                    Debug.LogWarning("[Animal] Missing bubble_monkey sprite.");
                }
            }

            if (data.species == AnimalSpecies.Dog && LevelHasHindrance(level, HindranceType.DogHelmet))
            {
                Sprite dogHelmet = Resources.Load<Sprite>("icons/hindrances/dog_helmet");
                if (dogHelmet != null)
                {
                    displaySprite = dogHelmet;
                    IsDogHelmeted = true;
                }
                else
                {
                    Debug.LogWarning("[Animal] Missing dog_helmet sprite.");
                }
            }

            _sr.sprite = displaySprite;
            if (_sr.sprite == null)
                Debug.LogWarning($"[Animal] Null sprite for species {data.species}.");
            _sr.color = Color.white;
            _sr.sortingOrder = 5;

            // Normalize the selected display sprite so special variants keep the
            // same on-screen footprint as normal animals.
            CurrentScale = ComputeNormalisedScale(_sr.sprite, _targetWorldSize);

            if (_col != null && _sr.sprite != null)
            {
                _col.size = _sr.sprite.bounds.size * 0.85f;
                _col.offset = _sr.sprite.bounds.center;
                _col.isTrigger = true;
            }

            transform.localScale = Vector3.one * CurrentScale;
            transform.rotation = Quaternion.identity;

            _movement.Configure(data, level);
            ActiveAnimalRegistry.Register(this);
            _lifetimeCoroutine = StartCoroutine(LifetimeCoroutine(data.lifetime));
        }

        public void SetDisplaySprite(Sprite sprite)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite != null
                ? sprite
                : ImageLibrary.GetAnimalSprite(Data != null ? Data.species : AnimalSpecies.None);
            CurrentScale = ComputeNormalisedScale(_sr.sprite, _targetWorldSize);
            transform.localScale = Vector3.one * CurrentScale;
            FitColliderToDisplaySprite();
        }

        public void RestoreDisplaySprite()
        {
            SetDisplaySprite(ImageLibrary.GetAnimalSprite(Data != null ? Data.species : AnimalSpecies.None));
        }

        private void FitColliderToDisplaySprite()
        {
            if (_col == null) _col = GetComponent<BoxCollider2D>();
            if (_col == null || _sr == null || _sr.sprite == null) return;
            _col.size = _sr.sprite.bounds.size * 0.85f;
            _col.offset = _sr.sprite.bounds.center;
            _col.isTrigger = true;
        }


        /// <summary>Returns a uniform scale so the sprite's largest world dimension equals TargetWorldSize.</summary>
        private static float ComputeNormalisedScale(Sprite sprite, float targetWorldSize)
        {
            if (sprite == null) return IdealScale;
            Vector2 size = sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            if (largest <= 0.0001f) return IdealScale;
            return targetWorldSize / largest;
        }

        private static float GetResponsiveTargetWorldSize()
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic || camera.aspect <= 0f)
                return TargetWorldSize;

            float viewportWorldWidth = camera.orthographicSize * 2f * camera.aspect;
            return viewportWorldWidth * TargetViewportWidth;
        }

        public bool TryClaimExclusive(object owner)
        {
            if (owner == null || (ExclusiveOwner != null && !ReferenceEquals(ExclusiveOwner, owner))) return false;
            ExclusiveOwner = owner;
            return true;
        }

        public void ReleaseExclusive(object owner)
        {
            if (ReferenceEquals(ExclusiveOwner, owner)) ExclusiveOwner = null;
        }

        public TapResult HandleTap()
        {
            if (_isReturned || IsCollected) return TapResult.Wrong;

            if (ExclusiveOwner is IAnimalTapGate gate && !gate.CanCollect(this))
            {
                gate.OnBlockedTap(this);
                return TapResult.HindranceBlocked;
            }

            if (IsDogHelmeted)
            {
                IsDogHelmeted = false;
                RestoreDisplaySprite();
                GameEvents.OnSfxRequested?.Invoke(SfxType.ShieldHit);
                return TapResult.ShieldBroken;
            }

            if (IsIceFrozen)
            {
                IsIceFrozen = false;
                RestoreDisplaySprite();
                GameEvents.OnSfxRequested?.Invoke(SfxType.ShieldHit);
                GameEvents.OnIceBroken?.Invoke(this);
                return TapResult.IceCubeFrozen;
            }

            if (Data.type == AnimalType.Bomb)
            {
                GameEvents.OnBombTapped?.Invoke(transform.position);
                PlayPopAndReturn(false);
                return TapResult.BombExploded;
            }

            if (Data.type == AnimalType.FakeAnimal)
            {
                GameEvents.OnWrongTap?.Invoke();
                PlayPopAndReturn(false);
                return TapResult.FakeCollected;
            }

            if (Data.type == AnimalType.CursedSkull)
            {
                GameEvents.OnCursedSkullTapped?.Invoke();
                PlayPopAndReturn(false);
                return TapResult.CursedSkullDestroyed;
            }

            if (HelmetLayers > 0)
            {
                HelmetLayers--;
                DOTween.Kill(gameObject);
                transform.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.2f, 5, 0.5f);
                if (HelmetLayers > 0) return TapResult.ShieldBroken;
            }

            if (Data.type == AnimalType.Shielded && CurrentShield > 0)
            {
                CurrentShield--;
                if (CurrentShield > 0)
                {
                    FlashYellow();
                    return TapResult.ShieldBroken;
                }
            }

            if (IsBubble)
            {
                IsBubble = false;
                RestoreDisplaySprite();
                _movement?.ResumeFall();
                GameEvents.OnBubblePopped?.Invoke(this);
                return TapResult.BubblePopped;
            }

            if (IsPaired && PairedPartner != null && !PairedPartner.IsCollected)
            {
                GameEvents.OnPairedAnimalTapped?.Invoke(this);
                return TapResult.PairedWaiting;
            }

            if (Data.type == AnimalType.Rainbow)
            {
                OnCollected();
                return TapResult.Rainbow;
            }

            if (Data.type == AnimalType.Golden)
            {
                OnCollected();
                return TapResult.Golden;
            }

            if (Data.type == AnimalType.Ghost && GhostAlpha < 0.15f)
            {
                ReturnToPool();
                return TapResult.GhostMissed;
            }

            OnCollected();
            return TapResult.Correct;
        }

        public void OnCollected()
        {
            if (_isReturned || IsCollected) return;
            IsCollected = true;

            if (_lifetimeCoroutine != null) { StopCoroutine(_lifetimeCoroutine); _lifetimeCoroutine = null; }

            var species = Data != null ? Data.species : AnimalSpecies.None;
            var type    = Data != null ? Data.type    : AnimalType.Normal;
            var pos     = transform.position;

            // Notify before animation so goals/score update immediately
            GameEvents.OnAnimalCollected?.Invoke(species, type, pos);

            if (GameManager.Instance != null && Data != null)
                GameManager.Instance.OnCorrectTap(Data.pointValue);

            PlayPopAndReturn(true);
        }

        public void PlayPopAndReturn(bool success)
        {
            if (_movement != null) _movement.enabled = false;
            DOTween.Kill(gameObject);

            // Hide sprite quickly while VFX plays; squash-stretch pop
            var seq = DOTween.Sequence().SetId(gameObject);
            seq.Append(transform.DOScale(CurrentScale * new Vector3(1.35f, 0.65f, 1f), 0.06f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(CurrentScale * 1.15f, 0.08f).SetEase(Ease.OutBack));
            if (_sr != null) seq.Join(_sr.DOFade(0f, 0.12f));
            seq.OnComplete(ReturnToPool);
        }

        private IEnumerator LifetimeCoroutine(float lifetime)
        {
            if (!_waitCache.TryGetValue(lifetime, out var wait))
            {
                wait = new WaitForSeconds(lifetime);
                _waitCache[lifetime] = wait;
            }
            yield return wait;
            if (!IsCollected) ReturnToPool();
        }

        /// <summary>Public entry for movement / external systems that despawn this animal.</summary>
        public void Despawn() => ReturnToPool();

        private void ReturnToPool()
        {
            if (_isReturned) return;
            _isReturned = true;
            ActiveAnimalRegistry.Unregister(this);
            ExclusiveOwner = null;
            IsCollected = true;
            if (_lifetimeCoroutine != null) { StopCoroutine(_lifetimeCoroutine); _lifetimeCoroutine = null; }
            DOTween.Kill(gameObject);
            if (_movement != null) _movement.enabled = false;
            if (_sr != null) _sr.color = Color.white;
            transform.localScale = Vector3.one * CurrentScale;

            // Free a slot so the continuous spawn wave can keep going
            Spawner.Instance?.OnAnimalReturned();
            ObjectPooler.Instance?.ReturnToPool(gameObject);
        }

        private void FlashYellow()
        {
            DOTween.Kill(gameObject);
            var seq = DOTween.Sequence().SetId(gameObject);
            seq.Append(_sr.DOColor(Color.yellow, 0.08f));
            seq.Append(_sr.DOColor(Color.white, 0.08f));
            seq.SetLoops(2, LoopType.Yoyo);
        }


        private static bool LevelHasHindrance(LevelData level, HindranceType type)
        {
            HindranceConfig[] configs = level != null ? level.Hindrances : null;
            if (configs == null) return false;

            for (int i = 0; i < configs.Length; i++)
            {
                if (configs[i] != null && configs[i].type == type)
                    return true;
            }

            return false;
        }
    }
}
