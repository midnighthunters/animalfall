using System;
using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.MegaLevel
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class Villain : MonoBehaviour
    {
        [SerializeField] private VillainData data;

        public VillainData Data => data;
        public int CurrentHP { get; private set; }
        public int MaxHP => data != null ? data.maxHP : 100;
        public bool IsVulnerable { get; set; }
        public bool IsDefeated => CurrentHP <= 0;

        public event Action<int, int> OnHPChanged;
        public event Action OnDefeated;
        public event Action OnPhaseChanged;

        private int currentPhase = 1;
        public int CurrentPhase => currentPhase;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void Setup(VillainData villainData)
        {
            data = villainData;
            CurrentHP = data.maxHP;
            currentPhase = 1;
            IsVulnerable = false;

            if (sr != null && data.sprite != null)
                sr.sprite = data.sprite;
        }

        public void TakeDamage(int amount)
        {
            if (IsDefeated) return;

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnHPChanged?.Invoke(CurrentHP, data.maxHP);

            if (sr != null)
                StartCoroutine(FlashRed());

            CheckPhase();

            if (IsDefeated)
                OnDefeated?.Invoke();
        }

        private void CheckPhase()
        {
            float hpPercent = (float)CurrentHP / data.maxHP;
            int newPhase = currentPhase;

            if (hpPercent <= data.phase3HPPercent)
                newPhase = 3;
            else if (hpPercent <= data.phase2HPPercent)
                newPhase = 2;

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke();
            }
        }

        private void OnMouseDown()
        {
            if (IsVulnerable && !IsDefeated)
            {
                TakeDamage(data.damagePerHit);
                AudioManager.Instance?.PlaySFX(AudioManager.SfxType.ShieldBreak);
            }
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (sr == null) yield break;
            Color orig = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            sr.color = orig;
        }
    }
}
