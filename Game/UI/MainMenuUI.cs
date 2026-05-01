using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming you are using TextMeshPro

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI levelText; // Drag the text inside the button here

    private void Start()
    {
        UpdateLevelUI();
        
        // Remove old listeners to prevent double clicks
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(OnPlayClicked);
    }

    private void UpdateLevelUI()
    {
        // 1. Get the highest level the player has reached (Default is 0)
        int nextLevelIndex = LevelManager.Instance.GetHighestUnlockedLevel();
        
        // 2. Check if we have run out of levels
        if (nextLevelIndex >= LevelManager.Instance.TotalLevels)
        {
            // Player finished all levels, loop back to start or show "Done"
            // For now, let's wrap around or show the last level
            nextLevelIndex = 0; 
        }

        // 3. Update Text (Index 0 = "Level 1")
        levelText.text = "Level " + (nextLevelIndex + 1);
    }

    private void OnPlayClicked()
    {
        // Get the level we want to play
        int levelToLoad = LevelManager.Instance.GetHighestUnlockedLevel();
        
        // Safety check for array bounds
        if (levelToLoad >= LevelManager.Instance.TotalLevels) levelToLoad = 0;

        // Tell LevelManager to load this specific index
        LevelManager.Instance.LoadGameSceneForLevel(levelToLoad);
    }
}