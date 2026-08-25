using UnityEngine;
using System.Text;
using AnimalFall.Core.Animals;

public static class SizeDiag
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        var animals = Object.FindObjectsByType<Animal>(FindObjectsSortMode.None);
        sb.AppendLine($"[SizeDiag] {animals.Length} animals");
        foreach (var a in animals)
        {
            if (a.Data == null) continue;
            var sr = a.GetComponent<SpriteRenderer>();
            float worldH = 0f, worldW = 0f;
            if (sr != null && sr.sprite != null)
            {
                var b = sr.sprite.bounds.size;
                worldW = b.x * a.transform.localScale.x;
                worldH = b.y * a.transform.localScale.y;
            }
            sb.AppendLine($"  {a.Data.species}: scale={a.transform.localScale.x:F3} rendered={worldW:F2}x{worldH:F2} world units");
        }
        Debug.Log(sb.ToString());
    }
}
