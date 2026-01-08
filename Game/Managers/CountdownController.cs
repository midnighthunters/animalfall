using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownController : MonoBehaviour
{
    public TMP_Text countdownText;
    public float stepDuration = 0.5f;

    public IEnumerator PlayCountdown(System.Action onComplete)
    {
        countdownText.gameObject.SetActive(true);
        Time.timeScale = 0f;

        yield return Show("3");
        yield return Show("2");
        yield return Show("1");
        yield return Show("GO!");

        countdownText.gameObject.SetActive(false);
        Time.timeScale = 1f;

        onComplete?.Invoke();
    }

    IEnumerator Show(string value)
    {
        countdownText.text = value;
        countdownText.transform.localScale = Vector3.zero;

        float t = 0;
        while (t < stepDuration)
        {
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Sin((t / stepDuration) * Mathf.PI);
            countdownText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }
}
