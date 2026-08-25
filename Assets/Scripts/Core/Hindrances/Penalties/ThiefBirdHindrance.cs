// Task 4.3 — ThiefBirdHindrance: steals a random on-screen animal
using System.Collections;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;

namespace AnimalFall.Core.Hindrances.Penalties
{
    public class ThiefBirdHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.ThiefBird;

        protected override void OnActivate()
        {
            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.ThiefBird);

            StartCoroutine(StealCoroutine());
        }

        protected override void OnDeactivate() { }

        private IEnumerator StealCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // brief delay before stealing

            var target = _ctx.HindranceManager?.GetRandomActiveAnimal();
            if (target == null)
            {
                Deactivate();
                yield break;
            }

            // Tween animal off-screen to the right over 1.5s
            float offScreenX = Camera.main != null
                ? Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 0.5f, Mathf.Abs(Camera.main.transform.position.z))).x
                : 20f;

            target.transform.DOLocalMoveX(offScreenX, 1.5f)
                .SetEase(Ease.InQuad)
                .SetId(target.gameObject)
                .OnComplete(() =>
                {
                    if (target != null) ObjectPooler.Instance?.ReturnToPool(target.gameObject);
                });

            yield return new WaitForSeconds(1.5f);
            Deactivate();
        }
    }
}
