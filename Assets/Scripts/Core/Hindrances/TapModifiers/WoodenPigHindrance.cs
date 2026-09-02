using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    /// <summary>A convincing falling decoy. Taps are consumed and intentionally do nothing.</summary>
    public sealed class WoodenPigHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private Sprite _woodenPigSprite;
        [SerializeField, Min(0.1f)] private float _fallSpeed = 1.1f;
        [SerializeField, Min(1f)] private float _visibleLifetime = 8f;
        private Collider2D _collider;
        private float _phase;

        public override HindranceType Type => HindranceType.WoodenPig;
        public int InteractionPriority => 245;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite sprite) { _woodenPigSprite = sprite; UnityEditor.EditorUtility.SetDirty(this); }
#endif

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<Collider2D>();
        }

        protected override void OnActivate()
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
            if (_sr != null)
            {
                _sr.sprite = _woodenPigSprite != null ? _woodenPigSprite : FirstSprite("icons/hindrances/wooden_pig");
                _sr.sortingOrder = 6;
                _sr.enabled = true;
            }
            FitVisualAndCollider(Animal.TargetWorldSize * 1.05f);
            if (_collider != null) _collider.enabled = true;
            StartCoroutine(RetireAfter(_visibleLifetime));
        }

        private void Update()
        {
            if (!_isActive) return;
            Vector3 position = transform.position;
            position.y -= _fallSpeed * Time.deltaTime;
            position.x += Mathf.Sin(Time.time * 2.1f + _phase) * 0.12f * Time.deltaTime;
            transform.position = position;
            if (Camera.main != null && Camera.main.WorldToViewportPoint(position).y < -0.08f) Deactivate();
        }

        // This decoy deliberately consumes the tap without feedback, penalty, collection, or score.
        public bool TryHandleTap(WorldPointerEvent pointerEvent) => _isActive;

        protected override void OnDeactivate() { }

        private IEnumerator RetireAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_isActive) Deactivate();
        }

        private void FitVisualAndCollider(float worldSize)
        {
            if (_sr == null || _sr.sprite == null) return;
            Vector2 size = _sr.sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            if (largest > 0.001f) transform.localScale = Vector3.one * (worldSize / largest);
            if (_collider is CircleCollider2D circle)
            {
                circle.isTrigger = true;
                circle.radius = largest * 0.5f;
                circle.offset = _sr.sprite.bounds.center;
            }
        }

        private static Sprite FirstSprite(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }
    }
}
