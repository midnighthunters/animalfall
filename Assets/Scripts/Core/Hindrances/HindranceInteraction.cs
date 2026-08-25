using UnityEngine;

namespace AnimalFall.Core.Hindrances
{
    public interface IAnimalTapGate
    {
        bool CanCollect(AnimalFall.Core.Animals.Animal animal);
        void OnBlockedTap(AnimalFall.Core.Animals.Animal animal);
    }
    public readonly struct WorldPointerEvent
    {
        public readonly Vector2 ScreenPosition;
        public readonly Vector2 WorldPosition;
        public readonly Vector2 ScreenDelta;
        public readonly float Duration;
        public readonly bool IsSynthetic;

        public WorldPointerEvent(Vector2 screenPosition, Vector2 worldPosition,
            Vector2 screenDelta, float duration, bool isSynthetic = false)
        {
            ScreenPosition = screenPosition;
            WorldPosition = worldPosition;
            ScreenDelta = screenDelta;
            Duration = duration;
            IsSynthetic = isSynthetic;
        }
    }

    public interface IPointerTapTarget
    {
        int InteractionPriority { get; }
        bool TryHandleTap(WorldPointerEvent pointerEvent);
    }

    public interface IPointerGestureTarget
    {
        int InteractionPriority { get; }
        void OnPointerDown(WorldPointerEvent pointerEvent);
        void OnPointerMove(WorldPointerEvent pointerEvent);
        void OnPointerUp(WorldPointerEvent pointerEvent, bool canceled);
    }
}
