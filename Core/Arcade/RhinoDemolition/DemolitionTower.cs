using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Arcade.Shared;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    public class DemolitionTower : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TowerLayout layout;
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private GameObject tntPrefab;

        private readonly List<GameObject> spawnedBlocks = new List<GameObject>();

        public void BuildTower(TowerLayout towerLayout = null)
        {
            Clear();
            TowerLayout activeLayout = towerLayout != null ? towerLayout : layout;
            if (activeLayout == null || activeLayout.blocks == null) return;

            foreach (var entry in activeLayout.blocks)
            {
                Vector2 pos = activeLayout.towerOrigin + entry.position;

                GameObject block;

                if (entry.material == BlockMaterial.TNT && tntPrefab != null)
                {
                    block = Instantiate(tntPrefab, pos, Quaternion.Euler(0, 0, entry.rotation), transform);
                }
                else if (blockPrefab != null)
                {
                    block = Instantiate(blockPrefab, pos, Quaternion.Euler(0, 0, entry.rotation), transform);
                    var destructible = block.GetComponent<DestructibleBlock>();
                    if (destructible != null)
                        destructible.material = entry.material;
                }
                else
                {
                    block = new GameObject("Block");
                    block.transform.position = pos;
                    block.transform.rotation = Quaternion.Euler(0, 0, entry.rotation);
                    block.transform.SetParent(transform);

                    var sr = block.AddComponent<SpriteRenderer>();
                    var col = block.AddComponent<BoxCollider2D>();
                    var rb = block.AddComponent<Rigidbody2D>();
                    rb.mass = 1f;

                    var destBlock = block.AddComponent<DestructibleBlock>();
                    destBlock.material = entry.material;
                }

                block.transform.localScale = new Vector3(entry.scale.x, entry.scale.y, 1f);
                spawnedBlocks.Add(block);
            }
        }

        public void BuildProceduralTower(int rows, int columns)
        {
            Clear();

            Vector2 origin = new Vector2(5f, -3f);
            float blockWidth = 0.8f;
            float blockHeight = 0.4f;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    float x = origin.x + (col - columns / 2f) * blockWidth;
                    float y = origin.y + row * blockHeight;

                    BlockMaterial mat;
                    if (row == 0) mat = BlockMaterial.Stone;
                    else if (Random.value < 0.1f) mat = BlockMaterial.TNT;
                    else if (Random.value < 0.3f) mat = BlockMaterial.Glass;
                    else mat = BlockMaterial.Wood;

                    var block = new GameObject($"Block_{row}_{col}");
                    block.transform.position = new Vector3(x, y, 0);
                    block.transform.SetParent(transform);
                    block.transform.localScale = new Vector3(blockWidth, blockHeight, 1f);

                    block.AddComponent<SpriteRenderer>();
                    block.AddComponent<BoxCollider2D>();
                    var rb = block.AddComponent<Rigidbody2D>();
                    rb.mass = mat == BlockMaterial.Stone ? 3f : 1f;

                    if (mat == BlockMaterial.TNT)
                    {
                        block.AddComponent<TNTBarrel>();
                    }
                    else
                    {
                        var destBlock = block.AddComponent<DestructibleBlock>();
                        destBlock.material = mat;
                    }

                    spawnedBlocks.Add(block);
                }
            }
        }

        public void Clear()
        {
            foreach (var block in spawnedBlocks)
            {
                if (block != null) Destroy(block);
            }
            spawnedBlocks.Clear();
        }
    }
}
