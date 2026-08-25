using System.Collections;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaWaveDirector : MonoBehaviour
    {
        [SerializeField] private MegaEnemySpawner _spawner;

        private MegaShooterGameManager _game;
        private MegaLevelData _level;
        private int _activeEnemies;
        private int _activePriorityTargets;
        private bool _spawning;

        public int CurrentWaveIndex { get; private set; } = -1;
        public int ActiveEnemies => _activeEnemies;

        public void Configure(MegaShooterGameManager game, MegaLevelData level)
        {
            _game = game;
            _level = level;
            _activeEnemies = 0;
            _activePriorityTargets = 0;
            CurrentWaveIndex = -1;
            _spawner.Configure(game, this, level);
        }

        public void Begin() => StartCoroutine(RunWaves());

        private IEnumerator RunWaves()
        {
            MegaWaveData[] waves = _level.waves;
            for (int i = 0; i < waves.Length; i++)
            {
                if (!_game.CanAdvanceCombat) yield break;
                CurrentWaveIndex = i;
                MegaWaveData wave = waves[i];
                _game.EnterWave(i, waves.Length, wave);
                if (wave.startDelay > 0f) yield return new WaitForSeconds(wave.startDelay);

                _spawning = true;
                for (int g = 0; g < wave.spawnGroups.Length; g++)
                    yield return _spawner.SpawnGroup(wave.spawnGroups[g], wave);
                _spawning = false;

                if (wave.completionCondition == MegaWaveCompletion.SurviveDuration)
                {
                    yield return new WaitForSeconds(Mathf.Max(0.1f, wave.surviveDuration));
                    _game.DespawnAllEnemies();
                }
                else if (wave.completionCondition == MegaWaveCompletion.DefeatPriorityTargets)
                {
                    while ((_activePriorityTargets > 0 || _spawning) && _game.CanAdvanceCombat) yield return null;
                    _game.DespawnAllEnemies();
                }
                else
                {
                    while ((_activeEnemies > 0 || _spawning) && _game.CanAdvanceCombat) yield return null;
                }

                if (!_game.CanAdvanceCombat) yield break;
                _game.EnterWaveTransition();
                if (wave.completionDelay > 0f) yield return new WaitForSeconds(wave.completionDelay);
            }

            if (_game.CanAdvanceCombat) _game.AllWavesCompleted();
        }

        public void EnemySpawned(MegaEnemyController enemy)
        {
            _activeEnemies++;
            if (enemy != null && enemy.IsPriority) _activePriorityTargets++;
        }

        public void EnemyRemoved(MegaEnemyController enemy, bool defeated, bool wasPriority)
        {
            _activeEnemies = Mathf.Max(0, _activeEnemies - 1);
            if (wasPriority) _activePriorityTargets = Mathf.Max(0, _activePriorityTargets - 1);
        }

        public void StopDirector()
        {
            StopAllCoroutines();
            _spawning = false;
            _activeEnemies = 0;
            _activePriorityTargets = 0;
        }
    }
}
