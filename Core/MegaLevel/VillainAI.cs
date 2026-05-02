using System.Collections;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Managers;

namespace AnimalFall.Core.MegaLevel
{
    public enum VillainState
    {
        Idle,
        Attacking,
        Vulnerable,
        Defeated
    }

    [RequireComponent(typeof(Villain))]
    public class VillainAI : MonoBehaviour
    {
        private Villain villain;
        private VillainData data;

        public VillainState CurrentState { get; private set; }

        [SerializeField] private float floatAmplitude = 0.5f;
        [SerializeField] private float floatFrequency = 1f;

        private float baseY;
        private float attackTimer;
        private float vulnerableTimer;
        private float shieldTimer;
        private float attackSpeedMultiplier = 1f;

        private void Awake()
        {
            villain = GetComponent<Villain>();
        }

        public void Initialize(VillainData villainData)
        {
            data = villainData;
            villain.Setup(villainData);

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 topPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.85f, 10f));
                topPos.z = 0f;
                transform.position = topPos;
            }

            baseY = transform.position.y;
            CurrentState = VillainState.Idle;
            attackTimer = data.attackInterval;
            shieldTimer = data.shieldDuration;
            attackSpeedMultiplier = 1f;

            villain.OnPhaseChanged += OnPhaseChanged;
            villain.OnDefeated += OnVillainDefeated;

            StartCoroutine(AILoop());
        }

        private void Update()
        {
            if (CurrentState == VillainState.Defeated) return;

            float y = baseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            float x = transform.position.x + Mathf.Sin(Time.time * 0.5f) * 0.02f;
            transform.position = new Vector3(x, y, 0);
        }

        private IEnumerator AILoop()
        {
            yield return new WaitForSeconds(2f);

            while (CurrentState != VillainState.Defeated)
            {
                CurrentState = VillainState.Idle;
                villain.IsVulnerable = false;
                shieldTimer = data.shieldDuration / attackSpeedMultiplier;

                while (shieldTimer > 0)
                {
                    shieldTimer -= Time.deltaTime;

                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        yield return StartCoroutine(PerformAttack());
                        attackTimer = data.attackInterval / attackSpeedMultiplier;
                    }

                    yield return null;
                }

                CurrentState = VillainState.Vulnerable;
                villain.IsVulnerable = true;
                vulnerableTimer = data.vulnerableWindow;

                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                Color origColor = sr != null ? sr.color : Color.white;
                if (sr != null) sr.color = Color.yellow;

                while (vulnerableTimer > 0 && CurrentState == VillainState.Vulnerable)
                {
                    vulnerableTimer -= Time.deltaTime;
                    yield return null;
                }

                villain.IsVulnerable = false;
                if (sr != null) sr.color = origColor;

                yield return null;
            }
        }

        private IEnumerator PerformAttack()
        {
            CurrentState = VillainState.Attacking;

            if (data.attackHindrances != null && data.attackHindrances.Length > 0 &&
                HindranceManager.Instance != null)
            {
                yield return new WaitForSeconds(0.3f);
            }

            int projectileCount = data.projectilesPerAttack;
            for (int i = 0; i < projectileCount; i++)
            {
                SpawnProjectile();
                yield return new WaitForSeconds(0.2f);
            }

            CurrentState = VillainState.Idle;
        }

        private void SpawnProjectile()
        {
            if (data.projectilePrefab == null) return;

            GameObject proj = Instantiate(data.projectilePrefab,
                transform.position + Vector3.down * 0.5f,
                Quaternion.identity);

            var vp = proj.GetComponent<VillainProjectile>();
            if (vp != null)
                vp.Initialize(data.projectileSpeed, data.damagePerHit);
        }

        private void OnPhaseChanged()
        {
            switch (villain.CurrentPhase)
            {
                case 2:
                    attackSpeedMultiplier = data.phase2AttackSpeedMultiplier;
                    break;
                case 3:
                    attackSpeedMultiplier = data.phase3AttackSpeedMultiplier;
                    break;
            }
        }

        private void OnVillainDefeated()
        {
            CurrentState = VillainState.Defeated;
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (villain != null)
            {
                villain.OnPhaseChanged -= OnPhaseChanged;
                villain.OnDefeated -= OnVillainDefeated;
            }
        }
    }
}
