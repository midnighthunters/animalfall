// ============================================================
//  MainMenuUI.cs  –  Animal Fall  (REFACTORED)
//  Previously: MainMenuManager
//  Changes:
//    • Subscribes to OnCoinsChanged for live coin display
//    • Subscribes to OnSaveDataLoaded to refresh UI
//    • FirebaseManager.Instance.DisplayName shown
//    • Button hooks wired in Awake, not Start (order-safe)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text  levelText;
    [SerializeField] private TMP_Text  coinsText;
    [SerializeField] private TMP_Text  playerNameText;

    [Header("Buttons")]
    [SerializeField] private Button    playButton;
    [SerializeField] private Button    settingsButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        playButton?.onClick.RemoveAllListeners();
        playButton?.onClick.AddListener(OnPlayClicked);

        settingsButton?.onClick.RemoveAllListeners();
        settingsButton?.onClick.AddListener(OnSettingsClicked);
    }

    private void Start()
    {
        EventBus.Subscribe<OnCoinsChanged>   (OnCoinsChanged);
        EventBus.Subscribe<OnSaveDataLoaded> (OnSaveLoaded);
        EventBus.Subscribe<OnFirebaseAuthReady>(OnAuthReady);

        RefreshAll();
        AudioManager.Instance?.PlayMusic(AudioManager.MusicTrack.MainMenu);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnCoinsChanged>   (OnCoinsChanged);
        EventBus.Unsubscribe<OnSaveDataLoaded> (OnSaveLoaded);
        EventBus.Unsubscribe<OnFirebaseAuthReady>(OnAuthReady);
    }

    // ── Event handlers ────────────────────────────────────────
    private void OnCoinsChanged(OnCoinsChanged e)      => SetCoins(e.newTotal);
    private void OnSaveLoaded(OnSaveDataLoaded _)      => RefreshAll();
    private void OnAuthReady(OnFirebaseAuthReady e)    => SetPlayerName(e.isSignedIn
        ? FirebaseManager.Instance?.DisplayName ?? "Player"
        : "Guest");

    // ── UI refresh ────────────────────────────────────────────
    private void RefreshAll()
    {
        UpdateLevelUI();

        if (SaveManager.Instance != null)
            SetCoins(SaveManager.Instance.GetCoins());

        if (FirebaseManager.Instance != null)
            SetPlayerName(FirebaseManager.Instance.IsSignedIn
                ? FirebaseManager.Instance.DisplayName
                : "Guest");
    }

    private void UpdateLevelUI()
    {
        int next = LevelManager.Instance?.GetHighestUnlockedLevel() ?? 0;
        if (LevelManager.Instance != null && next >= LevelManager.Instance.TotalLevels)
            next = 0;

        if (levelText) levelText.text = "Level " + (next + 1);
    }

    private void SetCoins(int coins)
    {
        if (coinsText) coinsText.text = coins.ToString("N0");
    }

    private void SetPlayerName(string name)
    {
        if (playerNameText) playerNameText.text = name;
    }

    // ── Button handlers ───────────────────────────────────────
    private void OnPlayClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.UIClick);
        int idx = LevelManager.Instance?.GetHighestUnlockedLevel() ?? 0;
        if (LevelManager.Instance != null && idx >= LevelManager.Instance.TotalLevels)
            idx = 0;
        LevelManager.Instance?.LoadGameSceneForLevel(idx);
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.UIClick);
        settingsPanel?.SetActive(!(settingsPanel.activeSelf));
    }
}