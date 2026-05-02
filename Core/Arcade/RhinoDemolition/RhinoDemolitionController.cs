using UnityEngine;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    public class RhinoDemolitionController : MonoBehaviour, IArcadeMiniGame
    {
        [Header("References")]
        [SerializeField] private PendulumChain pendulumChain;
        [SerializeField] private RhinoProjectile rhinoProjectile;
        [SerializeField] private DemolitionTower demolitionTower;
        [SerializeField] private DamageScoreTracker damageTracker;
        [SerializeField] private TowerLayout towerLayout;

        public MiniGameType GameType => MiniGameType.RhinoDemolition;
        public bool IsComplete { get; private set; }
        public int CurrentScore { get; private set; }

        private ArcadeSessionData config;
        private bool chainSnapped;
        private float settleTimer;
        private const float SettleTime = 3f;

        public void Setup(ArcadeSessionData sessionConfig)
        {
            config = sessionConfig;
            IsComplete = false;
            CurrentScore = 0;
            chainSnapped = false;
            settleTimer = 0f;

            if (damageTracker != null)
                damageTracker.Configure(config.requiredDamageScore);

            if (rhinoProjectile != null)
                rhinoProjectile.Configure(config.rhinoMass, config.groundPoundForce);

            if (pendulumChain != null)
                pendulumChain.BuildChain();

            if (demolitionTower != null)
            {
                if (towerLayout != null)
                    demolitionTower.BuildTower(towerLayout);
                else
                    demolitionTower.BuildProceduralTower(8, 4);
            }
        }

        public void StartGame()
        {
            // Game is live after Setup
        }

        public void OnUpdate()
        {
            if (IsComplete) return;

            if (!chainSnapped && pendulumChain != null)
            {
                if (Input.GetMouseButtonDown(0) && pendulumChain.RhinoBody != null)
                {
                    Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    float distToRhino = Vector2.Distance(tapPos, pendulumChain.RhinoBody.position);

                    if (distToRhino > 2.5f && pendulumChain.RhinoBody.velocity.magnitude > 1f)
                    {
                        pendulumChain.SnapChain();
                        chainSnapped = true;
                    }
                }
                return;
            }

            if (chainSnapped)
            {
                if (rhinoProjectile != null)
                {
                    var rb = rhinoProjectile.GetComponent<Rigidbody2D>();
                    if (rb != null && rb.velocity.magnitude < 0.3f)
                    {
                        settleTimer += Time.deltaTime;
                        if (settleTimer >= SettleTime)
                        {
                            FinishGame();
                        }
                    }
                    else
                    {
                        settleTimer = 0f;
                    }
                }
            }

            CurrentScore = damageTracker != null
                ? Mathf.RoundToInt(damageTracker.TotalDamageScore)
                : 0;
        }

        public void EndGame()
        {
            demolitionTower?.Clear();
        }

        private void FinishGame()
        {
            IsComplete = true;
            CurrentScore = damageTracker != null
                ? Mathf.RoundToInt(damageTracker.TotalDamageScore)
                : 0;
        }
    }
}
