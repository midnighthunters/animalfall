using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.New
{
    /// <summary>Spider Gun fires a visible web over active animals until each web is tapped.</summary>
    public sealed class SpiderGunHindrance : HindranceBase, IPointerTapTarget
    {
        private readonly List<SpiderWebCapture> _captures = new List<SpiderWebCapture>(4);
        private Sprite _webSprite;

        public override HindranceType Type => HindranceType.SpiderGun;
        public int InteractionPriority => 180;

		protected override void OnActivate()
        {
            NormalizeToAnimalSize();
            _webSprite = Resources.Load<Sprite>("icons/hindrances/spiderweb");
            StartCoroutine(FireWebs());
        }

        protected override void OnDeactivate()
        {
            for (int i = _captures.Count - 1; i >= 0; i--)
                if (_captures[i] != null) _captures[i].Release();
            _captures.Clear();
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive) return false;
            Deactivate();
            return true;
        }

        internal void OnCaptureRemoved(SpiderWebCapture capture)
        {
            _captures.Remove(capture);
        }

        private IEnumerator FireWebs()
        {
            yield return new WaitForSeconds(0.45f);
            var animals = ActiveAnimalRegistry.All;
            int captured = 0;
            for (int i = 0; i < animals.Count && captured < 3; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected || !animal.gameObject.activeInHierarchy) continue;
                if (!animal.TryClaimExclusive(this)) continue;

                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                if (movement == null || !movement.TryAttach(this))
                {
                    animal.ReleaseExclusive(this);
                    continue;
                }

                var webObject = new GameObject("SpiderWebCapture");
                webObject.transform.SetParent(animal.transform, false);
                webObject.transform.localPosition = Vector3.zero;
                float webSize = _webSprite != null ? Mathf.Max(_webSprite.bounds.size.x, _webSprite.bounds.size.y) : 1f;
                float parentScale = Mathf.Max(0.001f, animal.CurrentScale);
				webObject.transform.localScale = Vector3.one * (Animal.TargetWorldSize * 0.9f / (webSize * parentScale));

                var renderer = webObject.AddComponent<SpriteRenderer>();
                renderer.sprite = _webSprite;
                renderer.sortingOrder = 40;

                var collider = webObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = _webSprite != null ? _webSprite.bounds.size * 0.82f : Vector2.one;

                var capture = webObject.AddComponent<SpiderWebCapture>();
                capture.Initialize(animal, movement, this);
                _captures.Add(capture);
                captured++;
            }
        }
    

		private void NormalizeToAnimalSize()
        {
            if (_sr == null || _sr.sprite == null) return;
            Vector2 size = _sr.sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            if (largest > 0.001f)
                transform.localScale = Vector3.one * (Animal.TargetWorldSize / largest);
		}
}

    public sealed class SpiderWebCapture : MonoBehaviour, IPointerTapTarget
    {
        private Animal _animal;
        private AnimalMovement _movement;
        private SpiderGunHindrance _owner;
        private bool _released;

        public int InteractionPriority => 260;

        public void Initialize(Animal animal, AnimalMovement movement, SpiderGunHindrance owner)
        {
            _animal = animal;
            _movement = movement;
            _owner = owner;
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            Release();
            return true;
        }

        public void Release()
        {
            if (_released) return;
            _released = true;
            if (_movement != null) _movement.ReleaseAttachment(_owner, Vector2.down * 0.45f);
            if (_animal != null) _animal.ReleaseExclusive(_owner);
            if (_owner != null) _owner.OnCaptureRemoved(this);
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (!_released) Release();
        }
    }
}
