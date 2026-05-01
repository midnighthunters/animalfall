using UnityEngine;
using UnityEngine.UI;
using AnimalFall.Managers;
using AnimalFall.Services.Save;

namespace AnimalFall.UI.Screens
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Button closeButton;

        private void OnEnable()
        {
            if (SaveService.Instance != null)
            {
                sfxSlider.value = SaveService.Instance.GetSFXVolume();
                musicSlider.value = SaveService.Instance.GetMusicVolume();
            }

            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);

            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        }

        private void OnSFXChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(value);
            if (SaveService.Instance != null)
                SaveService.Instance.SetSFXVolume(value);
        }

        private void OnMusicChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
            if (SaveService.Instance != null)
                SaveService.Instance.SetMusicVolume(value);
        }
    }
}
