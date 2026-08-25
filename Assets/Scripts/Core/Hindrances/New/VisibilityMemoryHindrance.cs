using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.New
{
    public sealed class VisibilityMemoryHindrance : HindranceBase, IPointerGestureTarget
    {
        [SerializeField] private HindranceType _type;
        [SerializeField] private float _duration = 6f;
        private readonly SpriteRenderer[] _renderers = new SpriteRenderer[24];
        private readonly Color[] _originalColors = new Color[24];
        private int _count;
        private bool _dragging;

        public override HindranceType Type => _type;
        public int InteractionPriority => 220;

#if UNITY_EDITOR
        public void EditorConfigure(HindranceType type, float duration)
        { _type = type; _duration = duration; }
#endif

        protected override void OnActivate()
        {
            SnapshotAndApply();
            StartCoroutine(Lifetime());
        }

        protected override void OnDeactivate()
        {
            for (int i = 0; i < _count; i++)
                if (_renderers[i] != null) _renderers[i].color = _originalColors[i];
            _count = 0;
            _dragging = false;
        }

        private void SnapshotAndApply()
        {
            _count = 0;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count && _count < _renderers.Length; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                SpriteRenderer renderer = animal.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                _renderers[_count] = renderer;
                _originalColors[_count] = renderer.color;
                renderer.color = EffectColor(Type, renderer.color, i);
                _count++;
            }
        }

        private static Color EffectColor(HindranceType type, Color original, int index)
        {
            switch (type)
            {
                case HindranceType.EclipseSilhouettes: return new Color(0.12f, 0.13f, 0.2f, Mathf.Max(0.75f, original.a));
                case HindranceType.ColourWashRain:
                    return Color.HSVToRGB(Mathf.Repeat(index * 0.17f + Time.time * 0.1f, 1f), 0.55f, 1f);
                case HindranceType.LanternSpotlight: return new Color(0.25f, 0.25f, 0.3f, Mathf.Max(0.72f, original.a));
                default: return new Color(original.r, original.g, original.b, Mathf.Max(0.55f, original.a));
            }
        }

        public void OnPointerDown(WorldPointerEvent e) { if (Type == HindranceType.LanternSpotlight) _dragging = true; }
        public void OnPointerMove(WorldPointerEvent e)
        {
            if (!_dragging) return;
            transform.position = e.WorldPosition;
            for (int i = 0; i < _count; i++)
                if (_renderers[i] != null && Vector2.Distance(_renderers[i].transform.position, transform.position) < 1.4f)
                    _renderers[i].color = _originalColors[i];
        }
        public void OnPointerUp(WorldPointerEvent e, bool canceled) => _dragging = false;

        private IEnumerator Lifetime()
        { yield return new WaitForSeconds(Mathf.Max(2f, _duration)); Deactivate(); }
    }
}
