// Task 6.4 — AudioManager: background music, soundtracks, SFX pool, mute toggles
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimalFall.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private const string MusicMutedKey = "settings_music_muted";
        private const string SfxMutedKey = "settings_sfx_muted";

        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _bgmMainScene;   // audio/everytime
        [SerializeField] private AudioClip _bgmGameScene;   // audio/level
        [SerializeField] private AudioClip _sfxVictory;     // audio/victory
        [SerializeField] private AudioClip _sfxMatch;       // audio/match
        [SerializeField] private AudioClip[] _sfxClips;     // indexed by SfxType

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _victorySource;

        private AudioSource[] _pool;
        private int           _poolSize = 12;
        private int           _lastUsed = 0;
        private bool          _musicMuted;
        private bool          _sfxMuted;
        private bool          _isVictoryActive;

        public bool IsMusicMuted => _musicMuted;
        public bool IsSfxMuted => _sfxMuted;
        public bool IsVictoryActive => _isVictoryActive;
        public AudioSource BgmSource
        {
            get
            {
                EnsureSources();
                return _bgmSource;
            }
        }
        public AudioSource VictorySource
        {
            get
            {
                EnsureSources();
                return _victorySource;
            }
        }

        public event Action<bool> OnMusicMutedChanged;
        public event Action<bool> OnSfxMutedChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        public void Init()
        {
            LoadClips();
            EnsureSources();
            BuildPool();
            SubscribeEvents();

            _musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            _sfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
            ApplyMusicMute();
        }

        private void Start()
        {
            UpdateMusicForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (_instance == this) _instance = null;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SubscribeEvents();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeEvents();
        }

        public void SubscribeEvents()
        {
            GameEvents.OnAnimalCollected   -= OnAnimalCollected;
            GameEvents.OnAnimalCollected   += OnAnimalCollected;
            GameEvents.OnWrongTap          -= OnWrongTap;
            GameEvents.OnWrongTap          += OnWrongTap;
            GameEvents.OnBombTapped        -= OnBombTapped;
            GameEvents.OnBombTapped        += OnBombTapped;
            GameEvents.OnLevelWon          -= OnLevelWon;
            GameEvents.OnLevelWon          += OnLevelWon;
            GameEvents.OnLevelFailed       -= OnLevelFailed;
            GameEvents.OnLevelFailed       += OnLevelFailed;
            GameEvents.OnSfxRequested      -= OnSfxRequested;
            GameEvents.OnSfxRequested      += OnSfxRequested;
            GameEvents.OnSfxRequestedPitch -= OnSfxRequestedPitch;
            GameEvents.OnSfxRequestedPitch += OnSfxRequestedPitch;
        }

        public void UnsubscribeEvents()
        {
            GameEvents.OnAnimalCollected   -= OnAnimalCollected;
            GameEvents.OnWrongTap          -= OnWrongTap;
            GameEvents.OnBombTapped        -= OnBombTapped;
            GameEvents.OnLevelWon          -= OnLevelWon;
            GameEvents.OnLevelFailed       -= OnLevelFailed;
            GameEvents.OnSfxRequested      -= OnSfxRequested;
            GameEvents.OnSfxRequestedPitch -= OnSfxRequestedPitch;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SubscribeEvents();
            UpdateMusicForScene(scene.name);
        }

        public void UpdateMusicForScene(string sceneName)
        {
            LoadClips();
            EnsureSources();

            if (sceneName == "MainScene")
            {
                _isVictoryActive = false;
                if (_victorySource != null && _victorySource.isPlaying)
                {
                    _victorySource.Stop();
                }
                PlayMusic(_bgmMainScene, loop: true);
            }
            else if (sceneName == "GameScene" || sceneName == "MegaShooterScene")
            {
                _isVictoryActive = false;
                if (_victorySource != null && _victorySource.isPlaying)
                {
                    _victorySource.Stop();
                }
                PlayMusic(_bgmGameScene, loop: true);
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (_isVictoryActive) return;
            EnsureSources();
            if (clip == null) return;

            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                return;

            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.mute = _musicMuted;
            _bgmSource.Play();
        }

        public void StopMusic()
        {
            if (_bgmSource != null) _bgmSource.Stop();
        }

        private void OnSfxRequested(SfxType t) => PlaySFX(t);
        private void OnSfxRequestedPitch(SfxType t, float p) => PlaySFX(t, p);

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnAnimalCollected(Core.Animals.AnimalSpecies s, Core.Animals.AnimalType t, Vector3 _)
        {
            PlaySFX(SfxType.Collect);
        }

        private void OnWrongTap()         => PlaySFX(SfxType.WrongTap);
        private void OnBombTapped(Vector3 _) => PlaySFX(SfxType.Explosion);

        private void OnLevelWon()
        {
            _isVictoryActive = true;

            // Stop all background music
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }

            // Play victory soundtrack
            EnsureSources();
            if (_sfxVictory != null && !_musicMuted)
            {
                _victorySource.clip = _sfxVictory;
                _victorySource.mute = _musicMuted;
                _victorySource.Play();
            }
        }

        private void OnLevelFailed()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }
            PlaySFX(SfxType.LevelLose);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void PlaySFX(SfxType type, float pitch = 1f)
        {
            if (_sfxMuted) return;
            int idx = (int)type;
            if (_sfxClips == null || idx >= _sfxClips.Length || _sfxClips[idx] == null)
            {
                // Fallback for collect
                if (type == SfxType.Collect && _sfxMatch != null)
                {
                    PlayClipOneShot(_sfxMatch, pitch);
                }
                return;
            }

            PlayClipOneShot(_sfxClips[idx], pitch);
        }

        public void PlayClipOneShot(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            if (_sfxMuted || clip == null) return;
            var source = BorrowSource();
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        public void PlayButtonClick()
        {
            if (_sfxMuted) return;
            if (_sfxMatch != null)
            {
                PlayClipOneShot(_sfxMatch, 1.25f, 0.6f);
            }
        }

        public static void PlayClick() => Instance?.PlayButtonClick();

        public void ToggleMusicMuted() => SetMusicMuted(!_musicMuted);

        public void SetMusicMuted(bool muted)
        {
            _musicMuted = muted;
            PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMusicMute();

            if (!_musicMuted && !_isVictoryActive && _bgmSource != null && !_bgmSource.isPlaying && _bgmSource.clip != null)
            {
                _bgmSource.Play();
            }

            OnMusicMutedChanged?.Invoke(_musicMuted);
        }

        public void ToggleSfxMuted() => SetSfxMuted(!_sfxMuted);

        public void SetSfxMuted(bool muted)
        {
            _sfxMuted = muted;
            PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();

            if (_sfxMuted && _pool != null)
            {
                for (int i = 0; i < _pool.Length; i++)
                {
                    if (_pool[i] != null && _pool[i].isPlaying) _pool[i].Stop();
                }
            }

            OnSfxMutedChanged?.Invoke(_sfxMuted);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void LoadClips()
        {
            if (_bgmMainScene == null) _bgmMainScene = Resources.Load<AudioClip>("audio/everytime");
            if (_bgmGameScene == null) _bgmGameScene = Resources.Load<AudioClip>("audio/level");
            if (_sfxVictory == null)   _sfxVictory   = Resources.Load<AudioClip>("audio/victory");
            if (_sfxMatch == null)     _sfxMatch     = Resources.Load<AudioClip>("audio/match");

            if (_sfxClips == null || _sfxClips.Length < 12)
            {
                Array.Resize(ref _sfxClips, 12);
            }
            if (_sfxClips[(int)SfxType.Collect] == null)
                _sfxClips[(int)SfxType.Collect] = _sfxMatch;
            if (_sfxClips[(int)SfxType.LevelWin] == null)
                _sfxClips[(int)SfxType.LevelWin] = _sfxVictory;
        }

        private void EnsureSources()
        {
            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
                _bgmSource.spatialBlend = 0f;
            }
            if (_victorySource == null)
            {
                _victorySource = gameObject.AddComponent<AudioSource>();
                _victorySource.loop = false;
                _victorySource.playOnAwake = false;
                _victorySource.spatialBlend = 0f;
            }
        }

        private void BuildPool()
        {
            if (_pool != null && _pool.Length == _poolSize) return;

            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(transform);
                _pool[i] = go.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
                _pool[i].spatialBlend = 0f;
            }
        }

        private void ApplyMusicMute()
        {
            if (_bgmSource != null) _bgmSource.mute = _musicMuted;
            if (_victorySource != null) _victorySource.mute = _musicMuted;
        }

        private AudioSource BorrowSource()
        {
            if (_pool == null || _pool.Length == 0) BuildPool();

            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying) return _pool[i];
            }
            _lastUsed = (_lastUsed + 1) % _pool.Length;
            _pool[_lastUsed].Stop();
            return _pool[_lastUsed];
        }
    }
}
