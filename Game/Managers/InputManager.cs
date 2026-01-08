using UnityEngine;

[RequireComponent(typeof(Camera))]
public class InputManager : MonoBehaviour
{
    public float longPressThreshold = 0.4f; // optional if implementing long press

    void Update()
    {
        if (!GameManager.Instance.isRunning) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            // Set Z to the distance from camera to the game plane (e.g., 10 or -Camera.main.transform.position.z)
            mousePos.z = -Camera.main.transform.position.z;

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(mousePos);

            // Debug: Draw a line in the Scene view to see exactly where you are clicking
            Debug.DrawLine(Camera.main.transform.position, worldPoint, Color.red, 2.0f);

            // Use OverlapPoint (More accurate for clicks than Raycast)
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPoint);
            if (hitCollider != null)
            {
                Debug.Log("Hit: " + hitCollider.gameObject.name);
                var animal = hitCollider.GetComponent<Animal>();
                if (animal != null)
                {
                    var result = animal.HandleTap();
                    Debug.Log("Result" + result);
                    HandleTapResult(result, animal);
                }
                // else
                // {
                //     // tapped background => wrong tap
                //     GameManager.Instance.OnWrongTap(false);
                // }
            }
            // else
            // {
            //     // blank space
            //     GameManager.Instance.OnWrongTap(false);
            // }
        }
    }

    void HandleTapResult(TapResult result, Animal animal)
    {
        switch (result)
        {
            case TapResult.Correct:
                GameManager.Instance.audioManager.PlaySFX(AudioManager.SfxType.Collect);
                break;
            case TapResult.Wrong:
                GameManager.Instance.OnWrongTap(false);
                break;
            case TapResult.BombExploded:
                // handled in animal
                break;
            case TapResult.ShieldBroken:
                // maybe show small feedback. Shield broken doesn't count as collection
                GameManager.Instance.audioManager.PlaySFX(AudioManager.SfxType.ShieldBreak);
                break;
            case TapResult.Golden:
                // golden: bigger reward & optional chained taps
                GameManager.Instance.scoreManager.AddPoints(animal.data.pointValue * 2);
                GameManager.Instance.AddTime(1f); // example
                break;
        }
    }
}
