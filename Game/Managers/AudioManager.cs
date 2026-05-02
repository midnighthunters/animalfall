// ============================================================
//  AudioManager.cs  –  Animal Fall  (FULL REPLACEMENT)
//  Production-grade audio system:
//    • Dual AudioSource pool for SFX (non-blocking)
//    • Dedicated music source with crossfade
//    • Volume driven by SaveManager settings
//    • Full SFX enum matching existing callers
//    • EventBus integration for volume changes
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── SFX enum  (kept 100% backward compatible) ─────────────
    public enum SfxType
    {
        Collect,
        WrongTap,
        Explosion,
        LevelWin,
        LevelLose,
        ShieldBreak,
        Combo,
        PowerUp,
        UIClick,
        UIBack,
        CoinPickup,
        CountdownTick,
        CountdownGo
    }

    // ── Music tracks ──────────────────────────────────────────
    public enum MusicTrack { None, MainMenu, Gameplay, Victory }

    // ── Inspector ─────────────────────────────────────────────
    [Header("SFX Clips")]
    [SerializeField] private AudioClip collect;
    [SerializeField] private AudioClip wrong;
    [SerializeField] private AudioClip explosion;
    [SerializeField] private AudioClip levelWin;
    [SerializeField] private AudioClip levelLose;
    [SerializeField] private AudioClip shieldBreak;
    [SerializeField] private AudioClip combo;
    [SerializeField] private AudioClip powerUp;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip uiBack;
    [SerializeField] private AudioClip coinPickup;
    [SerializeField] private AudioClip countdownTick;
    [SerializeField] private AudioClip countdownGo;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip musicMainMenu;
    [SerializeField] private AudioClip musicGameplay;
    [SerializeField] private AudioClip musicVictory;

    [Header("Pool settings")]
    [SerializeField, Range(4, 16)] private int sfxPoolSize = 8;
    [SerializeField] private float crossFadeDuration = 0.8f;

    // ── Private state ─────────────────────────────────────────
    private List<AudioSource>  _sfxPool   = new();
    private AudioSource        _musicA;
    private AudioSource        _musicB;
    private bool               _usingMusicA = true;
    private MusicTrack         _currentTrack = MusicTrack.None;
    private Coroutine          _crossFadeCoroutine;

    // Volume (0-1), mirrored from SaveManager
    private float _masterVol = 1f;
    private float _sfxVol    = 1f;
    private float _musicVol  = 0.6f;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool();
    }

    private void Start()
    {
        ApplyVolumes();
        EventBus.Subscribe<OnSaveDataLoaded>(OnSaveDataLoaded);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnSaveDataLoaded>(OnSaveDataLoaded);
    }

    // ── Pool construction ─────────────────────────────────────
    private void BuildPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _sfxPool.Add(src);
        }

        _musicA = gameObject.AddComponent<AudioSource>();
        _musicA.playOnAwake = false;
        _musicA.loop        = true;

        _musicB = gameObject.AddComponent<AudioSource>();
        _musicB.playOnAwake = false;
        _musicB.loop        = true;
    }

    // ── Volume API ────────────────────────────────────────────
    /// <summary>Called by SaveManager whenever settings change.</summary>
    public void ApplyVolumes()
    {
        if (SaveManager.Instance != null)
        {
            _masterVol = SaveManager.Instance.Data.masterVolume;
            _sfxVol    = SaveManager.Instance.Data.sfxVolume;
            _musicVol  = SaveManager.Instance.Data.musicVolume;
        }

        float effectiveSfx   = _masterVol * _sfxVol;
        float effectiveMusic  = _masterVol * _musicVol;

        foreach (var src in _sfxPool) src.volume = effectiveSfx;
        _musicA.volume = effectiveMusic;
        _musicB.volume = effectiveMusic;
    }

    private void OnSaveDataLoaded(OnSaveDataLoaded _) => ApplyVolumes();

    // ── SFX API (fully backward compatible) ───────────────────
    public void PlaySFX(SfxType type)
    {
        AudioClip clip = GetClip(type);
        if (clip == null) return;

        AudioSource src = GetFreeSfxSource();
        if (src == null) return;

        src.volume = _masterVol * _sfxVol;
        src.PlayOneShot(clip);
    }

    public void PlaySFXAt(SfxType type, Vector3 worldPos)
    {
        AudioClip clip = GetClip(type);
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, worldPos, _masterVol * _sfxVol);
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var s in _sfxPool)
            if (!s.isPlaying) return s;
        // All busy – steal the oldest (first in list)
        return _sfxPool.Count > 0 ? _sfxPool[0] : null;
    }

    private AudioClip GetClip(SfxType type) => type switch
    {
        SfxType.Collect        => collect,
        SfxType.WrongTap       => wrong,
        SfxType.Explosion      => explosion,
        SfxType.LevelWin       => levelWin,
        SfxType.LevelLose      => levelLose,
        SfxType.ShieldBreak    => shieldBreak,
        SfxType.Combo          => combo,
        SfxType.PowerUp        => powerUp,
        SfxType.UIClick        => uiClick,
        SfxType.UIBack         => uiBack,
        SfxType.CoinPickup     => coinPickup,
        SfxType.CountdownTick  => countdownTick,
        SfxType.CountdownGo    => countdownGo,
        _                      => null
    };

    // ── Music API ─────────────────────────────────────────────
    public void PlayMusic(MusicTrack track, bool forceRestart = false)
    {
        if (_currentTrack == track && !forceRestart) return;
        _currentTrack = track;

        AudioClip clip = track switch
        {
            MusicTrack.MainMenu  => musicMainMenu,
            MusicTrack.Gameplay  => musicGameplay,
            MusicTrack.Victory   => musicVictory,
            _                    => null
        };

        if (_crossFadeCoroutine != null) StopCoroutine(_crossFadeCoroutine);
        _crossFadeCoroutine = StartCoroutine(CrossFade(clip));
    }

    public void StopMusic() => PlayMusic(MusicTrack.None);

    private IEnumerator CrossFade(AudioClip newClip)
    {
        AudioSource fadeOut = _usingMusicA ? _musicA : _musicB;
        AudioSource fadeIn  = _usingMusicA ? _musicB : _musicA;
        _usingMusicA = !_usingMusicA;

        float target = _masterVol * _musicVol;

        fadeIn.clip   = newClip;
        fadeIn.volume = 0f;
        if (newClip != null) fadeIn.Play();

        float elapsed = 0f;
        float startVol = fadeOut.volume;

        while (elapsed < crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / crossFadeDuration;
            fadeOut.volume = Mathf.Lerp(startVol, 0f,      t);
            fadeIn.volume  = Mathf.Lerp(0f,      target,   t);
            yield return null;
        }

        fadeOut.Stop();
        fadeOut.clip   = null;
        fadeIn.volume  = target;
        _crossFadeCoroutine = null;
    }
}
