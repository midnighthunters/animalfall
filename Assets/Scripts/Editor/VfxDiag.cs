using UnityEngine;
using UnityEditor;
using System.Text;

public static class VfxDiag
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        string[] prefabs = {
            "Assets/Resources/VFX/Battle_Effect_White.prefab",
            "Assets/Resources/VFX/Explosion_1_Bam.prefab",
            "Assets/Resources/VFX/Explosion_1_Zap.prefab"
        };
        foreach (var p in prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(p);
            sb.AppendLine("== " + p + " ==");
            var renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers)
            {
                var mat = r.sharedMaterial;
                string matName = mat != null ? mat.name : "NULL";
                string shaderName = (mat != null && mat.shader != null) ? mat.shader.name : "NONE";
                bool supported = mat != null && mat.shader != null && mat.shader.isSupported;
                sb.AppendLine("  " + r.name + ": material=" + matName + " shader=" + shaderName + " supported=" + supported);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        sb.AppendLine("Sprites/Default found: " + (Shader.Find("Sprites/Default") != null));
        sb.AppendLine("URP Particles Unlit found: " + (Shader.Find("Universal Render Pipeline/Particles/Unlit") != null));
        sb.AppendLine("URP Particles Lit found: " + (Shader.Find("Universal Render Pipeline/Particles/Lit") != null));
        sb.AppendLine("Legacy Particles Additive found: " + (Shader.Find("Legacy Shaders/Particles/Additive") != null));
        Debug.Log(sb.ToString());
    }
}
