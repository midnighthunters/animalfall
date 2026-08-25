using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    public class CanyonBuilder : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject bumperPrefab;
        [SerializeField] private GameObject scarabPrefab;
        [SerializeField] private GameObject rockPrefab;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        public List<GoldenScarab> SpawnedScarabs { get; private set; } = new List<GoldenScarab>();

        public void BuildCanyon(CanyonLayout layout)
        {
            Clear();
            if (layout == null) return;

            if (layout.walls != null)
            {
                foreach (var wall in layout.walls)
                    SpawnWall(wall);
            }

            if (layout.bumpers != null)
            {
                foreach (var bumper in layout.bumpers)
                    SpawnBumper(bumper);
            }

            if (layout.scarabs != null)
            {
                foreach (var scarab in layout.scarabs)
                    SpawnScarab(scarab);
            }

            if (layout.breakableRocks != null)
            {
                foreach (var rock in layout.breakableRocks)
                    SpawnRock(rock);
            }

            SpawnExitPit(layout.exitPitPosition, layout.exitPitSize);
        }

        public void BuildProceduralCanyon(int scarabCount)
        {
            Clear();

            SpawnWall(new CanyonLayout.WallSegment { position = new Vector2(-3f, 0f), size = new Vector2(0.3f, 14f), rotation = 0 });
            SpawnWall(new CanyonLayout.WallSegment { position = new Vector2(3f, 0f), size = new Vector2(0.3f, 14f), rotation = 0 });

            for (int i = 0; i < 8; i++)
            {
                float x = Random.Range(-2.5f, 2.5f);
                float y = 4f - i * 1.2f;
                SpawnBumper(new CanyonLayout.BumperEntry { position = new Vector2(x, y), radius = 0.3f, bounciness = 1.2f });
            }

            for (int i = 0; i < 4; i++)
            {
                float x = Random.Range(-2f, 2f);
                float y = Random.Range(-3f, 3f);
                float angle = Random.Range(-30f, 30f);
                SpawnWall(new CanyonLayout.WallSegment { position = new Vector2(x, y), size = new Vector2(1.5f, 0.2f), rotation = angle });
            }

            for (int i = 0; i < scarabCount; i++)
            {
                float x = Random.Range(-2f, 2f);
                float y = Random.Range(-4f, 2f);
                SpawnScarab(new CanyonLayout.ScarabEntry { position = new Vector2(x, y) });
            }

            for (int i = 0; i < 3; i++)
            {
                float x = Random.Range(-2f, 2f);
                float y = Random.Range(-2f, 1f);
                SpawnRock(new CanyonLayout.RockEntry { position = new Vector2(x, y), size = new Vector2(0.6f, 0.6f), hp = 30f });
            }

            SpawnExitPit(new Vector2(0, -6f), new Vector2(3f, 0.5f));
        }

        private void SpawnWall(CanyonLayout.WallSegment wall)
        {
            GameObject obj;
            if (wallPrefab != null)
            {
                obj = Instantiate(wallPrefab, wall.position, Quaternion.Euler(0, 0, wall.rotation), transform);
            }
            else
            {
                obj = new GameObject("Wall");
                obj.transform.position = wall.position;
                obj.transform.rotation = Quaternion.Euler(0, 0, wall.rotation);
                obj.transform.SetParent(transform);
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.4f, 0.4f, 0.45f);
                obj.AddComponent<BoxCollider2D>();
                var rb = obj.AddComponent<Rigidbody2D>();
                rb.isKinematic = true;

                var mat = new PhysicsMaterial2D("StoneMat") { bounciness = 0.3f, friction = 0.5f };
                obj.GetComponent<BoxCollider2D>().sharedMaterial = mat;
            }
            obj.transform.localScale = new Vector3(wall.size.x, wall.size.y, 1f);
            spawnedObjects.Add(obj);
        }

        private void SpawnBumper(CanyonLayout.BumperEntry bumper)
        {
            GameObject obj;
            if (bumperPrefab != null)
            {
                obj = Instantiate(bumperPrefab, bumper.position, Quaternion.identity, transform);
            }
            else
            {
                obj = new GameObject("Bumper");
                obj.transform.position = bumper.position;
                obj.transform.SetParent(transform);
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.color = Color.red;
                obj.AddComponent<CircleCollider2D>();
                obj.AddComponent<BumperPeg>().Configure(bumper.radius, bumper.bounciness);
            }
            spawnedObjects.Add(obj);
        }

        private void SpawnScarab(CanyonLayout.ScarabEntry scarab)
        {
            GameObject obj;
            if (scarabPrefab != null)
            {
                obj = Instantiate(scarabPrefab, scarab.position, Quaternion.identity, transform);
            }
            else
            {
                obj = new GameObject("GoldenScarab");
                obj.transform.position = scarab.position;
                obj.transform.SetParent(transform);
                obj.transform.localScale = Vector3.one * 0.4f;
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.color = new Color(1f, 0.84f, 0f);
                obj.AddComponent<CircleCollider2D>();
                obj.AddComponent<GoldenScarab>();
            }

            var gs = obj.GetComponent<GoldenScarab>();
            if (gs != null) SpawnedScarabs.Add(gs);
            spawnedObjects.Add(obj);
        }

        private void SpawnRock(CanyonLayout.RockEntry rock)
        {
            GameObject obj;
            if (rockPrefab != null)
            {
                obj = Instantiate(rockPrefab, rock.position, Quaternion.identity, transform);
                var br = obj.GetComponent<BreakableRock>();
                if (br != null) br.Configure(rock.hp);
            }
            else
            {
                obj = new GameObject("BreakableRock");
                obj.transform.position = rock.position;
                obj.transform.SetParent(transform);
                obj.transform.localScale = new Vector3(rock.size.x, rock.size.y, 1f);
                obj.AddComponent<SpriteRenderer>();
                obj.AddComponent<BoxCollider2D>();
                obj.AddComponent<BreakableRock>().Configure(rock.hp);
            }
            spawnedObjects.Add(obj);
        }

        private void SpawnExitPit(Vector2 position, Vector2 size)
        {
            var pit = new GameObject("ExitPit");
            pit.transform.position = position;
            pit.transform.SetParent(transform);
            pit.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = pit.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var rb = pit.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;

            spawnedObjects.Add(pit);
        }

        public void Clear()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            spawnedObjects.Clear();
            SpawnedScarabs.Clear();
        }
    }
}
