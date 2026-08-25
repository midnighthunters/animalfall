// EffectsController — distinct pop VFX per animal species / type
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Effects
{
    public static class VFXRefs
    {
        public static GameObject BattleEffectWhite;
        public static GameObject ExplosionBam;
        public static GameObject ExplosionZap;
    }

    public class EffectsController : MonoBehaviour
    {
        [SerializeField] private GameObject _battleEffectWhitePrefab;
        [SerializeField] private GameObject _explosionBamPrefab;
        [SerializeField] private GameObject _explosionZapPrefab;
        [SerializeField] private GameObject _missFlashPrefab;

        private readonly Dictionary<int, Stack<ParticleSystem>> _burstPools = new Dictionary<int, Stack<ParticleSystem>>();
        private Transform _vfxRoot;

        private static readonly Color[] SpeciesColors =
        {
            Color.white,                              // None
            new Color(1.00f, 0.85f, 0.25f),           // Chicken — gold
            new Color(0.55f, 0.35f, 0.15f),           // Dog — brown
            new Color(0.75f, 0.80f, 0.90f),           // Cow — silver
            new Color(1.00f, 0.55f, 0.75f),           // Cat — pink
            new Color(0.55f, 0.35f, 0.20f),           // Monkey
            new Color(1.00f, 0.55f, 0.70f),           // Pig
            new Color(0.95f, 0.95f, 1.00f),           // Rabbit
            new Color(0.35f, 0.55f, 0.95f),           // Penguin — blue
            new Color(0.70f, 0.45f, 0.95f),           // Owl — purple
            new Color(0.80f, 0.80f, 0.80f),           // Mouse
            new Color(0.30f, 0.30f, 0.30f),           // Zebra
            new Color(0.30f, 0.85f, 0.55f),           // Duck — green
        };

        private void Awake()
        {
            VFXRefs.BattleEffectWhite = _battleEffectWhitePrefab;
            VFXRefs.ExplosionBam      = _explosionBamPrefab;
            VFXRefs.ExplosionZap      = _explosionZapPrefab;

            _vfxRoot = new GameObject("RuntimeVFX").transform;
            _vfxRoot.SetParent(transform, false);
        }

        private void OnEnable()
        {
            GameEvents.OnAnimalCollected += SpawnCollectEffect;
            GameEvents.OnBombTapped      += SpawnExplosionEffect;
            GameEvents.OnAnimalMissed    += SpawnMissFlash;
        }

        private void OnDisable()
        {
            GameEvents.OnAnimalCollected -= SpawnCollectEffect;
            GameEvents.OnBombTapped      -= SpawnExplosionEffect;
            GameEvents.OnAnimalMissed    -= SpawnMissFlash;
        }

        private void SpawnCollectEffect(AnimalSpecies species, AnimalType type, Vector3 worldPos)
        {
            // Prefer custom burst so every species feels unique
            SpawnSpeciesBurst(species, type, worldPos);

            // Optional legacy prefab if assigned
            if (VFXRefs.BattleEffectWhite != null && ObjectPooler.Instance != null)
            {
                var go = ObjectPooler.Instance.SpawnFromPool(VFXRefs.BattleEffectWhite, worldPos, Quaternion.identity, transform);
                if (go != null)
                {
                    var ps = go.GetComponent<ParticleSystem>();
                    float life = ps != null ? ps.main.duration + 0.5f : 0.6f;
                    StartCoroutine(ReturnAfter(go, life));
                }
            }
        }

        private void SpawnSpeciesBurst(AnimalSpecies species, AnimalType type, Vector3 worldPos)
        {
            Color c = SpeciesColors[Mathf.Clamp((int)species, 0, SpeciesColors.Length - 1)];
            if (type == AnimalType.Golden)  c = new Color(1f, 0.85f, 0.15f);
            if (type == AnimalType.Rainbow) c = Color.HSVToRGB(Random.value, 0.85f, 1f);

            int style = ((int)species) % 4; // 4 visual styles
            var ps = GetOrCreateBurst(style);
            if (ps == null) return;

            ps.transform.position = worldPos;
            var main = ps.main;
            main.startColor = c;

            // Style-specific tuning
            var emission = ps.emission;
            var shape = ps.shape;
            switch (style)
            {
                case 0: // confetti burst
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    break;
                case 1: // star sparkle
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    break;
                case 2: // ring pop
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 4.5f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.15f;
                    break;
                default: // soft feathers
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    break;
            }

            ps.Clear(true);
            ps.Play(true);

            // Expanding shock ring (simple sprite-less scale pulse via temp GO)
            SpawnShockRing(worldPos, c);

            StartCoroutine(RecycleBurst(ps, 1.1f, style));
        }

        private void SpawnShockRing(Vector3 pos, Color color)
        {
            var go = new GameObject("ShockRing");
            go.transform.SetParent(_vfxRoot, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(color.r, color.g, color.b, 0.65f);
            sr.sortingOrder = 20;
            go.transform.localScale = Vector3.one * 0.15f;

            var seq = DOTween.Sequence().SetId(go);
            seq.Join(go.transform.DOScale(1.6f, 0.35f).SetEase(Ease.OutQuad));
            seq.Join(sr.DOFade(0f, 0.35f));
            seq.OnComplete(() => Destroy(go));
        }

        private static Sprite _circleSprite;
        private static Sprite CreateCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - r * 0.72f) / (r * 0.18f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return _circleSprite;
        }

        private ParticleSystem GetOrCreateBurst(int style)
        {
            if (!_burstPools.TryGetValue(style, out var stack))
            {
                stack = new Stack<ParticleSystem>();
                _burstPools[style] = stack;
            }

            if (stack.Count > 0)
            {
                var reused = stack.Pop();
                if (reused != null) return reused;
            }

            var go = new GameObject($"PopBurst_{style}");
            go.transform.SetParent(_vfxRoot, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = 0.55f;
            main.gravityModifier = 0.6f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = grad;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 25;

            return ps;
        }

        private IEnumerator RecycleBurst(ParticleSystem ps, float delay, int style)
        {
            yield return new WaitForSeconds(delay);
            if (ps == null) yield break;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (!_burstPools.TryGetValue(style, out var stack))
            {
                stack = new Stack<ParticleSystem>();
                _burstPools[style] = stack;
            }
            stack.Push(ps);
        }

        private void SpawnExplosionEffect(Vector3 worldPos)
        {
            // Red/orange bomb burst
            SpawnSpeciesBurst(AnimalSpecies.None, AnimalType.Bomb, worldPos);

            if (VFXRefs.ExplosionBam == null || ObjectPooler.Instance == null) return;
            var go = ObjectPooler.Instance.SpawnFromPool(VFXRefs.ExplosionBam, worldPos, Quaternion.identity, transform);
            if (go != null) StartCoroutine(ReturnAfter(go, 1.5f));
        }

        private void SpawnMissFlash()
        {
            if (Camera.main == null) return;
            // Soft red vignette pulse on camera (no prefab required)
            var cam = Camera.main;
            var original = cam.backgroundColor;
            DOTween.Kill(cam);
            cam.DOColor(Color.Lerp(original, new Color(0.9f, 0.25f, 0.25f), 0.35f), 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .SetId(cam);
        }

        private IEnumerator ReturnAfter(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            ObjectPooler.Instance?.ReturnToPool(go);
        }
    }
}
