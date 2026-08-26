// EffectsController — distinct pop VFX per animal species / type
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;
using AnimalFall.Utils;

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
        private readonly Stack<SpriteRenderer> _fragmentPool = new Stack<SpriteRenderer>(24);
        private readonly Dictionary<Sprite, Sprite[]> _fragmentSprites = new Dictionary<Sprite, Sprite[]>();
        private Transform _vfxRoot;
        private Material _particleMaterial;
        private int _burstSystemCount;
        private int _activeFragments;

        private const int MaxBurstSystems = 8;
        private const int MaxActiveFragments = 24;

        private static readonly ParticleSystem.Burst[] ConfettiBurst = { new ParticleSystem.Burst(0f, 8) };
        private static readonly ParticleSystem.Burst[] SparkleBurst = { new ParticleSystem.Burst(0f, 10) };
        private static readonly ParticleSystem.Burst[] RingBurst = { new ParticleSystem.Burst(0f, 8) };
        private static readonly ParticleSystem.Burst[] FeatherBurst = { new ParticleSystem.Burst(0f, 6) };

        private static readonly Color[] SpeciesColors =
        {
            Color.white,                              // None
            new Color(1.00f, 0.85f, 0.25f),           // Chicken — gold
            new Color(0.55f, 0.35f, 0.15f),           // Dog — brown
            new Color(0.75f, 0.80f, 0.90f),           // Cow — silver
            new Color(1.00f, 0.55f, 0.75f),           // Panda — pink
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

            Shader particleShader = Shader.Find("Sprites/Default");
            if (particleShader != null) _particleMaterial = new Material(particleShader);
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

        private void OnDestroy()
        {
            if (_particleMaterial != null) Destroy(_particleMaterial);
            foreach (Sprite[] pieces in _fragmentSprites.Values)
            {
                if (pieces == null) continue;
                for (int i = 0; i < pieces.Length; i++)
                    if (pieces[i] != null) Destroy(pieces[i]);
            }
        }

        private void SpawnCollectEffect(AnimalSpecies species, AnimalType type, Vector3 worldPos)
        {
            // Break the collected animal sprite into six small, short-lived pieces.
            SpawnSpriteFragments(species, worldPos);

            // Keep a restrained particle accent behind the fragments.
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
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    emission.SetBursts(ConfettiBurst);
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    break;
                case 1: // star sparkle
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
                    emission.SetBursts(SparkleBurst);
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    break;
                case 2: // ring pop
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.1f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
                    emission.SetBursts(RingBurst);
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.15f;
                    break;
                default: // soft feathers
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
                    main.startSize  = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                    emission.SetBursts(FeatherBurst);
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    break;
            }

            ps.Clear(true);
            ps.Play(true);

            StartCoroutine(RecycleBurst(ps, 1.1f, style));
        }

        private void SpawnSpriteFragments(AnimalSpecies species, Vector3 worldPos)
        {
            Sprite source = ImageLibrary.GetAnimalSprite(species);
            if (source == null) return;
            Sprite[] pieces = GetFragmentSprites(source);
            if (pieces == null) return;

            float largest = Mathf.Max(source.bounds.size.x, source.bounds.size.y);
            float normalisedScale = largest > 0.001f ? 1.45f / largest : 0.65f;
            const int columns = 3;
            const int rows = 2;

            for (int i = 0; i < pieces.Length && _activeFragments < MaxActiveFragments; i++)
            {
                SpriteRenderer sr;
                if (_fragmentPool.Count > 0)
                {
                    sr = _fragmentPool.Pop();
                }
                else
                {
                    var fragment = new GameObject("SpriteFragment");
                    fragment.transform.SetParent(_vfxRoot, false);
                    sr = fragment.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 24;
                }

                int column = i % columns;
                int row = i / columns;
                Vector2 localOffset = new Vector2(
                    (column - (columns - 1) * 0.5f) * source.bounds.size.x / columns,
                    (row - (rows - 1) * 0.5f) * source.bounds.size.y / rows) * normalisedScale;

                _activeFragments++;
                GameObject go = sr.gameObject;
                go.SetActive(true);
                sr.sprite = pieces[i];
                sr.color = Color.white;
                go.transform.position = worldPos + (Vector3)localOffset;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * normalisedScale;

                Vector2 outward = localOffset.sqrMagnitude > 0.001f ? localOffset.normalized : Random.insideUnitCircle.normalized;
                Vector3 end = go.transform.position + (Vector3)(outward * Random.Range(0.28f, 0.52f)) + Vector3.up * 0.08f;

                DOTween.Kill(go);
                var sequence = DOTween.Sequence().SetId(go);
                sequence.Join(go.transform.DOMove(end, 0.38f).SetEase(Ease.OutQuad));
                sequence.Join(go.transform.DORotate(new Vector3(0f, 0f, Random.Range(-100f, 100f)), 0.38f));
                sequence.Join(sr.DOFade(0f, 0.38f));
                sequence.OnComplete(() =>
                {
                    go.SetActive(false);
                    _fragmentPool.Push(sr);
                    _activeFragments = Mathf.Max(0, _activeFragments - 1);
                });
            }
        }

        private Sprite[] GetFragmentSprites(Sprite source)
        {
            if (_fragmentSprites.TryGetValue(source, out Sprite[] cached)) return cached;

            const int columns = 3;
            const int rows = 2;
            cached = new Sprite[columns * rows];
            Rect rect = source.rect;
            float width = rect.width / columns;
            float height = rect.height / rows;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                Rect pieceRect = new Rect(rect.x + column * width, rect.y + row * height, width, height);
                Sprite piece = Sprite.Create(source.texture, pieceRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                piece.name = $"{source.name}_piece_{row}_{column}";
                cached[row * columns + column] = piece;
            }
            _fragmentSprites[source] = cached;
            return cached;
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

            if (_burstSystemCount >= MaxBurstSystems) return null;

            var go = new GameObject($"PopBurst_{style}");
            go.transform.SetParent(_vfxRoot, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = 0.55f;
            main.gravityModifier = 0.6f;
            main.maxParticles = 24;
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
            if (_particleMaterial != null) renderer.sharedMaterial = _particleMaterial;
            renderer.sortingOrder = 25;

            _burstSystemCount++;

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
            // Compact bomb accent only. The large explosion prefab is intentionally
            // omitted so the effect never covers the play field.
            SpawnSpeciesBurst(AnimalSpecies.None, AnimalType.Bomb, worldPos);
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
