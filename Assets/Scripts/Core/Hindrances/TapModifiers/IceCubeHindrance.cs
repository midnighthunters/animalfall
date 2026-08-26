// IceCubeHindrance — freezes only pigs; first tap breaks the ice
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class IceCubeHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.IceCube;

        private Animal _targetAnimal;
        private Sprite _frozenPigSprite;

        protected override void OnActivate()
        {
            _targetAnimal = ActiveAnimalRegistry.GetEligibleSpecies(AnimalSpecies.Pig);
            if (_targetAnimal == null) { Deactivate(); return; }

            _frozenPigSprite = Resources.Load<Sprite>("icons/hindrances/frozen_pig");
            if (_frozenPigSprite == null)
            {
                Debug.LogWarning("[IceCubeHindrance] Missing frozen_pig sprite.");
                Deactivate();
                return;
            }

            _targetAnimal.IsIceFrozen = true;
            _targetAnimal.SetDisplaySprite(_frozenPigSprite);
            GameEvents.OnIceBroken += OnIceBroken;

            if (_sr != null) _sr.enabled = false;
        }

        protected override void OnDeactivate()
        {
            GameEvents.OnIceBroken -= OnIceBroken;
            if (_targetAnimal != null)
            {
                _targetAnimal.IsIceFrozen = false;
                _targetAnimal.RestoreDisplaySprite();
                _targetAnimal = null;
            }
        }

        private void OnIceBroken(Animal animal)
        {
            if (_isActive && animal == _targetAnimal)
                Deactivate();
        }





    }
}
