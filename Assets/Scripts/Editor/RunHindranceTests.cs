using UnityEngine;
using AnimalFall.Debugging;

public static class RunHindranceTests
{
    public static void Execute()
    {
        var existing = Object.FindFirstObjectByType<HindranceTestHarness>();
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
            Debug.Log("[RunHindranceTests] Destroyed stale host");
        }
        var host = new GameObject("HindranceTestHost");
        var runner = host.AddComponent<HindranceTestHarness>();
        runner.Run();
        Debug.Log("[RunHindranceTests] Harness started fresh");
    }
}
