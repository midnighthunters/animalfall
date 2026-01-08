using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private const string kCoins = "coins";

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public int GetCoins()
    {
        return PlayerPrefs.GetInt(kCoins, 0);
    }

    public void AddCoins(int amount)
    {
        int v = GetCoins() + amount;
        PlayerPrefs.SetInt(kCoins, v);
    }

    public void SpendCoins(int amount)
    {
        int v = GetCoins() - amount;
        PlayerPrefs.SetInt(kCoins, Mathf.Max(0, v));
    }
}
