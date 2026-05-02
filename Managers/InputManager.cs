using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Managers
{
    [RequireComponent(typeof(Camera))]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public Vector2 TapOffset { get; set; }
        public bool IsMirrorModeActive { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsRunning) return;

            if (Input.GetMouseButtonDown(0))
                ProcessTap(Input.mousePosition);

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    ProcessTap(touch.position);
            }
        }

        private void ProcessTap(Vector3 screenPos)
        {
            if (IsMirrorModeActive)
            {
                screenPos.x = Screen.width - screenPos.x;
            }

            screenPos.z = -Camera.main.transform.position.z;
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(screenPos);

            worldPoint += TapOffset;

            Collider2D hitCollider = Physics2D.OverlapPoint(worldPoint);
            if (hitCollider == null)
            {
                if (EasterEggManager.Instance != null)
                    CheckBackgroundCloudTap(worldPoint);
                return;
            }

            var animal = hitCollider.GetComponent<Animal>();
            if (animal != null)
            {
                TapResult result = animal.HandleTap();
                HandleTapResult(result, animal);
                return;
            }
        }

        private void HandleTapResult(TapResult result, Animal animal)
        {
            var gm = GameManager.Instance;

            switch (result)
            {
                case TapResult.Correct:
                    gm.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
                    break;
                case TapResult.Wrong:
                    gm.OnWrongTap(false);
                    break;
                case TapResult.ShieldBroken:
                    gm.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);
                    break;
                case TapResult.Golden:
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddPoints(animal.data.pointValue * 2);
                    gm.AddTime(1f);
                    break;
                case TapResult.BombExploded:
                    break;
                case TapResult.Rainbow:
                    EasterEggManager.Instance?.OnRainbowCollected();
                    gm.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
                    break;
                case TapResult.FakeCollected:
                    gm.OnWrongTap(false);
                    break;
                case TapResult.CursedSkullDestroyed:
                    gm.AudioManager?.PlaySFX(AudioManager.SfxType.Explosion);
                    break;
            }
        }

        private void CheckBackgroundCloudTap(Vector2 worldPoint)
        {
            EasterEggManager.Instance?.OnBackgroundCloudTapped();
        }
    }
}
