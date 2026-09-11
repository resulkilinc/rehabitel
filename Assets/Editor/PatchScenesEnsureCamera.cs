// Editor-only: ensures all scenes have a Main Camera.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PatchScenesEnsureCamera
{
    [MenuItem("RehabitEL/Bootstrap/Patch Scenes (Ensure Camera)")]
    public static void Patch_Menu() => Patch();

    // Batchmode: -executeMethod PatchScenesEnsureCamera.Patch
    public static void Patch()
    {
        var scenePaths = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Calibration.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/GameOver.unity",
        };

        foreach (var path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            EnsureMainCamera();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PatchScenesEnsureCamera] Patched: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureMainCamera()
    {
        // If any enabled camera exists, keep it; otherwise create a 2D main camera.
        var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c != null && c.enabled)
            {
                // Ensure one of them is tagged as MainCamera for scripts that use Camera.main
                if (c.CompareTag("MainCamera")) return;
            }
        }

        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = -10;
        cam.farClipPlane = 1000;
    }
}
#endif

