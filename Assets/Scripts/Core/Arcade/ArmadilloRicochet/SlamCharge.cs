using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    public class SlamCharge : MonoBehaviour
    {
        [SerializeField] private float slamForce = 15f;

        public int RemainingCharges { get; private set; }

        private Rigidbody2D targetRb;

        public void Configure(Rigidbody2D rb, int charges, float force)
        {
            targetRb = rb;
            RemainingCharges = charges;
            slamForce = force;
        }

        private void Update()
        {
            if (RemainingCharges <= 0 || targetRb == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 tapPos = Input.mousePosition;
                bool tapLeft = tapPos.x < Screen.width * 0.5f;

                Vector2 dir = tapLeft ? Vector2.left : Vector2.right;
                targetRb.AddForce(dir * slamForce, ForceMode2D.Impulse);
                RemainingCharges--;
            }
        }
    }
}
