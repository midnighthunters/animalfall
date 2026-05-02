using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AnimalFall.Managers;

namespace AnimalFall.UI.Screens
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;

        private void OnEnable()
        {
            resumeButton?.onClick.RemoveAllListeners();
            restartButton?.onClick.RemoveAllListeners();
            mainMenuButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();

            resumeButton?.onClick.AddListener(OnResume);
            restartButton?.onClick.AddListener(OnRestart);
            mainMenuButton?.onClick.AddListener(OnMainMenu);
            settingsButton?.onClick.AddListener(OnSettings);
        }

        private void OnResume()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.PopPause();
            gameObject.SetActive(false);
        }

        private void OnRestart()
        {
            Time.timeScale = 1f;
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadGameSceneForLevel(
                    LevelManager.Instance.CurrentLevelIndex);
            gameObject.SetActive(false);
        }

        private void OnMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainScene");
        }

        private void OnSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}
