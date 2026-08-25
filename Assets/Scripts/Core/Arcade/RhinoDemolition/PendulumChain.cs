using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    public class PendulumChain : MonoBehaviour
    {
        [Header("Chain Settings")]
        [SerializeField] private int chainLinkCount = 3;
        [SerializeField] private float linkLength = 0.5f;
        [SerializeField] private float linkMass = 0.5f;
        [SerializeField] private float dragForceMultiplier = 5f;

        [Header("References")]
        [SerializeField] private Transform craneAnchor;
        [SerializeField] private Rigidbody2D rhinoRb;
        [SerializeField] private GameObject chainLinkPrefab;

        public bool IsSnapped { get; private set; }
        public Rigidbody2D RhinoBody => rhinoRb;

        private readonly List<GameObject> chainLinks = new List<GameObject>();
        private readonly List<HingeJoint2D> joints = new List<HingeJoint2D>();
        private bool isDragging;
        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        public void BuildChain()
        {
            ClearChain();
            IsSnapped = false;

            if (craneAnchor == null || rhinoRb == null) return;

            Rigidbody2D previousBody = craneAnchor.GetComponent<Rigidbody2D>();
            if (previousBody == null)
            {
                previousBody = craneAnchor.gameObject.AddComponent<Rigidbody2D>();
                previousBody.isKinematic = true;
            }

            for (int i = 0; i < chainLinkCount; i++)
            {
                Vector3 pos = craneAnchor.position + Vector3.down * linkLength * (i + 1);

                GameObject link;
                if (chainLinkPrefab != null)
                {
                    link = Instantiate(chainLinkPrefab, pos, Quaternion.identity, transform);
                }
                else
                {
                    link = new GameObject($"ChainLink_{i}");
                    link.transform.position = pos;
                    link.transform.SetParent(transform);
                    var sr = link.AddComponent<SpriteRenderer>();
                    sr.color = Color.gray;
                    link.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, linkLength);
                }

                var linkRb = link.GetComponent<Rigidbody2D>();
                if (linkRb == null) linkRb = link.AddComponent<Rigidbody2D>();
                linkRb.mass = linkMass;

                var hinge = link.AddComponent<HingeJoint2D>();
                hinge.connectedBody = previousBody;
                hinge.autoConfigureConnectedAnchor = false;
                hinge.anchor = new Vector2(0, linkLength * 0.5f);
                hinge.connectedAnchor = new Vector2(0, -linkLength * 0.5f);

                chainLinks.Add(link);
                joints.Add(hinge);
                previousBody = linkRb;
            }

            var rhinoJoint = rhinoRb.gameObject.AddComponent<DistanceJoint2D>();
            rhinoJoint.connectedBody = previousBody;
            rhinoJoint.autoConfigureDistance = false;
            rhinoJoint.distance = linkLength;
            rhinoJoint.maxDistanceOnly = true;
        }

        private void Update()
        {
            if (IsSnapped) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos = cam.ScreenToWorldPoint(Input.mousePosition);
                float dist = Vector2.Distance(pos, (Vector2)rhinoRb.position);
                if (dist < 2f)
                    isDragging = true;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                Vector2 pos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = pos - rhinoRb.position;
                rhinoRb.AddForce(dir * dragForceMultiplier, ForceMode2D.Force);
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }

        public void SnapChain()
        {
            if (IsSnapped) return;
            IsSnapped = true;

            var distJoint = rhinoRb.GetComponent<DistanceJoint2D>();
            if (distJoint != null) Destroy(distJoint);

            foreach (var joint in joints)
            {
                if (joint != null) Destroy(joint);
            }

            foreach (var link in chainLinks)
            {
                if (link != null)
                {
                    var rb = link.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.isKinematic = true;
                    Destroy(link, 0.5f);
                }
            }

            chainLinks.Clear();
            joints.Clear();
        }

        private void ClearChain()
        {
            foreach (var link in chainLinks)
            {
                if (link != null) Destroy(link);
            }
            chainLinks.Clear();
            joints.Clear();

            var existingJoint = rhinoRb?.GetComponent<DistanceJoint2D>();
            if (existingJoint != null) Destroy(existingJoint);
        }
    }
}
