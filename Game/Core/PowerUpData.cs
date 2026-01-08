using UnityEngine;

public enum PowerUpType { SlowTime, Magnet, MultiTap, AutoTap, ShieldBreaker, BombClear, ScoreMultiplier, ExtraTime, FreezeHighlight }

[CreateAssetMenu(menuName = "Game/PowerUpData")]
public class PowerUpData : ScriptableObject
{
    public PowerUpType type;
    public string displayName;
    public Sprite icon;
    public int costCoins = 100;
    public float duration = 5f;
    public float value = 1f; // generic (e.g., slow factor or multiplier)
    public int usesPerLevel = 1;
    public bool isPremium = false;
}
