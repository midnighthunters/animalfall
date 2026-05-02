using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Advanced
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class DecoyChestHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.DecoyChest;

        [SerializeField] private float fallSpeed = 1.2f;
        [SerializeField] private float timePenaltyPerBomb = 3f;
        [SerializeField] private int bombCount = 3;
        [SerializeField] private GameObject bombPrefab;

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            float wobble = Mathf.Sin(Time.time * 2f) * 3f;
            transform.rotation = Quaternion.Euler(0, 0, wobble);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            for (int i = 0; i < bombCount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1.5f), 0);

                if (bombPrefab != null)
                {
                    Instantiate(bombPrefab, transform.position + offset,
                        Quaternion.identity, transform.parent);
                }
                else
                {
                    context?.GameManager?.AddTime(-timePenaltyPerBomb);
                }
            }

            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Explosion);
            Effects.EffectsController.Instance?.ShakeCamera();
            Deactivate();
        }
    }
}
