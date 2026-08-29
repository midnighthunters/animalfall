using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimalFall.MegaShooter
{
    [ExecuteAlways]
    public sealed class MegaHUD : MonoBehaviour
    {
        [Header("HUD")]
        public Text healthText;
        public Text waveText;
        public Text scoreText;
        public Image bossHealthFill;
        public GameObject bossHealthRoot;
        public Text bossNameText;
        public Image counterFill;
        public Button counterButton;
        public Image animalPortrait;
        public Button pauseButton;
        public Text bannerText;
        public GameObject bannerRoot;

        [Header("Selection")]
        public GameObject selectionRoot;
        public Text selectionTitle;
        public Text selectionDescription;
        public Image selectionPortrait;
        public Image selectionWeaponIcon;
        public Image villainOnePortrait;
        public Image villainTwoPortrait;
        public Image villainOneWeaponIcon;
        public Image villainTwoWeaponIcon;
        public Image bossPortrait;
        public Image bossWeaponIcon;
        public Text selectionLockText;
        public Button previousAnimalButton;
        public Button nextAnimalButton;
        public Button startButton;
        public Text countdownText;

        [Header("Pause & Results")]
        public GameObject pauseRoot;
        public Button resumeButton;
        public Button retryButton;
        public Button quitButton;
        public GameObject resultRoot;
        public Text resultTitle;
        public Text resultSummary;
        public Button resultRetryButton;
        public Button resultQuitButton;
        public GameObject unlockRoot;
        public Text unlockText;
        public Button unlockContinueButton;

        private MegaShooterGameManager _game;
        private bool _gameplayUiSuppressed;

        // Runtime announcement overlay (wave banners, captain name, countdown).
        // Built in code and parented to the canvas so the deliberately empty
        // gameplay HUD stays untouched.
        private RectTransform _overlayRoot;
        private Text _waveTitle;
        private Text _waveSubtitle;
        private Text _captainLabel;
        private Text _countdownLabel;
        private Coroutine _waveRoutine;
        private Coroutine _subtitleRoutine;
        private Coroutine _captainRoutine;
        private Font _runtimeFont;

        private void OnEnable()
        {
            // Keep the scene view clean as well as the running game. Legacy panel roots
            // are removed immediately when an older MegaShooterScene is opened.
            if (!Application.isPlaying)
            {
                RemoveLegacySceneUi();
                ShowIntroOnly();
            }
        }

        private void RemoveLegacySceneUi()
        {
            if (countdownText == null) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.gameObject != countdownText.gameObject)
                    DestroyImmediate(child.gameObject);
            }

            Transform flashOverlay = transform.parent != null ? transform.parent.Find("ReducedFlashOverlay") : null;
            if (flashOverlay != null) DestroyImmediate(flashOverlay.gameObject);
        }

        public void Bind(MegaShooterGameManager game)
        {
            _game = game;
            _gameplayUiSuppressed = false;
            ShowIntroOnly();
        }

        public void ShowIntroOnly()
        {
            // The mega shooter deliberately has no HUD or panels.  Keeping this in one
            // place also lets older scenes shed their legacy UI at runtime.
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(countdownText != null && child == countdownText.gameObject);
            }
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }

        public void SetSelection(SuperAnimalData animal, bool unlocked)
        {
            if (selectionTitle != null) selectionTitle.text = animal != null ? animal.displayName : "Unavailable";
            if (selectionDescription != null) selectionDescription.text = animal != null ? animal.selectionDescription : string.Empty;
            if (selectionPortrait != null) selectionPortrait.sprite = animal != null ? animal.portrait : null;
            if (selectionWeaponIcon != null) selectionWeaponIcon.sprite = animal != null ? animal.primaryWeapon?.icon : null;
            if (selectionLockText != null) selectionLockText.text = unlocked ? "READY" : $"LOCKED — LEVEL {animal?.unlockGameLevel}";
            if (startButton != null) startButton.interactable = unlocked;
        }

        public void SetMissionPreview(EnemyShipData first, EnemyShipData second, BossShipData boss)
        {
            if (_gameplayUiSuppressed) return;
            if (villainOnePortrait != null) villainOnePortrait.sprite = first != null ? first.sprite : null;
            if (villainTwoPortrait != null) villainTwoPortrait.sprite = second != null ? second.sprite : null;
            if (villainOneWeaponIcon != null) villainOneWeaponIcon.sprite = first != null ? first.weaponIcon : null;
            if (villainTwoWeaponIcon != null) villainTwoWeaponIcon.sprite = second != null ? second.weaponIcon : null;
            if (bossPortrait != null)
            {
                bossPortrait.sprite = boss != null ? boss.sprite : null;
                bossPortrait.enabled = boss != null && boss.sprite != null;
            }
            if (bossWeaponIcon != null)
            {
                bossWeaponIcon.sprite = boss != null ? boss.weaponIcon : null;
                bossWeaponIcon.enabled = boss != null && boss.weaponIcon != null;
            }
        }

        public void HideSelection()
        {
            _gameplayUiSuppressed = true;
            if (selectionRoot != null) selectionRoot.SetActive(false);
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);
        }

        public void ShowCountdown(string value)
        {
            if (!Application.isPlaying) return;
            if (_countdownLabel == null)
                _countdownLabel = CreateOverlayText("Countdown", 140, FontStyle.Bold,
                    new Vector2(0f, 0.4f), new Vector2(1f, 0.6f));
            _countdownLabel.gameObject.SetActive(true);
            _countdownLabel.text = value;
            _countdownLabel.color = new Color(1f, 1f, 1f, 1f);
        }

        public void HideCountdown()
        {
            if (_countdownLabel != null) _countdownLabel.gameObject.SetActive(false);
        }

        public void SetHealth(int current, int max) { }

        public void SetWave(int current, int total)
        {
            if (!Application.isPlaying) return;
            if (_waveTitle == null)
                _waveTitle = CreateOverlayText("WaveTitle", 96, FontStyle.Bold,
                    new Vector2(0f, 0.72f), new Vector2(1f, 0.87f));
            _waveTitle.text = total > 1 ? $"WAVE {current} / {total}" : $"WAVE {current}";
            _waveTitle.color = new Color(1f, 1f, 1f, _waveTitle.color.a);
            if (_waveRoutine != null) StopCoroutine(_waveRoutine);
            _waveRoutine = StartCoroutine(FlashText(_waveTitle, 0.25f, 1.15f, 0.55f));
        }

        public void SetScore(int score) { }

        public void ShowBoss(string bossName)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(bossName)) return;
            if (_captainLabel == null)
                _captainLabel = CreateOverlayText("CaptainName", 68, FontStyle.Bold,
                    new Vector2(0f, 0.42f), new Vector2(1f, 0.62f));
            _captainLabel.text = $"CAPTAIN\n{bossName.ToUpperInvariant()}";
            _captainLabel.color = new Color(1f, 0.82f, 0.28f, _captainLabel.color.a);
            if (_captainRoutine != null) StopCoroutine(_captainRoutine);
            _captainRoutine = StartCoroutine(FlashText(_captainLabel, 0.35f, 2.2f, 0.8f));
        }

        public void SetBossHealth(float normalized) { }

        public void SetCounter(float normalized, bool ready) { }

        public void PulseCounterReady() { }

        public void SetAnimalPortrait(Sprite sprite) { }

        public void ShowBanner(string text, float duration = 1.2f)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(text)) return;
            if (_waveSubtitle == null)
                _waveSubtitle = CreateOverlayText("WaveSubtitle", 44, FontStyle.Bold,
                    new Vector2(0f, 0.64f), new Vector2(1f, 0.71f));
            _waveSubtitle.text = text;
            _waveSubtitle.color = new Color(0.95f, 0.98f, 1f, _waveSubtitle.color.a);
            if (_subtitleRoutine != null) StopCoroutine(_subtitleRoutine);
            _subtitleRoutine = StartCoroutine(FlashText(_waveSubtitle, 0.2f, Mathf.Max(0.5f, duration), 0.5f));
        }

        private RectTransform EnsureOverlayRoot()
        {
            if (_overlayRoot != null) return _overlayRoot;
            Transform parent = transform.parent != null ? transform.parent : transform;
            var go = new GameObject("MegaRuntimeBanners", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
            _overlayRoot = rect;
            return rect;
        }

        private Text CreateOverlayText(string name, int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            var rect = (RectTransform)go.transform;
            rect.SetParent(EnsureOverlayRoot(), false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            var text = go.GetComponent<Text>();
            text.font = _runtimeFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.white;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            go.SetActive(false);
            return text;
        }

        private IEnumerator FlashText(Graphic target, float fadeIn, float hold, float fadeOut)
        {
            target.gameObject.SetActive(true);
            yield return FadeGraphic(target, 0f, 1f, fadeIn);
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;
            yield return FadeGraphic(target, 1f, 0f, fadeOut);
            target.gameObject.SetActive(false);
        }

        private IEnumerator FadeGraphic(Graphic target, float from, float to, float duration)
        {
            duration = Mathf.Max(0.01f, duration);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(target, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetAlpha(target, to);
        }

        private static void SetAlpha(Graphic target, float alpha)
        {
            Color color = target.color;
            color.a = alpha;
            target.color = color;
        }

        public void ShowPause(bool visible) { }

        public void ShowResult(bool won, int score, int stars, int coins)
        {
            // Results are handled by the normal level flow after the mega scene exits.
        }

        public void ShowUnlock(SuperAnimalData animal)
        {
            // Unlock presentation is intentionally omitted from mega levels.
        }

        public void HideUnlock() { }
    }
}
