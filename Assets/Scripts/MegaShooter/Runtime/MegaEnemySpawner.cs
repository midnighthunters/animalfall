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

            // Enemies arrive rank by rank. A whole column is released, then the
            // spawner waits before the next rank so the army reads as a formation
            // instead of a chaotic, wall-to-wall swarm.
            int columns = Mathf.Max(1, group.columns);
            for (int i = 0; i < group.count; i++)
            {
                int cap = _game.EffectiveMaxActiveEnemies(_level.maximumActiveEnemies, wave.maximumSimultaneousEnemies);
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

                // Tight spacing within a rank, a longer beat between ranks.
                bool endOfRank = (i + 1) % columns == 0;
                float baseCadence = Mathf.Max(0.05f, group.cadence * _level.spawnCadenceMultiplier) * _game.SpawnCadenceScale;
                float cadence = endOfRank ? baseCadence * 2.4f : baseCadence * 0.55f;
                yield return new WaitForSeconds(cadence);
            }
        }

        // Builds an orderly formation near the top of the arena. Ships are laid out in
        // centered ranks (row/column grid) and the chosen formation only nudges the
        // grid into a recognizable shape (V, arc, column ...). Everything stays inside
        // the visible combat lane so the army always reads as a deliberate pattern.
        private Vector2 GetSpawnPosition(EnemySpawnGroup group, int index)
        {
            Rect bounds = _level.cameraBounds;
            const float sideInset = 0.9f;
            const float topInset = 1.1f;
            const float bottomInset = 0.9f;
            float minX = bounds.xMin + sideInset;
            float maxX = bounds.xMax - sideInset;
            float minY = bounds.yMin + bottomInset;
            float maxY = bounds.yMax - topInset;

            int columns = Mathf.Clamp(group.columns, 1, 6);
            int column = index % columns;
            int row = index / columns;
            int columnsThisFormation = Mathf.Min(columns, group.count);
            float centered = column - (columnsThisFormation - 1) * 0.5f;

            // A single readable lane, biased left/right by the spawn path so successive
            // waves approach from alternating flanks without ever leaving the screen.
            float laneCenter = Mathf.Lerp(minX, maxX, group.normalizedEntry);
            if (group.spawnPath == MegaSpawnPath.Left) laneCenter = Mathf.Lerp(minX, maxX, 0.28f);
            else if (group.spawnPath == MegaSpawnPath.Right) laneCenter = Mathf.Lerp(minX, maxX, 0.72f);
            else if (group.spawnPath == MegaSpawnPath.Center) laneCenter = (minX + maxX) * 0.5f;

            float spacing = Mathf.Max(0.6f, group.spacing);
            float rowSpacing = Mathf.Max(0.75f, spacing);
            float x = laneCenter + centered * spacing;
            float y = maxY - row * rowSpacing;

            switch (group.formation)
            {
                case MegaFormationType.V:
                    y -= Mathf.Abs(centered) * spacing * 0.5f;
                    break;
                case MegaFormationType.Arc:
                    y -= (1f - Mathf.Abs(centered) / Mathf.Max(1f, columnsThisFormation * 0.5f)) * spacing * 0.5f;
                    break;
                case MegaFormationType.Column:
                    x = laneCenter;
                    y = maxY - index * rowSpacing * 0.8f;
                    break;
                case MegaFormationType.AlternatingSides:
                    x = laneCenter + (column % 2 == 0 ? -1f : 1f) * (0.6f + row * 0.15f) * spacing;
                    break;
                case MegaFormationType.Mirrored:
                    x = laneCenter + Mathf.Sign(centered == 0f ? 1f : centered) * (0.5f + Mathf.Abs(centered)) * spacing;
                    break;
                case MegaFormationType.Line:
                case MegaFormationType.Grid:
                default:
                    break;
            }

            return new Vector2(Mathf.Clamp(x, minX, maxX), Mathf.Clamp(y, minY, maxY));
        }
    }
}
