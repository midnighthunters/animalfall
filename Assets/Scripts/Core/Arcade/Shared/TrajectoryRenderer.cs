using UnityEngine;

namespace AnimalFall.Core.Arcade.Shared
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int samplePoints = 60;
        [SerializeField] private float predictionDuration = 3f;
        [SerializeField] private float fadeStartPercent = 0.5f;

        private LineRenderer lr;

        private void Awake()
        {
            lr = GetComponent<LineRenderer>();
            lr.positionCount = 0;
            lr.useWorldSpace = true;

            SetupGradient();
        }

        private void SetupGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, fadeStartPercent),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            lr.colorGradient = gradient;
        }

        public void RenderArc(Vector2 origin, Vector2 velocity, Vector2 wind, float gravity)
        {
            lr.positionCount = samplePoints;
            float step = predictionDuration / (samplePoints - 1);

            for (int i = 0; i < samplePoints; i++)
            {
                float t = i * step;
                float x = origin.x + (velocity.x + wind.x) * t;
                float y = origin.y + velocity.y * t + 0.5f * gravity * t * t;
                lr.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        public void Clear()
        {
            lr.positionCount = 0;
        }

        public static Vector2 PredictPosition(Vector2 origin, Vector2 velocity, Vector2 wind, float gravity, float t)
        {
            float x = origin.x + (velocity.x + wind.x) * t;
            float y = origin.y + velocity.y * t + 0.5f * gravity * t * t;
            return new Vector2(x, y);
        }

        public static Vector2 CalculateLaunchVelocity(Vector2 dragDelta, float powerMultiplier)
        {
            return -dragDelta * powerMultiplier;
        }
    }
}
