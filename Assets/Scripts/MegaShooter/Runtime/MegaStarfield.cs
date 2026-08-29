using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaStarfield : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _layers;
        private float[] _speeds;
        private float _height = 18f;
        private bool _running;

        public void Configure(MegaLevelData level)
        {
            _speeds = new float[_layers != null ? _layers.Length : 0];
            for (int i = 0; i < _speeds.Length; i++)
            {
                Sprite sprite = level.backgroundLayers != null && level.backgroundLayers.Length > 0
                    ? level.backgroundLayers[i % level.backgroundLayers.Length]
                    : null;
                if (_layers[i] != null)
                {
                    _layers[i].sprite = sprite;
                    _layers[i].color = Color.Lerp(level.backgroundColor, Color.white, 0.18f + i * 0.09f);
                }
                // Mega backgrounds are intentionally static.  Keep the authored
                // layer sprites/colors, but never move them during gameplay.
                _speeds[i] = 0f;
            }
            _running = false;
        }

        private void Update()
        {
            if (!_running || _layers == null) return;
            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i] == null) continue;
                Vector3 p = _layers[i].transform.position;
                p.y -= _speeds[i] * Time.deltaTime;
                if (p.y <= -_height) p.y += _height * 2f;
                _layers[i].transform.position = p;
            }
        }
    }
}
