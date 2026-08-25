using UnityEngine;
using UnityEngine.EventSystems;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaShooterInput : MonoBehaviour
    {
        private SuperAnimalController _player;
        private Camera _camera;
        private int _dragFinger = -1;
        private bool _mouseDragging;
        private Vector2 _pointerOffset;

        public void Configure(SuperAnimalController player, Camera worldCamera)
        {
            _player = player;
            _camera = worldCamera;
            _dragFinger = -1;
            _mouseDragging = false;
        }

        private void Update()
        {
            if (_player == null) return;
            HandleTouches();
            HandleMouse();
            HandleKeyboard();
        }

        private void HandleTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && _dragFinger < 0)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) continue;
                    _dragFinger = touch.fingerId;
                    _pointerOffset = (Vector2)_player.transform.position - ScreenToWorld(touch.position);
                }
                if (touch.fingerId != _dragFinger) continue;
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    _player.SetDesiredPosition(ScreenToWorld(touch.position) + _pointerOffset);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    _dragFinger = -1;
            }
        }

        private void HandleMouse()
        {
            if (Input.touchCount > 0) return;
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                _mouseDragging = true;
                _pointerOffset = (Vector2)_player.transform.position - ScreenToWorld(Input.mousePosition);
            }
            if (_mouseDragging && Input.GetMouseButton(0))
                _player.SetDesiredPosition(ScreenToWorld(Input.mousePosition) + _pointerOffset);
            if (Input.GetMouseButtonUp(0)) _mouseDragging = false;
        }

        private void HandleKeyboard()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(x) + Mathf.Abs(y) < 0.01f) return;
            _player.SetDesiredPosition((Vector2)_player.transform.position + new Vector2(x, y).normalized * 1.2f);
            if (Input.GetKeyDown(KeyCode.Space)) _player.Counter?.TryActivate();
        }

        private Vector2 ScreenToWorld(Vector2 screen)
        {
            if (_camera == null) return screen;
            Vector3 result = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return result;
        }
    }
}
