// JourneyMapController — dynamic map driven by LevelDatabase.TotalLevels.
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AnimalFall.Managers;
using AnimalFall.Utils;
using AnimalFall.Services;
using AnimalFall.Data;

namespace AnimalFall.UI
{
    public class JourneyMapController : MonoBehaviour
    {
        [SerializeField] private Transform   _nodeContainer;
        [SerializeField] private GameObject  _nodePrefab;
        [SerializeField] private ScrollRect  _scrollRect;
        [SerializeField] private SaveService _save;
        [SerializeField] private LevelDatabase _levelDatabase;
        [Header("Mega Shooter Nodes (optional, replaceable)")]
        [SerializeField] private Sprite _megaUnlockedIcon;
        [SerializeField] private Sprite _megaLockedIcon;
        [SerializeField] private Color _megaTint = new Color(0.35f, 0.95f, 1f, 1f);

        private static readonly string[] CHAPTER_NAMES =
            { "Sunny Meadow", "Tropical Jungle", "Snowy Arctic", "Mystic Forest", "Storm Peaks" };

        private JourneyMapNode[] _nodes = Array.Empty<JourneyMapNode>();

        private void Start()
        {
            BuildMap();
            ScrollToFirstIncomplete();
        }

        private void BuildMap()
        {
            if (_nodePrefab == null || _nodeContainer == null) return;

            LevelDatabase database = _levelDatabase != null ? _levelDatabase : LevelManager.Instance?.Database;
            int totalLevels = database != null ? database.TotalLevels : 0;
            if (totalLevels <= 0) return;
            _nodes = new JourneyMapNode[totalLevels];
            int highestUnlocked = _save != null ? _save.GetHighestUnlockedLevel() : 0;

            for (int i = 0; i < totalLevels; i++)
            {
                int levelNum = i + 1;
                LevelData level = database.GetLevelOrNull(i);
                bool available = level != null;
                bool isMega = level != null && level.IsConfiguredMegaShooter;
                var go   = Instantiate(_nodePrefab, _nodeContainer);
                var node = go.GetComponent<JourneyMapNode>();
                if (node == null) node = go.AddComponent<JourneyMapNode>();

                int   stars   = _save?.GetStars(i) ?? -1;
                bool  unlocked = available && i <= highestUnlocked;
                bool  current  = available && i == highestUnlocked;

                Sprite btn = isMega
                    ? (unlocked ? _megaUnlockedIcon : _megaLockedIcon)
                    : (unlocked ? ImageLibrary.GetLevelButton1() : ImageLibrary.GetLevelButton2());
                if (btn == null)
                    btn = unlocked ? ImageLibrary.GetLevelButton1() : ImageLibrary.GetLevelButton2();

                node.Setup(levelNum, stars, unlocked, btn, isMega, _megaTint);

                int capturedIndex = i;
                var btn2 = go.GetComponent<Button>();
                if (btn2 != null)
                    btn2.onClick.AddListener(() => OnNodeTapped(capturedIndex, unlocked, go));

                // Pulse current node
                if (current)
                    go.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetId(go);

                _nodes[i] = node;
            }
        }

        private void OnNodeTapped(int index, bool unlocked, GameObject nodeGO)
        {
            if (!unlocked)
            {
                nodeGO.transform.DOShakePosition(0.3f, 5f, 10).SetId(nodeGO);
                // Show locked toast
                return;
            }
            LevelManager.Instance?.LoadGameSceneForLevel(index);
        }

        private void ScrollToFirstIncomplete()
        {
            if (_save == null || _scrollRect == null) return;
            int maxIndex = Mathf.Max(1, _nodes.Length - 1);
            int firstIncomplete = Mathf.Clamp(_save.GetHighestUnlockedLevel(), 0, maxIndex);
            float normalizedPos = 1f - (firstIncomplete / (float)maxIndex);
            _scrollRect.verticalNormalizedPosition = normalizedPos;
        }
    }

    public class JourneyMapNode : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text  _levelLabel;
        [SerializeField] private Image[] _stars;

        public void Setup(int levelNum, int starCount, bool unlocked, Sprite icon, bool isMega, Color megaTint)
        {
            if (_levelLabel != null) _levelLabel.text = levelNum.ToString();
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.color = isMega ? megaTint : Color.white;
            }

            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null) _stars[i].enabled = (i < starCount);
                }
            }
        }
    }
}
