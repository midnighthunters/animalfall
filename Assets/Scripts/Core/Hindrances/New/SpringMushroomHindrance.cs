using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.New
{
    /// <summary>
    /// A stationary spring bumper. Animals that touch it are launched back upward
    /// and out of the playfield without being counted as rescued or missed.
    /// </summary>
    public sealed class SpringMushroomHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private Sprite _pressedSprite;
        [SerializeField] private Sprite _openedSprite;
        [SerializeField, Range(0.1f, 0.9f)] private float _viewportHeight = 0.36f;
        [SerializeField, Min(0.05f)] private float _displayScale = 0.23f;
        [SerializeField, Min(1f)] private float _visibleLifetime = 9f;
        [SerializeField, Min(0.05f)] private float _compressionDuration = 0.1f;
        [SerializeField, Min(0.05f)] private float _reboundDuration = 0.18f;

        private bool _animating;

        public override HindranceType Type => HindranceType.SpringMushroomBumpers;
        public int InteractionPriority => 170;
        public int AnimalsBounced { get; private set; }

#if UNITY_EDITOR
        public void EditorConfigure(Sprite pressedSprite, Sprite openedSprite)
        {
            _pressedSprite = pressedSprite;
            _openedSprite = openedSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            PositionInsidePlayfield();
            transform.localScale = Vector3.one * _displayScale;
            AnimalsBounced = 0;
            _animating = false;
            if (_sr != null)
            {
                _sr.sprite = _openedSprite != null ? _openedSprite : _pressedSprite;
                _sr.sortingOrder = 28;
                _sr.enabled = true;
            }

            StartCoroutine(Lifetime());
        }

        protected override void OnDeactivate()
        {
            _animating = false;
            if (_sr != null) _sr.sprite = _openedSprite != null ? _openedSprite : _pressedSprite;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;
            Animal animal = other.GetComponent<Animal>() ?? other.GetComponentInParent<Animal>();
            if (animal == null || animal.IsCollected || !animal.gameObject.activeInHierarchy) return;

            AnimalMovement movement = animal.GetComponent<AnimalMovement>();
            if (movement == null) return;

            Vector2 away = (Vector2)(animal.transform.position - transform.position);
            float horizontal = Mathf.Abs(away.x) < 0.08f
                ? (Random.value < 0.5f ? -0.55f : 0.55f)
                : Mathf.Sign(away.x) * 0.55f;
            movement.LaunchOutOfScreen(new Vector2(horizontal, 1f));
            AnimalsBounced++;
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);

            if (!_animating) StartCoroutine(CompressAndRelease());
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive) return false;
            if (!_animating) StartCoroutine(CompressAndRelease());
            return true;
        }

        private IEnumerator CompressAndRelease()
        {
            _animating = true;
            if (_sr != null && _pressedSprite != null) _sr.sprite = _pressedSprite;
            transform.localScale = new Vector3(_displayScale * 1.08f, _displayScale * 0.82f, 1f);
            yield return new WaitForSeconds(_compressionDuration);

            if (_sr != null && _openedSprite != null) _sr.sprite = _openedSprite;
            transform.localScale = new Vector3(_displayScale * 0.88f, _displayScale * 1.14f, 1f);
            yield return new WaitForSeconds(_reboundDuration);

            transform.localScale = Vector3.one * _displayScale;
            _animating = false;
        }

        private IEnumerator Lifetime()
        {
            yield return new WaitForSeconds(Mathf.Max(1f, _visibleLifetime));
            Deactivate();
        }

        private void PositionInsidePlayfield()
        {
            if (Camera.main == null) return;
            float z = Mathf.Abs(Camera.main.transform.position.z);
            float x = Mathf.Clamp(Camera.main.WorldToViewportPoint(transform.position).x, 0.16f, 0.84f);
            transform.position = Camera.main.ViewportToWorldPoint(new Vector3(x, _viewportHeight, z));
            transform.rotation = Quaternion.identity;
        }
    }
}