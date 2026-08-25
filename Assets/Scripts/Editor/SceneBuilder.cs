#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace AnimalFall.Editor
{
    public static class SceneBuilder
    {
        // ── Helpers ────────────────────────────────────────────────────────
        static GameObject Find(string name)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
                if (g.name == name && g.scene == SceneManager.GetActiveScene())
                    return g;
            return null;
        }

        static T EnsureComp<T>(GameObject go) where T : Component
            => go.GetComponent<T>() ?? go.AddComponent<T>();

        static void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = Vector2.one * 0.5f;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void AnchorTop(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0, -height); rt.offsetMax = Vector2.zero;
        }

        static void AnchorBottom(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0, height);
        }

        static Image Img(GameObject go, Sprite spr, bool raycast = false,
                         Image.Type t = Image.Type.Sliced)
        {
            var img = EnsureComp<Image>(go);
            if (spr != null) { img.sprite = spr; img.type = t; }
            img.raycastTarget = raycast;
            return img;
        }

        static Text Txt(GameObject go, string text, int size, Color col,
                        FontStyle fs = FontStyle.Bold,
                        TextAnchor ta = TextAnchor.MiddleCenter)
        {
            var t = EnsureComp<Text>(go);
            t.text = text; t.fontSize = size; t.color = col;
            t.fontStyle = fs; t.alignment = ta; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static GameObject MakeChild(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void SetupCanvas(GameObject go, int sortOrder)
        {
            var canvas = EnsureComp<Canvas>(go);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = EnsureComp<CanvasScaler>(go);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComp<GraphicRaycaster>(go);
        }

        // ── Color palette ──────────────────────────────────────────────────
        static readonly Color C_WARM_BG    = new Color(0.95f, 0.91f, 0.55f, 1f);
        static readonly Color C_YELLOW     = new Color(1f, 0.90f, 0.10f, 1f);
        static readonly Color C_WHITE      = Color.white;
        static readonly Color C_DARK_BG    = new Color(0.12f, 0.07f, 0.30f, 0.95f);
        static readonly Color C_PANEL_BG   = new Color(0.18f, 0.10f, 0.38f, 0.96f);
        static readonly Color C_TIMER_RED  = new Color(1f, 0.28f, 0.28f, 1f);
        static readonly Color C_COMBO_ORG  = new Color(1f, 0.60f, 0.08f, 1f);
        static readonly Color C_GREEN      = new Color(0.20f, 0.80f, 0.35f, 1f);
        static readonly Color C_BLUE_SLOT  = new Color(0.30f, 0.60f, 1f, 0.92f);
        static readonly Color C_PURPLE_SLOT= new Color(0.85f, 0.35f, 1f, 0.92f);
        static readonly Color C_TEAL_SLOT  = new Color(0.25f, 0.85f, 0.65f, 0.92f);
        static readonly Color C_SHADOW     = new Color(0f, 0f, 0f, 0.45f);
        static readonly Color C_HP_GREEN   = new Color(0.20f, 0.88f, 0.30f, 1f);

        // ══════════════════════════════════════════════════════════════════
        // GAME SCENE BUILDER
        // ══════════════════════════════════════════════════════════════════
        [MenuItem("AnimalFall/Build GameScene UI")]
        public static void BuildGameSceneUI()
        {
            // Load sprites
            Sprite panel      = Resources.Load<Sprite>("panels/panel");
            Sprite panel2     = Resources.Load<Sprite>("panels/panel2");
            Sprite mainTop    = Resources.Load<Sprite>("panels/main_top");
            Sprite mainBot    = Resources.Load<Sprite>("panels/main_bottom");
            Sprite tileBack   = Resources.Load<Sprite>("panels/tile_back");
            Sprite topGameBck = Resources.Load<Sprite>("panels/top_game_back");
            Sprite lightPnl   = Resources.Load<Sprite>("panels/light_panel");
            Sprite redBtns    = Resources.Load<Sprite>("panels/red_buttons");
            Sprite exitSpr    = Resources.Load<Sprite>("panels/exit");
            Sprite clockSpr   = Resources.Load<Sprite>("icons/clock");
            Sprite coinSpr    = Resources.Load<Sprite>("icons/coinstack");
            Sprite blueBtnSpr = Resources.Load<Sprite>("panels/blue_button");
            Sprite panelTopSpr= Resources.Load<Sprite>("panels/panel_top");

            // Camera
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = C_WARM_BG;

            BuildStaticCanvas(tileBack, topGameBck, mainBot, lightPnl, exitSpr);
            BuildDynamicCanvas(panel, panel2, redBtns, clockSpr, coinSpr, lightPnl, panelTopSpr);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] GameScene UI built and saved.");
        }

        static void BuildStaticCanvas(Sprite tileBack, Sprite topGameBck,
                                      Sprite mainBot, Sprite lightPnl, Sprite exitSpr)
        {
            // ── ChapterBackground ─────────────────────────────────────────
            var bgGO = Find("ChapterBackground");
            if (bgGO != null) {
                FullStretch(bgGO.GetComponent<RectTransform>());
                var img = Img(bgGO, tileBack, false, Image.Type.Tiled);
                img.color = C_WARM_BG;
            }

            // ── TopBar ────────────────────────────────────────────────────
            var topBar = Find("TopBar");
            if (topBar != null) {
                AnchorTop(topBar.GetComponent<RectTransform>(), 168);
                var img = Img(topBar, topGameBck, false, Image.Type.Sliced);
                img.color = C_WHITE;
            }

            // Title text
            var titleGO = Find("ChapterNameText");
            if (titleGO != null) {
                var rt = titleGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.04f, 0.05f);
                rt.anchorMax = new Vector2(0.78f, 0.95f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                Txt(titleGO, "ANIMAL FALL", 58, C_YELLOW, FontStyle.Bold, TextAnchor.MiddleLeft);
            }

            // Settings button
            var settGO = Find("SettingsButton");
            if (settGO != null) {
                var rt = settGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.82f, 0.10f);
                rt.anchorMax = new Vector2(0.96f, 0.90f);
                rt.pivot = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = Img(settGO, exitSpr, true, Image.Type.Simple);
                img.color = C_WHITE; img.preserveAspect = true;
            }

            // ── BottomBar ─────────────────────────────────────────────────
            var botBar = Find("BottomBar");
            if (botBar != null) {
                AnchorBottom(botBar.GetComponent<RectTransform>(), 188);
                var img = Img(botBar, mainBot, false, Image.Type.Sliced);
                img.color = C_WHITE;
                var hlg = EnsureComp<HorizontalLayoutGroup>(botBar);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 40; hlg.padding = new RectOffset(50, 50, 24, 18);
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            }

            // Power-up slots
            string[] slots = { "PowerUpSlot_0", "PowerUpSlot_1", "PowerUpSlot_2" };
            Color[] slotColors = { C_BLUE_SLOT, C_PURPLE_SLOT, C_TEAL_SLOT };
            string[] slotIcons = { "⚡", "🧲", "👆" };
            for (int i = 0; i < slots.Length; i++) {
                var sg = Find(slots[i]);
                if (sg == null) continue;
                var img = Img(sg, lightPnl, true, Image.Type.Sliced);
                img.color = slotColors[i];
                var le = EnsureComp<LayoutElement>(sg);
                le.preferredWidth = 120; le.preferredHeight = 120;
                if (sg.GetComponentInChildren<Text>() == null) {
                    var ig = new GameObject("Icon"); ig.transform.SetParent(sg.transform, false);
                    var rt = ig.AddComponent<RectTransform>();
                    FullStretch(rt);
                    Txt(ig, slotIcons[i], 54, C_WHITE);
                }
            }
        }

        static void BuildDynamicCanvas(Sprite panel, Sprite panel2, Sprite redBtns,
                                       Sprite clockSpr, Sprite coinSpr,
                                       Sprite lightPnl, Sprite panelTopSpr)
        {
            // ── TimerDisplay ──────────────────────────────────────────────
            var timerDisp = Find("TimerDisplay");
            if (timerDisp != null) {
                var rt = timerDisp.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.3f, 0.88f);
                rt.anchorMax = new Vector2(0.7f, 0.97f);
                rt.pivot = new Vector2(0.5f, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = Img(timerDisp, panelTopSpr, false, Image.Type.Sliced);
                img.color = C_DARK_BG;
                var hlg = EnsureComp<HorizontalLayoutGroup>(timerDisp);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 14; hlg.padding = new RectOffset(28, 28, 14, 14);
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            }

            // Clock icon
            var clockGO = Find("ClockIcon");
            if (clockGO != null) {
                var img = Img(clockGO, clockSpr, false, Image.Type.Simple);
                img.color = C_WHITE; img.preserveAspect = true;
                var le = EnsureComp<LayoutElement>(clockGO);
                le.preferredWidth = 64; le.preferredHeight = 64;
            }

            // Timer text
            var timerTxt = Find("TimerText");
            if (timerTxt != null) {
                var le = EnsureComp<LayoutElement>(timerTxt);
                le.preferredWidth = 130; le.preferredHeight = 64;
                Txt(timerTxt, "60", 68, C_WHITE, FontStyle.Bold, TextAnchor.MiddleCenter);
            }

            // ── ScoreDisplay ──────────────────────────────────────────────
            var scoreGO = Find("ScoreDisplay");
            if (scoreGO != null) {
                var rt = scoreGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.0f, 0.88f);
                rt.anchorMax = new Vector2(0.29f, 0.97f);
                rt.pivot = new Vector2(0f, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = Img(scoreGO, panelTopSpr, false, Image.Type.Sliced);
                img.color = C_DARK_BG;
                var txt = scoreGO.GetComponentInChildren<Text>();
                if (txt == null) {
                    var tg = new GameObject("Text"); tg.transform.SetParent(scoreGO.transform, false);
                    var tgRT = tg.AddComponent<RectTransform>(); FullStretch(tgRT);
                    txt = Txt(tg, "0", 52, C_WHITE, FontStyle.Bold);
                } else {
                    txt.text = "0"; txt.fontSize = 52; txt.color = C_WHITE; txt.fontStyle = FontStyle.Bold;
                }
            }

            // ── ComboDisplay ──────────────────────────────────────────────
            var comboGO = Find("ComboDisplay");
            if (comboGO != null) {
                var rt = comboGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.71f, 0.88f);
                rt.anchorMax = new Vector2(1.0f, 0.97f);
                rt.pivot = new Vector2(1f, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = Img(comboGO, panelTopSpr, false, Image.Type.Sliced);
                img.color = new Color(0.6f, 0.15f, 0.85f, 0.92f);
                var txt = comboGO.GetComponentInChildren<Text>();
                if (txt == null) {
                    var tg = new GameObject("Text"); tg.transform.SetParent(comboGO.transform, false);
                    var tgRT = tg.AddComponent<RectTransform>(); FullStretch(tgRT);
                    Txt(tg, "COMBO x1", 42, C_COMBO_ORG, FontStyle.Bold);
                } else {
                    txt.text = ""; txt.fontSize = 42; txt.color = C_COMBO_ORG;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // RESULTS / VILLAIN / PROGRESS / INTRO / COUNTDOWN / TOAST
        // ══════════════════════════════════════════════════════════════════
        [MenuItem("AnimalFall/Build GameScene UI Part2")]
        public static void BuildGameSceneUIPart2()
        {
            Sprite panel      = Resources.Load<Sprite>("panels/panel");
            Sprite panel2     = Resources.Load<Sprite>("panels/panel2");
            Sprite redBtns    = Resources.Load<Sprite>("panels/red_buttons");
            Sprite lightPnl   = Resources.Load<Sprite>("panels/light_panel");
            Sprite blueBtnSpr = Resources.Load<Sprite>("panels/blue_button");
            Sprite panelTopSpr= Resources.Load<Sprite>("panels/panel_top");
            Sprite coinSpr    = Resources.Load<Sprite>("icons/coinstack");

            BuildGoalPanel(lightPnl, panelTopSpr);
            BuildProgressBar(lightPnl);
            BuildVillainHUD(lightPnl);
            BuildLevelIntro(panel, lightPnl);
            BuildCountdown(lightPnl);
            BuildResultsPanel(panel, panel2, redBtns, coinSpr, blueBtnSpr);
            BuildToast(lightPnl);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Part 2 built and saved.");
        }

        static void BuildGoalPanel(Sprite lightPnl, Sprite panelTopSpr)
        {
            var goalGO = Find("GoalPanel");
            if (goalGO == null) return;
            var rt = goalGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.60f);
            rt.anchorMax = new Vector2(0f, 0.87f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(16, 0); rt.offsetMax = new Vector2(180, 0);
            var img = Img(goalGO, panelTopSpr, false, Image.Type.Sliced);
            img.color = C_DARK_BG;
            var vlg = EnsureComp<VerticalLayoutGroup>(goalGO);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 8; vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var csf = EnsureComp<ContentSizeFitter>(goalGO);
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Style existing GoalRow_Template
            var rowGO = Find("GoalRow_Template");
            if (rowGO != null) {
                var rowRT = rowGO.GetComponent<RectTransform>();
                rowRT.anchorMin = Vector2.zero; rowRT.anchorMax = new Vector2(1f, 0f);
                rowRT.pivot = new Vector2(0.5f, 0f);
                var rowLE = EnsureComp<LayoutElement>(rowGO);
                rowLE.preferredHeight = 70; rowLE.minHeight = 70;
                var hlg = EnsureComp<HorizontalLayoutGroup>(rowGO);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.spacing = 10; hlg.padding = new RectOffset(8, 8, 4, 4);
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
                // Icon child
                if (rowGO.transform.childCount == 0) {
                    var iconGO = new GameObject("SpeciesIcon"); iconGO.transform.SetParent(rowGO.transform, false);
                    var iconRT = iconGO.AddComponent<RectTransform>();
                    var iconImg = iconGO.AddComponent<Image>();
                    iconImg.color = C_WHITE; iconImg.preserveAspect = true;
                    var le = iconGO.AddComponent<LayoutElement>();
                    le.preferredWidth = 56; le.preferredHeight = 56;
                    // Count text
                    var cntGO = new GameObject("CountText"); cntGO.transform.SetParent(rowGO.transform, false);
                    cntGO.AddComponent<RectTransform>();
                    Txt(cntGO, "0/5", 40, C_WHITE, FontStyle.Bold, TextAnchor.MiddleLeft);
                    var cntLE = cntGO.AddComponent<LayoutElement>();
                    cntLE.preferredWidth = 80; cntLE.preferredHeight = 56;
                }
            }
        }

        static void BuildProgressBar(Sprite lightPnl)
        {
            var pbGO = Find("ProgressBar");
            if (pbGO == null) return;
            var rt = pbGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.845f);
            rt.anchorMax = new Vector2(0.95f, 0.873f);
            rt.pivot = new Vector2(0.5f, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var slider = EnsureComp<Slider>(pbGO);
            slider.minValue = 0; slider.maxValue = 1; slider.value = 0.35f;
            slider.interactable = false;
            // Background
            var bgImg = EnsureComp<Image>(pbGO);
            if (lightPnl != null) bgImg.sprite = lightPnl;
            bgImg.color = C_DARK_BG; bgImg.type = Image.Type.Sliced;
        }

        static void BuildVillainHUD(Sprite lightPnl)
        {
            var vhGO = Find("VillainHUD");
            if (vhGO == null) return;
            vhGO.SetActive(false); // hidden until MegaLevel
            var rt = vhGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.88f);
            rt.anchorMax = new Vector2(1.0f, 0.99f);
            rt.pivot = new Vector2(1f, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = Img(vhGO, lightPnl, false, Image.Type.Sliced);
            img.color = new Color(0.5f, 0.08f, 0.08f, 0.9f);

            var portraitGO = Find("Portrait");
            if (portraitGO != null) {
                var prt = portraitGO.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.02f, 0.05f);
                prt.anchorMax = new Vector2(0.28f, 0.95f);
                prt.pivot = new Vector2(0f, 0.5f); prt.offsetMin = prt.offsetMax = Vector2.zero;
                Img(portraitGO, lightPnl, false, Image.Type.Simple).color = new Color(1f, 0.7f, 0.7f, 1f);
            }

            var hpBarGO = Find("HPBar");
            if (hpBarGO != null) {
                var hprt = hpBarGO.GetComponent<RectTransform>();
                hprt.anchorMin = new Vector2(0.32f, 0.35f);
                hprt.anchorMax = new Vector2(0.96f, 0.65f);
                hprt.pivot = new Vector2(0.5f, 0.5f); hprt.offsetMin = hprt.offsetMax = Vector2.zero;
                var hpImg = EnsureComp<Image>(hpBarGO);
                hpImg.color = C_HP_GREEN; hpImg.type = Image.Type.Filled;
                hpImg.fillMethod = Image.FillMethod.Horizontal; hpImg.fillAmount = 1f;
            }
        }

        static void BuildLevelIntro(Sprite panel, Sprite lightPnl)
        {
            var introGO = Find("LevelIntroOverlay");
            if (introGO == null) return;
            var rt = introGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.25f);
            rt.anchorMax = new Vector2(0.92f, 0.78f);
            rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            introGO.SetActive(false);

            // Panel bg
            var panelGO = Find("Panel");
            if (panelGO != null) {
                FullStretch(panelGO.GetComponent<RectTransform>());
                var img = Img(panelGO, panel, false, Image.Type.Sliced);
                img.color = C_PANEL_BG;
            }

            // Level number
            var lvlTxt = Find("LevelNumberText");
            if (lvlTxt != null) {
                var lvlRT = lvlTxt.GetComponent<RectTransform>();
                lvlRT.anchorMin = new Vector2(0.1f, 0.7f); lvlRT.anchorMax = new Vector2(0.9f, 0.95f);
                lvlRT.pivot = new Vector2(0.5f, 1f); lvlRT.offsetMin = lvlRT.offsetMax = Vector2.zero;
                Txt(lvlTxt, "LEVEL 1", 72, C_YELLOW, FontStyle.Bold);
            }
            // Chapter text
            var chTxt = Find("ChapterText");
            if (chTxt != null) {
                var chRT = chTxt.GetComponent<RectTransform>();
                chRT.anchorMin = new Vector2(0.1f, 0.52f); chRT.anchorMax = new Vector2(0.9f, 0.72f);
                chRT.pivot = new Vector2(0.5f, 1f); chRT.offsetMin = chRT.offsetMax = Vector2.zero;
                Txt(chTxt, "Sunny Meadow", 44, C_WHITE, FontStyle.Italic);
            }
            // Time limit
            var timeTxt = Find("TimeLimitText");
            if (timeTxt != null) {
                var tmRT = timeTxt.GetComponent<RectTransform>();
                tmRT.anchorMin = new Vector2(0.1f, 0.34f); tmRT.anchorMax = new Vector2(0.9f, 0.54f);
                tmRT.pivot = new Vector2(0.5f, 1f); tmRT.offsetMin = tmRT.offsetMax = Vector2.zero;
                Txt(timeTxt, "⏱ 60 seconds", 40, new Color(0.7f, 0.9f, 1f, 1f), FontStyle.Normal);
            }
            // Goal icons root
            var goalRoot = Find("GoalIconsRoot");
            if (goalRoot != null) {
                var grRT = goalRoot.GetComponent<RectTransform>();
                grRT.anchorMin = new Vector2(0.05f, 0.04f); grRT.anchorMax = new Vector2(0.95f, 0.32f);
                grRT.pivot = new Vector2(0.5f, 0f); grRT.offsetMin = grRT.offsetMax = Vector2.zero;
                var hlg = EnsureComp<HorizontalLayoutGroup>(goalRoot);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 24; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            }
        }

        static void BuildCountdown(Sprite lightPnl)
        {
            var cdGO = Find("CountdownOverlay");
            if (cdGO == null) return;
            var rt = cdGO.GetComponent<RectTransform>();
            FullStretch(rt);
            cdGO.SetActive(false);
            var bg = EnsureComp<Image>(cdGO);
            bg.color = new Color(0f, 0f, 0f, 0.45f); bg.raycastTarget = false;

            var cdTxt = Find("CountdownText");
            if (cdTxt != null) {
                var cdRT = cdTxt.GetComponent<RectTransform>();
                cdRT.anchorMin = new Vector2(0.2f, 0.35f); cdRT.anchorMax = new Vector2(0.8f, 0.65f);
                cdRT.pivot = new Vector2(0.5f, 0.5f); cdRT.offsetMin = cdRT.offsetMax = Vector2.zero;
                Txt(cdTxt, "3", 260, C_YELLOW, FontStyle.Bold);
            }
        }

        static void BuildResultsPanel(Sprite panel, Sprite panel2, Sprite redBtns,
                                      Sprite coinSpr, Sprite blueBtnSpr)
        {
            var rpGO = Find("ResultsPanel");
            if (rpGO == null) return;
            var rt = rpGO.GetComponent<RectTransform>();
            FullStretch(rt);
            rpGO.SetActive(false);
            var bg = EnsureComp<Image>(rpGO);
            bg.color = new Color(0f, 0f, 0f, 0.6f); bg.raycastTarget = true;

            // WinRoot
            var winGO = Find("WinRoot");
            if (winGO != null) {
                var wRT = winGO.GetComponent<RectTransform>();
                wRT.anchorMin = new Vector2(0.08f, 0.25f); wRT.anchorMax = new Vector2(0.92f, 0.80f);
                wRT.pivot = new Vector2(0.5f, 0.5f); wRT.offsetMin = wRT.offsetMax = Vector2.zero;
                var wImg = Img(winGO, panel, false, Image.Type.Sliced);
                wImg.color = C_PANEL_BG;
                // Build win contents
                BuildWinContents(winGO, coinSpr, blueBtnSpr);
            }

            // LoseRoot
            var loseGO = Find("LoseRoot");
            if (loseGO != null) {
                var lRT = loseGO.GetComponent<RectTransform>();
                lRT.anchorMin = new Vector2(0.08f, 0.28f); lRT.anchorMax = new Vector2(0.92f, 0.78f);
                lRT.pivot = new Vector2(0.5f, 0.5f); lRT.offsetMin = lRT.offsetMax = Vector2.zero;
                var lImg = Img(loseGO, panel2, false, Image.Type.Sliced);
                lImg.color = new Color(0.55f, 0.05f, 0.08f, 0.97f);
                BuildLoseContents(loseGO, redBtns);
            }
        }

        static void BuildWinContents(GameObject winRoot, Sprite coinSpr, Sprite blueBtnSpr)
        {
            // Title
            var vtlg = EnsureComp<VerticalLayoutGroup>(winRoot);
            vtlg.childAlignment = TextAnchor.UpperCenter;
            vtlg.spacing = 18; vtlg.padding = new RectOffset(32, 32, 28, 28);
            vtlg.childForceExpandWidth = true; vtlg.childForceExpandHeight = false;

            // "LEVEL COMPLETE!"
            GameObject titleGO = new GameObject("WinTitle"); titleGO.transform.SetParent(winRoot.transform, false);
            titleGO.AddComponent<RectTransform>();
            var le0 = titleGO.AddComponent<LayoutElement>(); le0.preferredHeight = 90;
            Txt(titleGO, "LEVEL COMPLETE!", 62, C_YELLOW, FontStyle.Bold);

            // Stars row
            GameObject starsRow = new GameObject("StarsRow"); starsRow.transform.SetParent(winRoot.transform, false);
            starsRow.AddComponent<RectTransform>();
            var leStars = starsRow.AddComponent<LayoutElement>(); leStars.preferredHeight = 110;
            var hlg = starsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 20;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            string[] starNames = { "Star1", "Star2", "Star3" };
            foreach (var sn in starNames) {
                var sg = new GameObject(sn); sg.transform.SetParent(starsRow.transform, false);
                sg.AddComponent<RectTransform>();
                var sle = sg.AddComponent<LayoutElement>(); sle.preferredWidth = 100; sle.preferredHeight = 100;
                var sImg = sg.AddComponent<Image>();
                sImg.color = C_YELLOW; sImg.preserveAspect = true;
                Txt(sg, "★", 80, C_YELLOW, FontStyle.Bold);
            }

            // Score row
            GameObject scoreRow = new GameObject("ScoreRow"); scoreRow.transform.SetParent(winRoot.transform, false);
            scoreRow.AddComponent<RectTransform>();
            var leScore = scoreRow.AddComponent<LayoutElement>(); leScore.preferredHeight = 70;
            Txt(scoreRow, "Score: 1250", 50, C_WHITE, FontStyle.Bold);

            // Coins row
            GameObject coinsRow = new GameObject("CoinsRow"); coinsRow.transform.SetParent(winRoot.transform, false);
            coinsRow.AddComponent<RectTransform>();
            var leCoins = coinsRow.AddComponent<LayoutElement>(); leCoins.preferredHeight = 68;
            var cHlg = coinsRow.AddComponent<HorizontalLayoutGroup>();
            cHlg.childAlignment = TextAnchor.MiddleCenter; cHlg.spacing = 14;
            cHlg.childForceExpandWidth = false; cHlg.childForceExpandHeight = false;
            var coinIconGO = new GameObject("CoinIcon"); coinIconGO.transform.SetParent(coinsRow.transform, false);
            coinIconGO.AddComponent<RectTransform>();
            var ciLE = coinIconGO.AddComponent<LayoutElement>(); ciLE.preferredWidth = 60; ciLE.preferredHeight = 60;
            var ciImg = coinIconGO.AddComponent<Image>();
            if (coinSpr != null) { ciImg.sprite = coinSpr; ciImg.type = Image.Type.Simple; ciImg.preserveAspect = true; }
            ciImg.color = C_WHITE;
            var coinTxtGO = new GameObject("CoinsText"); coinTxtGO.transform.SetParent(coinsRow.transform, false);
            coinTxtGO.AddComponent<RectTransform>();
            var ctLE = coinTxtGO.AddComponent<LayoutElement>(); ctLE.preferredWidth = 120; ctLE.preferredHeight = 60;
            Txt(coinTxtGO, "+100", 48, C_YELLOW, FontStyle.Bold);

            // Continue button
            GameObject btnGO = new GameObject("ContinueButton"); btnGO.transform.SetParent(winRoot.transform, false);
            btnGO.AddComponent<RectTransform>();
            var btnLE = btnGO.AddComponent<LayoutElement>(); btnLE.preferredHeight = 100;
            var btnImg = btnGO.AddComponent<Image>();
            if (blueBtnSpr != null) { btnImg.sprite = blueBtnSpr; btnImg.type = Image.Type.Sliced; }
            btnImg.color = new Color(0.25f, 0.75f, 0.35f, 1f); btnImg.raycastTarget = true;
            var btn = btnGO.AddComponent<Button>();
            var btnTxtGO = new GameObject("Text"); btnTxtGO.transform.SetParent(btnGO.transform, false);
            var btnRT = btnTxtGO.AddComponent<RectTransform>(); FullStretch(btnRT);
            Txt(btnTxtGO, "CONTINUE ▶", 52, C_WHITE, FontStyle.Bold);
        }

        static void BuildLoseContents(GameObject loseRoot, Sprite redBtns)
        {
            var vtlg = EnsureComp<VerticalLayoutGroup>(loseRoot);
            vtlg.childAlignment = TextAnchor.UpperCenter;
            vtlg.spacing = 20; vtlg.padding = new RectOffset(32, 32, 32, 32);
            vtlg.childForceExpandWidth = true; vtlg.childForceExpandHeight = false;

            // "LEVEL FAILED"
            GameObject titleGO = new GameObject("LoseTitle"); titleGO.transform.SetParent(loseRoot.transform, false);
            titleGO.AddComponent<RectTransform>();
            titleGO.AddComponent<LayoutElement>().preferredHeight = 90;
            Txt(titleGO, "LEVEL FAILED", 66, C_TIMER_RED, FontStyle.Bold);

            // Score
            GameObject scoreGO = new GameObject("ScoreText"); scoreGO.transform.SetParent(loseRoot.transform, false);
            scoreGO.AddComponent<RectTransform>();
            scoreGO.AddComponent<LayoutElement>().preferredHeight = 68;
            Txt(scoreGO, "Score: 0", 50, C_WHITE, FontStyle.Normal);

            // Buttons row
            GameObject btnsRow = new GameObject("ButtonsRow"); btnsRow.transform.SetParent(loseRoot.transform, false);
            btnsRow.AddComponent<RectTransform>();
            btnsRow.AddComponent<LayoutElement>().preferredHeight = 110;
            var hlg = btnsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 30;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            string[] btnLabels = { "🔄 RETRY", "🏠 QUIT" };
            Color[] btnColors = { new Color(0.15f, 0.55f, 0.95f, 1f), new Color(0.7f, 0.15f, 0.15f, 1f) };
            foreach (var kvp in new[] { (btnLabels[0], btnColors[0], "RetryButton"), (btnLabels[1], btnColors[1], "QuitButton") }) {
                var bg = new GameObject(kvp.Item3); bg.transform.SetParent(btnsRow.transform, false);
                bg.AddComponent<RectTransform>();
                var le = bg.AddComponent<LayoutElement>(); le.preferredWidth = 260; le.preferredHeight = 100;
                var bImg = bg.AddComponent<Image>();
                if (redBtns != null) { bImg.sprite = redBtns; bImg.type = Image.Type.Sliced; }
                bImg.color = kvp.Item2; bImg.raycastTarget = true;
                bg.AddComponent<Button>();
                var btg = new GameObject("Text"); btg.transform.SetParent(bg.transform, false);
                var btRT = btg.AddComponent<RectTransform>(); FullStretch(btRT);
                Txt(btg, kvp.Item1, 44, C_WHITE, FontStyle.Bold);
            }
        }

        static void BuildToast(Sprite lightPnl)
        {
            var toastGO = Find("ToastNotification");
            if (toastGO == null) return;
            var rt = toastGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.08f); rt.anchorMax = new Vector2(0.9f, 0.17f);
            rt.pivot = new Vector2(0.5f, 0f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            toastGO.SetActive(false);
            var img = Img(toastGO, lightPnl, false, Image.Type.Sliced);
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            var toastTxt = Find("ToastText");
            if (toastTxt != null) {
                FullStretch(toastTxt.GetComponent<RectTransform>());
                Txt(toastTxt, "New hindrance unlocked!", 38, C_WHITE, FontStyle.Normal);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // MAIN SCENE BUILDER
        // ══════════════════════════════════════════════════════════════════
        [MenuItem("AnimalFall/Build MainScene UI")]
        public static void BuildMainSceneUI()
        {
            if (SceneManager.GetActiveScene().name != "MainScene") {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            }
            Sprite tileBack  = Resources.Load<Sprite>("panels/tile_back");
            Sprite mainTop   = Resources.Load<Sprite>("panels/main_top");
            Sprite mainBot   = Resources.Load<Sprite>("panels/main_bottom");
            Sprite mainBot2  = Resources.Load<Sprite>("panels/main_bottom_2");
            Sprite blueBtn   = Resources.Load<Sprite>("panels/blue_button");
            Sprite lightPnl  = Resources.Load<Sprite>("panels/light_panel");
            Sprite panelTopS = Resources.Load<Sprite>("panels/panel_top");
            Sprite lvlBtn1   = Resources.Load<Sprite>("panels/levelbutton1");
            Sprite lvlBtn2   = Resources.Load<Sprite>("panels/levelbutton2");
            Sprite exitSpr   = Resources.Load<Sprite>("panels/exit");
            Sprite coinSpr   = Resources.Load<Sprite>("icons/coinstack");
            Sprite ropeSpr   = Resources.Load<Sprite>("panels/rope");

            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.15f, 0.08f, 0.32f, 1f);

            // ── MAP CANVAS ────────────────────────────────────────────────
            var mapCanvas = Find("[UI — MapCanvas]");
            if (mapCanvas != null) {
                SetupCanvas(mapCanvas, 0);
            }

            // Background
            var bgGO = Find("Background");
            if (bgGO != null) {
                FullStretch(bgGO.GetComponent<RectTransform>());
                var img = Img(bgGO, tileBack, false, Image.Type.Tiled);
                img.color = new Color(0.15f, 0.08f, 0.32f, 1f);
            }

            // TopBar
            var topBar = Find("TopBar");
            if (topBar != null) {
                AnchorTop(topBar.GetComponent<RectTransform>(), 155);
                var img = Img(topBar, mainTop, false, Image.Type.Sliced);
                img.color = C_WHITE;
                var hlg = EnsureComp<HorizontalLayoutGroup>(topBar);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 0; hlg.padding = new RectOffset(30, 30, 20, 20);
                hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false;
            }

            // Logo text
            var logoGO = Find("LogoText");
            if (logoGO != null) {
                var le = EnsureComp<LayoutElement>(logoGO);
                le.flexibleWidth = 1;
                Txt(logoGO, "🐾 ANIMAL FALL", 58, C_YELLOW, FontStyle.Bold, TextAnchor.MiddleLeft);
            }

            // Lives panel
            var livesPanel = Find("LivesPanel");
            if (livesPanel != null) {
                var leLP = EnsureComp<LayoutElement>(livesPanel);
                leLP.preferredWidth = 160;
                var img = Img(livesPanel, lightPnl, false, Image.Type.Sliced);
                img.color = new Color(0.8f, 0.15f, 0.15f, 0.85f);
                var hlgLP = EnsureComp<HorizontalLayoutGroup>(livesPanel);
                hlgLP.childAlignment = TextAnchor.MiddleCenter;
                hlgLP.spacing = 8; hlgLP.padding = new RectOffset(12, 12, 8, 8);
                hlgLP.childForceExpandWidth = false; hlgLP.childForceExpandHeight = false;
            }

            var heartGO = Find("HeartIcon");
            if (heartGO != null) {
                var img = EnsureComp<Image>(heartGO);
                img.color = new Color(1f, 0.3f, 0.3f, 1f);
                img.raycastTarget = false; img.preserveAspect = true;
                var txt = EnsureComp<Text>(heartGO); txt.text = "❤"; txt.fontSize = 42; txt.color = C_WHITE;
                txt.alignment = TextAnchor.MiddleCenter; txt.raycastTarget = false;
                var le = EnsureComp<LayoutElement>(heartGO); le.preferredWidth = 44; le.preferredHeight = 44;
            }

            var livesTxt = Find("LivesText");
            if (livesTxt != null) {
                Txt(livesTxt, "5", 46, C_WHITE, FontStyle.Bold);
                var le = EnsureComp<LayoutElement>(livesTxt); le.preferredWidth = 52; le.preferredHeight = 44;
            }

            // Coins panel
            var coinsPanel = Find("CoinsPanel");
            if (coinsPanel != null) {
                var le = EnsureComp<LayoutElement>(coinsPanel); le.preferredWidth = 170;
                var img = Img(coinsPanel, lightPnl, false, Image.Type.Sliced);
                img.color = new Color(0.8f, 0.6f, 0.05f, 0.88f);
                var hlg = EnsureComp<HorizontalLayoutGroup>(coinsPanel);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 8; hlg.padding = new RectOffset(12, 12, 8, 8);
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            }

            var coinIconGO = Find("CoinIcon");
            if (coinIconGO != null) {
                var img = Img(coinIconGO, coinSpr, false, Image.Type.Simple);
                img.color = C_WHITE; img.preserveAspect = true;
                var le = EnsureComp<LayoutElement>(coinIconGO); le.preferredWidth = 46; le.preferredHeight = 46;
            }

            var coinsTxt = Find("CoinsText");
            if (coinsTxt != null) {
                Txt(coinsTxt, "0", 46, C_YELLOW, FontStyle.Bold);
                var le = EnsureComp<LayoutElement>(coinsTxt); le.preferredWidth = 80; le.preferredHeight = 46;
            }

            // ScrollView
            var scrollView = Find("ScrollView");
            if (scrollView != null) {
                var rt = scrollView.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(0, 120); rt.offsetMax = new Vector2(0, -155);
                var sr = EnsureComp<ScrollRect>(scrollView);
                sr.vertical = true; sr.horizontal = false;
                sr.scrollSensitivity = 35;
                var bg = EnsureComp<Image>(scrollView);
                bg.color = new Color(0f, 0f, 0f, 0f); bg.raycastTarget = true;
            }

            // NodeContainer
            var nodeContainer = Find("NodeContainer");
            if (nodeContainer != null) {
                var vlg = EnsureComp<VerticalLayoutGroup>(nodeContainer);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.spacing = 28; vlg.padding = new RectOffset(60, 60, 40, 40);
                vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
                var csf = EnsureComp<ContentSizeFitter>(nodeContainer);
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var nodeRT = nodeContainer.GetComponent<RectTransform>();
                nodeRT.anchorMin = Vector2.zero; nodeRT.anchorMax = new Vector2(1, 1);
                nodeRT.pivot = new Vector2(0.5f, 1f);
                nodeRT.offsetMin = nodeRT.offsetMax = Vector2.zero;
            }

            // BottomBar
            var botBar = Find("BottomBar");
            if (botBar != null) {
                AnchorBottom(botBar.GetComponent<RectTransform>(), 128);
                var img = Img(botBar, mainBot2, false, Image.Type.Sliced);
                img.color = C_WHITE;
                var hlg = EnsureComp<HorizontalLayoutGroup>(botBar);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 24; hlg.padding = new RectOffset(40, 40, 20, 16);
                hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false;
            }

            // Play button
            var playBtn = Find("PlayButton");
            if (playBtn != null) {
                var img = Img(playBtn, blueBtn, true, Image.Type.Sliced);
                img.color = new Color(0.15f, 0.72f, 0.30f, 1f);
                EnsureComp<Button>(playBtn);
                var le = EnsureComp<LayoutElement>(playBtn); le.preferredHeight = 88; le.flexibleWidth = 1;
                var et = playBtn.GetComponentInChildren<Text>();
                if (et == null) {
                    var tg = new GameObject("Text"); tg.transform.SetParent(playBtn.transform, false);
                    var tgRT = tg.AddComponent<RectTransform>(); FullStretch(tgRT);
                    Txt(tg, "▶  PLAY", 54, C_WHITE, FontStyle.Bold);
                } else { et.text = "▶  PLAY"; et.fontSize = 54; et.color = C_WHITE; }
            }

            // Shop / Settings buttons
            string[] smallBtnNames = { "ShopButton", "SettingsButton" };
            string[] smallBtnLabels = { "🛒", "⚙" };
            foreach (var pair in new[] { (smallBtnNames[0], smallBtnLabels[0]), (smallBtnNames[1], smallBtnLabels[1]) }) {
                var sbGO = Find(pair.Item1);
                if (sbGO == null) continue;
                var img = Img(sbGO, lightPnl, true, Image.Type.Sliced);
                img.color = new Color(0.35f, 0.25f, 0.55f, 0.92f);
                EnsureComp<Button>(sbGO);
                var le = EnsureComp<LayoutElement>(sbGO); le.preferredWidth = 90; le.preferredHeight = 88;
                var et = sbGO.GetComponentInChildren<Text>();
                if (et == null) {
                    var tg = new GameObject("Icon"); tg.transform.SetParent(sbGO.transform, false);
                    var tgRT = tg.AddComponent<RectTransform>(); FullStretch(tgRT);
                    Txt(tg, pair.Item2, 50, C_WHITE);
                } else { et.text = pair.Item2; et.fontSize = 50; }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] MainScene UI built and saved.");
        }

        // ── Run all at once ────────────────────────────────────────────────
        [MenuItem("AnimalFall/Build ALL Scenes")]
        public static void BuildAll()
        {
            // GameScene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
            BuildGameSceneUI();
            BuildGameSceneUIPart2();
            // MainScene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            BuildMainSceneUI();
            // Return to GameScene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        }
    }
}
#endif
