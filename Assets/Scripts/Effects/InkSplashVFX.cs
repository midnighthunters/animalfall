using System.Collections;
using UnityEngine;

namespace AnimalFall.Effects
{
    /// <summary>
    /// Self-contained black ink splash VFX. When played it spreads black ink from a
    /// world point: a growing central ink blot plus a burst of ink droplets that
    /// fling outward. It builds its own sprite/material at runtime and cleans itself
    /// up, so callers need no scene wiring, prefabs or serialized references.
    /// </summary>
    public sealed class InkSplashVFX : MonoBehaviour
    {
        private const int BlotSortingOrder = 95;
        private const int DropletSortingOrder = 100;

        private static Sprite _blotSprite;
        private static Sprite _dropletSprite;
        private static Material _particleMaterial;

        /// <summary>Spawns and plays a black ink splash at a world position.</summary>
        public static void Play(Vector3 worldPosition, float duration = 5f)
        {
            var go = new GameObject("InkSplashVFX");
            go.transform.position = worldPosition;
            go.AddComponent<InkSplashVFX>().Build(Mathf.Max(1f, duration));
        }

        private void Build(float duration)
        {
            EnsureSharedAssets();
            BuildDroplets(duration);

            SpriteRenderer blot = BuildBlot();
            StartCoroutine(AnimateBlot(blot, duration));
        }

        // ── Central spreading ink blot ─────────────────────────────────────────

        private SpriteRenderer BuildBlot()
        {
            var blotGo = new GameObject("InkBlot");
            blotGo.transform.SetParent(transform, false);

            var sr = blotGo.AddComponent<SpriteRenderer>();
            sr.sprite = _blotSprite;
            sr.color = new Color(0f, 0f, 0f, 0.92f);
            sr.sortingOrder = BlotSortingOrder;
            blotGo.transform.localScale = Vector3.one * 0.1f;
            return sr;
        }

        private IEnumerator AnimateBlot(SpriteRenderer sr, float duration)
        {
            const float growTime = 0.45f;
            const float fadeTime = 1f;
            float hold = Mathf.Max(0f, duration - growTime - fadeTime);

            float target = ScreenCoverScale();

            // Spread outward from the tap point.
            float t = 0f;
            while (t < growTime)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / growTime);
                sr.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, target, k);
                yield return null;
            }
            sr.transform.localScale = Vector3.one * target;

            yield return new WaitForSeconds(hold);

            // Ink dissolves away.
            Color start = sr.color;
            t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, t / fadeTime);
                sr.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }

        private static float ScreenCoverScale()
        {
            Camera cam = Camera.main;
            if (cam == null) return 30f;
            float worldHeight = cam.orthographic ? cam.orthographicSize * 2f : 12f;
            float worldWidth = worldHeight * cam.aspect;
            // Blob sprite is 1 world unit across at scale 1; oversize so it covers the
            // screen even when the octopus is tapped near a corner.
            return Mathf.Max(worldWidth, worldHeight) * 2.2f;
        }

        // ── Ink droplet burst ──────────────────────────────────────────────────

        private void BuildDroplets(float duration)
        {
            var ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float dropletLife = Mathf.Clamp(duration * 0.5f, 1.2f, 3f);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = dropletLife;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.7f);
            main.startColor = Color.black;
            main.gravityModifier = 0.25f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Cleanup is driven by the blot coroutine (Destroy at end of AnimateBlot)
            // so the whole effect lives for the full duration, not just the droplets.

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 46) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.9f)));

            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.6f;                                // droplets decelerate as the ink settles
            limit.limit = new ParticleSystem.MinMaxCurve(2.5f); // clamp speed so they don't fly off-screen

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.black, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(grad);

            var psRenderer = GetComponent<ParticleSystemRenderer>();
            psRenderer.material = _particleMaterial;
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.alignment = ParticleSystemRenderSpace.View;
            psRenderer.sortingOrder = DropletSortingOrder;

            ps.Play();
        }

        // ── Shared runtime-built assets ──────────────────────────────────────────

        private static void EnsureSharedAssets()
        {
            // Blot: mostly solid so, once scaled up, it actually covers the play area.
            if (_blotSprite == null) _blotSprite = CreateDiskSprite(0.72f);
            // Droplet: soft radial for inky splatter.
            if (_dropletSprite == null) _dropletSprite = CreateDiskSprite(0.12f);

            if (_particleMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Unlit/Transparent");

                _particleMaterial = new Material(shader) { name = "InkSplashParticle" };
                if (_dropletSprite != null) _particleMaterial.mainTexture = _dropletSprite.texture;
            }
        }

        /// <summary>
        /// Builds a white (black-tintable) circular sprite. Pixels within
        /// <paramref name="solidFraction"/> of the radius are fully opaque; the rest softly
        /// fades to transparent. A large fraction gives a solid ink disk with an inky rim;
        /// a small fraction gives a soft droplet.
        /// </summary>
        private static Sprite CreateDiskSprite(float solidFraction)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float maxDist = size / 2f;
            float solid = Mathf.Clamp01(solidFraction);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = d <= solid
                        ? 1f
                        : Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((d - solid) / (1f - solid)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
