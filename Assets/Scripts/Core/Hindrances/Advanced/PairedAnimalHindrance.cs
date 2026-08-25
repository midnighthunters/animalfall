// Task 4.7 — PairedAnimalHindrance: 2 animals must both be tapped within 2s
using System.Collections;
using UnityEngine;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Advanced
{
    public class PairedAnimalHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.PairedAnimal;

        [SerializeField] private GameObject _animalPrefab;
        private Animal _animalA, _animalB;
        private Coroutine _windowCoroutine;
        private Animal _armedAnimal;
        private float _deadline;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            _armedAnimal = null;
            GameEvents.OnPairedAnimalTapped += OnPairedTapped;
            _windowCoroutine = StartCoroutine(PairWindow());
        }

        protected override void OnDeactivate()
        {
            if (_windowCoroutine != null) { StopCoroutine(_windowCoroutine); _windowCoroutine = null; }
            GameEvents.OnPairedAnimalTapped -= OnPairedTapped;
            ReleasePair();
        }

        private IEnumerator PairWindow()
        {
            // Get two active animals from the manager
            _animalA = _ctx.HindranceManager?.GetRandomActiveAnimal();
            _animalB = _ctx.HindranceManager?.GetRandomActiveAnimal();

            if (_animalA == null || _animalB == null || _animalA == _animalB)
            {
                Deactivate();
                yield break;
            }

            _animalA.IsPaired = true; _animalA.PairedPartner = _animalB; _animalA.PairedTimer = 2f;
            _animalB.IsPaired = true; _animalB.PairedPartner = _animalA; _animalB.PairedTimer = 2f;

            while (_isActive && _armedAnimal == null) yield return null;
            while (_isActive && Time.time < _deadline) yield return null;
            if (_isActive) { GameEvents.OnWrongTap?.Invoke(); Deactivate(); }
        }

        private void OnPairedTapped(Animal animal)
        {
            if (!_isActive || (animal != _animalA && animal != _animalB)) return;
            if (_armedAnimal == null)
            {
                _armedAnimal = animal;
                _deadline = Time.time + 2f;
                animal.GetComponent<AnimalMovement>()?.TryAttach(this);
                return;
            }
            if (animal == _armedAnimal || Time.time > _deadline) return;
            Animal first = _armedAnimal;
            ReleasePair();
            first.OnCollected();
            animal.OnCollected();
            Deactivate();
        }

        private void ReleasePair()
        {
            if (_animalA != null)
            {
                _animalA.IsPaired = false; _animalA.PairedPartner = null;
                _animalA.GetComponent<AnimalMovement>()?.ReleaseAttachment(this, Vector2.down * 0.5f);
            }
            if (_animalB != null)
            {
                _animalB.IsPaired = false; _animalB.PairedPartner = null;
                _animalB.GetComponent<AnimalMovement>()?.ReleaseAttachment(this, Vector2.down * 0.5f);
            }
        }
    }
}
