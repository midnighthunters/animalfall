// GameHUD — reference-style bottom HUD built from Resources/icons/bottom_panel.
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
    [ExecuteAlways]
    public class GameHUD : MonoBehaviour
    {
        [Header("Bottom Bar")]
        [SerializeField] private RectTransform _bottomBar;
        [SerializeField] private Image _bottomBarBg;
        [SerializeField] private Transform _goalsRow;
        [SerializeField] private Image _targetIcon;
        [SerializeField] private TextMeshProUGUI _targetText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _timerRing;
        [SerializeField] private GameObject _goalChipPrefab;

        [Header("Style")]
        [SerializeField] private Color _timerNormal = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _timerWarning = new Color(1f, 0.35f, 0.3f, 1f);
        [SerializeField] private Color _barColor = new Color(0.12f, 0.14f, 0.2f, 0.92f);
        [SerializeField] private Color _countColor = Color.white;

        [Header("Boosters")]
        [SerializeField] private BoosterManager _boosterManager;

        private readonly Dictionary<AnimalSpecies, GoalChip> _chips = new Dictionary<AnimalSpecies, GoalChip>();
        private readonly Dictionary<BoosterType, BoosterButton> _boosterButtons = new Dictionary<BoosterType, BoosterButton>();
        private float _totalTime = 60f;
        private bool _warningActive;
        private bool _goalBound;
        private GoalData _summaryGoal;

        private const string BottomPanelResource = "icons/bottom_panel";
        private static Texture2D _bottomPanelTexture;
        private static readonly Dictionary<string, Sprite> _bottomPanelSprites =
            new Dictionary<string, Sprite>();
        private int _displayedTimerSecond = int.MinValue;

        private class GoalChip
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Count;
            public CanvasGroup Group;
            public Image Check;
        }

        private class BoosterButton
        {
            public GameObject Root;
            public Button Button;
            public Image Icon;
            public Image Frame;
            public TextMeshProUGUI CountText;
            public BoosterType Type;
        }

        public void Setup(LevelData level)
        {
            ImageLibrary.LoadAll();
            _totalTime = level != null ? level.TimeLimit : 60f;
            _displayedTimerSecond = int.MinValue;
            _warningActive = false;
            _summaryGoal = level != null ? level.Goal : null;
            _countColor = new Color(0.25f, 0.10f, 0.48f, 1f);

            BuildReferenceBottomPanel();

            if (_bottomBarBg != null)
            {
                _bottomBarBg.sprite = null;
                _bottomBarBg.color = Color.clear;
            }
            if (_timerText != null)
            {
                _timerText.color = _timerNormal;
                _timerText.text = Mathf.CeilToInt(_totalTime).ToString();
            }
            if (_timerRing != null) _timerRing.fillAmount = 1f;

            EnsureBottomLayout();
            BuildGoalChips(_summaryGoal);
            UpdateTargetSummary();
        }

        /// <summary>
        /// Goals sit on the left half only; timer is fixed dead-center.
        /// Prevents chips from sliding under the timer.
        /// </summary>
        private void EnsureBottomLayout()
        {
            if (_bottomBar == null) return;

            // Match the reference composition: goal card, central counter and
            // three circular boosters spanning the full lower edge.
            _bottomBar.anchorMin = new Vector2(0f, 0f);
            _bottomBar.anchorMax = new Vector2(1f, 0f);
            _bottomBar.pivot = new Vector2(0.5f, 0f);
            _bottomBar.sizeDelta = new Vector2(0f, 184f);
            _bottomBar.anchoredPosition = new Vector2(0f, 12f);

            // Goal chips live inside the cream area of the purple goal card.
            if (_goalsRow != null)
            {
                var goalsRt = _goalsRow as RectTransform ?? _goalsRow.GetComponent<RectTransform>();
                if (goalsRt != null)
                {
                    var parentLe = goalsRt.GetComponent<LayoutElement>();
                    if (parentLe != null) SafeDestroy(parentLe);

                    Transform goalCard = _bottomBar.Find("GoalCard");
                    goalsRt.SetParent(goalCard != null ? goalCard : _bottomBar, false);
                    goalsRt.anchorMin = new Vector2(0.035f, 0.04f);
                    goalsRt.anchorMax = new Vector2(0.965f, 0.73f);
                    goalsRt.offsetMin = Vector2.zero;
                    goalsRt.offsetMax = Vector2.zero;
                    goalsRt.pivot = new Vector2(0.5f, 0.5f);
                }

                var hlg = _goalsRow.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null) hlg = _goalsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 4f;
                hlg.padding = new RectOffset(4, 4, 0, 0);
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }

            // Counter sits between the goal card and booster cluster.
            if (_timerText != null)
            {
                var timerSlot = _timerText.transform.parent;
                if (timerSlot != null && timerSlot.parent != null)
                {
                    var slot = timerSlot.parent;
                    var slotRt = slot as RectTransform ?? slot.GetComponent<RectTransform>();
                    if (slotRt != null)
                    {
                        var le = slotRt.GetComponent<LayoutElement>();
                        if (le != null) SafeDestroy(le);

                        slotRt.SetParent(_bottomBar, false);
                        slotRt.anchorMin = new Vector2(0.5f, 0f);
                        slotRt.anchorMax = new Vector2(0.5f, 0f);
                        slotRt.pivot = new Vector2(0.5f, 0.5f);
                        slotRt.sizeDelta = new Vector2(205f, 124f);
                        slotRt.anchoredPosition = new Vector2(20f, 76f);
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

        private void BuildReferenceBottomPanel()
        {
            if (_bottomBar == null) return;

            _bottomPanelTexture = _bottomPanelTexture != null
                ? _bottomPanelTexture
                : Resources.Load<Texture2D>(BottomPanelResource);

            if (_bottomPanelTexture == null)
            {
                Debug.LogWarning($"[GameHUD] Missing spritesheet Resources/{BottomPanelResource}.png");
                return;
            }

            // Coordinates use Unity's bottom-left texture origin.
            Sprite goalPanel = AtlasSprite("goal_panel", new Rect(70f, 788f, 770f, 290f));
            Sprite counterPanel = AtlasSprite("counter_panel", new Rect(855f, 786f, 535f, 292f));
            Sprite boosterFrame = AtlasSprite("booster_frame", new Rect(290f, 514f, 280f, 280f));

            Image goalCard = EnsureImage("GoalCard", _bottomBar, goalPanel);
            SetFixed(goalCard.rectTransform, new Vector2(430f, 162f), new Vector2(0f, 0f),
                new Vector2(18f, 10f), new Vector2(0f, 0f));
            goalCard.preserveAspect = false;

            TextMeshProUGUI goalLabel = EnsureText("GoalLabel", goalCard.transform);
            SetFixed(goalLabel.rectTransform, new Vector2(360f, 42f), new Vector2(0.5f, 1f),
                new Vector2(0f, -2f), new Vector2(0.5f, 1f));
            goalLabel.text = "GOAL";
            goalLabel.fontSize = 27f;
            goalLabel.fontStyle = FontStyles.Bold;
            goalLabel.alignment = TextAlignmentOptions.Center;
            goalLabel.color = Color.white;
            goalLabel.raycastTarget = false;

            if (_targetIcon != null && _targetIcon.transform.parent != null)
                _targetIcon.transform.parent.gameObject.SetActive(false);
            if (_targetText != null && _targetText.transform.parent != null)
                _targetText.transform.parent.gameObject.SetActive(false);

            if (_timerText != null)
            {
                Transform timerFaceTransform = _timerText.transform.parent;
                Image timerFace = timerFaceTransform != null
                    ? timerFaceTransform.GetComponent<Image>()
                    : null;
                if (timerFace != null)
                {
                    timerFace.sprite = counterPanel;
                    timerFace.color = Color.white;
                    timerFace.type = Image.Type.Simple;
                    timerFace.preserveAspect = false;
                }

                Transform timeLabel = timerFaceTransform != null ? timerFaceTransform.Find("TimeLabel") : null;
                if (timeLabel != null) timeLabel.gameObject.SetActive(false);

                RectTransform textRt = _timerText.rectTransform;
                textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 0.5f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.sizeDelta = new Vector2(170f, 96f);
                textRt.anchoredPosition = new Vector2(0f, -2f);
                _timerText.fontSize = 72f;
                _timerText.fontStyle = FontStyles.Bold;
                _timerText.alignment = TextAlignmentOptions.Center;
                _timerText.color = Color.white;

                Shadow shadow = _timerText.GetComponent<Shadow>();
                if (shadow == null) shadow = _timerText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.18f, 0.03f, 0.34f, 0.72f);
                shadow.effectDistance = new Vector2(0f, -4f);
            }

            BuildBooster("BoosterBomb", BoosterType.Bomb, -330f, boosterFrame,
                AtlasSprite("bomb", new Rect(324f, 0f, 267f, 259f)), 80f);
            BuildBooster("BoosterRainbow", BoosterType.Rainbow, -200f, boosterFrame,
                AtlasSprite("rainbow", new Rect(611f, 15f, 247f, 232f)), 82f);
            BuildBooster("BoosterRocket", BoosterType.Rocket, -70f, boosterFrame,
                AtlasSprite("rocket", new Rect(881f, 17f, 249f, 230f)), 84f);
        }

        private void BuildBooster(string name, BoosterType type, float x, Sprite frame, Sprite icon, float iconSize)
        {
            Image frameImage = EnsureImage(name, _bottomBar, frame);
            SetFixed(frameImage.rectTransform, new Vector2(120f, 120f), new Vector2(1f, 0f),
                new Vector2(x, 16f), new Vector2(0.5f, 0f));
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = true; // Enable raycasting for button

            // Add Button component
            Button button = frameImage.gameObject.GetComponent<Button>();
            if (button == null) button = frameImage.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.7f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = new Color(1f, 1f, 0.5f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            // Wire up click event
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnBoosterClicked(type));

            // Icon
            Image iconImage = EnsureImage("Icon", frameImage.transform, icon);
            SetFixed(iconImage.rectTransform, new Vector2(iconSize, iconSize), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(0.5f, 0.5f));
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Count text (positioned at bottom-right corner)
            TextMeshProUGUI countText = EnsureText("CountText", frameImage.transform);
            SetFixed(countText.rectTransform, new Vector2(50f, 40f), new Vector2(1f, 0f),
                new Vector2(-5f, 5f), new Vector2(1f, 0f));
            countText.fontSize = 28;
            countText.fontStyle = FontStyles.Bold;
            countText.color = Color.white;
            countText.alignment = TextAlignmentOptions.BottomRight;
            countText.text = "3";
            
            // Add outline for better visibility
            var outline = countText.gameObject.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null) outline = countText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Store reference
            var boosterBtn = new BoosterButton
            {
                Root = frameImage.gameObject,
                Button = button,
                Frame = frameImage,
                Icon = iconImage,
                CountText = countText,
                Type = type
            };
            _boosterButtons[type] = boosterBtn;
        }

        private void OnBoosterClicked(BoosterType type)
        {
            if (_boosterManager == null)
            {
                Debug.LogWarning("[GameHUD] BoosterManager reference not set!");
                return;
            }

            _boosterManager.SelectBooster(type);
        }

        /// <summary>
        /// Update booster count display and button interactability.
        /// </summary>
        public void UpdateBoosterCount(BoosterType type, int count)
        {
            if (!_boosterButtons.TryGetValue(type, out var btn)) return;

            btn.CountText.text = count.ToString();
            btn.Button.interactable = count > 0;

            // Fade out if count is 0
            if (count <= 0)
            {
                btn.Frame.color = new Color(1f, 1f, 1f, 0.5f);
                btn.Icon.color = new Color(1f, 1f, 1f, 0.5f);
            }
            else
            {
                btn.Frame.color = Color.white;
                btn.Icon.color = Color.white;
            }
        }

        /// <summary>
        /// Highlight the selected booster.
        /// </summary>
        public void HighlightBooster(BoosterType type)
        {
            foreach (var kvp in _boosterButtons)
            {
                if (kvp.Key == type)
                {
                    // Highlight selected
                    kvp.Value.Frame.DOKill();
                    kvp.Value.Frame.transform.DOScale(1.15f, 0.2f).SetEase(Ease.OutBack);
                    kvp.Value.Frame.DOColor(new Color(1f, 1f, 0.5f, 1f), 0.2f);
                }
                else
                {
                    // Dim others
                    kvp.Value.Frame.DOKill();
                    kvp.Value.Frame.transform.DOScale(1f, 0.2f);
                    kvp.Value.Frame.DOColor(new Color(0.6f, 0.6f, 0.6f, 1f), 0.2f);
                }
            }
        }

        /// <summary>
        /// Remove highlight from all boosters.
        /// </summary>
        public void ClearBoosterHighlight()
        {
            foreach (var kvp in _boosterButtons)
            {
                kvp.Value.Frame.DOKill();
                kvp.Value.Frame.transform.DOScale(1f, 0.2f);
                kvp.Value.Frame.DOColor(Color.white, 0.2f);
            }
        }

        private void OnEnable()
        {
            if (_boosterManager != null)
            {
                _boosterManager.OnBoosterCountChanged += UpdateBoosterCount;
                _boosterManager.OnBoosterSelected += HighlightBooster;
                _boosterManager.OnBoosterDeselected += ClearBoosterHighlight;
            }
        }

        private void OnDisable()
        {
            if (_boosterManager != null)
            {
                _boosterManager.OnBoosterCountChanged -= UpdateBoosterCount;
                _boosterManager.OnBoosterSelected -= HighlightBooster;
                _boosterManager.OnBoosterDeselected -= ClearBoosterHighlight;
            }
        }

        private static Image EnsureImage(string name, Transform parent, Sprite sprite)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null) go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            if (image == null) image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI EnsureText(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            if (existing == null) go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            if (text == null) text = go.AddComponent<TextMeshProUGUI>();
            ApplyDefaultFont(text);
            return text;
        }

        private static void SetFixed(RectTransform rt, Vector2 size, Vector2 anchor,
            Vector2 position, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            rt.localScale = Vector3.one;
        }

        private static Sprite AtlasSprite(string key, Rect rect)
        {
            if (_bottomPanelSprites.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Sprite sprite = Sprite.Create(_bottomPanelTexture, rect, new Vector2(0.5f, 0.5f), 100f,
                0u, SpriteMeshType.FullRect);
            sprite.name = "bottom_panel_" + key;
            _bottomPanelSprites[key] = sprite;
            return sprite;
        }

        private void BuildGoalChips(GoalData goal)
        {
            _chips.Clear();
            if (_goalsRow == null) return;

            for (int i = _goalsRow.childCount - 1; i >= 0; i--)
                SafeDestroy(_goalsRow.GetChild(i).gameObject);

            if (goal == null || goal.Targets == null) return;

            for (int i = 0; i < goal.Targets.Length; i++)
            {
                var t = goal.Targets[i];
                if (t.species == AnimalSpecies.None || t.count <= 0) continue;
                CreateChip(t.species, t.count);
            }
        }

private void UpdateTargetSummary()
        {
            if (_targetIcon == null && _targetText == null) return;

            AnimalSpecies displayedSpecies = AnimalSpecies.None;
            int remaining = 0;
            int target = 0;
            var targets = _summaryGoal != null ? _summaryGoal.Targets : null;
            GoalTracker tracker = GoalTracker.Instance;

            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var goalTarget = targets[i];
                    if (goalTarget.species == AnimalSpecies.None || goalTarget.count <= 0) continue;

                    int goalRemaining = tracker != null
                        ? tracker.GetRemaining(goalTarget.species)
                        : goalTarget.count;
                    if (goalRemaining <= 0) continue;

                    displayedSpecies = goalTarget.species;
                    remaining = goalRemaining;
                    target = tracker != null ? tracker.GetTarget(displayedSpecies) : goalTarget.count;
                    if (target <= 0) target = goalTarget.count;
                    break;
                }
            }

            bool hasTarget = displayedSpecies != AnimalSpecies.None;
            if (_targetIcon != null)
            {
                _targetIcon.gameObject.SetActive(hasTarget);
                if (hasTarget)
                {
                    _targetIcon.sprite = ImageLibrary.GetAnimalSprite(displayedSpecies);
                    _targetIcon.preserveAspect = true;
                    _targetIcon.color = Color.white;
                }
            }

            if (_targetText != null)
            {
                _targetText.gameObject.SetActive(hasTarget);
                if (hasTarget)
                    _targetText.text = $"{displayedSpecies.ToString().ToUpperInvariant()} {remaining}/{target}";
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
                chip.Icon.sprite = GetGoalPortrait(species) ?? ImageLibrary.GetAnimalSprite(species);
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

        private static Sprite GetGoalPortrait(AnimalSpecies species)
        {
            if (_bottomPanelTexture == null) return null;

            switch (species)
            {
                case AnimalSpecies.Penguin:
                    return AtlasSprite("portrait_penguin", new Rect(78f, 242f, 279f, 343f));
                case AnimalSpecies.Pig:
                    return AtlasSprite("portrait_pig", new Rect(397f, 242f, 306f, 300f));
                case AnimalSpecies.Dog:
                    return AtlasSprite("portrait_dog", new Rect(1056f, 238f, 316f, 301f));
                case AnimalSpecies.Raccoon:
                    return AtlasSprite("portrait_raccoon", new Rect(720f, 238f, 310f, 315f));
                default:
                    return null;
            }
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
            rt.sizeDelta = new Vector2(92f, 106f);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 92f;
            le.preferredHeight = 106f;
            le.minWidth = 76f;
            le.minHeight = 98f;
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
            iconRt.anchoredPosition = new Vector2(0f, 2f);
            iconRt.sizeDelta = new Vector2(68f, 72f);
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
            countRt.anchoredPosition = Vector2.zero;
            countRt.sizeDelta = new Vector2(92f, 38f);
            var tmp = count.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 31f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            ApplyDefaultFont(tmp);

            return go;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                // Editor preview: build the same styled panel players see at runtime.
                BuildEditorPreview();
                return;
            }

            BindGoalTracker();
            GameEvents.OnTimerWarning += OnTimerWarning;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            UnbindGoalTracker();
            GameEvents.OnTimerWarning -= OnTimerWarning;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            BindGoalTracker();
        }

        /// <summary>
        /// Rebuilds the reference bottom panel in the editor (no play mode) so the HUD
        /// shown in edit mode matches what appears during play. Pulls a representative
        /// goal from Level 1 for the chip preview.
        /// </summary>
        private void BuildEditorPreview()
        {
            if (Application.isPlaying || _bottomBar == null) return;

            ImageLibrary.LoadAll();
            _countColor = new Color(0.25f, 0.10f, 0.48f, 1f);
            _totalTime = 60f;
            _summaryGoal = FindPreviewGoal();

            BuildReferenceBottomPanel();

            if (_bottomBarBg != null)
            {
                _bottomBarBg.sprite = null;
                _bottomBarBg.color = Color.clear;
            }
            if (_timerText != null)
            {
                _timerText.color = _timerNormal;
                _timerText.text = Mathf.CeilToInt(_totalTime).ToString();
            }
            if (_timerRing != null) _timerRing.fillAmount = 1f;

            EnsureBottomLayout();
            BuildGoalChips(_summaryGoal);
            UpdateTargetSummary();
        }

        private GoalData FindPreviewGoal()
        {
#if UNITY_EDITOR
            var level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(
                "Assets/Levels/LevelData/Level_01.asset");
            if (level != null) return level.Goal;
#endif
            return _summaryGoal;
        }

        /// <summary>Destroy that works in both play mode and the editor preview.</summary>
        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

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
            int second = Mathf.CeilToInt(t);
            if (second != _displayedTimerSecond)
            {
                _displayedTimerSecond = second;
                _timerText.text = second.ToString();
            }

            if (_timerRing != null && _totalTime > 0.01f)
                _timerRing.fillAmount = Mathf.Clamp01(t / _totalTime);
        }

private void OnGoalProgress(AnimalSpecies species, int remaining, int target)
        {
            if (_chips.TryGetValue(species, out GoalChip chip) && chip != null)
            {
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
                    if (chip.Count != null)
                    {
                        chip.Count.gameObject.SetActive(true);
                        chip.Count.text = "✓";
                        chip.Count.color = new Color(0.18f, 0.66f, 0.25f, 1f);
                    }
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

            UpdateTargetSummary();
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
