using UnityEngine;
using UnityEngine.UI;
using AnimalFall.Managers;

namespace AnimalFall.UI.Screens
{
    public class JourneyMapController : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private JourneyMapNode nodePrefab;

        [Header("Layout")]
        [SerializeField] private float nodeSpacing = 150f;
        [SerializeField] private float zigzagOffset = 100f;

        private JourneyMapNode[] nodes;

        private void OnEnable()
        {
            PopulateMap();
        }

        public void PopulateMap()
        {
            if (nodePrefab == null || contentContainer == null) return;
            if (LevelManager.Instance == null) return;

            ClearNodes();

            int totalLevels = LevelManager.Instance.TotalLevels;
            int highestUnlocked = LevelManager.Instance.GetHighestUnlockedLevel();
            nodes = new JourneyMapNode[totalLevels];

            float contentHeight = totalLevels * nodeSpacing + 200f;
            contentContainer.sizeDelta = new Vector2(
                contentContainer.sizeDelta.x, contentHeight);

            for (int i = 0; i < totalLevels; i++)
            {
                JourneyMapNode node = Instantiate(nodePrefab, contentContainer);
                RectTransform rt = node.GetComponent<RectTransform>();

                float xOffset = (i % 2 == 0) ? -zigzagOffset : zigzagOffset;
                rt.anchoredPosition = new Vector2(xOffset, i * nodeSpacing + 100f);

                NodeState state;
                if (i < highestUnlocked)
                    state = NodeState.Completed;
                else if (i == highestUnlocked)
                    state = NodeState.Current;
                else if (i == highestUnlocked + 1)
                    state = NodeState.Unlocked;
                else
                    state = NodeState.Locked;

                bool isMega = (i + 1) % 5 == 0;
                node.Setup(i, state, isMega);
                node.OnNodeClicked += OnLevelSelected;
                node.ShowPathTo(i < totalLevels - 1);

                nodes[i] = node;
            }

            ScrollToLevel(highestUnlocked);
        }

        public void ScrollToLevel(int levelIndex)
        {
            if (scrollRect == null || contentContainer == null || nodes == null) return;
            if (levelIndex < 0 || levelIndex >= nodes.Length) return;

            float normalizedPos = (float)levelIndex / Mathf.Max(1, nodes.Length - 1);
            scrollRect.verticalNormalizedPosition = normalizedPos;
        }

        private void OnLevelSelected(int levelIndex)
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadGameSceneForLevel(levelIndex);
        }

        private void ClearNodes()
        {
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node != null)
                    {
                        node.OnNodeClicked -= OnLevelSelected;
                        Destroy(node.gameObject);
                    }
                }
            }
            nodes = null;
        }

        private void OnDisable()
        {
            ClearNodes();
        }
    }
}
