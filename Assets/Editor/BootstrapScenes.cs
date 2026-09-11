// Editor-only: creates required scenes & build settings.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using RehabitEL.Core;
using RehabitEL.Monsters;
using RehabitEL.Drawing;
using RehabitEL.Input;
using RehabitEL.Calibration;
using RehabitEL.Data;
using RehabitEL.Effects;
using RehabitEL.UI;

public static class BootstrapScenes
{
    private const string ScenesDir = "Assets/Scenes";

    [MenuItem("RehabitEL/Bootstrap/Create Scenes + Build Settings")]
    public static void CreateScenesAndBuildSettings_Menu() => CreateScenesAndBuildSettings();

    // Allows batchmode: -executeMethod BootstrapScenes.CreateScenesAndBuildSettings
    public static void CreateScenesAndBuildSettings()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        CreateEmptyScene("MainMenu", includeCoreObjects: false);
        CreateEmptyScene("Calibration", includeCoreObjects: false);
        CreateGameScene("Game");
        CreateEmptyScene("GameOver", includeCoreObjects: false);

        var buildScenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesDir}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/Calibration.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/Game.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/GameOver.unity", true),
        };
        EditorBuildSettings.scenes = buildScenes;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapScenes] Scenes created and added to Build Settings.");
    }

    private static void CreateEmptyScene(string sceneName, bool includeCoreObjects)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;

        // Camera (2D) — required for UI/scene visibility in Game view
        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
        }

        if (includeCoreObjects)
        {
            // Reserved if needed later.
        }

        SaveScene(sceneName);
    }

    private static void CreateGameScene(string sceneName)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;

        // Camera (2D)
        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        // Core singletons / managers
        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("DifficultyManager").AddComponent<DifficultyManager>();
        new GameObject("MonsterSpawner").AddComponent<MonsterSpawner>();
        new GameObject("DrawingCanvas").AddComponent<DrawingCanvas>();
        new GameObject("InputManager").AddComponent<InputManager>();
        new GameObject("CalibrationManager").AddComponent<CalibrationManager>();
        new GameObject("SessionRecorder").AddComponent<SessionRecorder>();
        new GameObject("FeedbackManager").AddComponent<FeedbackManager>();
        new GameObject("DebugOverlay").AddComponent<DebugOverlay>();

        SaveScene(sceneName);
    }

    private static void SaveScene(string sceneName)
    {
        var path = $"{ScenesDir}/{sceneName}.unity";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        Debug.Log($"[BootstrapScenes] Saved scene: {path}");
    }
}
#endif

