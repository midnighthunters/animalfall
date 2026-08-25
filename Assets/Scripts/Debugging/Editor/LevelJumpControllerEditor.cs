#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnimalFall.Data;

namespace AnimalFall.Debugging.Editor
{
    [CustomEditor(typeof(LevelJumpController))]
    public sealed class LevelJumpControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();
            LevelJumpController controller = (LevelJumpController)target;
            LevelData selected = controller.SelectedLevel;

            EditorGUILayout.Space(8);
            if (controller.Database == null)
                EditorGUILayout.HelpBox("Assign Assets/Levels/LevelDatabase.asset.", MessageType.Error);
            else if (selected == null)
                EditorGUILayout.HelpBox($"Level {controller.LevelNumber} is an unconfigured database slot.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox($"Target: {selected.name}\nMode: {selected.Mode}", MessageType.Info);

            using (new EditorGUI.DisabledScope(selected == null))
            {
                if (Application.isPlaying)
                {
                    if (GUILayout.Button($"Jump To Level {controller.LevelNumber}", GUILayout.Height(30)))
                        controller.JumpToSelectedLevel();
                }
                else if (GUILayout.Button("Enable Auto-Jump And Enter Play Mode", GUILayout.Height(30)))
                {
                    SerializedProperty autoJump = serializedObject.FindProperty("_jumpOnGameSceneStart");
                    autoJump.boolValue = true;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(controller);
                    EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                    EditorSceneManager.SaveOpenScenes();
                    EditorApplication.isPlaying = true;
                }
            }
        }
    }
}
#endif
