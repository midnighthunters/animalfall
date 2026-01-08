using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int levelNumber;
    public int rewardCoins;

    [Header("Timer / Goal")]
    public float timeLimit; // seconds
    public Goal goal;

    [Header("Spawner")]
    public float spawnInterval = 0.6f;
    public float spawnVariance = 0.15f;
    public int maxOnScreen = 8;

    [Header("Mechanics toggles")]
    public bool enableBombs = false;
    public bool enableShielded = false;
    public bool enableDecoys = false;

    [Header("Penalties")]
    public float wrongTapTimePenalty = 1.0f;
    public int wrongTapScorePenalty = 30;
    public float bombTimePenalty = 3.0f;
    public int bombScorePenalty = 50;

    // Optionally add a targetTaps if you want a separate "collect X" different than sum(goal)
    // public int targetTaps = 0;
}
