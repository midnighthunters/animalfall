using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace AnimalFall.UI.Splash
{
    public class SplashScreenManager : MonoBehaviour
    {
        [Serializable]
        public struct SplashScreen
        {
            public string title;
            [TextArea(2, 4)]
            public string subtitle;
            public Sprite backgroundImage;
            public Sprite logoImage;
            public float displayDuration;
            public Color backgroundColor;
        }

        [Header("Splash Screens")]
        [SerializeField] private SplashScreen[] splashScreens = new SplashScreen[]
        {
            new SplashScreen
            {
                title = "Midnight Hunters",
                subtitle = "presents",
                displayDuration = 2f,
                backgroundColor = new Color(0.05f, 0.05f, 0.15f)
            },
            new SplashScreen
            {
                title = "AnimalFall",
                subtitle = "Catch them all before they escape!",
                displayDuration = 2.5f,
                backgroundColor = new Color(0.1f, 0.3f, 0.1f)
            },
            new SplashScreen
            {
                title = "Loading...",
                subtitle = "Preparing your adventure",
                displayDuration = 2f,
                backgroundColor = new Color(0.15f, 0.1f, 0.25f)
            }
        };

        [Header("UI References")]
        [SerializeField] private Image backgroundPanel;
        [SerializeField] private Image logoImageDisplay;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image progressBar;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private CanvasGroup fadeGroup;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private string nextSceneName = "AuthScene";
        [SerializeField] private bool skipIfLoggedIn;

        private int currentScreenIndex;
        private float overallProgress;

        private void Start()
        {
            if (fadeGroup != null) fadeGroup.alpha = 0f;
            StartCoroutine(RunSplashSequence());
        }

        private IEnumerator RunSplashSequence()
        {
            int totalScreens = splashScreens.Length;

            for (int i = 0; i < totalScreens; i++)
            {
                currentScreenIndex = i;
                var screen = splashScreens[i];

                SetupScreen(screen);
                yield return FadeIn();
                yield return SimulateLoading(screen.displayDuration, i, totalScreens);
                yield return FadeOut();
            }

            yield return LoadNextScene();
        }

        private void SetupScreen(SplashScreen screen)
        {
            if (backgroundPanel != null)
                backgroundPanel.color = screen.backgroundColor;

            if (titleText != null)
                titleText.text = screen.title;

            if (subtitleText != null)
                subtitleText.text = screen.subtitle;

            if (logoImageDisplay != null)
            {
                logoImageDisplay.sprite = screen.logoImage;
                logoImageDisplay.gameObject.SetActive(screen.logoImage != null);
            }

            if (backgroundPanel != null && screen.backgroundImage != null)
                backgroundPanel.sprite = screen.backgroundImage;
        }

        private IEnumerator SimulateLoading(float duration, int screenIndex, int totalScreens)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float screenProgress = elapsed / duration;
                overallProgress = ((float)screenIndex + screenProgress) / totalScreens;

                if (progressBar != null)
                    progressBar.fillAmount = overallProgress;

                if (progressText != null)
                    progressText.text = $"{Mathf.RoundToInt(overallProgress * 100)}%";

                yield return null;
            }
        }

        private IEnumerator FadeIn()
        {
            if (fadeGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (fadeGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = 0f;
        }

        private IEnumerator LoadNextScene()
        {
            if (progressBar != null)
                progressBar.fillAmount = 1f;

            if (progressText != null)
                progressText.text = "100%";

            string targetScene = nextSceneName;

            if (skipIfLoggedIn && AnimalFall.Services.Auth.FirebaseAuthService.Instance != null &&
                AnimalFall.Services.Auth.FirebaseAuthService.Instance.IsLoggedIn)
            {
                targetScene = "MainScene";
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
            if (asyncLoad == null) yield break;

            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                if (progressBar != null)
                    progressBar.fillAmount = asyncLoad.progress;
                yield return null;
            }

            asyncLoad.allowSceneActivation = true;
        }
    }
}
