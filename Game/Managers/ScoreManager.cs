// ============================================================
//  ScoreManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • Emits EventBus.Publish(OnScoreChanged) instead of
//      directly touching UIManager
//    • UIManager subscribes via EventBus (decoupled)
//    • ui reference kept as optional fallback for legacy wiring
// ============================================================

using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int   _score           = 0;
    private float _comboMultiplier = 1f;

    // Legacy shim: if old code hard-references ui, it still works
    [SerializeField] private UIManager ui;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddPoints(int pts)
    {
        int applied = Mathf.RoundToInt(pts * _comboMultiplier);
        _score += applied;
        if (_score < 0) _score = 0;

        EventBus.Publish(new OnScoreChanged { newScore = _score });
        ui?.UpdateScoreText(_score);   // fallback direct call
    }

    public void SetComboMultiplier(float m)
    {
        _comboMultiplier = m;
        EventBus.Publish(new OnComboUpdated { streak = 0, multiplier = m });
        ui?.UpdateComboUI(m);
    }

    public void ResetScore()
    {
        _score           = 0;
        _comboMultiplier = 1f;
        EventBus.Publish(new OnScoreChanged { newScore = _score });
        ui?.UpdateScoreText(_score);
    }

    public int   GetScore()         => _score;
    public float GetMultiplier()    => _comboMultiplier;
}
