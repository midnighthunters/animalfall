// Task 4.4 — BubbleShieldHindrance: animal floats up, first tap pops bubble
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class BubbleShieldHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.BubbleShield;

        protected override void OnActivate()
        {
            var target = _ctx.HindranceManager?.GetRandomActiveAnimal();
            if (target == null) { Deactivate(); return; }

            target.IsBubble = true;
            // Hide self — effect is on the animal
            if (_sr != null) _sr.enabled = false;
        }

        protected override void OnDeactivate() { }
    }
}
