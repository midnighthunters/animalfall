// Task 4.1 — IHindrance interface
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Core.Hindrances
{
    public interface IHindrance
    {
        HindranceType Type { get; }
        void Activate(HindranceContext ctx);
        void Deactivate();
    }
}
