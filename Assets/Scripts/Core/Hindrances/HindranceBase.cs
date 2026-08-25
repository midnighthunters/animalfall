// Task 4.1 — HindranceBase: abstract MonoBehaviour implementing IHindrance
using System.Collections;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;

namespace AnimalFall.Core.Hindrances
{
    public abstract class HindranceBase : MonoBehaviour, IHindrance
    {
        protected HindranceContext _ctx;
        protected SpriteRenderer   _sr;
        protected bool             _isActive;

        /// <summary>
        /// Absolute safety cap. No hindrance may persist longer than this even if its
        /// own effect logic fails to clean up (e.g. its target animal despawned).
        /// Prevents orphaned hindrances leaking into the scene.
        /// </summary>
        [SerializeField] private float _maxLifetimeSeconds = 14f;

        public abstract HindranceType Type { get; }

        protected virtual void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Activate(HindranceContext ctx)
        {
            StopAllCoroutines();
            _ctx     = ctx;
            _isActive = true;
            if (_sr != null) { _sr.enabled = true; _sr.color = Color.white; }
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            OnActivate();
            // Only start the watchdog if the effect did not immediately deactivate.
            if (_isActive && isActiveAndEnabled)
                StartCoroutine(LifetimeWatchdog());
        }

        private IEnumerator LifetimeWatchdog()
        {
            float safety = Mathf.Max(1f, _maxLifetimeSeconds);
            yield return new WaitForSeconds(safety);
            if (_isActive) Deactivate();
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            StopAllCoroutines();
            OnDeactivate();
            DOTween.Kill(gameObject);
            _ctx.HindranceManager?.OnHindranceDeactivated(this);
            ObjectPooler.Instance?.ReturnToPool(gameObject);   // NOT Destroy
        }

        protected virtual void OnDisable()
        {
            if (_isActive)
            {
                _isActive = false;
                StopAllCoroutines();
                OnDeactivate();
                _ctx.HindranceManager?.OnHindranceDeactivated(this);
            }
        }

        protected abstract void OnActivate();
        protected abstract void OnDeactivate();
    }
}
