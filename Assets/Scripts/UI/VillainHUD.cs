// Task 8.1 — VillainHUD: HP bar, phase transitions
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AnimalFall.Data;
using AnimalFall.Managers;

namespace AnimalFall.UI
{
    public class VillainHUD : MonoBehaviour
    {
        [SerializeField] private Image  _portrait;
        [SerializeField] private Image  _hpBar;
        [SerializeField] private GameObject _villainRoot;

        private void OnEnable()  => GameEvents.OnVillainPhaseChanged += OnPhaseChanged;
        private void OnDisable() => GameEvents.OnVillainPhaseChanged -= OnPhaseChanged;

        public void Setup(VillainData data)
        {
            if (_portrait != null && data.portrait != null)
                _portrait.sprite = data.portrait;
            if (_hpBar != null)
                _hpBar.fillAmount = 1f;
            Show();
        }

        private void Show()
        {
            if (_villainRoot != null) _villainRoot.SetActive(true);
        }

        private void OnPhaseChanged(int current, int total)
        {
            if (_hpBar == null) return;
            float target = 1f - ((float)current / total);
            DOTween.Kill(_hpBar);
            _hpBar.DOFillAmount(target, 0.3f);

            // Phase transition: screen flash + punch scale
            Effects.ScreenEffects.Instance?.FlashWhite();
            if (_portrait != null)
                _portrait.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 0.5f).SetId(_portrait.gameObject);
        }
    }
}
