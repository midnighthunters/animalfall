#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnimalFall.Automation;

namespace AnimalFall.EditorTools
{
    public static class PlayAllLevelsMenuItem
    {
        [MenuItem("Tools/Animal Fall/Play All 100 Levels")]
        public static void PlayAllLevels()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[PlayAllLevels] In Play Mode. Spawning runner...");
                SpawnAndStartRunner();
                return;
            }

            // Save and open MainScene
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/MainScene.unity")
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            }

            PlayerPrefs.DeleteKey("AnimalFall.DebugLevel");
            PlayerPrefs.Save();
            EditorPrefs.SetBool("AnimalFall.AutoStartPlaythrough", true);
            EditorApplication.isPlaying = true;
        }

        public static void SpawnAndStartRunner()
        {
            if (LevelPlaythroughRunner.Instance == null)
            {
                var go = new GameObject("LevelPlaythroughRunner");
                var runner = go.AddComponent<LevelPlaythroughRunner>();
                runner.StartPlaythrough();
            }
        }
    }
}
#endif
