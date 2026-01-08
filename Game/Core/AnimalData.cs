using UnityEngine;

public enum AnimalType { Normal, Decoy, Bomb, Shielded, Golden, Special }

[CreateAssetMenu(menuName = "Game/AnimalData")]
public class AnimalData : ScriptableObject
{
    public string displayName;
    public Sprite sprite;
    public AnimalType type = AnimalType.Normal;
    public AnimalSpecies species = AnimalSpecies.None;   // <-- NEW: species to map to Goal
    public int pointValue = 50;
    public bool isTargetSpecies = true; // whether this animal counts for current objective
    public bool requiresDoubleTap = false; // shield style
    public int shieldHP = 1; // taps to break
    public float speedMin = 1f;
    public float speedMax = 2f;
    public float lifetime = 12f;
    public GameObject prefab; // optional link
    public Color outlineColor = Color.white; // UI halo color
}
