using UnityEngine;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    public class WindVector : MonoBehaviour
    {
        [Header("Wind Settings")]
        [SerializeField] private float strengthMin = -3f;
        [SerializeField] private float strengthMax = 3f;
        [SerializeField] private float changeInterval = 5f;

        public Vector2 CurrentWind { get; private set; }
        public float WindStrength => CurrentWind.x;

        private float changeTimer;

        public void Configure(float min, float max, float interval)
        {
            strengthMin = min;
            strengthMax = max;
            changeInterval = interval;
            Randomize();
        }

        private void Start()
        {
            Randomize();
        }

        private void Update()
        {
            changeTimer += Time.deltaTime;
            if (changeTimer >= changeInterval)
            {
                Randomize();
                changeTimer = 0f;
            }
        }

        private void Randomize()
        {
            float strength = Random.Range(strengthMin, strengthMax);
            CurrentWind = new Vector2(strength, 0f);
        }
    }
}
