using UnityEngine;
using AnimalFall.Core.Arcade.Shared;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    public class GorillaLauncher : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private float powerMultiplier = 8f;
        [SerializeField] private float maxDragDistance = 3f;
        [SerializeField] private Transform launchPoint;

        [Header("Projectile Prefabs")]
        [SerializeField] private GameObject boulderPrefab;
        [SerializeField] private GameObject scatterShotPrefab;
        [SerializeField] private GameObject mudBallPrefab;

        [Header("Trajectory")]
        [SerializeField] private TrajectoryRenderer trajectoryRenderer;

        public AmmoType CurrentAmmo { get; set; } = AmmoType.Boulder;
        public bool IsAiming { get; private set; }
        public int RemainingShots { get; private set; }
        public WindVector Wind { get; set; }

        private Vector2 dragStart;
        private Vector2 dragCurrent;
        private Camera cam;
        private bool launched;

        private void Awake()
        {
            cam = Camera.main;
            if (launchPoint == null) launchPoint = transform;
        }

        public void ResetAmmo(int shots)
        {
            RemainingShots = shots;
            launched = false;
        }

        private void Update()
        {
            if (RemainingShots <= 0 || launched) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos = cam.ScreenToWorldPoint(Input.mousePosition);
                float dist = Vector2.Distance(pos, (Vector2)launchPoint.position);
                if (dist < 2f)
                {
                    IsAiming = true;
                    dragStart = pos;
                }
            }

            if (IsAiming && Input.GetMouseButton(0))
            {
                dragCurrent = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 delta = dragStart - dragCurrent;
                if (delta.magnitude > maxDragDistance)
                    delta = delta.normalized * maxDragDistance;

                Vector2 velocity = TrajectoryRenderer.CalculateLaunchVelocity(delta, powerMultiplier);
                Vector2 wind = Wind != null ? Wind.CurrentWind : Vector2.zero;
                trajectoryRenderer?.RenderArc(launchPoint.position, velocity, wind, Physics2D.gravity.y);
            }

            if (IsAiming && Input.GetMouseButtonUp(0))
            {
                IsAiming = false;
                trajectoryRenderer?.Clear();

                Vector2 delta = dragStart - dragCurrent;
                if (delta.magnitude > maxDragDistance)
                    delta = delta.normalized * maxDragDistance;

                if (delta.magnitude > 0.2f)
                {
                    Vector2 velocity = TrajectoryRenderer.CalculateLaunchVelocity(delta, powerMultiplier);
                    Fire(velocity);
                }
            }
        }

        private void Fire(Vector2 velocity)
        {
            if (RemainingShots <= 0) return;

            GameObject prefab = GetPrefabForAmmo();
            if (prefab == null) return;

            GameObject proj = Instantiate(prefab, launchPoint.position, Quaternion.identity);

            switch (CurrentAmmo)
            {
                case AmmoType.Boulder:
                    var boulder = proj.GetComponent<BoulderProjectile>();
                    boulder?.Launch(velocity, Wind);
                    break;
                case AmmoType.ScatterShot:
                    var scatter = proj.GetComponent<ScatterShotProjectile>();
                    scatter?.Launch(velocity, Wind);
                    break;
                case AmmoType.MudBall:
                    var mud = proj.GetComponent<MudBallProjectile>();
                    mud?.Launch(velocity, Wind);
                    break;
            }

            RemainingShots--;
        }

        private GameObject GetPrefabForAmmo()
        {
            switch (CurrentAmmo)
            {
                case AmmoType.Boulder:     return boulderPrefab;
                case AmmoType.ScatterShot: return scatterShotPrefab;
                case AmmoType.MudBall:     return mudBallPrefab;
                default:                   return boulderPrefab;
            }
        }
    }
}
