using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.Penalties
{
    /// <summary>Flies horizontally across the playfield and carries away up to two animals.</summary>
    public sealed class EagleHindrance : HindranceBase, IAnimalTapGate
    {
        [SerializeField] private Sprite _eagleSprite;
        [SerializeField, Min(1f)] private float _flightSpeed = 4.8f;
        [SerializeField] private Vector2 _firstCarryOffset = new Vector2(-0.42f, -0.48f);
        [SerializeField] private Vector2 _secondCarryOffset = new Vector2(0.42f, -0.48f);

        private readonly List<Animal> _carried = new List<Animal>(2);
        private float _endX;
        private int _direction;

        public override HindranceType Type => HindranceType.Eagle;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite sprite) { _eagleSprite = sprite; UnityEditor.EditorUtility.SetDirty(this); }
#endif

        protected override void OnActivate()
        {
            if (_sr != null)
            {
                _sr.sprite = _eagleSprite != null ? _eagleSprite : FirstSprite("icons/hindrances/eagle");
                _sr.sortingOrder = 40;
                _sr.enabled = true;
            }
            // Give the eagle a stronger visual silhouette and a wider pickup profile
            // so its two-animal snatch is easy to read during play.
            Normalize(Animal.TargetWorldSize * 2.4f);
            _direction = Random.value < 0.5f ? 1 : -1;
            PlaceAtFlightStart();
            CaptureTwoAnimals();
        }

        private void Update()
        {
            if (!_isActive) return;
            transform.Translate(Vector3.right * (_direction * _flightSpeed * Time.deltaTime), Space.World);
            for (int i = _carried.Count - 1; i >= 0; i--)
            {
                Animal animal = _carried[i];
                if (animal == null || animal.IsCollected) { _carried.RemoveAt(i); continue; }
                Vector2 offset = i == 0 ? _firstCarryOffset : _secondCarryOffset;
                offset.x *= _direction;
                animal.transform.position = transform.position + (Vector3)offset;
            }

            if ((_direction > 0 && transform.position.x >= _endX) || (_direction < 0 && transform.position.x <= _endX))
            {
                CarryTargetsAway();
                Deactivate();
            }
        }

        public bool CanCollect(Animal animal) => !_carried.Contains(animal);
        public void OnBlockedTap(Animal animal) { }

        protected override void OnDeactivate()
        {
            for (int i = _carried.Count - 1; i >= 0; i--)
            {
                Animal animal = _carried[i];
                if (animal == null) continue;
                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                movement?.ReleaseAttachment(this, Vector2.down * 0.5f);
                animal.ReleaseExclusive(this);
            }
            _carried.Clear();
        }

        private void CaptureTwoAnimals()
        {
            for (int i = 0; i < 2; i++)
            {
                Animal animal = ActiveAnimalRegistry.GetEligible(i);
                if (animal == null || !animal.TryClaimExclusive(this)) continue;
                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                if (movement == null || !movement.TryAttach(this))
                {
                    animal.ReleaseExclusive(this);
                    continue;
                }
                _carried.Add(animal);
            }
        }

        private void CarryTargetsAway()
        {
            for (int i = _carried.Count - 1; i >= 0; i--)
            {
                Animal animal = _carried[i];
                if (animal == null || animal.IsCollected) continue;
                animal.ReleaseExclusive(this);
                animal.Despawn();
            }
            _carried.Clear();
        }

        private void PlaceAtFlightStart()
        {
            Camera camera = Camera.main;
            if (camera == null) { transform.position = new Vector3(-8f * _direction, 1f, 0f); _endX = 8f * _direction; return; }
            float depth = Mathf.Abs(camera.transform.position.z);
            float y = Random.Range(0.38f, 0.78f);
            float startViewport = _direction > 0 ? -0.14f : 1.14f;
            float endViewport = _direction > 0 ? 1.14f : -0.14f;
            Vector3 start = camera.ViewportToWorldPoint(new Vector3(startViewport, y, depth));
            Vector3 end = camera.ViewportToWorldPoint(new Vector3(endViewport, y, depth));
            start.z = 0f;
            transform.position = start;
            _endX = end.x;
            if (_sr != null) _sr.flipX = _direction < 0;
        }

        private void Normalize(float worldSize)
        {
            if (_sr == null || _sr.sprite == null) return;
            float largest = Mathf.Max(_sr.sprite.bounds.size.x, _sr.sprite.bounds.size.y);
            if (largest > 0.001f) transform.localScale = Vector3.one * (worldSize / largest);
        }

        private static Sprite FirstSprite(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }
    }
}
