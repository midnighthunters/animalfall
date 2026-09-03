#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using AnimalFall.Managers;
using AnimalFall.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimalFall.Editor
{
    /// <summary>
    /// Rebuilds only the standard MainScene and GameScene UI with the approved
    /// main_screen_panels spritesheet. MegaShooterScene is intentionally untouched.
    /// </summary>
    public static class PanelUIRedesign
    {
        private const string SheetPath = "panels/main_screen_panels";
        private const string LuckiestPath = "Assets/Fonts/LuckiestGuy-Regular SDF.asset";
        private const string FredokaPath = "Assets/Fonts/Fredoka-Regular SDF.asset";

        private static TMP_FontAsset _headingFont;
        private static TMP_FontAsset _bodyFont;
        private static Sprite[] _sheet;

        [MenuItem("AnimalFall/Redesign Panels (Main + Game + Mega)")]
        public static void RedesignAll()
        {
            LoadStyleAssets();
            BuildMainScene();
            BuildGameScene();
            BuildMegaShooterResultCard();
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            Debug.Log("[PanelUIRedesign] MainScene, GameScene, and MegaShooterScene rebuilt with unified panels.");
        }

        [MenuItem("AnimalFall/Redesign Panels/Main Scene")]
        public static void BuildMainScene()
        {
            LoadStyleAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);

            DestroySceneObject("[UI — MapCanvas]");

            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.18f, 0.08f, 0.38f, 1f);
            }

            var canvas = CreateCanvas("[UI — MapCanvas]", 10, camera);
            var controller = canvas.gameObject.AddComponent<MainScreenController>();

            // Preserve the illustrated game background. All panel chrome below uses only the approved sheet.
            var background = CreateImage("Background", canvas.transform,
                Resources.Load<Sprite>("misc/sample (7)"), Color.white, false);
            Stretch(background.rectTransform);
            background.type = Image.Type.Simple;

            var shade = CreateImage("ReadabilityShade", canvas.transform, null,
                new Color(0.04f, 0.08f, 0.20f, 0.12f), false);
            Stretch(shade.rectTransform);

            BuildMainTopCluster(canvas.transform, controller);
            BuildMainCard(canvas.transform, controller);
            BuildMainBottomBar(canvas.transform, controller);

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("AnimalFall/Redesign Panels/Game Scene")]
        public static void BuildGameScene()
        {
            LoadStyleAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

            DestroySceneObject("[UI — GameCanvas]");
            DestroySceneObject("[UI — StaticCanvas]");
            DestroySceneObject("[UI — DynamicCanvas]");

            var camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.55f, 0.78f, 0.95f, 1f);
            }

            var worldBackground = FindSceneObject("WorldBackground");
            if (worldBackground != null) worldBackground.SetActive(false);

            var canvas = CreateCanvas("[UI — GameCanvas]", 20, camera);
            BuildGameBottomBar(canvas.transform, canvas.gameObject);
            BuildCountdownCard(canvas.transform, canvas.gameObject);
            BuildResultCard(canvas.transform, canvas.gameObject);
            WireGameManager(canvas.gameObject, camera);

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("AnimalFall/Redesign Panels/MegaShooter Scene")]
        public static void BuildMegaShooterResultCard()
        {
            LoadStyleAssets();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MegaShooterScene.unity", OpenSceneMode.Single);
            var canvas = GameObject.Find("MegaShooterCanvas");
            if (canvas == null)
            {
                Debug.LogError("[PanelUIRedesign] MegaShooterCanvas not found in MegaShooterScene.");
                return;
            }

            var existingVictory = canvas.GetComponent<VictoryOverlay>();
            if (existingVictory != null) UnityEngine.Object.DestroyImmediate(existingVictory);
            var existingOverlay = canvas.transform.Find("VictoryOverlay");
            if (existingOverlay != null) UnityEngine.Object.DestroyImmediate(existingOverlay.gameObject);

            BuildResultCard(canvas.transform, canvas.gameObject);

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PanelUIRedesign] Attached identical VictoryOverlay result card to MegaShooterCanvas in MegaShooterScene.");
        }

        private static void BuildMainTopCluster(Transform root, MainScreenController controller)
        {
            var profile = CreateImage("Profile", root, Sprite("profile_background"), Color.white, false);
            SetFixed(profile.rectTransform, new Vector2(142f, 142f), new Vector2(0f, 1f), new Vector2(98f, -102f));

            var initials = CreateText("ProfileInitial", profile.transform, "AF", 42f, Color.white, _headingFont);
            Stretch(initials.rectTransform, 12f, 12f, 12f, 12f);

            var player = CreateText("PlayerName", root, "ANIMAL RESCUER", 31f, Color.white, _headingFont,
                TextAlignmentOptions.Left);
            SetFixed(player.rectTransform, new Vector2(470f, 54f), new Vector2(0f, 1f), new Vector2(340f, -70f));

            var lives = CreateText("LivesText", root, "5 LIVES", 28f,
                new Color(1f, 0.92f, 0.35f, 1f), _bodyFont, TextAlignmentOptions.Left);
            SetFixed(lives.rectTransform, new Vector2(220f, 48f), new Vector2(0f, 1f), new Vector2(300f, -123f));

            var coins = CreateText("CoinsText", root, "0 COINS", 28f,
                new Color(1f, 0.92f, 0.35f, 1f), _bodyFont, TextAlignmentOptions.Left);
            SetFixed(coins.rectTransform, new Vector2(230f, 48f), new Vector2(0f, 1f), new Vector2(530f, -123f));

            var settings = CreateButton("SettingsButton", root, Sprite("settings"), string.Empty,
                new Vector2(112f, 112f), new Vector2(1f, 1f), new Vector2(-82f, -96f), null, 1f);

            SetPrivate(controller, "_avatarImage", profile);
            SetPrivate(controller, "_livesText", lives);
            SetPrivate(controller, "_coinsText", coins);
            SetPrivate(controller, "_backgroundImage", root.Find("Background")?.GetComponent<Image>());
            settings.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private static void BuildMainCard(Transform root, MainScreenController controller)
        {
            var panel = CreateImage("LevelPanel", root, Sprite("main_panel"), Color.white, false);
            SetFixed(panel.rectTransform, new Vector2(760f, 676f), new Vector2(0.5f, 0.5f), new Vector2(0f, 72f));

            var banner = CreateImage("LevelBanner", panel.transform, Sprite("banner"), Color.white, false);
            SetFixed(banner.rectTransform, new Vector2(620f, 155f), new Vector2(0.5f, 1f), new Vector2(0f, 18f));

            var levelTitle = CreateText("LevelTitle", banner.transform, "LEVEL 1", 58f, Color.white, _headingFont);
            Stretch(levelTitle.rectTransform, 36f, 36f, 22f, 28f);

            var kicker = CreateText("Kicker", panel.transform, "READY FOR THE NEXT RESCUE?", 34f,
                new Color(0.10f, 0.38f, 0.76f, 1f), _headingFont);
            SetFixed(kicker.rectTransform, new Vector2(630f, 74f), new Vector2(0.5f, 0.5f), new Vector2(0f, 106f));

            var body = CreateText("Body", panel.transform,
                "Find the animals, beat the timer,\nand bring everyone home.", 29f,
                new Color(0.18f, 0.28f, 0.42f, 1f), _bodyFont);
            SetFixed(body.rectTransform, new Vector2(600f, 120f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f));

            var play = CreateButton("PlayButton", panel.transform, Sprite("level_button"), "PLAY",
                new Vector2(570f, 182f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), _headingFont, 52f);

            SetPrivate(controller, "_playButton", play);
            SetPrivate(controller, "_levelButtonText", levelTitle);
            SetPrivate(controller, "_levelButtonBg", play.GetComponent<Image>());
        }

        private static void BuildMainBottomBar(Transform root, MainScreenController controller)
        {
            var bar = CreateImage("BottomBar", root, Sprite("bottom"), Color.white, false);
            SetFixed(bar.rectTransform, new Vector2(980f, 172f), new Vector2(0.5f, 0f), new Vector2(0f, 26f));

            string[] tabs = { "MAP", "MISSIONS", "PETS", "SHOP" };
            float[] xs = { -335f, -115f, 115f, 335f };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = CreateButton(tabs[i] + "Tab", bar.transform, null, tabs[i],
                    new Vector2(205f, 104f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], -2f), _bodyFont, 25f);
                var img = tab.GetComponent<Image>();
                img.color = i == 0 ? new Color(0.22f, 0.62f, 1f, 0.24f) : Color.clear;
            }

            SetPrivate(controller, "_bottomBarBg", bar);
        }

        private static void BuildGameBottomBar(Transform root, GameObject canvasObject)
        {
            var bar = CreateImage("BottomBar", root, Sprite("bottom"), Color.white, false);
            bar.rectTransform.anchorMin = new Vector2(0.04f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0.96f, 0f);
            bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bar.rectTransform.sizeDelta = new Vector2(0f, 190f);
            bar.rectTransform.anchoredPosition = new Vector2(0f, 18f);

            var goals = CreateRect("GoalsRow", bar.transform);
            goals.anchorMin = new Vector2(0.04f, 0.10f);
            goals.anchorMax = new Vector2(0.42f, 0.90f);
            goals.offsetMin = goals.offsetMax = Vector2.zero;

            var timerSlot = CreateRect("TimerSlot", bar.transform);
            SetFixed(timerSlot, new Vector2(154f, 154f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var timerFace = CreateImage("TimerFace", timerSlot, Sprite("profile_background"), Color.white, false);
            Stretch(timerFace.rectTransform);

            var timeLabel = CreateText("TimeLabel", timerFace.transform, "TIME", 18f,
                new Color(0.80f, 0.94f, 1f, 1f), _bodyFont);
            SetFixed(timeLabel.rectTransform, new Vector2(120f, 32f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f));

            var timerText = CreateText("TimerText", timerFace.transform, "60", 52f, Color.white, _headingFont);
            SetFixed(timerText.rectTransform, new Vector2(124f, 76f), new Vector2(0.5f, 0.5f), new Vector2(0f, -13f));

            // Compact active target summary on the left side of the timer panel.
            var targetSummary = CreateRect("TargetSummary", bar.transform);
            SetFixed(targetSummary, new Vector2(300f, 54f), new Vector2(0f, 1f), new Vector2(130f, -28f));

            var targetIcon = CreateImage("TargetIcon", targetSummary, null, Color.white, false);
            SetFixed(targetIcon.rectTransform, new Vector2(42f, 42f), new Vector2(0f, 0.5f), new Vector2(24f, 0f));
            targetIcon.preserveAspect = true;

            var targetText = CreateText("TargetText", targetSummary, "TARGET", 22f,
                new Color(0.78f, 0.92f, 1f, 1f), _bodyFont, TextAlignmentOptions.Left);
            SetFixed(targetText.rectTransform, new Vector2(228f, 46f), new Vector2(0f, 0.5f), new Vector2(164f, 0f));

            var hud = canvasObject.AddComponent<GameHUD>();
            SetPrivate(hud, "_bottomBar", bar.rectTransform);
            SetPrivate(hud, "_bottomBarBg", bar);
            SetPrivate(hud, "_goalsRow", goals.transform);
            SetPrivate(hud, "_targetIcon", targetIcon);
            SetPrivate(hud, "_targetText", targetText);
            SetPrivate(hud, "_timerText", timerText);
            SetPrivate(hud, "_timerRing", null);
            SetPrivate(hud, "_barColor", Color.white);
            SetPrivate(hud, "_countColor", Color.white);
        }

        private static void BuildCountdownCard(Transform root, GameObject canvasObject)
        {
            var overlay = CreateImage("CountdownOverlay", root, null, new Color(0.02f, 0.08f, 0.20f, 0.34f), true);
            Stretch(overlay.rectTransform);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            var panel = CreateImage("CountdownPanel", overlay.transform, Sprite("main_panel"), Color.white, false);
            SetFixed(panel.rectTransform, new Vector2(590f, 525f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f));

            var banner = CreateImage("CountdownBanner", panel.transform, Sprite("banner"), Color.white, false);
            SetFixed(banner.rectTransform, new Vector2(510f, 126f), new Vector2(0.5f, 1f), new Vector2(0f, 12f));
            var ready = CreateText("ReadyText", banner.transform, "GET READY!", 43f, Color.white, _headingFont);
            Stretch(ready.rectTransform, 30f, 30f, 18f, 22f);

            var number = CreateText("CountdownText", panel.transform, "3", 164f,
                new Color(0.12f, 0.42f, 0.82f, 1f), _headingFont);
            SetFixed(number.rectTransform, new Vector2(420f, 260f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f));

            overlay.gameObject.SetActive(false);
            var countdown = canvasObject.AddComponent<CountdownController>();
            SetPrivate(countdown, "_root", overlay.gameObject);
            SetPrivate(countdown, "_countdownText", number);
            SetPrivate(countdown, "_canvasGroup", group);
        }

        private static void BuildResultCard(Transform root, GameObject canvasObject)
        {
            var overlay = CreateImage("VictoryOverlay", root, null, new Color(0.02f, 0.08f, 0.20f, 0.48f), true);
            Stretch(overlay.rectTransform);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            var panel = CreateImage("ResultPanel", overlay.transform, Sprite("main_panel"), Color.white, false);
            SetFixed(panel.rectTransform, new Vector2(700f, 760f), new Vector2(0.5f, 0.5f), new Vector2(0f, 56f));

            var banner = CreateImage("ResultBanner", panel.transform, Sprite("banner"), Color.white, false);
            SetFixed(banner.rectTransform, new Vector2(590f, 148f), new Vector2(0.5f, 1f), new Vector2(0f, 18f));
            var title = CreateText("ResultTitle", banner.transform, "LEVEL CLEARED!", 44f, Color.white, _headingFont);
            Stretch(title.rectTransform, 28f, 28f, 20f, 24f);

            var subtitle = CreateText("ResultSubtitle", panel.transform, "Every animal is safe", 31f,
                new Color(0.12f, 0.28f, 0.55f, 1f), _bodyFont);
            SetFixed(subtitle.rectTransform, new Vector2(570f, 104f), new Vector2(0.5f, 0.5f), new Vector2(0f, 116f));

            var primary = CreateButton("PrimaryButton", panel.transform, Sprite("level_button"), "CONTINUE",
                new Vector2(530f, 160f), new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), _headingFont, 42f);
            var primaryLabel = primary.transform.Find("Label").GetComponent<TextMeshProUGUI>();

            var home = CreateButton("HomeButton", panel.transform, Sprite("level_button"), "HOME",
                new Vector2(440f, 132f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), _headingFont, 34f);
            home.GetComponent<Image>().color = new Color(0.88f, 0.96f, 1f, 1f);

            overlay.gameObject.SetActive(false);
            var result = canvasObject.AddComponent<VictoryOverlay>();
            SetPrivate(result, "_root", overlay.gameObject);
            SetPrivate(result, "_titleText", title);
            SetPrivate(result, "_subtitleText", subtitle);
            SetPrivate(result, "_canvasGroup", group);
            SetPrivate(result, "_panel", panel.rectTransform);
            SetPrivate(result, "_primaryButton", primary);
            SetPrivate(result, "_primaryButtonLabel", primaryLabel);
            SetPrivate(result, "_homeButton", home);
        }

        private static void WireGameManager(GameObject canvasObject, Camera camera)
        {
            var managers = FindSceneObject("[Managers]");
            GoalTracker tracker = null;
            if (managers != null)
            {
                tracker = managers.GetComponentInChildren<GoalTracker>(true);
                if (tracker == null)
                {
                    var go = new GameObject("GoalTracker");
                    go.transform.SetParent(managers.transform, false);
                    tracker = go.AddComponent<GoalTracker>();
                }
            }

            var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gameManager == null) return;
            SetPrivate(gameManager, "_goalTracker", tracker);
            SetPrivate(gameManager, "_hud", canvasObject.GetComponent<GameHUD>());
            SetPrivate(gameManager, "_countdown", canvasObject.GetComponent<CountdownController>());
            SetPrivate(gameManager, "_victoryOverlay", canvasObject.GetComponent<VictoryOverlay>());
            SetPrivate(gameManager, "_plainBackground", new Color(0.55f, 0.78f, 0.95f, 1f));
            SetPrivate(gameManager, "_camera", camera);
        }

        private static Canvas CreateCanvas(string name, int order, Camera camera)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = camera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = order;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static Button CreateButton(string name, Transform parent, Sprite sprite, string label,
            Vector2 size, Vector2 anchor, Vector2 position, TMP_FontAsset font, float fontSize)
        {
            var image = CreateImage(name, parent, sprite, sprite == null ? Color.clear : Color.white, true);
            SetFixed(image.rectTransform, size, anchor, position);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.9f, 1f, 1f);
            colors.colorMultiplier = 1.1f;
            button.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                var text = CreateText("Label", image.transform, label, fontSize, Color.white, font ?? _headingFont);
                Stretch(text.rectTransform, 28f, 28f, 18f, 25f);
            }
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size,
            Color color, TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            if (font == _headingFont)
            {
                text.outlineWidth = 0.12f;
                text.outlineColor = new Color32(17, 55, 126, 210);
            }
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void SetFixed(RectTransform rt, Vector2 size, Vector2 anchor, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor.x < 0.01f ? new Vector2(0f, anchor.y) :
                anchor.x > 0.99f ? new Vector2(1f, anchor.y) : new Vector2(0.5f, anchor.y);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void LoadStyleAssets()
        {
            _sheet ??= Resources.LoadAll<Sprite>(SheetPath);
            _headingFont ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LuckiestPath);
            _bodyFont ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FredokaPath);
            if (_sheet == null || _sheet.Length == 0)
                throw new InvalidOperationException("main_screen_panels spritesheet could not be loaded.");
            if (_headingFont == null || _bodyFont == null)
                throw new InvalidOperationException("Required fonts in Assets/Fonts could not be loaded.");
        }

        private static Sprite Sprite(string name)
        {
            var sprite = _sheet.FirstOrDefault(s => s != null && s.name == name);
            if (sprite == null) throw new InvalidOperationException($"Sprite '{name}' is missing from {SheetPath}.");
            return sprite;
        }

        private static GameObject FindSceneObject(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => go != null && go.name == name && go.scene == SceneManager.GetActiveScene());
        }

        private static void DestroySceneObject(string name)
        {
            var go = FindSceneObject(name);
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EditorUtility.SetDirty(go);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                Debug.LogWarning($"[PanelUIRedesign] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            field.SetValue(target, value);
            if (target is UnityEngine.Object unityObject) EditorUtility.SetDirty(unityObject);
        }
    }
}
#endif

