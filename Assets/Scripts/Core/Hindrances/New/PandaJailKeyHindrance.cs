using System.Collections;
using DG.Tweening;
using UnityEngine;
using AnimalFall.Utils;

namespace AnimalFall.Core.Hindrances.New
{
    /// <summary>
    /// The jail and key are separate world objects. The key follows the jail's
    /// fall cadence, and tapping it changes the jail image into the freed panda.
    /// </summary>
    public sealed class PandaJailKeyHindrance : HindranceBase
    {
        [SerializeField] private Sprite _pandaJailSprite;
        [SerializeField] private Sprite _pandaSprite;
        [SerializeField, Min(0.1f)] private float _fallSpeed = 1.1f;
        [SerializeField, Min(0.1f)] private float _keyHorizontalOffset = 1.15f;
        [SerializeField, Min(0.1f)] private float _jailWorldSize = 0.9f;

        private Transform _keyTransform;
        private Collider2D _keyCollider;
        private float _screenBottom;
        private bool _unlocked;

        private static Sprite _keySprite;

        public override HindranceType Type => HindranceType.PandaJailKey;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite pandaJailSprite, Sprite pandaSprite, float fallSpeed)
        {
            _pandaJailSprite = pandaJailSprite;
            _pandaSprite = pandaSprite;
            _fallSpeed = Mathf.Max(0.1f, fallSpeed);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            EnsureKey();
            _unlocked = false;
            _sr.sprite = _pandaJailSprite;
            _sr.enabled = true;
            transform.localScale = GetScaleFor(_sr.sprite, _jailWorldSize);

            // The key is an independent sibling, with its own collider and tap target.
            _keyTransform.SetParent(transform.parent, true);
            _keyTransform.position = transform.position + Vector3.right * _keyHorizontalOffset;
            _keyTransform.localScale = Vector3.one * 0.52f;
            _keyTransform.gameObject.SetActive(true);
            _keyCollider.enabled = true;

            if (Camera.main != null)
                _screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f,
                    Mathf.Abs(Camera.main.transform.position.z - transform.position.z))).y;
        }

        protected override void OnDeactivate()
        {
            DOTween.Kill(gameObject);
            if (_keyTransform == null) return;
            DOTween.Kill(_keyTransform.gameObject);
            _keyCollider.enabled = false;
            _keyTransform.gameObject.SetActive(false);
            _keyTransform.SetParent(transform, false);
        }

        private void Update()
        {
            if (!_isActive || _unlocked) return;
            Vector3 fall = Vector3.down * (_fallSpeed * Time.deltaTime);
            transform.position += fall;
            if (_keyTransform != null && _keyTransform.gameObject.activeSelf)
                _keyTransform.position += fall;
            if (transform.position.y < _screenBottom - 1.2f) Deactivate();
        }

        public bool TryUnlock()
        {
            if (!_isActive || _unlocked) return false;
            _unlocked = true;
            StartCoroutine(UnlockJail());
            return true;
        }

        private IEnumerator UnlockJail()
        {
            _keyCollider.enabled = false;
            _keyTransform.DOPunchScale(Vector3.one * 0.18f, 0.18f, 5, 0.6f).SetId(_keyTransform.gameObject);
            yield return new WaitForSeconds(0.16f);

            // The jail renderer itself becomes the Panda sprite.
            _sr.sprite = _pandaSprite != null ? _pandaSprite : ImageLibrary.GetAnimalSprite(Core.Animals.AnimalSpecies.Panda);
            transform.localScale = GetScaleFor(_sr.sprite, _jailWorldSize * 0.8f);
            transform.DOPunchScale(Vector3.one * 0.16f, 0.25f, 4, 0.6f).SetId(gameObject);
            _keyTransform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).SetId(_keyTransform.gameObject);
            yield return new WaitForSeconds(0.55f);
            Deactivate();
        }

        private void EnsureKey()
        {
            if (_keyTransform != null) return;
            var key = new GameObject("PandaJailKey_SeparateKey");
            _keyTransform = key.transform;
            _keyTransform.SetParent(transform, false);
            var renderer = key.AddComponent<SpriteRenderer>();
            renderer.sprite = GetKeySprite();
            renderer.color = new Color(1f, 0.77f, 0.12f);
            renderer.sortingOrder = 34;
            _keyCollider = key.AddComponent<CircleCollider2D>();
            ((CircleCollider2D)_keyCollider).radius = 0.48f;
            _keyCollider.isTrigger = true;
            key.AddComponent<PandaJailKeyTapTarget>().Bind(this);
        }

        private static Vector3 GetScaleFor(Sprite sprite, float targetSize)
        {
            if (sprite == null) return Vector3.one;
            Vector2 size = sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            return largest > 0.0001f ? Vector3.one * (targetSize / largest) : Vector3.one;
        }

        private static Sprite GetKeySprite()
        {
            if (_keySprite != null) return _keySprite;
            const int width = 48;
            const int height = 24;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[width * height]);
            for (int y = 8; y <= 14; y++) for (int x = 15; x <= 42; x++) texture.SetPixel(x, y, Color.white);
            for (int y = 5; y <= 18; y++) for (int x = 34; x <= 38; x++) texture.SetPixel(x, y, Color.white);
            for (int y = 5; y <= 10; y++) for (int x = 39; x <= 44; x++) texture.SetPixel(x, y, Color.white);
            for (int y = 0; y < height; y++) for (int x = 0; x < 16; x++)
            {
                float dx = x - 8f;
                float dy = y - 11.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance < 8f && distance > 3.8f) texture.SetPixel(x, y, Color.white);
            }
            texture.Apply();
            _keySprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f, 40f);
            return _keySprite;
        }
    }

    public sealed class PandaJailKeyTapTarget : MonoBehaviour, IPointerTapTarget
    {
        private PandaJailKeyHindrance _owner;
        public int InteractionPriority => 360;
        public void Bind(PandaJailKeyHindrance owner) => _owner = owner;
        public bool TryHandleTap(WorldPointerEvent pointerEvent) => _owner != null && _owner.TryUnlock();
    }
}
