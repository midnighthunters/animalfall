// Task 6.4 — AudioManager: 12 pooled AudioSources, SfxType enum, pitch modulation
using UnityEngine;

namespace AnimalFall.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private const string MusicMutedKey = "settings_music_muted";
        private const string SfxMutedKey = "settings_sfx_muted";

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioClip[] _sfxClips; // indexed by SfxType
        [SerializeField] private AudioSource _bgmSource;

        private AudioSource[] _pool;
        private int           _poolSize = 12;
        private int           _lastUsed = 0;
        private bool          _musicMuted;
        private bool          _sfxMuted;

        public bool IsMusicMuted => _musicMuted;
        public bool IsSfxMuted => _sfxMuted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildPool();
            _musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            _sfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
            ApplyMusicMute();
        }

        private void OnEnable()
        {
            GameEvents.OnAnimalCollected  += OnAnimalCollected;
            GameEvents.OnWrongTap         += OnWrongTap;
            GameEvents.OnBombTapped       += OnBombTapped;
            GameEvents.OnLevelWon         += OnLevelWon;
            GameEvents.OnLevelFailed      += OnLevelFailed;
            GameEvents.OnSfxRequested     += OnSfxRequested;
            GameEvents.OnSfxRequestedPitch += OnSfxRequestedPitch;
        }

        private void OnDisable()
        {
            GameEvents.OnAnimalCollected  -= OnAnimalCollected;
            GameEvents.OnWrongTap         -= OnWrongTap;
            GameEvents.OnBombTapped       -= OnBombTapped;
            GameEvents.OnLevelWon         -= OnLevelWon;
            GameEvents.OnLevelFailed      -= OnLevelFailed;
            GameEvents.OnSfxRequested     -= OnSfxRequested;
            GameEvents.OnSfxRequestedPitch -= OnSfxRequestedPitch;
        }

        private void OnSfxRequested(SfxType t) => PlaySFX(t);
        private void OnSfxRequestedPitch(SfxType t, float p) => PlaySFX(t, p);

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnAnimalCollected(Core.Animals.AnimalSpecies s, Core.Animals.AnimalType t, UnityEngine.Vector3 _) => PlaySFX(SfxType.Collect);
        private void OnWrongTap()         => PlaySFX(SfxType.WrongTap);
        private void OnBombTapped(UnityEngine.Vector3 _) => PlaySFX(SfxType.Explosion);
        private void OnLevelWon()         => PlaySFX(SfxType.LevelWin);
        private void OnLevelFailed()      => PlaySFX(SfxType.LevelLose);

        // ── Public API ────────────────────────────────────────────────────────

        public void PlaySFX(SfxType type, float pitch = 1f)
        {
            if (_sfxMuted) return;
            int idx = (int)type;
            if (_sfxClips == null || idx >= _sfxClips.Length || _sfxClips[idx] == null)
                return; // null clip → silent skip

            var source = BorrowSource();
            source.pitch = pitch;
            source.PlayOneShot(_sfxClips[idx]);
        }

        public void ToggleMusicMuted() => SetMusicMuted(!_musicMuted);

        public void SetMusicMuted(bool muted)
        {
            _musicMuted = muted;
            PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMusicMute();
        }

        public void ToggleSfxMuted() => SetSfxMuted(!_sfxMuted);

        public void SetSfxMuted(bool muted)
        {
            _sfxMuted = muted;
            PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void BuildPool()
        {
            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(transform);
                _pool[i] = go.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
            }
        }

        private void ApplyMusicMute()
        {
            if (_bgmSource != null) _bgmSource.mute = _musicMuted;
        }

        private AudioSource BorrowSource()
        {
            // Find first idle source
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying) return _pool[i];
            }
            // All 12 busy — interrupt oldest-playing
            _lastUsed = (_lastUsed + 1) % _pool.Length;
            _pool[_lastUsed].Stop();
            return _pool[_lastUsed];
        }
    }
}
