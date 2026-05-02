// ============================================================
//  ComboManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • Publishes OnComboUpdated to EventBus
//    • Visual feedback added via UIManager.ShowMessage
// ============================================================

using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [SerializeField] private float comboWindow = 2f;

    public int   Consecutive      { get; private set; }
    private float _lastCorrectTime = -999f;

    public void OnCorrect()
    {
        float now = Time.time;
        Consecutive = (now - _lastCorrectTime <= comboWindow)
            ? Consecutive + 1
            : 1;
        _lastCorrectTime = now;

        float multiplier = 1f + 0.1f * (Consecutive - 1);
        ScoreManager.Instance?.SetComboMultiplier(multiplier);

        EventBus.Publish(new OnComboUpdated
        {
            streak     = Consecutive,
            multiplier = multiplier
        });

        if (Consecutive >= 3)
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.Combo);
    }

    public void ResetCombo()
    {
        Consecutive = 0;
        ScoreManager.Instance?.SetComboMultiplier(1f);
        EventBus.Publish(new OnComboUpdated { streak = 0, multiplier = 1f });
    }
}
