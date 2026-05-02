using UnityEngine;
using AnimalFall.Managers;
using AnimalFall.Utils;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class PairedAnimalHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.PairedAnimal;

        [SerializeField] private float fallSpeed = 1.3f;
        [SerializeField] private float pairDistance = 1.5f;
        [SerializeField] private int pointValue = 100;
        [SerializeField] private float simultaneousWindow = 0.3f;

        [Header("Pair Visuals")]
        [SerializeField] private LineRenderer stringRenderer;
        [SerializeField] private Transform partnerTransform;

        private GameObject partner;
        private bool selfTapped;
        private bool partnerTapped;
        private float firstTapTime = -999f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            selfTapped = false;
            partnerTapped = false;

            if (partnerTransform == null)
            {
                partner = new GameObject("PairedPartner");
                partner.transform.position = transform.position + Vector3.right * pairDistance;
                partner.transform.SetParent(transform.parent);

                var sr = partner.AddComponent<SpriteRenderer>();
                SpriteRenderer mySr = GetComponent<SpriteRenderer>();
                if (mySr != null) sr.sprite = mySr.sprite;

                var col = partner.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;

                var partnerTap = partner.AddComponent<PairedPartnerTap>();
                partnerTap.owner = this;

                partnerTransform = partner.transform;
            }

            if (stringRenderer != null)
            {
                stringRenderer.positionCount = 2;
                stringRenderer.SetPosition(0, transform.position);
                stringRenderer.SetPosition(1, partnerTransform.position);
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
            if (partnerTransform != null)
                partnerTransform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            if (stringRenderer != null && partnerTransform != null)
            {
                stringRenderer.SetPosition(0, transform.position);
                stringRenderer.SetPosition(1, partnerTransform.position);
            }

            if (firstTapTime > 0 && Time.time - firstTapTime > simultaneousWindow)
            {
                selfTapped = false;
                partnerTapped = false;
                firstTapTime = -999f;
            }

            if (transform.position.y < -6f)
            {
                if (partner != null) Destroy(partner);
                Deactivate();
            }
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            RegisterTap(true);
        }

        public void OnPartnerTapped()
        {
            if (!IsActive) return;
            RegisterTap(false);
        }

        private void RegisterTap(bool isSelf)
        {
            if (isSelf) selfTapped = true;
            else partnerTapped = true;

            if (firstTapTime < 0)
                firstTapTime = Time.time;

            if (selfTapped && partnerTapped)
            {
                context?.GameManager?.OnCorrectTap(2, pointValue);
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
                if (partner != null) Destroy(partner);
                Deactivate();
            }
        }

        protected override void OnDestroy()
        {
            if (partner != null) Destroy(partner);
            base.OnDestroy();
        }
    }

    public class PairedPartnerTap : MonoBehaviour
    {
        public PairedAnimalHindrance owner;

        private void OnMouseDown()
        {
            if (owner != null)
                owner.OnPartnerTapped();
        }
    }
}
