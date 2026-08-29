using UnityEngine;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    /// <summary>
    /// Marker definition for the level-wide Dog Helmet rule. Animal.SetupForPool
    /// applies the visual and one-tap break state to every dog in configured levels.
    /// </summary>
    public sealed class DogHelmetHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.DogHelmet;

        protected override void OnActivate()
        {
            // This rule is intentionally applied when dogs spawn, not as a random object.
            Deactivate();
        }

        protected override void OnDeactivate() { }
    }
}