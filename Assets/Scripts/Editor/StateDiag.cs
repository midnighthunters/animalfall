using UnityEngine;
using System.Text;
using AnimalFall.Core;
using AnimalFall.Managers;

public static class StateDiag
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[StateDiag] ObjectPooler.Instance={(ObjectPooler.Instance!=null?"OK":"NULL")}");
        sb.AppendLine($"[StateDiag] Spawner.Instance={(AnimalFall.Core.Animals.Spawner.Instance!=null?"OK":"NULL")}");
        var gm = Object.FindFirstObjectByType<GameManager>();
        sb.AppendLine($"[StateDiag] GameManager={(gm!=null?"OK":"NULL")}");
        var pooler = Object.FindFirstObjectByType<ObjectPooler>();
        sb.AppendLine($"[StateDiag] ObjectPooler component in scene={(pooler!=null?pooler.gameObject.name:"NONE")}");
        if (pooler != null) sb.AppendLine($"[StateDiag] Pooler activeInHierarchy={pooler.gameObject.activeInHierarchy} enabled={pooler.enabled}");
        Debug.Log(sb.ToString());
    }
}
