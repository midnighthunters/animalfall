using AnimalFall.Managers;
using NUnit.Framework;
using UnityEngine;

namespace AnimalFall.Tests.Editor
{
    public sealed class AudioFlowEditModeTests
    {
        private GameObject _audioObject;
        private AudioManager _audioManager;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();

            if (AudioManager.Instance != null)
            {
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            }

            _audioObject = new GameObject("TestAudioManager");
            _audioManager = _audioObject.AddComponent<AudioManager>();
            _audioManager.Init();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();

            if (_audioObject != null)
            {
                Object.DestroyImmediate(_audioObject);
            }

            PlayerPrefs.DeleteKey("settings_music_muted");
            PlayerPrefs.DeleteKey("settings_sfx_muted");
        }

        [Test]
        public void AudioClips_LoadSuccessfullyFromResources()
        {
            var everytime = Resources.Load<AudioClip>("audio/everytime");
            var level = Resources.Load<AudioClip>("audio/level");
            var victory = Resources.Load<AudioClip>("audio/victory");
            var match = Resources.Load<AudioClip>("audio/match");

            Assert.That(everytime, Is.Not.Null, "audio/everytime.ogg should exist in Resources");
            Assert.That(level, Is.Not.Null, "audio/level.ogg should exist in Resources");
            Assert.That(victory, Is.Not.Null, "audio/victory.mp3 should exist in Resources");
            Assert.That(match, Is.Not.Null, "audio/match.mp3 should exist in Resources");
        }

        [Test]
        public void AudioManager_InitializesWithBgmAndVictorySources()
        {
            Assert.That(_audioManager.BgmSource, Is.Not.Null);
            Assert.That(_audioManager.VictorySource, Is.Not.Null);
        }

        [Test]
        public void SceneTransitions_SwitchBetweenEverytimeAndLevelTracks()
        {
            _audioManager.UpdateMusicForScene("MainScene");
            Assert.That(_audioManager.BgmSource.clip, Is.Not.Null);
            Assert.That(_audioManager.BgmSource.clip.name, Is.EqualTo("everytime"));
            Assert.That(_audioManager.BgmSource.loop, Is.True);

            _audioManager.UpdateMusicForScene("GameScene");
            Assert.That(_audioManager.BgmSource.clip, Is.Not.Null);
            Assert.That(_audioManager.BgmSource.clip.name, Is.EqualTo("level"));
            Assert.That(_audioManager.BgmSource.loop, Is.True);
        }

        [Test]
        public void LevelWon_DisablesOtherMusic_AndPlaysVictorySoundtrack()
        {
            _audioManager.UpdateMusicForScene("GameScene");
            Assert.That(_audioManager.BgmSource.clip.name, Is.EqualTo("level"));

            GameEvents.OnLevelWon?.Invoke();

            Assert.That(_audioManager.IsVictoryActive, Is.True);
            Assert.That(_audioManager.BgmSource.isPlaying, Is.False, "BGM should stop on victory");
            Assert.That(_audioManager.VictorySource.clip, Is.Not.Null);
            Assert.That(_audioManager.VictorySource.clip.name, Is.EqualTo("victory"));

            // Other music should be disabled while victory is active
            var everytime = Resources.Load<AudioClip>("audio/everytime");
            _audioManager.PlayMusic(everytime);
            Assert.That(_audioManager.BgmSource.clip.name, Is.EqualTo("level"), "Should not change clip while victory active");
        }

        [Test]
        public void ReturningToMainScene_ClearsVictory_AndPlaysEverytime()
        {
            _audioManager.UpdateMusicForScene("GameScene");
            GameEvents.OnLevelWon?.Invoke();
            Assert.That(_audioManager.IsVictoryActive, Is.True);

            _audioManager.UpdateMusicForScene("MainScene");
            Assert.That(_audioManager.IsVictoryActive, Is.False, "Victory active state should clear on return to MainScene");
            Assert.That(_audioManager.BgmSource.clip.name, Is.EqualTo("everytime"));
        }

        [Test]
        public void MusicAndSfxMuteToggles_UpdateAndPersist()
        {
            _audioManager.SetMusicMuted(true);
            Assert.That(_audioManager.IsMusicMuted, Is.True);
            Assert.That(_audioManager.BgmSource.mute, Is.True);
            Assert.That(PlayerPrefs.GetInt("settings_music_muted", 0), Is.EqualTo(1));

            _audioManager.SetMusicMuted(false);
            Assert.That(_audioManager.IsMusicMuted, Is.False);
            Assert.That(_audioManager.BgmSource.mute, Is.False);
            Assert.That(PlayerPrefs.GetInt("settings_music_muted", 0), Is.EqualTo(0));

            _audioManager.SetSfxMuted(true);
            Assert.That(_audioManager.IsSfxMuted, Is.True);
            Assert.That(PlayerPrefs.GetInt("settings_sfx_muted", 0), Is.EqualTo(1));

            _audioManager.SetSfxMuted(false);
            Assert.That(_audioManager.IsSfxMuted, Is.False);
            Assert.That(PlayerPrefs.GetInt("settings_sfx_muted", 0), Is.EqualTo(0));
        }
    }
}
