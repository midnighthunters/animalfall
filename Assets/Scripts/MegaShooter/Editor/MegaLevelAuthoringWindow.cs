#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimalFall.MegaShooter.Editor
{
    public sealed class MegaLevelAuthoringWindow : EditorWindow
    {
        private MegaLevelData _selectedLevel;

        [MenuItem("Tools/Animal Fall/Mega Shooter/Authoring Window")]
        public static void Open() => GetWindow<MegaLevelAuthoringWindow>("Mega Shooter Authoring");

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Animal Fall — Mega Shooter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Idempotent authoring tools. Existing art, prefabs, data assets, and scene wiring are preserved; only missing content is created.", MessageType.Info);
            _selectedLevel = (MegaLevelData)EditorGUILayout.ObjectField("Selected Mega Level", _selectedLevel, typeof(MegaLevelData), false);
            EditorGUILayout.Space(6);

            if (GUILayout.Button("Generate Complete Feature", GUILayout.Height(30))) MegaShooterGenerator.GenerateCompleteFeature();
            if (GUILayout.Button("Generate Missing Placeholder Art")) MegaShooterGenerator.GenerateMissingPlaceholderArt();
            if (GUILayout.Button("Generate Missing Prefabs")) MegaShooterGenerator.GenerateMissingPrefabs();
            if (GUILayout.Button("Generate / Update Mega Levels")) MegaShooterGenerator.GenerateOrUpdateMegaLevelsOnly();
            if (GUILayout.Button("Generate Dedicated Scene")) MegaShooterGenerator.GenerateMegaShooterScene();
            if (GUILayout.Button("Validate All Mega Content", GUILayout.Height(26))) MegaShooterValidator.ValidateAll(true);

            using (new EditorGUI.DisabledScope(_selectedLevel == null))
            {
                if (GUILayout.Button("Open Selected Mega Level Asset"))
                {
                    Selection.activeObject = _selectedLevel;
                    EditorGUIUtility.PingObject(_selectedLevel);
                }
                if (GUILayout.Button("Play Selected Mega Level")) PlaySelected();
            }
        }

        private void PlaySelected()
        {
            EditorSceneManager.OpenScene(MegaShooterGenerator.ScenePath, OpenSceneMode.Single);
            MegaShooterGameManager manager = FindFirstObjectByType<MegaShooterGameManager>();
            if (manager == null) { Debug.LogError("[MegaLevelAuthoring] MegaShooterGameManager is missing from the scene."); return; }
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty debugLevel = serialized.FindProperty("debugLevel");
            if (debugLevel != null) debugLevel.objectReferenceValue = _selectedLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            EditorApplication.isPlaying = true;
        }
    }

    [CustomEditor(typeof(MegaLevelData))]
    public sealed class MegaLevelDataEditor : UnityEditor.Editor
    {
        private bool _identity = true;
        private bool _player = true;
        private bool _arena = true;
        private bool _difficulty = true;
        private bool _waves = true;
        private bool _boss = true;
        private bool _rewards = true;
        private bool _presentation = true;
        private bool _vfx = true;
        private bool _audio = true;
        private bool _debug = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawSection(ref _identity, "Identity", "gameLevelNumber", "displayTitle", "megaSequenceIndex", "description");
            DrawSection(ref _player, "Player", "featuredAnimal", "allowedAnimals", "startingHealth", "movementSpeedMultiplier", "playerPowerMultiplier", "invulnerabilityDuration", "counterChargeMultiplier");
            DrawSection(ref _arena, "Arena", "cameraBounds", "playerMovementBounds", "safeUiMargins", "scrollSpeed", "bottomProjectileExclusion");
            DrawSection(ref _difficulty, "Difficulty", "enemyHealthMultiplier", "enemyDamageMultiplier", "enemyProjectileSpeedMultiplier", "ordinaryEnemyFireInterval", "enemyFireIntervalMultiplier", "spawnCadenceMultiplier", "maximumActiveEnemies", "maximumHostileProjectiles", "targetEnemyCount");
            DrawSection(ref _waves, "Waves", "waves");
            DrawSection(ref _boss, "Boss", "boss", "bossOverrides");
            DrawSection(ref _rewards, "Rewards", "scoreMultiplier", "comboTimeout", "nearMissScore", "counterScore", "parTime", "coinReward", "arcadeTokenReward", "unlockReward");
            DrawSection(ref _presentation, "Presentation", "backgroundLayers", "backgroundLayerSpeeds", "backgroundSpeed", "backgroundColor", "accentColor", "introImage", "bossWarningText");
            DrawSection(ref _vfx, "VFX", "vfxProfile", "cameraShakeScale", "flashScale", "reducedEffectsCompatible");
            DrawSection(ref _audio, "Audio", "audio");
            DrawSection(ref _debug, "Debug", "deterministicSeed", "randomizeSeed");
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(ref bool foldout, string title, params string[] properties)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, title);
            if (foldout)
            {
                EditorGUI.indentLevel++;
                foreach (string propertyName in properties)
                {
                    SerializedProperty property = serializedObject.FindProperty(propertyName);
                    if (property != null) EditorGUILayout.PropertyField(property, true);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
#endif
