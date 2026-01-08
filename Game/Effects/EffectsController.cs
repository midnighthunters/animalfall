using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsController : MonoBehaviour
{
    public static EffectsController Instance { get; private set; }

    [Header("Generic FX Prefabs (assign per animal)")]
    public GameObject eagleExplosionPrefab;    // feathers / slow bloom
    public GameObject gorillaSmashPrefab;      // big rocks + dust stomp
    public GameObject tigerSlashPrefab;        // horizontal slash trail
    public GameObject bullChargePrefab;        // vertical dust line
    public GameObject foxBurstPrefab;          // tiny sparkles + particles
    public GameObject wolfRipplePrefab;        // radial ripple / spectral particles

    [Header("Shared FX")]
    public GameObject cubeCrackEffectPrefab;
    public GameObject balloonCrackEffectPrefab;
    public GameObject rocketTrailEffectPrefab;
    [SerializeField] private GameObject ballonCrackEffectPrefab;


    [Header("Camera shake settings")]
    public float cameraShakeDuration = 0.35f;
    public float cameraShakeMagnitude = 0.15f;

    [Header("Animal Colors")]
    [SerializeField] private Color chickenColor; // Replaces redColor
    [SerializeField] private Color dogColor;     // Replaces blueColor
    [SerializeField] private Color cowColor;     // Replaces yellowColor
    [SerializeField] private Color catColor;     // Replaces greenColor
    [SerializeField] private Color monkeyColor;  // Replaces purpleColor

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        chickenColor = chickenColor == default ? new Color(1f, 0.85f, 0.6f) : chickenColor; // warm cream
        dogColor = dogColor == default ? new Color(0.45f, 0.75f, 1f) : dogColor;      // soft blue
        cowColor = cowColor == default ? new Color(1f, 0.9f, 0.55f) : cowColor;     // yellowish
        catColor = catColor == default ? new Color(0.6f, 1f, 0.6f) : catColor;      // mint green
        monkeyColor = monkeyColor == default ? new Color(0.75f, 0.6f, 1f) : monkeyColor;   // purple

    }

    // // Modern particle color assignment helper (works for main + sub-emitters)
    // private void SetParticleSystemColor(GameObject go, Color color)
    // {
    //     var ps = go.GetComponent<ParticleSystem>();
    //     if (ps != null)
    //     {
    //         var main = ps.main;
    //         main.startColor = color;
    //     }

    //     // also handle children
    //     foreach (ParticleSystem child in go.GetComponentsInChildren<ParticleSystem>())
    //     {
    //         var main = child.main;
    //         main.startColor = color;
    //     }
    // }
    // // spawn cube crack (updated to modern API)
    // public void SpawnCubeCrackEffect(Vector3 spawnPos, CubeTypes cubeType)
    // {
    //     if (cubeCrackEffectPrefab == null) return;
    //     GameObject spawnedEffect = Instantiate(cubeCrackEffectPrefab, spawnPos, Quaternion.identity);
    //     SetParticleSystemColor(spawnedEffect, DetectCubeColor(cubeType));
    //     var ps = spawnedEffect.GetComponent<ParticleSystem>();
    //     if (ps != null) ps.Play();
    //     Destroy(spawnedEffect, 3f);
    // }

    // // NEW: spawn animal-specific explosion by type
    // public void SpawnAnimalExplosion(BlockTypes type, Vector3 position, float globalScale = 1f)
    // {
    //     GameObject prefab = null;
    //     Color tint = Color.white;

    //     switch (type)
    //     {
    //         case BlockTypes.Eagle:
    //             prefab = eagleExplosionPrefab;
    //             tint = new Color(1f, 0.95f, 0.8f); // warm feather-ish
    //             break;
    //         case BlockTypes.Gorilla:
    //             prefab = gorillaSmashPrefab;
    //             tint = new Color(0.9f, 0.85f, 0.8f); // dusty rock
    //             break;
    //         case BlockTypes.Tiger:
    //             prefab = tigerSlashPrefab;
    //             tint = new Color(1f, 0.85f, 0.5f); // orange slash
    //             break;
    //         case BlockTypes.Bull:
    //             prefab = bullChargePrefab;
    //             tint = new Color(1f, 0.95f, 0.7f); // dusty
    //             break;
    //         case BlockTypes.Fox:
    //             prefab = foxBurstPrefab;
    //             tint = new Color(1f, 0.6f, 0.25f); // playful orange
    //             break;
    //         case BlockTypes.Wolf:
    //             prefab = wolfRipplePrefab;
    //             tint = new Color(0.8f, 0.9f, 1f); // spectral blue
    //             break;
    //         default:
    //             prefab = eagleExplosionPrefab;
    //             break;
    //     }

    //     if (prefab == null) return;
    //     GameObject go = Instantiate(prefab, position, Quaternion.identity);
    //     go.transform.localScale = Vector3.one * globalScale;
    //     SetParticleSystemColor(go, tint);

    //     // play all particle systems on prefab and its children
    //     foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
    //     {
    //         ps.Play();
    //     }

    //     // automatic cleanup (prefab should have short-lived particles)
    //     Destroy(go, 3.5f * globalScale);
    // }

    // // NEW: staggered per-block FX — spawn cracking + small burst for each target
    // public IEnumerator StaggeredClearFX(IEnumerable<Block> blocks, float interval = 0.06f)
    // {
    //     foreach (var b in blocks)
    //     {
    //         if (b == null) continue;

    //         // spawn cube crack effect if cube
    //         if (b is CubeBlock cb)
    //         {
    //             SpawnCubeCrackEffect(b.transform.position, cb.cubeType);
    //             // play small pop SFX if audio manager exists
    //             AudioManager.Instance?.PlayCubeExplosionAudio();
    //         }

    //         // short per-block flash
    //         StartCoroutine(OneFrameFlash(b.transform.position, 0.12f));
    //         yield return new WaitForSeconds(interval);
    //     }
    // }

    // // tiny flash used for emphasis
    // private IEnumerator OneFrameFlash(Vector3 pos, float duration)
    // {
    //     // Create a short white sprite or light; simplest: spawn a sprite that fades
    //     GameObject flash = new GameObject("Flash");
    //     flash.transform.position = pos;
    //     var sr = flash.AddComponent<SpriteRenderer>();
    //     sr.sprite = ImageLibrary.whiteCircleSprite; // add a tiny white circle to ImageLibrary
    //     sr.sortingLayerName = "VFX";
    //     sr.sortingOrder = 500;
    //     sr.transform.localScale = Vector3.one * 0.35f;
    //     Color c = sr.color;
    //     c.a = 0.9f;
    //     sr.color = c;

    //     // fade out
    //     float t = 0f;
    //     while (t < duration)
    //     {
    //         t += Time.deltaTime;
    //         float a = Mathf.Lerp(0.9f, 0f, t / duration);
    //         var col = sr.color; col.a = a; sr.color = col;
    //         yield return null;
    //     }
    //     Destroy(flash);
    // }

    // // simple camera shake (attach to main camera)
    // public IEnumerator CameraShake(float duration, float magnitude)
    // {
    //     Transform cam = Camera.main.transform;
    //     Vector3 originalPos = cam.localPosition;
    //     float elapsed = 0.0f;

    //     while (elapsed < duration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float x = Random.Range(-1f, 1f) * magnitude;
    //         float y = Random.Range(-1f, 1f) * magnitude;
    //         cam.localPosition = originalPos + new Vector3(x, y, 0f);
    //         yield return null;
    //     }

    //     cam.localPosition = originalPos;
    // }

    // // detect color (existing)
    // private Color DetectCubeColor(CubeTypes cubeType)
    // {
    //     // use your serialised colors or fallback
    //     switch (cubeType)
    //     {
    //         case CubeTypes.Chicken: return chickenColor;
    //         case CubeTypes.Dog: return dogColor;
    //         case CubeTypes.Cow: return cowColor;
    //         case CubeTypes.Cat: return catColor;
    //         case CubeTypes.Monkey: return monkeyColor;
    //         default: return Color.white;
    //     }
    // }

    // public void SpawnBalloonCrackEffect(Vector3 spawnPos)
    // {
    //     GameObject spawnedEffect = Instantiate(ballonCrackEffectPrefab, spawnPos, Quaternion.identity);
    //     spawnedEffect.GetComponent<ParticleSystem>().Play();
    //     Destroy(spawnedEffect, 3f);
    // }
}
