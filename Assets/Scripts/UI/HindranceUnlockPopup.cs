using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.UI
{
    /// <summary>Clean, asset-free unlock card shown before countdown and timer start.</summary>
    public static class HindranceUnlockPopup
    {
        public static IEnumerator Show(HindranceType type, float seconds = 2.8f)
        {
            HindranceRegistry registry = Resources.Load<HindranceRegistry>("Hindrances/HindranceRegistry");
            HindranceData data = registry != null ? registry.GetData(type) : null;
            string title = data != null && !string.IsNullOrWhiteSpace(data.displayName)
                ? data.displayName
                : SplitName(type.ToString());
            string instruction = data != null && !string.IsNullOrWhiteSpace(data.tutorialInstruction)
                ? data.tutorialInstruction
                : "Watch for this new challenge.";

            GameObject canvasObject = new GameObject("[UI] Hindrance Unlock", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject dim = CreateImage("Dim", canvasObject.transform, new Color(0.02f, 0.04f, 0.08f, 0.88f));
            Stretch(dim.GetComponent<RectTransform>());

            GameObject card = CreateImage("Card", dim.transform, new Color(0.96f, 0.98f, 1f, 1f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 650f);
            cardRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI eyebrow = CreateText("Eyebrow", card.transform, "NEW HINDRANCE", 34f, new Color(0.16f, 0.45f, 0.95f, 1f));
            SetRect(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(640f, 58f), new Vector2(0f, -58f));

            GameObject iconObject = CreateImage("Icon", card.transform, Color.white);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = data != null ? data.icon : null;
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : new Color(0.16f, 0.45f, 0.95f, 1f);
            SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(210f, 210f), new Vector2(0f, -145f));

            TextMeshProUGUI name = CreateText("Name", card.transform, title, 58f, new Color(0.06f, 0.10f, 0.18f, 1f));
            name.fontStyle = FontStyles.Bold;
            SetRect(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(660f, 90f), new Vector2(0f, -55f));

            TextMeshProUGUI tip = CreateText("Instruction", card.transform, instruction, 34f, new Color(0.22f, 0.28f, 0.38f, 1f));
            tip.enableWordWrapping = true;
            SetRect(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640f, 130f), new Vector2(0f, -170f));

            TextMeshProUGUI footer = CreateText("Footer", card.transform, "GET READY  •  TIMER STARTS NEXT", 24f, new Color(0.40f, 0.46f, 0.56f, 1f));
            SetRect(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(650f, 54f), new Vector2(0f, 42f));

            yield return new WaitForSecondsRealtime(Mathf.Max(1.5f, seconds));
            Object.Destroy(canvasObject);
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static string SplitName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "New Hindrance";
            var builder = new System.Text.StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1])) builder.Append(' ');
                builder.Append(value[i]);
            }
            return builder.ToString();
        }
    }
}