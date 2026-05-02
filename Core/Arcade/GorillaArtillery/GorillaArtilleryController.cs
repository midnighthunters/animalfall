using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    public class GorillaArtilleryController : MonoBehaviour, IArcadeMiniGame
    {
        [Header("References")]
        [SerializeField] private GorillaLauncher launcher;
        [SerializeField] private WindVector windVector;
        [SerializeField] private Transform droneContainer;
        [SerializeField] private Transform barrierContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject dronePrefab;
        [SerializeField] private GameObject barrierPrefab;

        public MiniGameType GameType => MiniGameType.GorillaArtillery;
        public bool IsComplete { get; private set; }
        public int CurrentScore { get; private set; }

        private ArcadeSessionData config;
        private readonly List<DroneTarget> drones = new List<DroneTarget>();
        private int dronesDestroyed;

        public void Setup(ArcadeSessionData sessionConfig)
        {
            config = sessionConfig;
            IsComplete = false;
            CurrentScore = 0;
            dronesDestroyed = 0;
            drones.Clear();

            if (windVector != null)
                windVector.Configure(config.windStrengthMin, config.windStrengthMax, config.windChangeInterval);

            if (launcher != null)
            {
                launcher.Wind = windVector;
                launcher.ResetAmmo(3);
            }

            SpawnLevel();
        }

        public void StartGame()
        {
            // Game is live after Setup
        }

        public void OnUpdate()
        {
            if (IsComplete) return;

            bool allDestroyed = true;
            foreach (var drone in drones)
            {
                if (drone != null && !drone.IsDestroyed)
                {
                    allDestroyed = false;
                    break;
                }
            }

            if (allDestroyed)
            {
                IsComplete = true;
                CurrentScore = dronesDestroyed * 100;
                return;
            }

            if (launcher != null && launcher.RemainingShots <= 0 && !launcher.IsAiming)
            {
                bool anyProjectilesAlive = Object.FindObjectOfType<BoulderProjectile>() != null ||
                                           Object.FindObjectOfType<ScatterShotProjectile>() != null ||
                                           Object.FindObjectOfType<MudBallProjectile>() != null;
                if (!anyProjectilesAlive)
                {
                    IsComplete = true;
                    CurrentScore = dronesDestroyed * 100;
                }
            }
        }

        public void EndGame()
        {
            foreach (var drone in drones)
            {
                if (drone != null) Destroy(drone.gameObject);
            }
            drones.Clear();
        }

        private void SpawnLevel()
        {
            int droneCount = config != null ? config.targetCount : 5;

            float startX = 2f;
            float spacing = 2.5f;
            float baseY = 4f;

            for (int i = 0; i < droneCount; i++)
            {
                float x = startX + i * spacing;
                float y = baseY + Random.Range(-0.5f, 0.5f);
                Vector3 pos = new Vector3(x, y, 0);

                if (dronePrefab != null)
                {
                    var droneObj = Instantiate(dronePrefab, pos, Quaternion.identity, droneContainer);
                    var drone = droneObj.GetComponent<DroneTarget>();
                    if (drone != null)
                    {
                        drone.OnDroneDestroyed += OnDroneDestroyed;
                        drones.Add(drone);
                    }
                }

                if (barrierPrefab != null && i % 2 == 0)
                {
                    Vector3 barrierPos = pos + Vector3.down * 0.8f;
                    Instantiate(barrierPrefab, barrierPos, Quaternion.identity, barrierContainer);
                }
            }
        }

        private void OnDroneDestroyed(DroneTarget drone)
        {
            dronesDestroyed++;
            CurrentScore = dronesDestroyed * 100;
        }
    }
}
