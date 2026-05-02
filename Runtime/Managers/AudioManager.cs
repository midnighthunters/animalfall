using UnityEngine;

namespace AnimalFall.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public enum SfxType
        {
            Collect,
            WrongTap,
            Explosion,
            LevelWin,
            LevelLose,
            ShieldBreak,
            ButtonClick,
            PowerUpActivate
        }

        [Header("Clips")]
        [SerializeField] private AudioClip collectClip;
        [SerializeField] private AudioClip wrongTapClip;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private AudioClip levelWinClip;
        [SerializeField] private AudioClip levelLoseClip;
        [SerializeField] private AudioClip shieldBreakClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip powerUpActivateClip;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        private AudioSource sfxSource;
        private AudioSource musicSource;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        public void PlaySFX(SfxType type)
        {
            AudioClip clip = GetClipForType(type);
            if (clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        private AudioClip GetClipForType(SfxType type)
        {
            switch (type)
            {
                case SfxType.Collect:          return collectClip;
                case SfxType.WrongTap:         return wrongTapClip;
                case SfxType.Explosion:        return explosionClip;
                case SfxType.LevelWin:         return levelWinClip;
                case SfxType.LevelLose:        return levelLoseClip;
                case SfxType.ShieldBreak:      return shieldBreakClip;
                case SfxType.ButtonClick:      return buttonClickClip;
                case SfxType.PowerUpActivate:  return powerUpActivateClip;
                default:                       return null;
            }
        }
    }
}
