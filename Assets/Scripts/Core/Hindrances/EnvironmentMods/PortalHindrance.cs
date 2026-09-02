using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>Two linked portals teleport animals from either entrance to the other.</summary>
    public sealed class PortalHindrance : HindranceBase
    {
        [SerializeField] private Sprite _bluePortalSprite;
        [SerializeField] private Sprite _orangePortalSprite;
        [SerializeField, Min(0.1f)] private float _portalRadius = 0.62f;
        [SerializeField, Min(1f)] private float _duration = 9f;
        [SerializeField, Min(0.1f)] private float _reentryCooldown = 0.8f;

        private SpriteRenderer _blueRenderer;
        private SpriteRenderer _orangeRenderer;
        private readonly Dictionary<Animal, float> _cooldownUntil = new Dictionary<Animal, float>();
        private Vector3 _bluePosition;
        private Vector3 _orangePosition;

        public override HindranceType Type => HindranceType.Portal;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite blue, Sprite orange)
        {
            _bluePortalSprite = blue;
            _orangePortalSprite = orange;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            EnsureVisuals();
            PositionPortals();
            _cooldownUntil.Clear();
            _blueRenderer.enabled = _orangeRenderer.enabled = true;
            StartCoroutine(RetireAfter(_duration));
        }

        private void Update()
        {
            if (!_isActive) return;
            if (_blueRenderer != null) _blueRenderer.transform.Rotate(0f, 0f, 42f * Time.deltaTime);
            if (_orangeRenderer != null) _orangeRenderer.transform.Rotate(0f, 0f, -42f * Time.deltaTime);

            var animals = ActiveAnimalRegistry.All;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                if (_cooldownUntil.TryGetValue(animal, out float until) && Time.time < until) continue;

                Vector3 destination;
                if (Vector2.Distance(animal.transform.position, _bluePosition) <= _portalRadius)
                    destination = _orangePosition;
                else if (Vector2.Distance(animal.transform.position, _orangePosition) <= _portalRadius)
                    destination = _bluePosition;
                else
                    continue;

                animal.transform.position = destination + Vector3.down * (_portalRadius + 0.16f);
                animal.GetComponent<AnimalMovement>()?.AddImpulse(Vector2.down * 0.65f);
                _cooldownUntil[animal] = Time.time + _reentryCooldown;
                GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);
            }
        }

        protected override void OnDeactivate()
        {
            _cooldownUntil.Clear();
            if (_blueRenderer != null) _blueRenderer.enabled = false;
            if (_orangeRenderer != null) _orangeRenderer.enabled = false;
        }

        private void EnsureVisuals()
        {
            if (_bluePortalSprite == null || _orangePortalSprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>("icons/hindrances/portal");
                System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
                if (_bluePortalSprite == null && sprites.Length > 0) _bluePortalSprite = sprites[0];
                if (_orangePortalSprite == null && sprites.Length > 1) _orangePortalSprite = sprites[sprites.Length - 1];
            }
            _blueRenderer = EnsureRenderer("Blue Portal", _bluePortalSprite, 28);
            _orangeRenderer = EnsureRenderer("Orange Portal", _orangePortalSprite, 28);
        }

        private SpriteRenderer EnsureRenderer(string name, Sprite sprite, int order)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(transform, false);
            }
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            float largest = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            renderer.transform.localScale = Vector3.one * (1.35f / Mathf.Max(0.001f, largest));
            return renderer;
        }

        private void PositionPortals()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                _bluePosition = new Vector3(-2f, 2f, 0f);
                _orangePosition = new Vector3(2f, -1f, 0f);
            }
            else
            {
                float depth = Mathf.Abs(camera.transform.position.z);
                _bluePosition = camera.ViewportToWorldPoint(new Vector3(0.28f, 0.68f, depth));
                _orangePosition = camera.ViewportToWorldPoint(new Vector3(0.72f, 0.36f, depth));
                _bluePosition.z = _orangePosition.z = 0f;
            }
            transform.position = Vector3.zero;
            _blueRenderer.transform.position = _bluePosition;
            _orangeRenderer.transform.position = _orangePosition;
        }

        private IEnumerator RetireAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_isActive) Deactivate();
        }
    }
}
