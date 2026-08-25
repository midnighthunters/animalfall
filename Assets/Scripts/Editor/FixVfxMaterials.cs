using UnityEngine;
using UnityEditor;

public static class FixVfxMaterials
{
    public static void Execute()
    {
        // 1) Ensure a clean URP-safe particle material exists.
        //    Sprites/Default is unlit, URP-compatible, and respects particle vertex colors.
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) { Debug.LogError("[FixVfx] Sprites/Default shader missing"); return; }

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX_Sprite.mat");
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, "Assets/Materials/VFX_Sprite.mat");
        }
        else
        {
            mat.shader = shader;
        }
        AssetDatabase.SaveAssets();

        string[] prefabs = {
            "Assets/Resources/VFX/Battle_Effect_White.prefab",
            "Assets/Resources/VFX/Explosion_1_Bam.prefab",
            "Assets/Resources/VFX/Explosion_1_Zap.prefab"
        };

        int fixedRenderers = 0;
        foreach (var p in prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(p);
            var renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers)
            {
                bool needsFix = r.sharedMaterial == null || !r.sharedMaterial.shader.isSupported;
                if (needsFix)
                {
                    r.sharedMaterial = mat;
                    fixedRenderers++;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, p);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();
        Debug.Log("[FixVfx] Assigned URP-safe material to " + fixedRenderers + " particle renderer(s) across 3 VFX prefabs.");
    }
}
