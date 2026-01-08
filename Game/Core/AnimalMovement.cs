using UnityEngine;

public enum MovementPattern { Static, Drift, ZigZag, Teleport, Bounce }

[RequireComponent(typeof(Rigidbody2D))]
public class AnimalMovement : MonoBehaviour
{
    public MovementPattern pattern = MovementPattern.Drift;
    public float speed = 1f;
    public float zigzagAmplitude = 0.5f;
    public float zigzagFrequency = 2f;

    // Optional: set a safe margin so animals don't sit exactly on the edge
    [Header("Bounds")]
    public float screenMargin = 0.05f; // fraction of screen width/height to inset
    public bool destroyWhenBelowScreen = true; // destroy when completely below bottom

    private Vector3 startPos;
    private float spawnTime;
    private Rigidbody2D rb;
    private float moveDirX;

    // calculated bounds
    private Camera cam;
    private float minX, maxX, minY, maxY;
    private float halfWidth, halfHeight;
    private float zDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        startPos = transform.position;
        spawnTime = Time.time;
        moveDirX = Random.Range(-0.6f, 0.6f);

        // get camera & compute bounds
        cam = Camera.main;
        // compute z distance from camera so ViewportToWorldPoint works correctly
        zDistance = Mathf.Abs(transform.position.z - (cam != null ? cam.transform.position.z : 0f));

        // sprite extents (if there's a SpriteRenderer)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            halfWidth = sr.bounds.extents.x;
            halfHeight = sr.bounds.extents.y;
        }
        else
        {
            halfWidth = 0.5f;
            halfHeight = 0.5f;
        }

        RecalcBounds();
    }

    // Call this if camera changes or screen size changes
    private void RecalcBounds()
    {
        if (cam == null) return;

        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f + screenMargin, 0f + screenMargin, zDistance));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f - screenMargin, 1f - screenMargin, zDistance));
        minX = bl.x;
        minY = bl.y;
        maxX = tr.x;
        maxY = tr.y;
    }

    public void ConfigureRandomSpeed(float min, float max)
    {
        speed = Random.Range(min, max);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        switch (pattern)
        {
            case MovementPattern.Static:
                transform.Translate(Vector3.down * speed * dt);
                break;
            case MovementPattern.Drift:
                transform.Translate((Vector3.down + Vector3.right * moveDirX * 0.1f) * speed * dt);
                break;
            case MovementPattern.ZigZag:
                {
                    float x = Mathf.Sin((Time.time - spawnTime) * zigzagFrequency) * zigzagAmplitude;
                    // move down and adjust x around the original start X
                    transform.position += Vector3.down * speed * dt;
                    transform.position = new Vector3(startPos.x + x, transform.position.y, transform.position.z);
                    break;
                }
            case MovementPattern.Teleport:
                transform.Translate(Vector3.down * speed * dt);
                if (Random.value < 0.002f)
                {
                    float down = Random.Range(0.5f, 1.2f);
                    transform.position += Vector3.down * down;
                }
                break;
            case MovementPattern.Bounce:
                transform.Translate(Vector3.down * speed * dt);
                // bounce may be physics-driven, but we'll still clamp below
                break;
        }

        // Recalculate bounds if camera or screen resized (cheap guard)
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            // recalc when needed (could optimize); safe to call each frame for small projects
            RecalcBounds();
        }

        // Clamp horizontally to stay visible (taking sprite size into account)
        float clampedX = Mathf.Clamp(transform.position.x, minX + halfWidth, maxX - halfWidth);
        float clampedY = transform.position.y; // we'll check bottom separately

        // If teleport or zigzag set x away from startPos, clamping keeps it on screen
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);

        // Optionally destroy when fully below the screen (with sprite height)
        if (destroyWhenBelowScreen)
        {
            if (transform.position.y < (minY - halfHeight - 0.1f))
            {
                Destroy(gameObject);
            }
        }
        else
        {
            // keep Y not too far below (optional)
            transform.position = new Vector3(transform.position.x, Mathf.Max(transform.position.y, minY - 5f), transform.position.z);
        }
    }
}
