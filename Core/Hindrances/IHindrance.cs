using UnityEngine;

namespace AnimalFall.Core.Hindrances
{
    public interface IHindrance
    {
        HindranceType Type { get; }
        bool IsActive { get; }
        void Activate(HindranceContext context);
        void Deactivate();
    }

    public abstract class HindranceBase : MonoBehaviour, IHindrance
    {
        public abstract HindranceType Type { get; }
        public bool IsActive { get; protected set; }

        protected HindranceContext context;

        public virtual void Activate(HindranceContext ctx)
        {
            context = ctx;
            IsActive = true;
        }

        public virtual void Deactivate()
        {
            IsActive = false;
            if (gameObject != null)
                Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            IsActive = false;
        }
    }
}
