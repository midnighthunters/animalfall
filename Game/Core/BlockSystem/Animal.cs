// ============================================================
//  Animal.cs  –  Animal Fall  (REFACTORED)
//  Changes vs original:
//    • Destroy(gameObject)   → return to AnimalPool
//    • Explicit audio calls  → AudioManager.Instance
//    • VFX calls             → VFXPoolRegistry
//    • EventBus emission     → OnAnimalCollected / OnAnimalMissed
//    • All GameManager refs  → GameManager.Instance (unchanged API)
// ============================================================

using System.Collections;
using UnityEngine;

public enum TapResult { Correct, Wrong, BombExploded, ShieldBroken, Golden }

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AnimalMovement))]
public class Animal : MonoBehaviour
{
    // ── Data ──────────────────────────────────────────────────
    public AnimalData data;
    public AnimalPool OwningPool;   // set by pool on Borrow

    // ── Private ───────────────────────────────────────────────
    private LevelData       _level;
    private SpriteRenderer  _sr;
    private AnimalMovement  _movement;
    private float           _spawnTime;
    public  int             currentShield;
    private float           _lastTapTime = -999f;
    private int             _tapCount    = 0;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        _sr       = GetComponent<SpriteRenderer>();
        _movement = GetComponent<AnimalMovement>();
    }

    private void OnDisable()
    {
        // Reset state when returned to pool
        _tapCount    = 0;
        _lastTapTime = -999f;
        _sr.color    = Color.white;
    }

    // ── Setup (called by Spawner) ─────────────────────────────
    public void Setup(AnimalData d, LevelData lv)
    {
        data         = d;
        _level       = lv;
        _sr.sprite   = d.sprite;
        _spawnTime   = Time.time;
        currentShield = d.shieldHP;
        _tapCount    = 0;
        _lastTapTime = -999f;

        _movement.ConfigureRandomSpeed(d.speedMin, d.speedMax);

        // Visual cues
        _sr.color = d.type == AnimalType.Decoy
            ? Color.Lerp(Color.white, Color.grey, 0.25f)
            : Color.white;
    }

    // ── Tap handling ──────────────────────────────────────────
    public TapResult HandleTap()
    {
        _tapCount++;
        float now         = Time.time;
        _lastTapTime      = now;

        // Bomb
        if (data.type == AnimalType.Bomb)
        {
            Explode();
            return TapResult.BombExploded;
        }

        // Shield
        if (data.requiresDoubleTap || data.type == AnimalType.Shielded)
        {
            currentShield--;
            if (currentShield > 0)
            {
                StartCoroutine(FlashOutline());
                AudioManager.Instance?.PlaySFX(AudioManager.SfxType.ShieldBreak);
                return TapResult.ShieldBroken;
            }
        }

        // Golden
        if (data.type == AnimalType.Golden)
        {
            OnCollected(isGolden: true);
            return TapResult.Golden;
        }

        // Normal / special / decoy
        if (data.isTargetSpecies)
        {
            OnCollected();
            return TapResult.Correct;
        }
        else
        {
            GameManager.Instance?.OnWrongTap(false);
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.WrongTap);
            Release();
            return TapResult.Wrong;
        }
    }

    // ── Internal ──────────────────────────────────────────────
    private void OnCollected(bool isGolden = false)
    {
        int pts = isGolden ? data.pointValue * 3 : data.pointValue;

        GameManager.Instance?.OnCorrectTap(1, pts);
        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.Collect);
        VFXPoolRegistry.Instance?.Spawn(VFXPoolRegistry.Collect, transform.position);

        // Goal system
        if (data.species != AnimalSpecies.None && GoalPanel.Instance != null
            && GoalPanel.Instance.IsSpeciesRequired(data.species))
        {
            GoalPanel.Instance.DecreaseGoal(data.species);
            VFXPoolRegistry.Instance?.Spawn(VFXPoolRegistry.GoalPop,
                GoalPanel.Instance.GetGoalPosition(data.species));
        }

        EventBus.Publish(new OnAnimalCollected
        {
            species  = data.species,
            points   = pts,
            worldPos = transform.position
        });

        // Camera shake on bomb-cleared / golden
        if (isGolden)
            CameraController.Instance?.Shake(0.15f, 0.08f);

        Release();
    }

    private void Explode()
    {
        GameManager.Instance?.OnWrongTap(true);
        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.Explosion);
        VFXPoolRegistry.Instance?.Spawn(VFXPoolRegistry.Explosion, transform.position);
        CameraController.Instance?.Shake();
        Release();
    }

    private void Release()
    {
        if (OwningPool != null)
            OwningPool.Return(gameObject);
        else
            Destroy(gameObject);
    }

    private IEnumerator FlashOutline()
    {
        Vector3 orig = transform.localScale;
        transform.localScale = orig * 1.08f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = orig;
    }

    // ── Lifetime ──────────────────────────────────────────────
    private void Update()
    {
        if (data == null) return;
        if (Time.time - _spawnTime > data.lifetime)
        {
            EventBus.Publish(new OnAnimalMissed { species = data.species });
            Release();
        }
    }
}

// ── AnimalSpecies  (kept in same file for zero change to callers)
public enum AnimalSpecies
{
    None,
    Chicken,
    Dog,
    Cow,
    Cat,
    Monkey,
    Balloon
}
