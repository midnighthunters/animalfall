#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Debugging;

namespace AnimalFall.EditorTools
{
    /// <summary>
    /// Editor window for selecting and starting configured Animal Fall levels.
    /// Menu: Tools -> Jump To Level
    /// </summary>
    public sealed class JumpToLevelWindow : EditorWindow
    {
        private const int MinLevel = 1;
        private const int DefaultLevel = 13;
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string DatabasePath = "Assets/Levels/LevelDatabase.asset";
        private const string SelectedLevelKey = "AnimalFall.JumpToLevelWindow.SelectedLevel";

        private LevelDatabase _database;
        private int _displayLevel = DefaultLevel;
        private string _status;
        private Vector2 _scroll;

        [MenuItem("Tools/Jump To Level")]
        public static void Open()
        {
            JumpToLevelWindow window = GetWindow<JumpToLevelWindow>("Jump To Level");
            window.minSize = new Vector2(360f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDatabase();
            _displayLevel = Mathf.Clamp(
                EditorPrefs.GetInt(SelectedLevelKey, DefaultLevel),
                MinLevel,
                MaxLevel);
            _status = $"Selected Level {_displayLevel}.";
        }

        private void OnGUI()
        {
            LoadDatabase();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Animal Fall Level Jumper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose a one-based level, apply it to the GameScene debug launcher, then press Play.\n" +
                "While playing, use Start Selected Level to call JumpToLevel immediately.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _displayLevel = EditorGUILayout.IntSlider("Level", _displayLevel, MinLevel, MaxLevel);
            _displayLevel = EditorGUILayout.IntField("Exact level", _displayLevel);
            _displayLevel = Mathf.Clamp(_displayLevel, MinLevel, MaxLevel);
            if (EditorGUI.EndChangeCheck())
                _status = string.Empty;

            LevelData selected = _database != null ? _database.GetLevelOrNull(_displayLevel - 1) : null;
            EditorGUILayout.LabelField("Configured", selected != null ? "Yes" : "No");
            if (selected != null)
            {
                EditorGUILayout.LabelField("Name", selected.name);
                EditorGUILayout.LabelField("Mode", selected.IsConfiguredMegaShooter ? "Mega Shooter" : "Normal");
            }
            else
            {
                EditorGUILayout.HelpBox("This database slot is empty.", MessageType.Warning);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Level", GUILayout.Height(28f)))
                ApplySelection();
            if (GUILayout.Button("Random Configured", GUILayout.Height(28f)))
            {
                PickRandomConfiguredLevel();
                ApplySelection();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Level 1")) SetAndApply(1);
            if (GUILayout.Button("Level 13")) SetAndApply(13);
            if (GUILayout.Button("Level 25")) SetAndApply(25);
            if (GUILayout.Button("Level 50")) SetAndApply(50);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button(
                        Application.isPlaying ? "Start Selected Level (Play Mode)" : "Start Selected Level (enter Play Mode first)",
                        GUILayout.Height(30f)))
                {
                    ApplySelection();
                    StartSelectedLevel();
                }
            }

            if (GUILayout.Button("Open GameScene"))
                OpenGameScene();

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(_status, MessageType.None);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Quick tips", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Choose a level and click Set Level.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. Open GameScene and press Play.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. While playing, click Start Selected Level.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndScrollView();
        }

        private int MaxLevel => _database != null ? Mathf.Max(MinLevel, _database.TotalLevels) : 100;

        private void LoadDatabase()
        {
            if (_database == null)
                _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        }

        private void PickRandomConfiguredLevel()
        {
            if (_database == null)
                return;

            int[] configured = new int[_database.TotalLevels];
            int count = 0;
            for (int index = 0; index < _database.TotalLevels; index++)
            {
                if (_database.GetLevelOrNull(index) != null)
                    configured[count++] = index + 1;
            }

            if (count > 0)
                _displayLevel = configured[Random.Range(0, count)];
        }

        private void SetAndApply(int level)
        {
            _displayLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
            ApplySelection();
        }

private void ApplySelection()
        {
            _displayLevel = Mathf.Clamp(_displayLevel, MinLevel, MaxLevel);
            EditorPrefs.SetInt(SelectedLevelKey, _displayLevel);
            PlayerPrefs.SetInt("AnimalFall.DebugLevel", _displayLevel - 1);
            PlayerPrefs.Save();

            JumpToLevel launcher = FindLauncher();
            if (launcher == null)
            {
                _status = $"Saved Level {_displayLevel}. Open GameScene to apply it to [Debug] Level Jump.";
                Repaint();
                return;
            }

            SerializedObject serialized = new SerializedObject(launcher);
            SerializedProperty levelProperty = serialized.FindProperty("_levelNumber");
            if (levelProperty != null)
                levelProperty.intValue = _displayLevel;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(launcher.gameObject.scene);
            _status = $"Set [Debug] Level Jump and MainScene debug level to Level {_displayLevel}.";
            Repaint();
        }

        private void StartSelectedLevel()
        {
            if (!Application.isPlaying)
            {
                _status = "Enter Play Mode first, then start the selected level.";
                return;
            }

            JumpToLevel launcher = FindLauncher();
            if (launcher != null)
            {
                launcher.StartLevel(_displayLevel);
                _status = $"Requested Level {_displayLevel}.";
                return;
            }

            LevelJumpController controller = Object.FindFirstObjectByType<LevelJumpController>();
            if (controller != null)
            {
                controller.JumpToLevel(_displayLevel);
                _status = $"Requested Level {_displayLevel}.";
                return;
            }

            _status = "No level-jump launcher is present in the active scene.";
        }

        private void OpenGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath);
            ApplySelection();
        }

        private static JumpToLevel FindLauncher()
        {
            GameObject debugObject = GameObject.Find("[Debug] Level Jump");
            return debugObject != null ? debugObject.GetComponent<JumpToLevel>() : null;
        }
    }
}
#endif