#if UNITY_EDITOR
// GameSceneRedesign — wipe old HUD and build the clean bottom-bar GameScene
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Managers;
using AnimalFall.UI;
using AnimalFall.Core.Animals;
using AnimalFall.Effects;
using AnimalFall.Core;

namespace AnimalFall.Editor
{
    public static class GameSceneRedesign
    {
        [MenuItem("AnimalFall/Redesign GameScene (Clean)")]
        public static void Redesign()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "GameScene")
            {
                var loaded = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
                scene = loaded;
            }

            // ── Camera plain background ──────────────────────────────────────
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.5f;
                cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // Hide world background sprite
            var worldBg = GameObject.Find("WorldBackground");
            if (worldBg != null) worldBg.SetActive(false);

            // ── Tear down old UI canvases ─────────────────────────────────────
            DestroyIfExists("[UI — StaticCanvas]");
            DestroyIfExists("[UI — DynamicCanvas]");
            DestroyIfExists("[UI — GameCanvas]");

            // ── Build single clean canvas ─────────────────────────────────────
            var canvasGO = new GameObject("[UI — GameCanvas]");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Bottom bar ───────────────────────────────────────────────────
            var bottomBar = CreateUI("BottomBar", canvasGO.transform);
            var bottomRT = bottomBar.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0f);
            bottomRT.pivot = new Vector2(0.5f, 0f);
            bottomRT.sizeDelta = new Vector2(0f, 220f);
            bottomRT.anchoredPosition = Vector2.zero;

            var barBg = bottomBar.AddComponent<Image>();
            barBg.color = new Color(0.10f, 0.12f, 0.18f, 0.94f);
            barBg.raycastTarget = false;

            // Soft top edge strip
            var edge = CreateUI("TopEdge", bottomBar.transform);
            var edgeRT = edge.GetComponent<RectTransform>();
            edgeRT.anchorMin = new Vector2(0f, 1f);
            edgeRT.anchorMax = new Vector2(1f, 1f);
            edgeRT.pivot = new Vector2(0.5f, 1f);
            edgeRT.sizeDelta = new Vector2(0f, 4f);
            edgeRT.anchoredPosition = Vector2.zero;
            var edgeImg = edge.AddComponent<Image>();
            edgeImg.color = new Color(1f, 1f, 1f, 0.12f);
            edgeImg.raycastTarget = false;

            // Layout row: Goals | Timer | Spacer(mirror)
            var content = CreateUI("Content", bottomBar.transform);
            Stretch(content.GetComponent<RectTransform>(), 24f, 18f, 24f, 16f);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 20f;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(16, 16, 8, 8);

            // Goals row (left)
            var goalsRow = CreateUI("GoalsRow", content.transform);
            var goalsLE = goalsRow.AddComponent<LayoutElement>();
            goalsLE.flexibleWidth = 1.2f;
            goalsLE.minWidth = 200f;
            var goalsHLG = goalsRow.AddComponent<HorizontalLayoutGroup>();
            goalsHLG.childAlignment = TextAnchor.MiddleLeft;
            goalsHLG.spacing = 14f;
            goalsHLG.childForceExpandWidth = false;
            goalsHLG.childForceExpandHeight = false;
            goalsHLG.padding = new RectOffset(8, 8, 0, 0);

            // Timer (center)
            var timerSlot = CreateUI("TimerSlot", content.transform);
            var timerLE = timerSlot.AddComponent<LayoutElement>();
            timerLE.preferredWidth = 160f;
            timerLE.flexibleWidth = 0f;
            timerLE.minWidth = 140f;

            var timerRingGO = CreateUI("TimerRing", timerSlot.transform);
            var ringRT = timerRingGO.GetComponent<RectTransform>();
            ringRT.anchorMin = ringRT.anchorMax = new Vector2(0.5f, 0.5f);
            ringRT.sizeDelta = new Vector2(130f, 130f);
            ringRT.anchoredPosition = Vector2.zero;
            var ringImg = timerRingGO.AddComponent<Image>();
            ringImg.color = new Color(1f, 1f, 1f, 0.18f);
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillOrigin = (int)Image.Origin360.Top;
            ringImg.fillClockwise = false;
            ringImg.fillAmount = 1f;
            ringImg.raycastTarget = false;

            var timerInner = CreateUI("TimerInner", timerSlot.transform);
            var innerRT = timerInner.GetComponent<RectTransform>();
            innerRT.anchorMin = innerRT.anchorMax = new Vector2(0.5f, 0.5f);
            innerRT.sizeDelta = new Vector2(100f, 100f);
            var innerImg = timerInner.AddComponent<Image>();
            innerImg.color = new Color(0.16f, 0.20f, 0.30f, 1f);
            innerImg.raycastTarget = false;

            var timerTextGO = CreateUI("TimerText", timerInner.transform);
            StretchFull(timerTextGO.GetComponent<RectTransform>());
            var timerTMP = timerTextGO.AddComponent<TextMeshProUGUI>();
            timerTMP.text = "60";
            timerTMP.fontSize = 52f;
            timerTMP.fontStyle = FontStyles.Bold;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color = Color.white;
            timerTMP.raycastTarget = false;

            // Right spacer (balances goals)
            var spacer = CreateUI("Spacer", content.transform);
            var spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = 1.2f;
            spacerLE.minWidth = 200f;

            // ── Countdown overlay ────────────────────────────────────────────
            var countdownRoot = CreateUI("CountdownOverlay", canvasGO.transform);
            StretchFull(countdownRoot.GetComponent<RectTransform>());
            var cdGroup = countdownRoot.AddComponent<CanvasGroup>();
            cdGroup.blocksRaycasts = true;
            var cdDim = countdownRoot.AddComponent<Image>();
            cdDim.color = new Color(0f, 0f, 0f, 0.35f);
            cdDim.raycastTarget = true;

            var cdTextGO = CreateUI("CountdownText", countdownRoot.transform);
            var cdRT = cdTextGO.GetComponent<RectTransform>();
            cdRT.anchorMin = cdRT.anchorMax = new Vector2(0.5f, 0.5f);
            cdRT.sizeDelta = new Vector2(800f, 400f);
            var cdTMP = cdTextGO.AddComponent<TextMeshProUGUI>();
            cdTMP.text = "3";
            cdTMP.fontSize = 180f;
            cdTMP.fontStyle = FontStyles.Bold;
            cdTMP.alignment = TextAlignmentOptions.Center;
            cdTMP.color = new Color(1f, 0.95f, 0.4f);
            cdTMP.raycastTarget = false;
            countdownRoot.SetActive(false);

            var countdown = canvasGO.AddComponent<CountdownController>();
            SetPrivate(countdown, "_root", countdownRoot);
            SetPrivate(countdown, "_countdownText", cdTMP);
            SetPrivate(countdown, "_canvasGroup", cdGroup);

            // ── Victory overlay ──────────────────────────────────────────────
            var victoryRoot = CreateUI("VictoryOverlay", canvasGO.transform);
            StretchFull(victoryRoot.GetComponent<RectTransform>());
            var vGroup = victoryRoot.AddComponent<CanvasGroup>();
            vGroup.blocksRaycasts = false;
            var vDim = victoryRoot.AddComponent<Image>();
            vDim.color = new Color(0f, 0f, 0f, 0.45f);
            vDim.raycastTarget = false;

            var vTitleGO = CreateUI("VictoryTitle", victoryRoot.transform);
            var vTitleRT = vTitleGO.GetComponent<RectTransform>();
            vTitleRT.anchorMin = vTitleRT.anchorMax = new Vector2(0.5f, 0.58f);
            vTitleRT.sizeDelta = new Vector2(900f, 220f);
            var vTitle = vTitleGO.AddComponent<TextMeshProUGUI>();
            vTitle.text = "VICTORY!";
            vTitle.fontSize = 110f;
            vTitle.fontStyle = FontStyles.Bold;
            vTitle.alignment = TextAlignmentOptions.Center;
            vTitle.color = new Color(1f, 0.92f, 0.25f);
            vTitle.raycastTarget = false;

            var vSubGO = CreateUI("VictorySubtitle", victoryRoot.transform);
            var vSubRT = vSubGO.GetComponent<RectTransform>();
            vSubRT.anchorMin = vSubRT.anchorMax = new Vector2(0.5f, 0.45f);
            vSubRT.sizeDelta = new Vector2(800f, 80f);
            var vSub = vSubGO.AddComponent<TextMeshProUGUI>();
            vSub.text = "All animals rescued";
            vSub.fontSize = 40f;
            vSub.alignment = TextAlignmentOptions.Center;
            vSub.color = Color.white;
            vSub.raycastTarget = false;
            victoryRoot.SetActive(false);

            var victory = canvasGO.AddComponent<VictoryOverlay>();
            SetPrivate(victory, "_root", victoryRoot);
            SetPrivate(victory, "_titleText", vTitle);
            SetPrivate(victory, "_subtitleText", vSub);
            SetPrivate(victory, "_canvasGroup", vGroup);

            // ── GameHUD component ────────────────────────────────────────────
            var hud = canvasGO.AddComponent<GameHUD>();
            SetPrivate(hud, "_bottomBar", bottomRT);
            SetPrivate(hud, "_bottomBarBg", barBg);
            SetPrivate(hud, "_goalsRow", goalsRow.transform);
            SetPrivate(hud, "_timerText", timerTMP);
            SetPrivate(hud, "_timerRing", ringImg);

            // ── GoalTracker on Managers ──────────────────────────────────────
            var managers = GameObject.Find("[Managers]");
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
            else
            {
                var go = new GameObject("GoalTracker");
                tracker = go.AddComponent<GoalTracker>();
            }

            // ── Wire GameManager ─────────────────────────────────────────────
            var gm = Object.FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                SetPrivate(gm, "_goalTracker", tracker);
                SetPrivate(gm, "_hud", hud);
                SetPrivate(gm, "_countdown", countdown);
                SetPrivate(gm, "_victoryOverlay", victory);
                SetPrivate(gm, "_plainBackground", new Color(0.55f, 0.78f, 0.95f, 1f));
                if (cam != null) SetPrivate(gm, "_camera", cam);
            }

            // ── Fix animal prefab scale / collider ───────────────────────────
            var animalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ANIMAL_PREFAB.prefab");
            if (animalPrefab != null)
            {
                var path = "Assets/Prefabs/ANIMAL_PREFAB.prefab";
                using (var edit = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    var root = edit.prefabContentsRoot;
                    root.transform.localScale = Vector3.one * Animal.IdealScale;

                    var col = root.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        col.size = new Vector2(7.5f, 7.5f);
                        col.isTrigger = true;
                    }

                    var rb = root.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Kinematic;
                        rb.simulated = true;
                        rb.gravityScale = 0f;
                    }

                    var sr = root.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 5;
                }
                Debug.Log("[GameSceneRedesign] ANIMAL_PREFAB updated (scale + collider).");
            }

            // Ensure EventSystem exists
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[GameSceneRedesign] Clean GameScene built: bottom bar goals + timer, countdown, victory.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        static GameObject CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void Stretch(RectTransform rt, float l, float r, float t, float b)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        static void SetPrivate(object target, string field, object value)
        {
            if (target == null) return;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var f = target.GetType().GetField(field, flags);
            if (f == null)
            {
                Debug.LogWarning($"[GameSceneRedesign] Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            f.SetValue(target, value);
            EditorUtility.SetDirty(target as Object);
        }
    }
}
#endif
