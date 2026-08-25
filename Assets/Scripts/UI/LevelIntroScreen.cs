// Task 10.3 — LevelIntroScreen: shows level objectives before gameplay
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AnimalFall.Data;
using AnimalFall.Utils;
using AnimalFall.Managers;

namespace AnimalFall.UI
{
    public class LevelIntroScreen : MonoBehaviour
    {
        [SerializeField] private Text       _levelNumberText;
        [SerializeField] private Text       _chapterNameText;
        [SerializeField] private Text       _timeLimitText;
        [SerializeField] private Transform  _goalIconsRoot;
        [SerializeField] private Transform  _hindranceIconsRoot;
        [SerializeField] private GameObject _iconPrefab;
        [SerializeField] private GameObject _root;

        private bool _tapReceived;
        private InputManager _input;

        public void Show(LevelData level, InputManager input, Action onDismiss)
        {
            _input = input;
            _input?.BlockInput(true);
            _tapReceived = false;

            if (_levelNumberText != null) _levelNumberText.text = $"Level {level.LevelNumber}";
            if (_chapterNameText != null) _chapterNameText.text = level.ChapterTheme;
            if (_timeLimitText   != null) _timeLimitText.text   = $"{level.TimeLimit:F0}s";

            // Goal icons
            if (_goalIconsRoot != null && level.Goal != null)
            {
                foreach (Transform child in _goalIconsRoot) Destroy(child.gameObject);
                var targets = level.Goal.Targets;
                for (int i = 0; i < targets.Length; i++)
                {
                    var icon = Instantiate(_iconPrefab, _goalIconsRoot);
                    var img  = icon.GetComponent<Image>();
                    if (img != null) img.sprite = ImageLibrary.GetAnimalSprite(targets[i].species);
                    var label = icon.GetComponentInChildren<Text>();
                    if (label != null) label.text = $"x{targets[i].count}";
                }
            }

            // Entrance
            if (_root != null)
            {
                _root.SetActive(true);
                _root.transform.localScale = Vector3.zero;
                _root.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetId(_root);
            }

            // Subscribe to tap for skip
            GameEvents.OnScreenTapped += OnTapToSkip;
            StartCoroutine(HoldThenDismiss(onDismiss));
        }

        private void OnTapToSkip(Vector2 _) => _tapReceived = true;

        private IEnumerator HoldThenDismiss(Action onDismiss)
        {
            float elapsed = 0f;
            while (elapsed < 2f && !_tapReceived)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            GameEvents.OnScreenTapped -= OnTapToSkip;

            // Exit animation
            if (_root != null)
            {
                _root.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetId(_root)
                    .OnComplete(() =>
                    {
                        _root.SetActive(false);
                        _input?.BlockInput(false);
                        onDismiss?.Invoke();
                    });
            }
            else
            {
                _input?.BlockInput(false);
                onDismiss?.Invoke();
            }
        }
    }
}
