using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AnimalFall.UI.Screens
{
    public class JourneyMapNode : MonoBehaviour
    {
        [SerializeField] private Button nodeButton;
        [SerializeField] private TMP_Text levelNumberText;
        [SerializeField] private Image nodeIcon;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image completedCheck;
        [SerializeField] private Image megaLevelGlow;
        [SerializeField] private Image pathLine;

        public int LevelIndex { get; private set; }
        public event Action<int> OnNodeClicked;

        public void Setup(int levelIndex, NodeState state, bool isMegaLevel)
        {
            LevelIndex = levelIndex;
            levelNumberText.text = (levelIndex + 1).ToString();

            lockIcon.gameObject.SetActive(state == NodeState.Locked);
            completedCheck.gameObject.SetActive(state == NodeState.Completed);
            megaLevelGlow.gameObject.SetActive(isMegaLevel);

            nodeButton.interactable = state != NodeState.Locked;

            switch (state)
            {
                case NodeState.Locked:
                    nodeIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                    break;
                case NodeState.Unlocked:
                    nodeIcon.color = Color.white;
                    break;
                case NodeState.Current:
                    nodeIcon.color = Color.yellow;
                    break;
                case NodeState.Completed:
                    nodeIcon.color = new Color(0.6f, 1f, 0.6f);
                    break;
            }

            if (isMegaLevel)
                transform.localScale = Vector3.one * 1.3f;

            nodeButton.onClick.RemoveAllListeners();
            nodeButton.onClick.AddListener(() => OnNodeClicked?.Invoke(LevelIndex));
        }

        public void ShowPathTo(bool show)
        {
            if (pathLine != null)
                pathLine.gameObject.SetActive(show);
        }
    }

    public enum NodeState
    {
        Locked,
        Unlocked,
        Current,
        Completed
    }
}
