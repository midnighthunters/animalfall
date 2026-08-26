// Task 4.4 — BubbleShieldHindrance: animal floats up, first tap pops bubble
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class BubbleShieldHindrance : HindranceBase
    {
        

        private Animal _targetAnimal;
        private Sprite _bubbleMonkeySprite;
        public override HindranceType Type => HindranceType.BubbleShield;

        protected override void OnActivate()
        {
            _targetAnimal = ActiveAnimalRegistry.GetEligibleSpecies(AnimalSpecies.Monkey);
            if (_targetAnimal == null) { Deactivate(); return; }

            _bubbleMonkeySprite = Resources.Load<Sprite>("icons/hindrances/bubble_monkey");
            if (_bubbleMonkeySprite == null)
            {
                Debug.LogWarning("[BubbleShieldHindrance] Missing bubble_monkey sprite.");
                Deactivate();
                return;
            }

            _targetAnimal.IsBubble = true;
            _targetAnimal.SetDisplaySprite(_bubbleMonkeySprite);
            GameEvents.OnBubblePopped += OnBubblePopped;

            if (_sr != null) _sr.enabled = false;
        }

        protected override void OnDeactivate()
        {
            GameEvents.OnBubblePopped -= OnBubblePopped;
            if (_targetAnimal != null)
            {
                _targetAnimal.IsBubble = false;
                _targetAnimal.RestoreDisplaySprite();
                _targetAnimal = null;
            }
        }

        private void OnBubblePopped(Animal animal)
        {
            if (_isActive && animal == _targetAnimal)
                Deactivate();
        }

    }
}
