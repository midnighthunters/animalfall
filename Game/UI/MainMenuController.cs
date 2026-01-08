using UnityEngine;
using UnityEngine.UI;
using TMPro; // If using TextMeshPro

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelButtonText;
    [SerializeField] private Button playButton;

    private void Start()
    {
        UpdateUI();

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(OnPlayClicked);
    }

    private void UpdateUI()
    {
        // Get the level index (0-based)
        int currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);

        // Display it (User sees "Level 1", "Level 2", etc.)
        levelButtonText.text = "Level " + (currentLevelIndex + 1);
    }

    private void OnPlayClicked()
    {
        // Tell LevelManager to load the game
        LevelManager.Instance.LoadCurrentLevel();
    }
}