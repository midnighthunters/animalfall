using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool _showInDevelopmentBuild = true;
        private MegaShooterGameManager _game;
        private float _smoothedDelta;

        public void Configure(MegaShooterGameManager game) => _game = game;

        private void Update() => _smoothedDelta = Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.08f);

        private void OnGUI()
        {
            if (_game == null || (!_showInDevelopmentBuild && !Debug.isDebugBuild)) return;
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.Box(new Rect(8, 8, 270, 154), string.Empty);
            GUI.Label(new Rect(18, 15, 250, 145),
                $"MEGA DEBUG  {fps:0} FPS\nState: {_game.State}\nWave: {_game.CurrentWaveDisplay}\n" +
                $"Enemies: {_game.ActiveEnemyCount}\nHostile shots: {_game.ActiveHostileProjectiles}\n" +
                $"Pool misses: {_game.PoolMisses}\nPlayer DPS: {_game.PlayerDps:0.0}\n" +
                $"Boss phase: {_game.BossPhaseDisplay}\nSeed: {_game.ActiveSeed}");
        }
    }
}
