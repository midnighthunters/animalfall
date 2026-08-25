using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimalFall.MegaShooter
{
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
        private Coroutine _bannerRoutine;

        public void Bind(MegaShooterGameManager game)
        {
            _game = game;
            counterButton?.onClick.AddListener(game.ActivateCounter);
            pauseButton?.onClick.AddListener(game.TogglePause);
            previousAnimalButton?.onClick.AddListener(() => game.SelectAnimal(-1));
            nextAnimalButton?.onClick.AddListener(() => game.SelectAnimal(1));
            startButton?.onClick.AddListener(game.ConfirmAnimalSelection);
            resumeButton?.onClick.AddListener(game.TogglePause);
            retryButton?.onClick.AddListener(game.Retry);
            quitButton?.onClick.AddListener(game.Quit);
            resultRetryButton?.onClick.AddListener(game.Retry);
            resultQuitButton?.onClick.AddListener(game.Quit);
            unlockContinueButton?.onClick.AddListener(HideUnlock);
            ShowIntroOnly();
        }

        public void ShowIntroOnly()
        {
            if (selectionRoot != null) selectionRoot.SetActive(true);
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (resultRoot != null) resultRoot.SetActive(false);
            if (bossHealthRoot != null) bossHealthRoot.SetActive(false);
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
            if (villainOnePortrait != null) villainOnePortrait.sprite = first != null ? first.sprite : null;
            if (villainTwoPortrait != null) villainTwoPortrait.sprite = second != null ? second.sprite : null;
            if (villainOneWeaponIcon != null) villainOneWeaponIcon.sprite = first != null ? first.weaponIcon : null;
            if (villainTwoWeaponIcon != null) villainTwoWeaponIcon.sprite = second != null ? second.weaponIcon : null;
            if (bossPortrait != null) bossPortrait.sprite = boss != null ? boss.sprite : null;
            if (bossWeaponIcon != null) bossWeaponIcon.sprite = boss != null ? boss.weaponIcon : null;
        }

        public void HideSelection()
        {
            if (selectionRoot != null) selectionRoot.SetActive(false);
        }

        public void ShowCountdown(string value)
        {
            if (countdownText == null) return;
            countdownText.gameObject.SetActive(true);
            countdownText.text = value;
        }

        public void HideCountdown() { if (countdownText != null) countdownText.gameObject.SetActive(false); }
        public void SetHealth(int current, int max) { if (healthText != null) healthText.text = $"HP {current}/{max}"; }
        public void SetWave(int current, int total) { if (waveText != null) waveText.text = $"WAVE {current}/{total}"; }
        public void SetScore(int score) { if (scoreText != null) scoreText.text = score.ToString("N0"); }

        public void ShowBoss(string bossName)
        {
            if (bossHealthRoot != null) bossHealthRoot.SetActive(true);
            if (bossNameText != null) bossNameText.text = bossName;
            SetBossHealth(1f);
        }

        public void SetBossHealth(float normalized) { if (bossHealthFill != null) bossHealthFill.fillAmount = Mathf.Clamp01(normalized); }

        public void SetCounter(float normalized, bool ready)
        {
            if (counterFill != null)
            {
                counterFill.fillAmount = Mathf.Clamp01(normalized);
                counterFill.color = ready ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.9f, 1f);
            }
            if (counterButton != null) counterButton.interactable = ready;
        }

        public void PulseCounterReady()
        {
            if (counterButton == null) return;
            counterButton.transform.localScale = Vector3.one * 1.12f;
        }

        public void SetAnimalPortrait(Sprite sprite) { if (animalPortrait != null) animalPortrait.sprite = sprite; }

        public void ShowBanner(string text, float duration = 1.2f)
        {
            if (bannerRoot == null || bannerText == null) return;
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(BannerRoutine(text, duration));
        }

        private IEnumerator BannerRoutine(string text, float duration)
        {
            bannerRoot.SetActive(true);
            bannerText.text = text;
            yield return new WaitForSecondsRealtime(duration);
            bannerRoot.SetActive(false);
            _bannerRoutine = null;
        }

        public void ShowPause(bool visible) { if (pauseRoot != null) pauseRoot.SetActive(visible); }

        public void ShowResult(bool won, int score, int stars, int coins)
        {
            if (resultRoot != null) resultRoot.SetActive(true);
            if (resultTitle != null) resultTitle.text = won ? "MEGA VICTORY!" : "MISSION FAILED";
            if (resultSummary != null) resultSummary.text = won
                ? $"Score {score:N0}\nStars {stars}/3\n+{coins} coins"
                : $"Score {score:N0}\nTry a different flight path.";
        }

        public void ShowUnlock(SuperAnimalData animal)
        {
            if (unlockRoot == null || animal == null) return;
            unlockRoot.SetActive(true);
            if (unlockText != null) unlockText.text = $"NEW SUPER ANIMAL\n{animal.displayName}";
        }

        public void HideUnlock() { if (unlockRoot != null) unlockRoot.SetActive(false); }
    }
}
