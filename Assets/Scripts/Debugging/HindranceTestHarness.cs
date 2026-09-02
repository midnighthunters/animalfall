// HindranceTestHarness — spawns every hindrance type in play mode and verifies lifecycle.
using System.Collections;
using System.Text;
using UnityEngine;
using AnimalFall.Core;
using AnimalFall.Core.Hindrances;
using AnimalFall.Effects;
using AnimalFall.Managers;

namespace AnimalFall.Debugging
{
    public class HindranceTestHarness : MonoBehaviour
    {
        private StringBuilder _sb;
        private int _pass, _fail;

        public void Run() => StartCoroutine(RunAll());

        public IEnumerator RunAll()
        {
            _sb = new StringBuilder();
            _pass = 0; _fail = 0;

            var registry = Resources.Load<HindranceRegistry>("Hindrances/HindranceRegistry");
            if (registry == null) { Debug.LogError("[Harness] No registry"); yield break; }

            var hm = Object.FindFirstObjectByType<HindranceManager>();
            var container = GameObject.Find("[Core]/AnimalContainer");
            Transform parent = container != null ? container.transform : null;

            _sb.AppendLine("=== HINDRANCE TEST HARNESS ===");

            for (int id = 1; id <= (int)HindranceType.BatSwarm; id++)
            {
                var type = (HindranceType)id;
                string result = TestOne(type, registry, hm, parent);
                _sb.AppendLine(result);
                yield return new WaitForSeconds(0.35f);
            }

            _sb.AppendLine($"=== DONE: {_pass} pass / {_fail} fail ===");
            Debug.Log(_sb.ToString());
        }

        private string TestOne(HindranceType type, HindranceRegistry registry,
                               HindranceManager hm, Transform parent)
        {
            int before = Object.FindObjectsByType<HindranceBase>(FindObjectsSortMode.None).Length;
            HindranceData data = registry.GetData(type);
            if (data == null || data.prefab == null)
            {
                _fail++;
                return $"[{type}] FAIL — no data/prefab";
            }

            IHindrance h = null;
            try
            {
                h = HindranceFactory.CreateAtRandomScreenTop(data, parent);
                if (h == null) { _fail++; return $"[{type}] FAIL — factory returned null"; }
                var ctx = new HindranceContext
                {
                    GameManager = Object.FindFirstObjectByType<GameManager>(),
                    HindranceManager = hm,
                    EnvironmentEffects = Object.FindFirstObjectByType<EnvironmentEffects>(),
                    ScreenEffects = Object.FindFirstObjectByType<ScreenEffects>(),
                    AudioManager = Object.FindFirstObjectByType<AudioManager>(),
                    LivesManager = Object.FindFirstObjectByType<LivesManager>(),
                    InputManager = Object.FindFirstObjectByType<InputManager>()
                };
                h.Activate(ctx);
            }
            catch (System.Exception ex)
            {
                _fail++;
                if (h != null) { var hb = h as HindranceBase; if (hb != null) ObjectPooler.Instance?.ReturnToPool(hb.gameObject); }
                return $"[{type}] EXCEPTION — {ex.GetType().Name}: {ex.Message}\n    at {ex.StackTrace?.Split('\n')[0]} | {ex.StackTrace?.Split('\n')[1]}";
            }

            // Verify it's alive and visible
            var baseComp = h as HindranceBase;
            bool activated = baseComp != null && baseComp.gameObject.activeInHierarchy;
            Vector3 pos = baseComp != null ? baseComp.transform.position : Vector3.zero;
            float scale = baseComp != null ? baseComp.transform.localScale.x : 0f;

            // Deactivate cleanly
            try { h.Deactivate(); }
            catch (System.Exception ex)
            {
                _fail++;
                return $"[{type}] DEACTIVATE EXCEPTION — {ex.GetType().Name}: {ex.Message}";
            }

            int after = Object.FindObjectsByType<HindranceBase>(FindObjectsSortMode.None).Length;

            if (!activated) { _fail++; return $"[{type}] WARN — deactivated itself on activate (pos={pos})"; }
            if (after > before) { _fail++; return $"[{type}] LEAK — object still active after Deactivate ({after}>{before})"; }

            _pass++;
            return $"[{type}] PASS — spawned at ({pos.x:F1},{pos.y:F1}) scale={scale:F2}";
        }
    }
}
