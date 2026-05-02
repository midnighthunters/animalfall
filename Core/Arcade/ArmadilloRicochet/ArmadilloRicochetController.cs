using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    public class ArmadilloRicochetController : MonoBehaviour, IArcadeMiniGame
    {
        [Header("References")]
        [SerializeField] private ArmadilloBall armadilloBall;
        [SerializeField] private CanyonBuilder canyonBuilder;
        [SerializeField] private CanyonLayout canyonLayout;

        [Header("Drop Zone")]
        [SerializeField] private float dropY = 6f;

        public MiniGameType GameType => MiniGameType.ArmadilloRicochet;
        public bool IsComplete { get; private set; }
        public int CurrentScore { get; private set; }

        private ArcadeSessionData config;
        private bool dropped;
        private int scarabsCollected;
        private int totalScarabs;

        public void Setup(ArcadeSessionData sessionConfig)
        {
            config = sessionConfig;
            IsComplete = false;
            CurrentScore = 0;
            dropped = false;
            scarabsCollected = 0;

            if (canyonBuilder != null)
            {
                if (canyonLayout != null)
                    canyonBuilder.BuildCanyon(canyonLayout);
                else
                    canyonBuilder.BuildProceduralCanyon(config.goldenScarabCount);

                totalScarabs = canyonBuilder.SpawnedScarabs.Count;

                foreach (var scarab in canyonBuilder.SpawnedScarabs)
                {
                    scarab.OnCollected += OnScarabCollected;
                }
            }

            if (armadilloBall != null)
            {
                armadilloBall.transform.position = new Vector3(0, dropY, 0);
                armadilloBall.Configure(config.slamCharges, config.slamForce);
            }
        }

        public void StartGame()
        {
            // Wait for player to tap drop point
        }

        public void OnUpdate()
        {
            if (IsComplete) return;

            if (!dropped && Input.GetMouseButtonDown(0))
            {
                Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (tapPos.y > dropY - 1f)
                {
                    float clampedX = Mathf.Clamp(tapPos.x, -2.5f, 2.5f);
                    armadilloBall?.Drop(clampedX);
                    dropped = true;
                }
            }

            if (dropped && armadilloBall != null && armadilloBall.HasReachedExit)
            {
                FinishGame();
            }

            CurrentScore = scarabsCollected * 200;
        }

        public void EndGame()
        {
            canyonBuilder?.Clear();
        }

        private void OnScarabCollected(GoldenScarab scarab)
        {
            scarabsCollected++;
            CurrentScore = scarabsCollected * 200;
        }

        private void FinishGame()
        {
            IsComplete = true;
            CurrentScore = scarabsCollected * 200;

            if (scarabsCollected >= totalScarabs)
                CurrentScore += 500;
        }
    }
}
