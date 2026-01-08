using System.Collections;
using UnityEngine;

public enum TapResult { Correct, Wrong, BombExploded, ShieldBroken, Golden }

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AnimalMovement))]
public class Animal : MonoBehaviour
{
    public AnimalData data;
    private LevelData level;
    private SpriteRenderer sr;
    private AnimalMovement movement;
    private float spawnTime;
    public int currentShield;
    private float lastTapTime = -999f;
    private int tapCount = 0;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        movement = GetComponent<AnimalMovement>();
    }

    public void Setup(AnimalData d, LevelData lv)
    {
        data = d;
        level = lv;
        sr.sprite = d.sprite;
        spawnTime = Time.time;
        currentShield = d.shieldHP;
        movement.ConfigureRandomSpeed(d.speedMin, d.speedMax);

        // visually indicate special types (outline, halo) - set material/shader if desired
        if (d.type == AnimalType.Decoy)
            sr.color = Color.Lerp(Color.white, Color.grey, 0.2f);
        if (d.type == AnimalType.Bomb)
            sr.color = Color.white; // bomb sprite should show explodable look
    }

    public TapResult HandleTap()
    {
        // tap timing/double tap logic for shielded animals
        tapCount++;
        float now = Time.time;
        bool isDoubleTap = (now - lastTapTime) < 0.4f;
        lastTapTime = now;

        // Bomb
        if (data.type == AnimalType.Bomb)
        {
            Explode();
            return TapResult.BombExploded;
        }

        // Shielded double-tap
        if (data.requiresDoubleTap || data.type == AnimalType.Shielded)
        {
            currentShield--;
            if (currentShield > 0)
            {
                // show break animation
                StartCoroutine(FlashOutline());
                return TapResult.ShieldBroken; // not counted as correct yet
            }
        }

        // Golden
        if (data.type == AnimalType.Golden)
        {
            OnCollected();
            return TapResult.Golden;
        }

        // Normal or special
        OnCollected();
        return TapResult.Correct;
    }

    IEnumerator FlashOutline()
    {
        // Placeholder: make a flash to indicate shield lost
        Vector3 origScale = transform.localScale;
        transform.localScale = origScale * 1.05f;
        yield return new WaitForSeconds(0.12f);
        transform.localScale = origScale;
    }

    void Explode()
    {
        // show fx then notify GameManager
        GameManager.Instance.OnWrongTap(true);
        Destroy(gameObject);
    }

    void OnCollected()
    {
        // play fx
        int pointValue = data.pointValue;
        bool isTarget = data.isTargetSpecies;

        if (isTarget)
        {
            GameManager.Instance.OnCorrectTap(1, pointValue);
        }
        else
        {
            GameManager.Instance.OnWrongTap(false);
        }

        // Decrease the goal if this animal maps to a species in Goal
        if (data.species != AnimalSpecies.None && GoalPanel.Instance != null && GoalPanel.Instance.IsSpeciesRequired(data.species))
        {
            GoalPanel.Instance.DecreaseGoal(data.species);
        }

        // visual + audio
        if (GameManager.Instance != null && GameManager.Instance.audioManager != null)
            GameManager.Instance.audioManager.PlaySFX(AudioManager.SfxType.Collect);

        Destroy(gameObject);
    }

    private void Update()
    {
        // lifespan
        if (Time.time - spawnTime > data.lifetime)
            Destroy(gameObject);
    }
}



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
