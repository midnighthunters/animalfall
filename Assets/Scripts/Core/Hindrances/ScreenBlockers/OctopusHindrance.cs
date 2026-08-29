using System.Collections;
using AnimalFall.Core.Animals;
using AnimalFall.Effects;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    /// <summary>Falls into view; tap the octopus to spread black ink across the screen.</summary>
    public sealed class OctopusHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private float _fallSpeed = 2f;
        [SerializeField] private float _inkDuration = 5f;

        private Collider2D _collider;
        private bool _splashed;
        private float _screenBottom;

        public override HindranceType Type => HindranceType.Octopus;
        public int InteractionPriority => 230;

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<Collider2D>();
        }

        protected override void OnActivate()
        {
            _splashed = false;
            NormalizeToAnimalSize();
            FitColliderToSprite();
            if (_collider != null) _collider.enabled = true;

            // The factory spawns hindrances just above the top edge (viewport y = 1.05),
            // so the octopus must fall into view to be seen and tapped.
            _screenBottom = Camera.main != null
                ? Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, Mathf.Abs(Camera.main.transform.position.z))).y
                : -6f;
        }

        private void Update()
        {
            if (!_isActive || _splashed) return;

            transform.Translate(0f, -_fallSpeed * Time.deltaTime, 0f);

            // Missed — let it leave the screen and recycle so it doesn't linger off-screen.
            if (transform.position.y < _screenBottom - 1f)
                Deactivate();
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive || _splashed) return false;

            _splashed = true;
            if (_sr != null) _sr.enabled = false;
            if (_collider != null) _collider.enabled = false;

            // Spread black ink from the octopus. Self-contained VFX so it always plays,
            // independent of scene wiring; also darken the screen via ScreenEffects when
            // that reference is available.
            InkSplashVFX.Play(transform.position, _inkDuration);

            StartCoroutine(FinishAfterInk());
            return true;
        }

        protected override void OnDeactivate()
        {
            _splashed = false;
            if (_collider != null) _collider.enabled = true;
        }

        private IEnumerator FinishAfterInk()
        {
            yield return new WaitForSeconds(_inkDuration);
            Deactivate();
        }

        private void NormalizeToAnimalSize()
        {
            if (_sr == null || _sr.sprite == null) return;
            Vector2 size = _sr.sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            if (largest > 0.001f)
                transform.localScale = Vector3.one * (Animal.TargetWorldSize / largest);
        }

        /// <summary>
        /// The collider radius is authored in local (pre-scale) space, so after
        /// <see cref="NormalizeToAnimalSize"/> shrinks the transform the default radius
        /// only covers the octopus's centre. Size it to the sprite bounds so the whole
        /// visible octopus is tappable.
        /// </summary>
        private void FitColliderToSprite()
        {
            if (_sr == null || _sr.sprite == null) return;
            if (_collider is CircleCollider2D circle)
            {
                Vector2 size = _sr.sprite.bounds.size;
                circle.radius = Mathf.Max(size.x, size.y) * 0.5f;
                circle.offset = Vector2.zero;
            }
        }
    }
}
