using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text timerText;
    public TMP_Text targetText;
    public TMP_Text scoreText;
    public TMP_Text comboText;

    [Header("UI")]
    public Image progressBar;
    public GameObject levelCompletePanel;
    public GameObject levelFailPanel;


    public void UpdateTimer(float seconds)
    {
        timerText.text = Mathf.CeilToInt(seconds).ToString("00") + "s";
        // pulsing effect near end can be implemented here
    }

    public void UpdateTargetText(int current, int target)
    {
        targetText.text = $"{current} / {target}";
    }

    public void UpdateScoreText(int score)
    {
        scoreText.text = score.ToString();
    }

    public void SetProgress(float t)
    {
        progressBar.fillAmount = t;
    }

    public void UpdateComboUI(float multiplier)
    {
        comboText.text = $"x{multiplier:0.0}";
    }

    public void ShowLevelComplete()
    {
        levelCompletePanel.SetActive(true);
    }

    public void ShowLevelFailed()
    {
        levelFailPanel.SetActive(true);
    }

    public void ShowFloating(string text, Vector3 screenPos)
    {
        // instantiate floating text prefab at screenPos, set text
    }

    public void ShowMessage(string msg)
    {
        // temporary banner / toast
    }
}
