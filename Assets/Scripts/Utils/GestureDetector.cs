// Task 6.6 — GestureDetector utility
using UnityEngine;

namespace AnimalFall.Utils
{
    /// <summary>Classifies touch input into tap vs swipe.</summary>
    public static class GestureDetector
    {
        private const float SWIPE_MIN_PX   = 80f;
        private const float SWIPE_MAX_SECS = 0.4f;

        /// <summary>Returns true if the touch constitutes a swipe. Also outputs swipe delta.</summary>
        public static bool IsSwipe(Vector2 startPos, Vector2 endPos, float duration, out Vector2 delta)
        {
            delta = endPos - startPos;
            return delta.magnitude >= SWIPE_MIN_PX && duration <= SWIPE_MAX_SECS;
        }
    }
}
