// GameHUD — clean bottom bar: goal chips (left) + timer (center). No top bar.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using AnimalFall.Core.Animals;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.Utils;

namespace AnimalFall.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Bottom Bar")]
        [SerializeField] private RectTransform _bottomBar;
        [SerializeField] private Image _bottomBarBg;
        [SerializeField] private Transform _goalsRow;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _timerRing;
        [SerializeField] private GameObject _goalChipPrefab;

        [Header("Style")]
        [SerializeField] private Color _timerNormal = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _timerWarning = new Color(1f, 0.35f, 0.3f, 1f);
        [SerializeField] private Color _barColor = new Color(0.12f, 0.14f, 0.2f, 0.92f);
        [SerializeField] private Color _countColor = Color.white;

        private readonly Dictionary<AnimalSpecies, GoalChip> _chips = new Dictionary<AnimalSpecies, GoalChip>();
        private float _totalTime = 60f;
        private bool _warningActive;
        private bool _goalBound;

        private class GoalChip
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Count;
            public CanvasGroup Group;
            public Image Check;
        }

        public void Setup(LevelData level)
        {
            ImageLibrary.LoadAll();
            _totalTime = level != null ? level.TimeLimit : 60f;
            _warningActive = false;

            if (_bottomBarBg != null) _bottomBarBg.color = _barColor;
            if (_timerText != null)
            {
                _timerText.color = _timerNormal;
                _timerText.text = Mathf.CeilToInt(_totalTime).ToString();
            }
            if (_timerRing != null) _timerRing.fillAmount = 1f;

            EnsureBottomLayout();
            BuildGoalChips(level?.Goal);
        }

        /// <summary>
        /// Goals sit on the left half only; timer is fixed dead-center.
        /// Prevents chips from sliding under the timer.
        /// </summary>
        private void EnsureBottomLayout()
        {
            if (_bottomBar == null) return;

            // Compact floating bar with breathing room from the screen edges.
            _bottomBar.anchorMin = new Vector2(0.04f, 0f);
            _bottomBar.anchorMax = new Vector2(0.96f, 0f);
            _bottomBar.pivot = new Vector2(0.5f, 0f);
            _bottomBar.sizeDelta = new Vector2(0f, 190f);
            _bottomBar.anchoredPosition = new Vector2(0f, 18f);

            // Goals row: left side only (0 → 0.42), never reaches center timer
            if (_goalsRow != null)
            {
                var goalsRt = _goalsRow as RectTransform ?? _goalsRow.GetComponent<RectTransform>();
                if (goalsRt != null)
                {
                    // Detach from any parent layout that forces expand
                    var parentLe = goalsRt.GetComponent<LayoutElement>();
                    if (parentLe != null) Destroy(parentLe);

                    goalsRt.SetParent(_bottomBar, false);
                    goalsRt.anchorMin = new Vector2(0.04f, 0.10f);
                    goalsRt.anchorMax = new Vector2(0.42f, 0.90f);
                    goalsRt.offsetMin = Vector2.zero;
                    goalsRt.offsetMax = Vector2.zero;
                    goalsRt.pivot = new Vector2(0f, 0.5f);
                }

                var hlg = _goalsRow.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null) hlg = _goalsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.spacing = 12f;
                hlg.padding = new RectOffset(8, 8, 4, 4);
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }

            // Timer: fixed absolute center of bottom bar
            if (_timerText != null)
            {
                var timerSlot = _timerText.transform.parent; // TimerInner
                if (timerSlot != null && timerSlot.parent != null)
                {
                    // Prefer TimerSlot (parent of TimerInner)
                    var slot = timerSlot.parent; // TimerSlot
                    var slotRt = slot as RectTransform ?? slot.GetComponent<RectTransform>();
                    if (slotRt != null)
                    {
                        var le = slotRt.GetComponent<LayoutElement>();
                        if (le != null) Destroy(le);

                        slotRt.SetParent(_bottomBar, false);
                        slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                        slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                        slotRt.pivot = new Vector2(0.5f, 0.5f);
                        slotRt.sizeDelta = new Vector2(150f, 150f);
                        slotRt.anchoredPosition = Vector2.zero;
                    }
                }
            }

            // Hide/remove old Content layout and Spacer if present
            var content = _bottomBar.Find("Content");
            if (content != null)
            {
                // Move GoalsRow/Timer out already done; destroy leftover
                // Keep Content only if empty-ish, else disable layout
                var h = content.GetComponent<HorizontalLayoutGroup>();
                if (h != null) h.enabled = false;
                content.gameObject.SetActive(false);
            }
        }

        private void BuildGoalChips(GoalData goal)
        {
            _chips.Clear();
            if (_goalsRow == null) return;

            for (int i = _goalsRow.childCount - 1; i >= 0; i--)
                Destroy(_goalsRow.GetChild(i).gameObject);

            if (goal == null || goal.Targets == null) return;

            for (int i = 0; i < goal.Targets.Length; i++)
            {
                var t = goal.Targets[i];
                if (t.species == AnimalSpecies.None || t.count <= 0) continue;
                CreateChip(t.species, t.count);
            }
        }

        private void CreateChip(AnimalSpecies species, int count)
        {
            // Always build clean runtime chips so layout/font are correct
            var go = BuildRuntimeChip(_goalsRow);
            go.name = $"Goal_{species}";
            go.SetActive(true);

            var chip = new GoalChip
            {
                Root  = go,
                Icon  = go.transform.Find("Icon")?.GetComponent<Image>(),
                Count = go.transform.Find("Count")?.GetComponent<TextMeshProUGUI>(),
                Group = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>(),
                Check = go.transform.Find("Check")?.GetComponent<Image>()
            };

            if (chip.Icon != null)
            {
                chip.Icon.sprite = ImageLibrary.GetAnimalSprite(species);
                chip.Icon.preserveAspect = true;
                chip.Icon.color = Color.white;
            }

            if (chip.Count != null)
            {
                ApplyDefaultFont(chip.Count);
                chip.Count.text = count.ToString();
                chip.Count.color = _countColor;
                chip.Count.ForceMeshUpdate(true);
            }

            if (chip.Check != null) chip.Check.gameObject.SetActive(false);

            _chips[species] = chip;
        }

        private static void ApplyDefaultFont(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            if (tmp.font != null) return;

            // TMP created at runtime often has no font → invisible text
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) tmp.font = font;
        }

        private static GameObject BuildRuntimeChip(Transform parent)
        {
            var go = new GameObject("GoalChip", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96f, 136f);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 96f;
            le.preferredHeight = 136f;
            le.minWidth = 88f;
            le.minHeight = 128f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            // No background — icon + count only

            // Vertical stack: icon on top, count under it (no background)
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -4f);
            iconRt.sizeDelta = new Vector2(78f, 78f);
            var iconImg = icon.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            // Count text under icon
            var count = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            count.transform.SetParent(go.transform, false);
            var countRt = count.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0.5f, 0f);
            countRt.anchorMax = new Vector2(0.5f, 0f);
            countRt.pivot = new Vector2(0.5f, 0f);
            countRt.anchoredPosition = new Vector2(0f, 6f);
            countRt.sizeDelta = new Vector2(96f, 44f);
            var tmp = count.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 38f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            ApplyDefaultFont(tmp);

            // Check mark (hidden until complete)
            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(go.transform, false);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.55f, 0.60f);
            checkRt.anchorMax = new Vector2(0.95f, 0.98f);
            checkRt.offsetMin = checkRt.offsetMax = Vector2.zero;
            var checkImg = check.GetComponent<Image>();
            checkImg.color = new Color(0.3f, 0.95f, 0.45f, 1f);
            checkImg.raycastTarget = false;
            check.SetActive(false);

            return go;
        }

        private void OnEnable()
        {
            BindGoalTracker();
            GameEvents.OnTimerWarning += OnTimerWarning;
        }

        private void OnDisable()
        {
            UnbindGoalTracker();
            GameEvents.OnTimerWarning -= OnTimerWarning;
        }

        private void Start() => BindGoalTracker();

        private void BindGoalTracker()
        {
            if (_goalBound || GoalTracker.Instance == null) return;
            GoalTracker.Instance.OnGoalProgress += OnGoalProgress;
            _goalBound = true;
        }

        private void UnbindGoalTracker()
        {
            if (!_goalBound || GoalTracker.Instance == null) { _goalBound = false; return; }
            GoalTracker.Instance.OnGoalProgress -= OnGoalProgress;
            _goalBound = false;
        }

        private void Update()
        {
            if (GameManager.Instance == null || _timerText == null) return;
            if (GameManager.Instance.State != GameState.Running) return;

            float t = Mathf.Max(0f, GameManager.Instance.RemainingTime);
            _timerText.text = Mathf.CeilToInt(t).ToString();

            if (_timerRing != null && _totalTime > 0.01f)
                _timerRing.fillAmount = Mathf.Clamp01(t / _totalTime);
        }

        private void OnGoalProgress(AnimalSpecies species, int remaining, int target)
        {
            if (!_chips.TryGetValue(species, out var chip) || chip == null) return;

            if (chip.Count != null)
            {
                chip.Count.text = remaining.ToString();
                DOTween.Kill(chip.Count.transform);
                chip.Count.transform.localScale = Vector3.one;
                chip.Count.transform.DOPunchScale(Vector3.one * 0.35f, 0.28f, 6, 0.6f)
                    .SetId(chip.Count.transform);
            }

            if (remaining <= 0)
            {
                if (chip.Count != null) chip.Count.gameObject.SetActive(false);
                if (chip.Check != null)
                {
                    chip.Check.gameObject.SetActive(true);
                    chip.Check.transform.localScale = Vector3.zero;
                    chip.Check.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
                }
                if (chip.Group != null)
                    chip.Group.DOFade(0.55f, 0.35f);
                if (chip.Root != null)
                    chip.Root.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 4, 0.5f);
            }
        }

        private void OnTimerWarning()
        {
            _warningActive = true;
            if (_timerText == null) return;
            _timerText.color = _timerWarning;
            DOTween.Kill(_timerText.transform);
            _timerText.transform.DOScale(1.25f, 0.18f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId(_timerText.transform);
        }

        public void ResetWarning()
        {
            _warningActive = false;
            if (_timerText != null)
            {
                DOTween.Kill(_timerText.transform);
                _timerText.transform.localScale = Vector3.one;
                _timerText.color = _timerNormal;
            }
        }
    }
}
