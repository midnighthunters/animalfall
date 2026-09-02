using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>
    /// Two mirrored crushers close in from either side of the playfield.  Each
    /// closing impact removes animals that were caught between the two jaws.
    /// </summary>
    public sealed class CrusherHindrance : HindranceBase
    {
        [SerializeField] private Sprite _crusherSprite;
        [SerializeField, Min(0.1f)] private float _pauseBetweenCrushes = 1.4f;
        [SerializeField, Min(0.1f)] private float _closingDuration = 0.48f;
        [SerializeField, Min(0.1f)] private float _openingDuration = 0.62f;
        [SerializeField, Min(1f)] private float _activeDuration = 8f;
        [SerializeField, Min(0.1f)] private float _crusherWorldSize = 1.75f;

        private SpriteRenderer _leftCrusher;
        private SpriteRenderer _rightCrusher;
        private Vector3 _outerLeft;
        private Vector3 _outerRight;
        private Vector3 _innerLeft;
        private Vector3 _innerRight;

        public override HindranceType Type => HindranceType.Crusher;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite crusherSprite)
        {
            _crusherSprite = crusherSprite;
            _crusherWorldSize = 1.75f;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            EnsureCrushers();
            PlaceAtPlayfieldEdges();
            StartCoroutine(CrusherRoutine());
        }

        protected override void OnDeactivate()
        {
            if (_leftCrusher != null) _leftCrusher.enabled = false;
            if (_rightCrusher != null) _rightCrusher.enabled = false;
        }

        private IEnumerator CrusherRoutine()
        {
            float finishAt = Time.time + _activeDuration;
            while (_isActive && Time.time < finishAt)
            {
                yield return new WaitForSeconds(_pauseBetweenCrushes);
                if (!_isActive || Time.time >= finishAt) break;

                yield return MoveCrushers(_outerLeft, _outerRight, _innerLeft, _innerRight, _closingDuration);
                if (!_isActive) yield break;

                EliminateAnimalsCaughtBetweenJaws();
                GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);

                yield return MoveCrushers(_innerLeft, _innerRight, _outerLeft, _outerRight, _openingDuration);
            }

            if (_isActive) Deactivate();
        }

        private IEnumerator MoveCrushers(Vector3 fromLeft, Vector3 fromRight, Vector3 toLeft, Vector3 toRight, float duration)
        {
            float elapsed = 0f;
            while (_isActive && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _leftCrusher.transform.position = Vector3.LerpUnclamped(fromLeft, toLeft, t);
                _rightCrusher.transform.position = Vector3.LerpUnclamped(fromRight, toRight, t);
                yield return null;
            }

            if (_leftCrusher != null) _leftCrusher.transform.position = toLeft;
            if (_rightCrusher != null) _rightCrusher.transform.position = toRight;
        }

        private void EliminateAnimalsCaughtBetweenJaws()
        {
            if (_leftCrusher == null || _rightCrusher == null) return;

            // The crusher only has force where both jaws overlap at the end
            // of their inward movement. Checking this shared rectangle keeps
            // animals above, below, or outside the closing faces safe.
            Bounds leftBounds = _leftCrusher.bounds;
            Bounds rightBounds = _rightCrusher.bounds;
            float minX = Mathf.Max(leftBounds.min.x, rightBounds.min.x);
            float maxX = Mathf.Min(leftBounds.max.x, rightBounds.max.x);
            float minY = Mathf.Max(leftBounds.min.y, rightBounds.min.y);
            float maxY = Mathf.Min(leftBounds.max.y, rightBounds.max.y);
            if (minX >= maxX || minY >= maxY) return;

            var animals = ActiveAnimalRegistry.All;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                Vector3 position = animal.transform.position;
                if (position.x >= minX && position.x <= maxX &&
                    position.y >= minY && position.y <= maxY)
                    animal.Despawn();
            }
        }

        private void EnsureCrushers()
        {
            Sprite source = _crusherSprite != null ? _crusherSprite : Resources.Load<Sprite>("icons/hindrances/crusher");
            // The supplied artwork is a complete hydraulic crusher. Keep the
            // whole image visible on each side and mirror the right copy so
            // both red crushing faces point into the playfield.
            _leftCrusher = EnsureCrusher("Left Crusher", source, false);
            _rightCrusher = EnsureCrusher("Right Crusher", source, true);
            _leftCrusher.enabled = _rightCrusher.enabled = true;
        }

        private SpriteRenderer EnsureCrusher(string name, Sprite sprite, bool flipX)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(transform, false);
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = flipX;
            renderer.sortingOrder = 33;
            float largest = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            child.localScale = Vector3.one * (_crusherWorldSize / Mathf.Max(0.001f, largest));
            return renderer;
        }

        private void PlaceAtPlayfieldEdges()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                _outerLeft = new Vector3(-4f, 0f, 0f);
                _outerRight = new Vector3(4f, 0f, 0f);
            }
            else
            {
                float depth = Mathf.Abs(camera.transform.position.z);
                float y = camera.ViewportToWorldPoint(new Vector3(0f, Random.Range(0.36f, 0.70f), depth)).y;
                _outerLeft = camera.ViewportToWorldPoint(new Vector3(0.04f, 0f, depth));
                _outerRight = camera.ViewportToWorldPoint(new Vector3(0.96f, 0f, depth));
                _outerLeft.y = _outerRight.y = y;
                _outerLeft.z = _outerRight.z = 0f;
            }

            float span = _outerRight.x - _outerLeft.x;
            _innerLeft = new Vector3(_outerLeft.x + span * 0.43f, _outerLeft.y, 0f);
            _innerRight = new Vector3(_outerRight.x - span * 0.43f, _outerRight.y, 0f);
            _leftCrusher.transform.position = _outerLeft;
            _rightCrusher.transform.position = _outerRight;
        }
    }
}
