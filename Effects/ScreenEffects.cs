using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimalFall.Effects
{
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        [Header("Overlay Panels")]
        [SerializeField] private Image inkSplatterOverlay;
        [SerializeField] private Image flashbangOverlay;
        [SerializeField] private Image stormCloudOverlay;
        [SerializeField] private RectTransform tornadoContainer;
        [SerializeField] private RectTransform leavesContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject inkSplatterPrefab;
        [SerializeField] private GameObject stormCloudPrefab;
        [SerializeField] private GameObject tornadoPrefab;
        [SerializeField] private GameObject fallingLeafPrefab;

        [Header("Settings")]
        [SerializeField] private Color inkColor = new Color(0.1f, 0.05f, 0.15f, 0.85f);
        [SerializeField] private int leafCount = 15;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            HideAll();
        }

        public void ShowInkSplatter(float duration)
        {
            StartCoroutine(InkSplatterRoutine(duration));
        }

        public void ShowFlashbang(float duration)
        {
            StartCoroutine(FlashbangRoutine(duration));
        }

        public void ShowStormClouds(float duration)
        {
            StartCoroutine(StormCloudRoutine(duration));
        }

        public void SpawnTornado(float duration)
        {
            StartCoroutine(TornadoRoutine(duration));
        }

        public void SpawnFallingLeaves(float duration)
        {
            StartCoroutine(FallingLeavesRoutine(duration));
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            HideAll();
        }

        private void HideAll()
        {
            if (inkSplatterOverlay != null) inkSplatterOverlay.gameObject.SetActive(false);
            if (flashbangOverlay != null) flashbangOverlay.gameObject.SetActive(false);
            if (stormCloudOverlay != null) stormCloudOverlay.gameObject.SetActive(false);
        }

        private IEnumerator InkSplatterRoutine(float duration)
        {
            if (inkSplatterOverlay == null) yield break;

            inkSplatterOverlay.gameObject.SetActive(true);
            inkSplatterOverlay.color = inkColor;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(inkColor.a, 0f, elapsed / duration);
                inkSplatterOverlay.color = new Color(inkColor.r, inkColor.g, inkColor.b, alpha);
                yield return null;
            }

            inkSplatterOverlay.gameObject.SetActive(false);
        }

        private IEnumerator FlashbangRoutine(float duration)
        {
            if (flashbangOverlay == null) yield break;

            flashbangOverlay.gameObject.SetActive(true);
            flashbangOverlay.color = Color.white;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                flashbangOverlay.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            flashbangOverlay.gameObject.SetActive(false);
        }

        private IEnumerator StormCloudRoutine(float duration)
        {
            if (stormCloudOverlay == null) yield break;

            stormCloudOverlay.gameObject.SetActive(true);
            Color stormColor = new Color(0.2f, 0.2f, 0.3f, 0.7f);
            stormCloudOverlay.color = stormColor;

            yield return new WaitForSeconds(duration);

            float fadeTime = 1f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                stormCloudOverlay.color = new Color(
                    stormColor.r, stormColor.g, stormColor.b,
                    Mathf.Lerp(stormColor.a, 0f, elapsed / fadeTime));
                yield return null;
            }

            stormCloudOverlay.gameObject.SetActive(false);
        }

        private IEnumerator TornadoRoutine(float duration)
        {
            if (tornadoPrefab == null || tornadoContainer == null) yield break;

            GameObject tornado = Instantiate(tornadoPrefab, tornadoContainer);
            RectTransform rt = tornado.GetComponent<RectTransform>();

            float elapsed = 0f;
            float startX = -Screen.width * 0.5f;
            float endX = Screen.width * 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (rt != null)
                {
                    float x = Mathf.Lerp(startX, endX, t);
                    float y = Mathf.Sin(t * Mathf.PI * 4f) * 100f;
                    rt.anchoredPosition = new Vector2(x, y);
                    rt.Rotate(0, 0, Time.deltaTime * 360f);
                }

                yield return null;
            }

            Destroy(tornado);
        }

        private IEnumerator FallingLeavesRoutine(float duration)
        {
            if (fallingLeafPrefab == null || leavesContainer == null) yield break;

            var leaves = new GameObject[leafCount];
            for (int i = 0; i < leafCount; i++)
            {
                leaves[i] = Instantiate(fallingLeafPrefab, leavesContainer);
                RectTransform rt = leaves[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(
                        Random.Range(-Screen.width * 0.5f, Screen.width * 0.5f),
                        Screen.height * 0.5f + Random.Range(0, 200f));
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                foreach (var leaf in leaves)
                {
                    if (leaf == null) continue;
                    RectTransform rt = leaf.GetComponent<RectTransform>();
                    if (rt == null) continue;

                    Vector2 pos = rt.anchoredPosition;
                    pos.y -= Time.deltaTime * Random.Range(100f, 250f);
                    pos.x += Mathf.Sin(Time.time * 2f + pos.y * 0.01f) * Time.deltaTime * 50f;
                    rt.anchoredPosition = pos;

                    if (pos.y < -Screen.height * 0.5f - 50f)
                    {
                        rt.anchoredPosition = new Vector2(
                            Random.Range(-Screen.width * 0.5f, Screen.width * 0.5f),
                            Screen.height * 0.5f + 50f);
                    }
                }
                yield return null;
            }

            foreach (var leaf in leaves)
            {
                if (leaf != null) Destroy(leaf);
            }
        }
    }
}
