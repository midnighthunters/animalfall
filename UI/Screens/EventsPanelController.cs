using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Managers;

namespace AnimalFall.UI.Screens
{
    public class EventsPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform questContainer;
        [SerializeField] private Transform eventContainer;
        [SerializeField] private GameObject questItemPrefab;
        [SerializeField] private GameObject eventItemPrefab;
        [SerializeField] private Button closeButton;

        [Header("Header")]
        [SerializeField] private TMP_Text dailyResetTimerText;

        private void OnEnable()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            PopulateQuests();
            PopulateEvents();
        }

        private void Update()
        {
            UpdateResetTimer();
        }

        private void PopulateQuests()
        {
            if (questContainer == null || questItemPrefab == null) return;
            if (EventManager.Instance == null) return;

            foreach (Transform child in questContainer)
                Destroy(child.gameObject);

            var quests = EventManager.Instance.ActiveQuests;
            foreach (var quest in quests)
            {
                GameObject item = Instantiate(questItemPrefab, questContainer);

                TMP_Text nameText = item.transform.Find("Name")?.GetComponent<TMP_Text>();
                TMP_Text progressText = item.transform.Find("Progress")?.GetComponent<TMP_Text>();
                Image progressBar = item.transform.Find("ProgressBar")?.GetComponent<Image>();
                Button claimButton = item.transform.Find("ClaimButton")?.GetComponent<Button>();

                if (nameText != null) nameText.text = quest.Description;
                if (progressText != null)
                    progressText.text = $"{quest.CurrentProgress}/{quest.TargetValue}";
                if (progressBar != null)
                    progressBar.fillAmount = quest.ProgressPercent;

                bool canClaim = quest.IsCompleted && !quest.IsClaimed;
                if (claimButton != null)
                {
                    claimButton.gameObject.SetActive(canClaim);
                    if (canClaim)
                    {
                        string questId = quest.QuestId;
                        claimButton.onClick.AddListener(
                            () => EventManager.Instance.ClaimReward(questId));
                    }
                }
            }
        }

        private void PopulateEvents()
        {
            if (eventContainer == null || eventItemPrefab == null) return;
            if (EventManager.Instance == null) return;

            foreach (Transform child in eventContainer)
                Destroy(child.gameObject);

            var events = EventManager.Instance.ActiveEvents;
            foreach (var evt in events)
            {
                GameObject item = Instantiate(eventItemPrefab, eventContainer);

                TMP_Text nameText = item.transform.Find("Name")?.GetComponent<TMP_Text>();
                TMP_Text descText = item.transform.Find("Description")?.GetComponent<TMP_Text>();
                TMP_Text timerText = item.transform.Find("Timer")?.GetComponent<TMP_Text>();

                if (nameText != null) nameText.text = evt.displayName;
                if (descText != null) descText.text = evt.description;
                if (timerText != null)
                    timerText.text = FormatTimeRemaining(evt.RemainingSeconds);
            }
        }

        private void UpdateResetTimer()
        {
            if (dailyResetTimerText == null || EventManager.Instance == null) return;
            float remaining = EventManager.Instance.TimeUntilDailyReset;
            dailyResetTimerText.text = "Resets in: " + FormatTimeRemaining(remaining);
        }

        private string FormatTimeRemaining(float seconds)
        {
            int hours = Mathf.FloorToInt(seconds / 3600f);
            int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{hours:00}:{minutes:00}:{secs:00}";
        }
    }
}
