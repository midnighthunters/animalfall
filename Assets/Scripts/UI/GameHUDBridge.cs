// GameHUDBridge — mirrors legacy Text values from GameUIManager into
// the visible TMP displays in the StaticCanvas TopBar / BottomBar.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AnimalFall.UI
{
    public class GameHUDBridge : MonoBehaviour
    {
        [Header("Source — legacy Text (driven by GameUIManager)")]
        [SerializeField] private Text _srcTimerText;
        [SerializeField] private Text _srcScoreText;

        [Header("Target — visible TMP in TopBar / BottomBar")]
        [SerializeField] private TextMeshProUGUI _dstTimerTMP;
        [SerializeField] private TextMeshProUGUI _dstScoreTMP;

        private string _lastTimer;
        private string _lastScore;

        private void Update()
        {
            if (_srcTimerText != null && _dstTimerTMP != null)
            {
                string v = _srcTimerText.text;
                if (v != _lastTimer) { _dstTimerTMP.text = v; _lastTimer = v; }
            }

            if (_srcScoreText != null && _dstScoreTMP != null)
            {
                string v = _srcScoreText.text;
                if (v != _lastScore) { _dstScoreTMP.text = v; _lastScore = v; }
            }
        }
    }
}
