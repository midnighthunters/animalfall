using System.Collections;
using UnityEngine;
using AnimalFall.Core.Goals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Animals
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AnimalMovement))]
    public class Animal : MonoBehaviour
    {
        public AnimalData data;
        public int currentShield;

        private SpriteRenderer sr;
        private AnimalMovement movement;
        private float spawnTime;
        private float lastTapTime = -999f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            movement = GetComponent<AnimalMovement>();
        }

        public void Setup(AnimalData animalData, Levels.LevelData levelData)
        {
            data = animalData;
            sr.sprite = animalData.sprite;
            spawnTime = Time.time;
            currentShield = animalData.shieldHP;
            movement.ConfigureRandomSpeed(animalData.speedMin, animalData.speedMax);

            if (animalData.type == AnimalType.Decoy)
                sr.color = Color.Lerp(Color.white, Color.grey, 0.2f);
        }

        public TapResult HandleTap()
        {
            float now = Time.time;
            lastTapTime = now;

            if (data.type == AnimalType.Bomb)
            {
                Explode();
                return TapResult.BombExploded;
            }

            if (data.requiresDoubleTap || data.type == AnimalType.Shielded)
            {
                currentShield--;
                if (currentShield > 0)
                {
                    StartCoroutine(FlashOutline());
                    return TapResult.ShieldBroken;
                }
            }

            if (data.type == AnimalType.Golden)
            {
                OnCollected();
                return TapResult.Golden;
            }

            OnCollected();
            return TapResult.Correct;
        }

        private IEnumerator FlashOutline()
        {
            Vector3 origScale = transform.localScale;
            transform.localScale = origScale * 1.05f;
            yield return new WaitForSeconds(0.12f);
            transform.localScale = origScale;
        }

        private void Explode()
        {
            GameManager.Instance.OnWrongTap(true);
            Destroy(gameObject);
        }

        private void OnCollected()
        {
            if (data.isTargetSpecies)
                GameManager.Instance.OnCorrectTap(1, data.pointValue);
            else
                GameManager.Instance.OnWrongTap(false);

            if (data.species != AnimalSpecies.None &&
                GoalPanel.Instance != null &&
                GoalPanel.Instance.IsSpeciesRequired(data.species))
            {
                GoalPanel.Instance.DecreaseGoal(data.species);
            }

            if (GameManager.Instance != null && GameManager.Instance.AudioManager != null)
                GameManager.Instance.AudioManager.PlaySFX(AudioManager.SfxType.Collect);

            Destroy(gameObject);
        }

        private void Update()
        {
            if (Time.time - spawnTime > data.lifetime)
                Destroy(gameObject);
        }
    }
}
