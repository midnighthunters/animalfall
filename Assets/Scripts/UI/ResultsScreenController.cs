// Task 10.4 — ResultsScreenController: star reveal, win/lose panels
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AnimalFall.Utils;
using AnimalFall.Managers;
using AnimalFall.Services;

namespace AnimalFall.UI
{
    public class ResultsScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject  _winRoot;
        [SerializeField] private GameObject  _loseRoot;
        [SerializeField] private Text        _scoreText;
        [SerializeField] private Text        _coinsText;
        [SerializeField] private Image[]     _starIcons;    // 3 star Image components
        [SerializeField] private Image       _winPanelBg;
        [SerializeField] private Image       _losePanelBg;
        [SerializeField] private Button      _retryButton;
        [SerializeField] private Button      _quitButton;

        private void OnEnable()  => GameEvents.OnLevelWon  += HandleLevelWon;
        private void OnDisable() => GameEvents.OnLevelWon  -= HandleLevelWon;

        private void HandleLevelWon() => ShowWin(0, 0, false);

        public void ShowWin(int score, int coins, bool isMegaLevel)
        {
            if (_winPanelBg != null) _winPanelBg.sprite = ImageLibrary.GetPanel();
            if (_scoreText  != null) _scoreText.text    = score.ToString("N0");
            if (_coinsText  != null) _coinsText.text    = $"+{coins}";

            if (_winRoot != null)
            {
                _winRoot.SetActive(true);
                _winRoot.transform.localScale = Vector3.zero;
                _winRoot.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetId(_winRoot);
            }

            StartCoroutine(StarReveal(3)); // TODO: pass actual star count
        }

        public void ShowLose(int score)
        {
            if (_losePanelBg != null) _losePanelBg.sprite = ImageLibrary.GetRedButtons();
            if (_scoreText   != null) _scoreText.text     = score.ToString("N0");

            if (_retryButton != null)
            {
                var img = _retryButton.GetComponent<Image>();
                if (img != null) img.sprite = ImageLibrary.GetRedButtons();
            }

            if (_loseRoot != null)
            {
                _loseRoot.SetActive(true);
                _loseRoot.transform.localScale = Vector3.zero;
                _loseRoot.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetId(_loseRoot);
            }
        }

        private IEnumerator StarReveal(int starCount)
        {
            if (_starIcons == null) yield break;

            for (int i = 0; i < _starIcons.Length; i++)
            {
                if (_starIcons[i] == null) continue;
                _starIcons[i].gameObject.SetActive(i < starCount);
                if (i < starCount)
                {
                    _starIcons[i].transform.localScale = Vector3.zero;
                    _starIcons[i].transform.DOScale(1f, 0.3f)
                        .SetEase(Ease.OutBounce)
                        .SetId(_starIcons[i].gameObject);
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
    }
}
