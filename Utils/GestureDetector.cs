using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Utils
{
    public class GestureDetector : MonoBehaviour
    {
        public static GestureDetector Instance { get; private set; }

        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float swipeTimeLimit = 0.3f;
        [SerializeField] private float simultaneousTapWindow = 0.1f;

        public event Action<Vector2, Vector2, SwipeDirection> OnSwipe;
        public event Action<Vector2[]> OnSimultaneousTap;

        private readonly Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
        private readonly List<TapRecord> recentTaps = new List<TapRecord>();

        private Vector2 mouseDownPos;
        private float mouseDownTime;
        private bool mouseTracking;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            ProcessTouchInput();
            ProcessMouseSwipe();
            CheckSimultaneousTaps();
        }

        private void ProcessTouchInput()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        activeTouches[touch.fingerId] = new TouchData
                        {
                            startPos = touch.position,
                            startTime = Time.time
                        };
                        recentTaps.Add(new TapRecord
                        {
                            position = touch.position,
                            time = Time.time
                        });
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (activeTouches.TryGetValue(touch.fingerId, out var data))
                        {
                            float elapsed = Time.time - data.startTime;
                            Vector2 delta = touch.position - data.startPos;

                            if (elapsed <= swipeTimeLimit && delta.magnitude >= swipeThreshold)
                            {
                                SwipeDirection dir = GetSwipeDirection(delta);
                                OnSwipe?.Invoke(data.startPos, touch.position, dir);
                            }

                            activeTouches.Remove(touch.fingerId);
                        }
                        break;
                }
            }
        }

        private void ProcessMouseSwipe()
        {
            if (Input.GetMouseButtonDown(0))
            {
                mouseDownPos = Input.mousePosition;
                mouseDownTime = Time.time;
                mouseTracking = true;
            }

            if (Input.GetMouseButtonUp(0) && mouseTracking)
            {
                mouseTracking = false;
                float elapsed = Time.time - mouseDownTime;
                Vector2 endPos = Input.mousePosition;
                Vector2 delta = endPos - mouseDownPos;

                if (elapsed <= swipeTimeLimit && delta.magnitude >= swipeThreshold)
                {
                    SwipeDirection dir = GetSwipeDirection(delta);
                    OnSwipe?.Invoke(mouseDownPos, endPos, dir);
                }
            }
        }

        private void CheckSimultaneousTaps()
        {
            if (recentTaps.Count < 2) return;

            float now = Time.time;
            recentTaps.RemoveAll(t => now - t.time > simultaneousTapWindow * 2f);

            for (int i = 0; i < recentTaps.Count - 1; i++)
            {
                for (int j = i + 1; j < recentTaps.Count; j++)
                {
                    if (Mathf.Abs(recentTaps[i].time - recentTaps[j].time) <= simultaneousTapWindow)
                    {
                        OnSimultaneousTap?.Invoke(new[]
                        {
                            recentTaps[i].position,
                            recentTaps[j].position
                        });
                        recentTaps.RemoveAt(j);
                        recentTaps.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        private SwipeDirection GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        private struct TouchData
        {
            public Vector2 startPos;
            public float startTime;
        }

        private struct TapRecord
        {
            public Vector2 position;
            public float time;
        }
    }

    public enum SwipeDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
