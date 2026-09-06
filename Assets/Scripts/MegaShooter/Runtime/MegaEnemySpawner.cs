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

            // Spawn villains in clean synchronized ranks/patterns so formations (V, Line, Arc, etc.)
            // appear as a deliberate unit rather than trickling in one-by-one.
            int columns = Mathf.Max(1, group.columns);
            int totalRows = Mathf.CeilToInt(group.count / (float)columns);
            for (int r = 0; r < totalRows; r++)
            {
                int startIndex = r * columns;
                int rankCount = Mathf.Min(columns, group.count - startIndex);

                int cap = _game.EffectiveMaxActiveEnemies(_level.maximumActiveEnemies, wave.maximumSimultaneousEnemies);
                while (_game.ActiveEnemyCount + rankCount > cap && _game.ActiveEnemyCount > 0 && _game.IsWaveRunning)
                    yield return null;
                if (!_game.IsWaveRunning) yield break;

                for (int c = 0; c < rankCount; c++)
                {
                    int index = startIndex + c;
                    Vector2 position = GetSpawnPosition(group, index);
                    GameObject go = MegaObjectPools.Instance.Spawn(group.enemy.prefab, position, Quaternion.identity, transform);
                    if (go != null)
                    {
                        MegaEnemyController controller = go.GetComponent<MegaEnemyController>();
                        bool elite = group.explicitElite || _game.NextRandom01() < group.eliteChance;
                        controller?.Configure(group.enemy, group, wave, _level, _game, _director, elite);
                    }
                }

                // Distinct cadence between successive formation ranks
                float baseCadence = Mathf.Max(0.55f, group.cadence * 2.8f * _level.spawnCadenceMultiplier) * _game.SpawnCadenceScale;
                yield return new WaitForSeconds(baseCadence);
            }
        }

        // Builds an orderly formation near the top of the arena. Ships are laid out in
        // centered ranks (row/column grid) and the chosen formation only nudges the
        // grid into a recognizable shape (V, arc, column ...). Everything stays inside
        // the visible combat lane so the army always reads as a deliberate pattern.
        private Vector2 GetSpawnPosition(EnemySpawnGroup group, int index)
        {
            Rect bounds = _level.cameraBounds;
            const float sideInset = 0.85f;
            const float topInset = 0.8f;
            const float bottomInset = 0.9f;
            float minX = bounds.xMin + sideInset;
            float maxX = bounds.xMax - sideInset;
            float minY = bounds.yMin + bottomInset;
            float maxY = bounds.yMax - topInset;

            int columns = Mathf.Clamp(group.columns, 1, 6);
            int column = index % columns;
            int row = index / columns;
            int columnsThisRow = Mathf.Min(columns, group.count - row * columns);
            float centered = column - (columnsThisRow - 1) * 0.5f;

            // A single readable lane, biased left/right by the spawn path so successive
            // waves approach from alternating flanks without ever leaving the screen.
            float laneCenter = Mathf.Lerp(minX, maxX, group.normalizedEntry);
            if (group.spawnPath == MegaSpawnPath.Left) laneCenter = Mathf.Lerp(minX, maxX, 0.35f);
            else if (group.spawnPath == MegaSpawnPath.Right) laneCenter = Mathf.Lerp(minX, maxX, 0.65f);
            else if (group.spawnPath == MegaSpawnPath.Center) laneCenter = (minX + maxX) * 0.5f;

            // Spacing sized comfortably for 2x villain ships so formations never overlap
            float desiredSpacing = Mathf.Max(1.4f, group.spacing);
            float fitSpacing = (maxX - minX) / Mathf.Max(1, columns);
            float spacing = Mathf.Min(desiredSpacing, fitSpacing);
            float rowSpacing = Mathf.Max(1.35f, spacing * 0.95f);
            float x = laneCenter + centered * spacing;
            float y = maxY - row * rowSpacing;

            switch (group.formation)
            {
                case MegaFormationType.V:
                    y -= Mathf.Abs(centered) * spacing * 0.45f;
                    break;
                case MegaFormationType.Arc:
                    y -= (1f - Mathf.Abs(centered) / Mathf.Max(1f, columnsThisRow * 0.5f)) * spacing * 0.55f;
                    break;
                case MegaFormationType.Column:
                    x = laneCenter;
                    y = maxY - index * rowSpacing * 0.85f;
                    break;
                case MegaFormationType.AlternatingSides:
                    x += (row % 2 == 0 ? -0.4f : 0.4f) * spacing;
                    y -= (column % 2 == 0 ? 0f : 0.3f) * spacing;
                    break;
                case MegaFormationType.Mirrored:
                    x = laneCenter + Mathf.Sign(centered == 0f ? (column % 2 == 0 ? -1f : 1f) : centered)
                        * (0.45f + Mathf.Abs(centered)) * spacing;
                    break;
                case MegaFormationType.Line:
                    y -= Mathf.Abs(centered) * 0.08f;
                    break;
                case MegaFormationType.Grid:
                    if (row % 2 == 1) x += spacing * 0.5f;
                    break;
                default:
                    break;
            }

            return new Vector2(Mathf.Clamp(x, minX, maxX), Mathf.Clamp(y, minY, maxY));
        }
    }
}
