using System;

namespace AnimalFall.Core.Hindrances
{
    public sealed class HindranceEffectToken : IDisposable
    {
        private Action _release;
        public bool IsValid => _release != null;

        public HindranceEffectToken(Action release) => _release = release;

        public void Dispose()
        {
            Action release = _release;
            _release = null;
            release?.Invoke();
        }
    }
}
