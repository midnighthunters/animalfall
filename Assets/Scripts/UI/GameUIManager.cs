// Task 10.1 — GameUIManager: StaticCanvas/DynamicCanvas split, GameEvents subscriber
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;
using AnimalFall.Utils;
using AnimalFall.Services;

namespace AnimalFall.UI
{
    public class GameUIManager : MonoBehaviour
    {
        // ── Canvases ──────────────────────────────────────────────────────────
        [SerializeField] private Canvas _staticCanvas;   // sort order 0
        [SerializeField] private Canvas _dynamicCanvas;  // sort order 1

        // ── Timer ─────────────────────────────────────────────────────────────
        [SerializeField] private Image _clockIcon;
        [SerializeField] private Text  _timerText;

        // ── Score / Combo ─────────────────────────────────────────────────────
        [SerializeField] private Text  _scoreText;
        [SerializeField] private Text  _comboText;
        [SerializeField] private Text  _multiplierText;

        // ── Goal panel ────────────────────────────────────────────────────────
        [SerializeField] private Transform _goalPanelRoot;

        // ── Floating text pool ────────────────────────────────────────────────
        [SerializeField] private GameObject _floatingTextPrefab;
        [SerializeField] private Transform  _floatingTextContainer;

        // ── Overlays ──────────────────────────────────────────────────────────
        [SerializeField] private ResultsScreenController _resultsScreen;
        [SerializeField] private LevelIntroScreen        _introScreen;
        [SerializeField] private CountdownController     _countdown;

        // ── Toast ─────────────────────────────────────────────────────────────
        [SerializeField] private Text       _toastText;
        [SerializeField] private GameObject _toastRoot;

        private SaveService _save;
        private float       _totalTime;

        // ── Setup ─────────────────────────────────────────────────────────────

        public void Setup(Data.LevelData level, SaveService save)
        {
            _save      = save;
            _totalTime = level.TimeLimit;

            if (_staticCanvas  != null) _staticCanvas.sortingOrder  = 0;
            if (_dynamicCanvas != null) _dynamicCanvas.sortingOrder  = 1;

            if (_clockIcon != null) _clockIcon.sprite = ImageLibrary.GetClock();
        }

        // ── GameEvents subscriptions ──────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnScoreChanged        += UpdateScore;
            GameEvents.OnComboChanged        += UpdateCombo;
            GameEvents.OnAnimalCollected     += OnAnimalCollected;
            GameEvents.OnLevelWon            += OnLevelWon;
            GameEvents.OnLevelFailed         += OnLevelFailed;
            GameEvents.OnTimerWarning        += OnTimerWarning;
            GameEvents.OnVillainPhaseChanged += OnVillainPhase;
            GameEvents.OnHindranceActivated  += OnHindranceActivated;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged        -= UpdateScore;
            GameEvents.OnComboChanged        -= UpdateCombo;
            GameEvents.OnAnimalCollected     -= OnAnimalCollected;
            GameEvents.OnLevelWon            -= OnLevelWon;
            GameEvents.OnLevelFailed         -= OnLevelFailed;
            GameEvents.OnTimerWarning        -= OnTimerWarning;
            GameEvents.OnVillainPhaseChanged -= OnVillainPhase;
            GameEvents.OnHindranceActivated  -= OnHindranceActivated;
        }

        // ── Timer update (called from GameManager via Update) ─────────────────

        private void Update()
        {
            if (GameManager.Instance != null && _timerText != null)
            {
                float t = GameManager.Instance.RemainingTime;
                _timerText.text = Mathf.CeilToInt(t).ToString();
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void UpdateScore(int score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString("N0");
        }

        private void UpdateCombo(int combo, float multiplier)
        {
            if (_comboText != null)
            {
                _comboText.text = combo > 1 ? $"x{combo}" : "";
                DOTween.Kill(_comboText.transform);
                _comboText.transform.DOPunchScale(Vector3.one * 0.3f, 0.25f, 5, 0.5f).SetId(_comboText.gameObject);
            }
            if (_multiplierText != null)
                _multiplierText.text = multiplier > 1f ? $"{multiplier:F1}x" : "";
        }

        private void OnAnimalCollected(AnimalSpecies species, AnimalType type, Vector3 worldPos)
        {
            ShowFloatingText("+50", worldPos);
        }

        private void OnLevelWon()
        {
            if (_resultsScreen != null) _resultsScreen.ShowWin(0, 0, false);
        }

        private void OnLevelFailed()
        {
            if (_resultsScreen != null) _resultsScreen.ShowLose(0);
        }

        private void OnTimerWarning()
        {
            if (_timerText == null) return;
            DOTween.Kill(_timerText.transform);
            _timerText.transform.DOScale(1.3f, 0.15f).SetLoops(6, LoopType.Yoyo)
                .SetEase(Ease.OutQuad).SetId(_timerText.gameObject);
            GameEvents.OnSfxRequested?.Invoke(SfxType.TimerWarning);
        }

        private void OnVillainPhase(int current, int total) { /* VillainHUD handles its own display */ }

        private void OnHindranceActivated(Core.Hindrances.HindranceType type)
        {
            if (_save != null && !_save.HasSeenHindrance(type))
            {
                _save.MarkHindranceSeen(type);
                GameManager.Instance?.PauseForHindranceTutorial(4f);
                ShowToast(GetHindranceTip(type), 4f);
            }
        }

        // ── Floating text ─────────────────────────────────────────────────────

        public void ShowFloatingText(string text, Vector3 worldPos)
        {
            if (_floatingTextPrefab == null) return;
            var go = ObjectPooler.Instance?.SpawnFromPool(_floatingTextPrefab, Vector3.zero, Quaternion.identity, _floatingTextContainer);
            if (go == null) return;

            var t = go.GetComponent<Text>();
            if (t != null) t.text = text;

            // Anchor to screen position
            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(worldPos)
                : worldPos;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.position = screenPos;

            // DOTween: float up 80 units + fade out over 1.2s
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            var seq = DOTween.Sequence().SetId(go);
            seq.Join(rt != null ? rt.DOAnchorPosY(rt.anchoredPosition.y + 80f, 1.2f).SetEase(Ease.OutQuad) : null);
            seq.Join(cg.DOFade(0f, 1.2f));
            seq.OnComplete(() => ObjectPooler.Instance?.ReturnToPool(go));
        }

        // ── Toast ─────────────────────────────────────────────────────────────

        private void ShowToast(string message, float duration)
        {
            if (_toastText == null || _toastRoot == null) return;
            _toastText.text = message;
            _toastRoot.SetActive(true);
            DOTween.Kill(_toastRoot);
            DOVirtual.DelayedCall(duration, () => { if (_toastRoot != null) _toastRoot.SetActive(false); }).SetId(_toastRoot);
        }

        private string GetHindranceTip(Core.Hindrances.HindranceType type)
        {
            var registry = Resources.Load<Core.Hindrances.HindranceRegistry>("Hindrances/HindranceRegistry");
            var definition = registry != null ? registry.GetData(type) : null;
            if (definition != null && !string.IsNullOrWhiteSpace(definition.tutorialInstruction))
                return $"{definition.displayName}: {definition.tutorialInstruction}";
            switch (type)
            {
                case Core.Hindrances.HindranceType.Bomb:         return "Bomb! Don't tap it!";
                case Core.Hindrances.HindranceType.AlarmClock:   return "Alarm Clock speeds up spawns!";
                case Core.Hindrances.HindranceType.PoisonVial:   return "Poison! Tapping costs a life!";
                case Core.Hindrances.HindranceType.KnightHelmet: return "Knight Helmet! Tap 3 times!";
                case Core.Hindrances.HindranceType.IceCube:      return "Ice Cube! Swipe to break it!";
                case Core.Hindrances.HindranceType.BubbleShield: return "Bubble! Tap to pop, then collect!";
                case Core.Hindrances.HindranceType.MirrorMode:   return "Mirror Mode! Controls reversed!";
                default: return $"New hindrance: {type}";
            }
        }
    }
}
