// A highly visible shower of varied leaf sprites that briefly obscures the playfield.
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

        private const string LeafSpriteResource = "icons/hindrances/leaves";
        private const int LeafCount = 36;

        private readonly List<GameObject> _activeLeaves = new List<GameObject>(LeafCount);
        private Sprite[] _leafSprites;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            _leafSprites = Resources.LoadAll<Sprite>(LeafSpriteResource);
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

            float screenLeft = -5f, screenRight = 5f, screenTop = 6f, screenBottom = -6f;
            if (Camera.main != null)
            {
                float z = Mathf.Abs(Camera.main.transform.position.z);
                screenLeft  = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
                screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, z)).x;
                screenTop   = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, z)).y;
                screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, z)).y;
            }

            for (int i = 0; i < LeafCount; i++)
            {
                float startX = Random.Range(screenLeft, screenRight);
                var leaf = ObjectPooler.Instance.SpawnFromPool(
                    _leafPrefab,
                    new Vector3(startX, screenTop + 0.5f, 0f),
                    Quaternion.identity, transform);

                if (leaf != null)
                {
                    _activeLeaves.Add(leaf);
                    ConfigureLeaf(leaf);

                    float duration = Random.Range(5.8f, 7.2f);
                    float driftX = startX + Random.Range(-2.6f, 2.6f);
                    float endY = screenBottom - 1.1f;
                    leaf.transform.DOMoveX(driftX, duration).SetEase(Ease.InOutSine).SetId(leaf);
                    leaf.transform.DOMoveY(endY, duration).SetEase(Ease.Linear).SetId(leaf);
                    leaf.transform.DORotate(
                        new Vector3(0f, 0f, Random.Range(-620f, 620f)), duration,
                        RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.Linear).SetId(leaf)
                        .OnComplete(() =>
                        {
                            if (leaf != null)
                            {
                                _activeLeaves.Remove(leaf);
                                ObjectPooler.Instance?.ReturnToPool(leaf);
                            }
                        });
                }

                yield return new WaitForSeconds(0.06f);
            }

            while (_activeLeaves.Count > 0) yield return null;
            Deactivate();
        }

        private void ConfigureLeaf(GameObject leaf)
        {
            SpriteRenderer renderer = leaf.GetComponent<SpriteRenderer>();
            if (renderer == null) return;

            if (_leafSprites != null && _leafSprites.Length > 0)
                renderer.sprite = _leafSprites[Random.Range(0, _leafSprites.Length)];

            renderer.sortingOrder = 90;
            renderer.color = new Color(1f, 1f, 1f, Random.Range(0.82f, 1f));
            leaf.transform.localScale = Vector3.one * Random.Range(0.18f, 0.29f);
            leaf.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
    }
}
