// Task 4.5 — FallingLeavesHindrance: spawns exactly 20 pooled leaf objects
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    public class FallingLeavesHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.FallingLeaves;

        [SerializeField] private GameObject _leafPrefab;

        private readonly List<GameObject> _activeLeaves = new List<GameObject>(20);

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            StartCoroutine(SpawnLeaves());
        }

        protected override void OnDeactivate()
        {
            StopAllCoroutines();
            for (int i = _activeLeaves.Count - 1; i >= 0; i--)
            {
                if (_activeLeaves[i] != null)
                {
                    DOTween.Kill(_activeLeaves[i]);
                    ObjectPooler.Instance?.ReturnToPool(_activeLeaves[i]);
                }
            }
            _activeLeaves.Clear();
        }

        private IEnumerator SpawnLeaves()
        {
            if (_leafPrefab == null || ObjectPooler.Instance == null)
            {
                Deactivate();
                yield break;
            }

            float screenLeft  = -5f, screenRight = 5f, screenTop = 6f;
            if (Camera.main != null)
            {
                float z = Mathf.Abs(Camera.main.transform.position.z);
                screenLeft  = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
                screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, z)).x;
                screenTop   = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, z)).y;
            }

            for (int i = 0; i < 20; i++)
            {
                float startX = Random.Range(screenLeft, screenRight);
                var leaf = ObjectPooler.Instance.SpawnFromPool(
                    _leafPrefab,
                    new Vector3(startX, screenTop + 0.5f, 0f),
                    Quaternion.identity, transform);

                if (leaf != null)
                {
                    _activeLeaves.Add(leaf);
                    float driftX = startX + Random.Range(-2f, 2f);
                    float endY   = -screenTop - 1f;
                    leaf.transform.DOMoveX(driftX, 5f).SetEase(Ease.InOutSine).SetId(leaf);
                    leaf.transform.DOMoveY(endY, 5f).SetEase(Ease.Linear).SetId(leaf)
                        .OnComplete(() =>
                        {
                            if (leaf != null)
                            {
                                _activeLeaves.Remove(leaf);
                                ObjectPooler.Instance?.ReturnToPool(leaf);
                            }
                        });
                }

                yield return new WaitForSeconds(0.1f);
            }

            while (_activeLeaves.Count > 0) yield return null;
            Deactivate();
        }
    }
}
