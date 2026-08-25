// Task 4.4 — GhostAnimalHindrance: tweens animal alpha 1→0.2 over 0.5s
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class GhostAnimalHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.GhostAnimal;

        protected override void OnActivate()
        {
            var target = _ctx.HindranceManager?.GetRandomActiveAnimal();
            if (target == null) { Deactivate(); return; }

            var sr = target.GetComponent<SpriteRenderer>();
            if (sr == null) { Deactivate(); return; }

            // Tween alpha 1 → 0.2 over 0.5s; ghost alpha persists until pool return
            target.GhostAlpha = 0.2f;
            DOTween.Kill(sr);
            sr.DOFade(0.2f, 0.5f).SetId(target.gameObject);

            if (_sr != null) _sr.enabled = false;
            Deactivate(); // one-shot effect, no sustained active state needed
        }

        protected override void OnDeactivate() { }
    }
}
