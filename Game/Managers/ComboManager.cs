using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public int consecutive = 0;
    public float comboWindow = 2f;
    private float lastCorrectTime = -999f;

    public void OnCorrect()
    {
        float now = Time.time;
        if (now - lastCorrectTime <= comboWindow)
        {
            consecutive++;
        }
        else
        {
            consecutive = 1;
        }
        lastCorrectTime = now;
        ApplyComboBonus();
    }

    public void ResetCombo()
    {
        consecutive = 0;
    }

    void ApplyComboBonus()
    {
        // combo multiplier = 1 + 0.1*(consecutive-1)
        float multiplier = 1f + 0.1f * (consecutive - 1);
        // delegate to ScoreManager
        ScoreManager.Instance.SetComboMultiplier(multiplier);
    }
}
