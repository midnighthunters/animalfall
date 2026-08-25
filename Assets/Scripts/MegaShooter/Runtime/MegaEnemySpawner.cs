using System.Collections;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaEnemySpawner : MonoBehaviour
    {
        private MegaShooterGameManager _game;
        private MegaWaveDirector _director;
        private MegaLevelData _level;

        public void Configure(MegaShooterGameManager game, MegaWaveDirector director, MegaLevelData level)
        {
            _game = game;
            _director = director;
            _level = level;
        }

        public IEnumerator SpawnGroup(EnemySpawnGroup group, MegaWaveData wave)
        {
            if (group == null || group.enemy == null || group.enemy.prefab == null) yield break;
            if (group.startDelay > 0f) yield return new WaitForSeconds(group.startDelay);

            for (int i = 0; i < group.count; i++)
            {
                int cap = Mathf.Min(_level.maximumActiveEnemies, wave.maximumSimultaneousEnemies);
                while (_game.ActiveEnemyCount >= cap && _game.IsWaveRunning)
                    yield return null;
                if (!_game.IsWaveRunning) yield break;

                Vector2 position = GetSpawnPosition(group, i);
                GameObject go = MegaObjectPools.Instance.Spawn(group.enemy.prefab, position, Quaternion.Euler(0f, 0f, 180f), transform);
                if (go != null)
                {
                    MegaEnemyController controller = go.GetComponent<MegaEnemyController>();
                    bool elite = group.explicitElite || _game.NextRandom01() < group.eliteChance;
                    controller?.Configure(group.enemy, group, wave, _level, _game, _director, elite);
                }
                float cadence = Mathf.Max(0.05f, group.cadence * _level.spawnCadenceMultiplier);
                yield return new WaitForSeconds(cadence);
            }
        }

        private Vector2 GetSpawnPosition(EnemySpawnGroup group, int index)
        {
            Rect bounds = _level.cameraBounds;
            int columns = Mathf.Max(1, group.columns);
            int column = index % columns;
            int row = index / columns;
            float centered = column - (Mathf.Min(columns, group.count) - 1) * 0.5f;
            float x = Mathf.Lerp(bounds.xMin + 0.7f, bounds.xMax - 0.7f, group.normalizedEntry) + centered * group.spacing;
            float y = bounds.yMax + 0.8f + row * group.spacing;

            if (group.formation == MegaFormationType.V)
                y += Mathf.Abs(centered) * group.spacing * 0.55f;
            else if (group.formation == MegaFormationType.Arc)
                y += Mathf.Abs(centered) * Mathf.Abs(centered) * 0.18f;
            else if (group.formation == MegaFormationType.AlternatingSides)
                x = index % 2 == 0 ? bounds.xMin - 0.4f : bounds.xMax + 0.4f;
            else if (group.formation == MegaFormationType.Mirrored)
                x = index % 2 == 0 ? -Mathf.Abs(x) : Mathf.Abs(x);

            if (group.spawnPath == MegaSpawnPath.Left) x = bounds.xMin - 0.4f;
            else if (group.spawnPath == MegaSpawnPath.Right) x = bounds.xMax + 0.4f;
            else if (group.spawnPath == MegaSpawnPath.Center) x = centered * group.spacing;
            else if (group.spawnPath == MegaSpawnPath.DiveLane) y += 1.2f;

            return new Vector2(x, y);
        }
    }
}
