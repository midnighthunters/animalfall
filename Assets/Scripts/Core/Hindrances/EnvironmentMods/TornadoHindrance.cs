// Task 4.6 — TornadoHindrance: moves across screen, pushes animals sideways
using UnityEngine;
using DG.Tweening;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class TornadoHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.Tornado;

        private const float PUSH_FORCE = 2.0f;
        private const float RADIUS     = 1.5f;

        protected override void OnActivate()
        {
            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.Tornado);

            float startX = -8f, endX = 8f;
            if (Camera.main != null)
            {
                float z = Mathf.Abs(Camera.main.transform.position.z);
                startX  = Camera.main.ViewportToWorldPoint(new Vector3(-0.1f, 0.5f, z)).x;
                endX    = Camera.main.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, z)).x;
            }

            transform.position = new Vector3(startX, 0f, 0f);
            transform.DOMoveX(endX, 4f).SetEase(Ease.Linear).SetId(gameObject)
                .OnComplete(Deactivate);
        }

        protected override void OnDeactivate() { }

        private void Update()
        {
            if (!_isActive) return;

            var animals = Animals.ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                var animal = animals[i];
                if (animal == null || Vector2.Distance(animal.transform.position, transform.position) > RADIUS) continue;

                Vector2 dir  = ((Vector2)animal.transform.position - (Vector2)transform.position).normalized;
                animal.GetComponent<Animals.AnimalMovement>()?.AddImpulse(new Vector2(dir.x * PUSH_FORCE * Time.deltaTime, 0f));
            }
        }
    }
}
