using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Core.Arcade;

namespace AnimalFall.UI.Screens
{
    public class ArcadeRoomController : MonoBehaviour
    {
        [Header("Game Selection")]
        [SerializeField] private TMP_Text eventTitleText;
        [SerializeField] private TMP_Text eventDescriptionText;
        [SerializeField] private Image eventIcon;
        [SerializeField] private TMP_Text tokenCostText;
        [SerializeField] private TMP_Text highScoreText;

        [Header("Token Display")]
        [SerializeField] private TMP_Text tokensOwnedText;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button backButton;

        [Header("Game References")]
        [SerializeField] private GameObject gorillaArtilleryRoot;
        [SerializeField] private GameObject rhinoDemolitionRoot;
        [SerializeField] private GameObject armadilloRicochetRoot;

        private ArcadeSessionData currentEvent;

        private void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            LoadCurrentEvent();
        }

        private void Update()
        {
            if (ArcadeTokenService.Instance != null && tokensOwnedText != null)
                tokensOwnedText.text = "Tokens: " + ArcadeTokenService.Instance.CurrentTokens;
        }

        private void LoadCurrentEvent()
        {
            if (PhysicsMiniGameManager.Instance == null) return;

            currentEvent = PhysicsMiniGameManager.Instance.GetCurrentEventGame();
            if (currentEvent == null) return;

            if (eventTitleText != null) eventTitleText.text = currentEvent.displayName;
            if (eventDescriptionText != null) eventDescriptionText.text = currentEvent.description;
            if (eventIcon != null && currentEvent.icon != null) eventIcon.sprite = currentEvent.icon;
            if (tokenCostText != null) tokenCostText.text = "Cost: " + currentEvent.tokenCost + " Token(s)";

            int hs = PlayerPrefs.GetInt("arcade_hs_" + currentEvent.gameType, 0);
            if (highScoreText != null) highScoreText.text = "High Score: " + hs;
        }

        private void OnPlayClicked()
        {
            if (currentEvent == null) return;

            if (ArcadeTokenService.Instance == null || !ArcadeTokenService.Instance.HasTokens(currentEvent.tokenCost))
            {
                Debug.Log("[ArcadeRoom] Not enough tokens.");
                return;
            }

            ActivateGameRoot(currentEvent.gameType);

            IArcadeMiniGame gameInstance = GetGameInstance(currentEvent.gameType);
            if (gameInstance == null) return;

            PhysicsMiniGameManager.Instance.TryStartGame(currentEvent, gameInstance);
        }

        private void ActivateGameRoot(MiniGameType type)
        {
            if (gorillaArtilleryRoot != null) gorillaArtilleryRoot.SetActive(type == MiniGameType.GorillaArtillery);
            if (rhinoDemolitionRoot != null) rhinoDemolitionRoot.SetActive(type == MiniGameType.RhinoDemolition);
            if (armadilloRicochetRoot != null) armadilloRicochetRoot.SetActive(type == MiniGameType.ArmadilloRicochet);
        }

        private IArcadeMiniGame GetGameInstance(MiniGameType type)
        {
            switch (type)
            {
                case MiniGameType.GorillaArtillery:
                    return gorillaArtilleryRoot?.GetComponentInChildren<IArcadeMiniGame>();
                case MiniGameType.RhinoDemolition:
                    return rhinoDemolitionRoot?.GetComponentInChildren<IArcadeMiniGame>();
                case MiniGameType.ArmadilloRicochet:
                    return armadilloRicochetRoot?.GetComponentInChildren<IArcadeMiniGame>();
                default:
                    return null;
            }
        }

        private void OnBackClicked()
        {
            if (PhysicsMiniGameManager.Instance != null && PhysicsMiniGameManager.Instance.IsPlaying)
                PhysicsMiniGameManager.Instance.ForceEnd();

            if (Managers.GameStateManager.Instance != null)
                Managers.GameStateManager.Instance.TransitionTo(Managers.GameState.MainMenu);
        }
    }
}
