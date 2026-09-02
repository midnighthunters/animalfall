using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Effects;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>
    /// A closed switch becomes open when tapped.  While open, normal gravity is
    /// reversed, so all active and newly spawned animals travel upward.
    /// </summary>
    public sealed class GravitySwitchHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private Sprite _closedSprite;
        [SerializeField] private Sprite _openSprite;
        [SerializeField, Min(1f)] private float _reverseGravityDuration = 6.5f;
        [SerializeField, Min(0.1f)] private float _upwardImpulse = 3.8f;
        [SerializeField, Min(0.1f)] private float _worldSize = 0.92f;

        private BoxCollider2D _collider;
        private HindranceEffectToken _reverseGravityToken;
        private bool _opened;

        public override HindranceType Type => HindranceType.GravitySwitch;
        public int InteractionPriority => 355;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite closedSprite, Sprite openSprite)
        {
            _closedSprite = closedSprite;
            _openSprite = openSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            EnsureCollider();
            _opened = false;
            _sr.sprite = _closedSprite;
            _sr.enabled = true;
            transform.localScale = GetScaleFor(_sr.sprite, _worldSize);
            ConfigureColliderForCurrentSprite();
            _collider.enabled = true;
            PlaceInView();
        }

        protected override void OnDeactivate()
        {
            _reverseGravityToken?.Dispose();
            _reverseGravityToken = null;
            if (_collider != null) _collider.enabled = false;
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive || _opened) return false;
            _opened = true;
            StartCoroutine(OpenSwitch());
            return true;
        }

        private IEnumerator OpenSwitch()
        {
            _sr.sprite = _openSprite != null ? _openSprite : _closedSprite;
            transform.localScale = GetScaleFor(_sr.sprite, _worldSize);
            _collider.enabled = false;
            EnvironmentEffects effects = _ctx.EnvironmentEffects != null
                ? _ctx.EnvironmentEffects
                : EnvironmentEffects.Instance;
            _reverseGravityToken = effects?.AddReverseGravity(this);
            LaunchCurrentAnimalsUpward();
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);

            yield return new WaitForSeconds(_reverseGravityDuration);
            if (_isActive) Deactivate();
        }

        private void LaunchCurrentAnimalsUpward()
        {
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                if (movement != null) movement.AddImpulse(Vector2.up * _upwardImpulse);
            }
        }

        private void EnsureCollider()
        {
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
        }

        private void ConfigureColliderForCurrentSprite()
        {
            if (_collider == null) return;
            if (_sr == null || _sr.sprite == null)
            {
                _collider.offset = Vector2.zero;
                _collider.size = Vector2.one * 1.2f;
                return;
            }

            // Collider dimensions are local-space values. Match the visual sprite
            // before the transform scale is applied, with a small tap-friendly pad.
            _collider.offset = _sr.sprite.bounds.center;
            _collider.size = _sr.sprite.bounds.size * 1.2f;
        }

        private void PlaceInView()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
            Vector3 position = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.74f, depth));
            position.z = transform.position.z;
            transform.position = position;
        }

        private static Vector3 GetScaleFor(Sprite sprite, float targetSize)
        {
            if (sprite == null) return Vector3.one;
            Vector2 size = sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            return largest > 0.0001f ? Vector3.one * (targetSize / largest) : Vector3.one;
        }
    }
}
