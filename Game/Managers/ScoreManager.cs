using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    private int score = 0;
    private float comboMultiplier = 1f;
    public UIManager ui;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public void AddPoints(int pts)
    {
        int applied = Mathf.RoundToInt(pts * comboMultiplier);
        score += applied;
        ui.UpdateScoreText(score);
    }

    public void SetComboMultiplier(float m)
    {
        comboMultiplier = m;
        ui.UpdateComboUI(comboMultiplier);
    }

    public void ResetScore()
    {
        score = 0;
        comboMultiplier = 1f;
        ui.UpdateScoreText(score);
    }

    public int GetScore() => score;
}
