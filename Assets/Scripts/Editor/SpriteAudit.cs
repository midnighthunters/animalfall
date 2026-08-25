using UnityEngine;
using UnityEditor;
using System.Text;

public static class SpriteAudit
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        string[] animals = { "CHICKEN", "DOG2", "ELEPHANT", "MONKEY2", "PANDA2", "PENGUIN", "PIG2" };
        sb.AppendLine("== ANIMAL SPRITES ==");
        foreach (var name in animals)
        {
            var path = $"Assets/Resources/icons/animals/{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (sprite != null)
            {
                var b = sprite.bounds.size;
                sb.AppendLine($"{name}: rect={sprite.rect.width}x{sprite.rect.height}px PPU={sprite.pixelsPerUnit} worldBounds={b.x:F2}x{b.y:F2} pivot={sprite.pivot}");
            }
            else sb.AppendLine($"{name}: NO SPRITE (importer type={(ti!=null?ti.textureType.ToString():"?")})");
        }
        Debug.Log(sb.ToString());
    }
}
