using System;
using System.Collections;
using UnityEngine;
using AnimalFall.Core.Levels;
using AnimalFall.Managers;

namespace AnimalFall.Core.MegaLevel
{
    public class MegaLevelController : MonoBehaviour
    {
        public static MegaLevelController Instance { get; private set; }

        [SerializeField] private GameObject villainPrefab;
        [SerializeField] private Transform villainSpawnPoint;

        private Villain activeVillain;
        private VillainAI activeAI;
        private bool villainDefeated;
        private bool animalsCollected;
        private LevelData currentLevel;

        public Villain ActiveVillain => activeVillain;
        public bool IsVillainDefeated => villainDefeated;
        public bool IsMegaLevelActive => currentLevel != null && currentLevel.isMegaLevel;

        public event Action OnMegaLevelWon;
        public event Action OnVillainDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void InitMegaLevel(LevelData level)
        {
            currentLevel = level;
            villainDefeated = false;
            animalsCollected = false;

            if (level.villain == null)
            {
                Debug.LogWarning("[MegaLevelController] No villain data for mega level.");
                return;
            }

            SpawnVillain(level.villain);
        }

        private void SpawnVillain(VillainData data)
        {
            Vector3 spawnPos = villainSpawnPoint != null
                ? villainSpawnPoint.position
                : new Vector3(0, 3.5f, 0);

            GameObject villainObj;
            if (villainPrefab != null)
                villainObj = Instantiate(villainPrefab, spawnPos, Quaternion.identity);
            else
            {
                villainObj = new GameObject("Villain");
                villainObj.transform.position = spawnPos;
                villainObj.AddComponent<SpriteRenderer>();
                villainObj.AddComponent<BoxCollider2D>();
            }

            activeVillain = villainObj.GetComponent<Villain>();
            if (activeVillain == null)
                activeVillain = villainObj.AddComponent<Villain>();

            activeAI = villainObj.GetComponent<VillainAI>();
            if (activeAI == null)
                activeAI = villainObj.AddComponent<VillainAI>();

            activeVillain.OnDefeated += HandleVillainDefeated;
            activeAI.Initialize(data);
        }

        public void OnAnimalQuotaMet()
        {
            animalsCollected = true;
            CheckMegaLevelComplete();
        }

        private void HandleVillainDefeated()
        {
            villainDefeated = true;
            OnVillainDefeated?.Invoke();
            CheckMegaLevelComplete();
        }

        private void CheckMegaLevelComplete()
        {
            if (villainDefeated && animalsCollected)
            {
                OnMegaLevelWon?.Invoke();

                if (GameManager.Instance != null)
                    GameManager.Instance.OnMegaLevelComplete();
            }
        }

        public void Cleanup()
        {
            if (activeVillain != null)
            {
                activeVillain.OnDefeated -= HandleVillainDefeated;
                Destroy(activeVillain.gameObject);
            }

            activeVillain = null;
            activeAI = null;
            currentLevel = null;
        }

        private void OnDestroy()
        {
            if (activeVillain != null)
                activeVillain.OnDefeated -= HandleVillainDefeated;
        }
    }
}
