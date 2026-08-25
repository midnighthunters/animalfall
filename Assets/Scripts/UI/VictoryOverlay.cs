// VictoryOverlay — big celebratory text animation on level complete
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using AnimalFall.Managers;

namespace AnimalFall.UI
{
    public class VictoryOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private TextMeshProUGUI _primaryButtonLabel;
        [SerializeField] private Button _homeButton;

        [Header("Copy")]
        [SerializeField] private string _winTitle = "LEVEL CLEARED!";
        [SerializeField] private string _winSubtitle = "Every animal is safe";
        [SerializeField] private string _loseTitle = "TIME'S UP!";
        [SerializeField] private string _loseSubtitle = "So close — give it another try";

        private bool _lastResultWasWin;

        private void OnEnable()
        {
            GameEvents.OnLevelWon    += OnWon;
            GameEvents.OnLevelFailed += OnFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelWon    -= OnWon;
            GameEvents.OnLevelFailed -= OnFailed;
        }

        private void Awake()
        {
            if (_root != null) _root.SetActive(false);
            if (_canvasGroup == null && _root != null)
                _canvasGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();

            if (_primaryButton != null)
                _primaryButton.onClick.AddListener(OnPrimaryPressed);
            if (_homeButton != null)
                _homeButton.onClick.AddListener(ReturnHome);
        }

        private void OnWon()    => Play(true);
        private void OnFailed() => Play(false);

        public void Play(bool won)
        {
            _lastResultWasWin = won;
            StopAllCoroutines();
            StartCoroutine(PlayRoutine(won));
        }

        private IEnumerator PlayRoutine(bool won)
        {
            if (_root == null) yield break;

            _root.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            if (_primaryButtonLabel != null)
                _primaryButtonLabel.text = won ? "CONTINUE" : "RETRY";

            if (_panel != null)
            {
                DOTween.Kill(_panel);
                _panel.localScale = Vector3.one * 0.78f;
                _panel.DOScale(1f, 0.34f).SetEase(Ease.OutBack).SetUpdate(true).SetId(_panel);
            }

            if (_titleText != null)
            {
                _titleText.text = won ? _winTitle : _loseTitle;
                _titleText.color = Color.white;
                _titleText.transform.localScale = Vector3.one * 0.2f;
                _titleText.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = won ? _winSubtitle : _loseSubtitle;
                _subtitleText.color = new Color(0.12f, 0.28f, 0.55f, 0f);
                _subtitleText.transform.localScale = Vector3.one;
            }

            // Fade backdrop
            if (_canvasGroup != null)
                _canvasGroup.DOFade(1f, 0.25f).SetUpdate(true);

            // Title slam-in
            if (_titleText != null)
            {
                DOTween.Kill(_titleText.transform);
                var seq = DOTween.Sequence().SetUpdate(true).SetId(_titleText.transform);
                seq.Append(_titleText.transform.DOScale(1.35f, 0.35f).SetEase(Ease.OutBack));
                seq.Append(_titleText.transform.DOScale(1f, 0.18f).SetEase(Ease.InOutSine));
                if (won)
                {
                    seq.Append(_titleText.transform.DOPunchScale(Vector3.one * 0.15f, 0.45f, 8, 0.7f));
                    // Gentle float
                    seq.Join(_titleText.rectTransform
                        .DOAnchorPosY(30f, 0.8f)
                        .SetEase(Ease.OutSine)
                        .SetLoops(2, LoopType.Yoyo));
                }
            }

            yield return new WaitForSecondsRealtime(0.35f);

            if (_subtitleText != null)
            {
                _subtitleText.DOFade(1f, 0.4f).SetUpdate(true);
                _subtitleText.transform.DOScale(1.05f, 0.4f).From(0.8f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            // Keep the result card visible until the player chooses an action.
        }

        private void OnPrimaryPressed()
        {
            if (_lastResultWasWin)
            {
                ReturnHome();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static void ReturnHome()
        {
            Time.timeScale = 1f;
            if (LevelManager.Instance != null)
                LevelManager.Instance.ReturnToMainScene();
            else
                SceneManager.LoadScene("MainScene");
        }
    }
}
