// ============================================================
//  BootstrapLoader.cs  –  Animal Fall
//  Attaches to a lightweight "Bootstrap" scene that is the
//  very first scene Unity loads (Build index 0).
//  Responsibility: spin up all DDOL singletons in the right
//  order before the SplashScreen or any gameplay scene loads.
//
//  Scene build order:
//    0 – Bootstrap   (tiny, no art)
//    1 – SplashScene (logo + loading)
//    2 – MainScene
//    3 – GameScene
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [Header("Manager Prefabs")]
    [Tooltip("Drag each manager prefab here in the correct init order.")]
    [SerializeField] private GameObject[] managerPrefabs;

    [Header("Settings")]
    [SerializeField] private string splashSceneName = "SplashScene";

    private IEnumerator Start()
    {
        // Instantiate singletons in order
        foreach (var prefab in managerPrefabs)
        {
            if (prefab == null) continue;
            var go = Instantiate(prefab);
            go.name = prefab.name;          // clean hierarchy name
            DontDestroyOnLoad(go);
        }

        // Brief frame delay to let Awake() calls complete on all managers
        yield return null;
        yield return null;

        // Load Splash (or directly to Main if you skip splash in Editor)
#if UNITY_EDITOR
        // In the editor, skip straight to Main if SKIP_SPLASH is defined
        #if SKIP_SPLASH
        SceneManager.LoadScene("MainScene");
        #else
        SceneManager.LoadScene(splashSceneName);
        #endif
#else
        SceneManager.LoadScene(splashSceneName);
#endif
    }
}
