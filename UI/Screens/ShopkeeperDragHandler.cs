using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AnimalFall.UI.Screens
{
    public class ShopkeeperDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public Action onDraggedOffScreen;

        private RectTransform rt;
        private Vector3 originalPosition;
        private bool triggered;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            if (rt != null)
                originalPosition = rt.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rt == null || triggered) return;
            rt.anchoredPosition += eventData.delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (rt == null || triggered) return;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            bool offScreen = false;
            foreach (var corner in corners)
            {
                if (corner.x < 0 || corner.x > Screen.width ||
                    corner.y < 0 || corner.y > Screen.height)
                {
                    offScreen = true;
                    break;
                }
            }

            if (offScreen)
            {
                triggered = true;
                onDraggedOffScreen?.Invoke();
            }

            rt.anchoredPosition = originalPosition;
        }
    }
}
