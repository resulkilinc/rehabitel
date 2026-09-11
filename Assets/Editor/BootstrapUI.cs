// Editor-only: creates minimal uGUI Canvas + hooks serialized refs for UI scripts.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using RehabitEL.UI;

public static class BootstrapUI
{
    [MenuItem("RehabitEL/Bootstrap/Create Minimal UI (All Scenes)")]
    public static void CreateMinimalUI_Menu() => CreateMinimalUI();

    // Batchmode: -executeMethod BootstrapUI.CreateMinimalUI
    public static void CreateMinimalUI()
    {
        PatchMainMenu();
        PatchCalibration();
        PatchGame();
        PatchGameOver();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapUI] Minimal UI created & wired.");
    }

    private static void PatchMainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        EnsureEventSystem();
        var canvas = EnsureCanvas("Canvas");

        var root = EnsureChild(canvas.transform, "MainMenuUI");
        var mm = EnsureComponent<MainMenuUI>(root);

        var title = CreateText(root.transform, "TitleText", "REHABIT-EL", 36, TextAnchor.MiddleCenter, new Vector2(0, 220));
        var subtitle = CreateText(root.transform, "SubtitleText", "Büyücü vs Canavarlar\nRehabilitasyon Oyunu", 18, TextAnchor.MiddleCenter, new Vector2(0, 160));

        var startBtn = CreateButton(root.transform, "StartButton", "BAŞLA", new Vector2(0, 60));
        var calibBtn = CreateButton(root.transform, "CalibrationButton", "KALİBRASYON", new Vector2(0, 0));

        var highScore = CreateText(root.transform, "HighScoreText", "Henüz skor yok", 14, TextAnchor.MiddleCenter, new Vector2(0, -120));
        var highMonster = CreateText(root.transform, "HighMonsterText", "", 14, TextAnchor.MiddleCenter, new Vector2(0, -150));

        Wire(mm,
            ("titleText", title),
            ("subtitleText", subtitle),
            ("startButton", startBtn.GetComponent<Button>()),
            ("calibrationButton", calibBtn.GetComponent<Button>()),
            ("highScoreText", highScore),
            ("highMonsterText", highMonster)
        );

        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchCalibration()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Calibration.unity", OpenSceneMode.Single);
        EnsureEventSystem();
        var canvas = EnsureCanvas("Canvas");

        var root = EnsureChild(canvas.transform, "CalibrationUI");
        var ui = EnsureComponent<CalibrationUI>(root);

        var title = CreateText(root.transform, "TitleText", "KALİBRASYON", 28, TextAnchor.MiddleCenter, new Vector2(0, 240));
        var instruction = CreateText(root.transform, "InstructionText", "Kalibrasyona başlamak için\naşağıdaki butonları kullanın.", 16, TextAnchor.MiddleCenter, new Vector2(0, 170));
        var status = CreateText(root.transform, "StatusText", "", 14, TextAnchor.UpperLeft, new Vector2(-220, 80));
        var raw = CreateText(root.transform, "RawValueText", "Ham: 0.000 | Filtreli: 0.000", 14, TextAnchor.MiddleCenter, new Vector2(0, 110));

        var progressBg = CreateImage(root.transform, "ProgressBg", new Vector2(0, 60), new Vector2(420, 18), new Color(1,1,1,0.12f));
        var progressFill = CreateImage(progressBg.transform, "Fill", Vector2.zero, new Vector2(420, 18), new Color(0.3f, 0.8f, 1f, 0.9f));
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;

        var minBtn = CreateButton(root.transform, "MinButton", "MIN (DÜZ)", new Vector2(-160, -40));
        var maxBtn = CreateButton(root.transform, "MaxButton", "MAX (BÜK)", new Vector2(160, -40));
        var resetBtn = CreateButton(root.transform, "ResetButton", "SIFIRLA", new Vector2(-160, -110));
        var doneBtn = CreateButton(root.transform, "DoneButton", "TAMAM", new Vector2(160, -110));
        var backBtn = CreateButton(root.transform, "BackButton", "GERİ", new Vector2(0, -180));

        Wire(ui,
            ("titleText", title),
            ("instructionText", instruction),
            ("statusText", status),
            ("rawValueText", raw),
            ("progressFill", progressFill),
            ("minButton", minBtn.GetComponent<Button>()),
            ("maxButton", maxBtn.GetComponent<Button>()),
            ("resetButton", resetBtn.GetComponent<Button>()),
            ("doneButton", doneBtn.GetComponent<Button>()),
            ("backButton", backBtn.GetComponent<Button>())
        );

        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchGame()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        EnsureEventSystem();
        var canvas = EnsureCanvas("Canvas");

        var root = EnsureChild(canvas.transform, "GameUI");
        var ui = EnsureComponent<GameUI>(root);

        var score = CreateText(root.transform, "ScoreText", "PUAN: 0", 18, TextAnchor.UpperLeft, new Vector2(-300, 220));
        var monsters = CreateText(root.transform, "MonsterCountText", "CANAVAR: 0", 18, TextAnchor.UpperLeft, new Vector2(-300, 190));

        var timerText = CreateText(root.transform, "TimerText", "0.0s", 24, TextAnchor.UpperCenter, new Vector2(0, 220));
        var timerBg = CreateImage(root.transform, "TimerBg", new Vector2(0, 190), new Vector2(320, 14), new Color(1,1,1,0.12f));
        var timerFill = CreateImage(timerBg.transform, "Fill", Vector2.zero, new Vector2(320, 14), new Color(0.3f, 0.9f, 0.3f, 0.9f));
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        timerFill.fillAmount = 1f;

        var shapeBg = CreateImage(root.transform, "ShapeBg", new Vector2(300, 210), new Vector2(120, 80), new Color(1,1,1,0.10f));
        var shapeName = CreateText(root.transform, "ShapeNameText", "DAİRE", 16, TextAnchor.MiddleCenter, new Vector2(300, 225));
        var shapeSymbol = CreateText(root.transform, "ShapeSymbolText", "●", 26, TextAnchor.MiddleCenter, new Vector2(300, 195));

        var notif = CreateText(root.transform, "NotificationText", "", 18, TextAnchor.MiddleCenter, new Vector2(0, -220));
        var notifGroup = notif.gameObject.AddComponent<CanvasGroup>();
        notifGroup.alpha = 0f;

        Wire(ui,
            ("scoreText", score),
            ("monsterCountText", monsters),
            ("timerText", timerText),
            ("timerFill", timerFill),
            ("shapeNameText", shapeName),
            ("shapeSymbolText", shapeSymbol),
            ("shapeBackground", shapeBg),
            ("notificationText", notif),
            ("notificationGroup", notifGroup)
        );

        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchGameOver()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameOver.unity", OpenSceneMode.Single);
        EnsureEventSystem();
        var canvas = EnsureCanvas("Canvas");

        var root = EnsureChild(canvas.transform, "GameOverUI");
        var ui = EnsureComponent<GameOverUI>(root);

        var title = CreateText(root.transform, "GameOverTitle", "OYUN BİTTİ!", 34, TextAnchor.MiddleCenter, new Vector2(0, 220));
        var monsterCount = CreateText(root.transform, "MonsterCountText", "Öldürülen Canavar: 0", 18, TextAnchor.MiddleCenter, new Vector2(0, 120));
        var score = CreateText(root.transform, "ScoreText", "Toplam Puan: 0", 18, TextAnchor.MiddleCenter, new Vector2(0, 85));
        var duration = CreateText(root.transform, "DurationText", "Süre: 00:00", 16, TextAnchor.MiddleCenter, new Vector2(0, 50));
        var highScore = CreateText(root.transform, "HighScoreText", "En Yüksek Skor: 0", 14, TextAnchor.MiddleCenter, new Vector2(0, 10));
        var newRec = CreateText(root.transform, "NewRecordText", "★ YENİ REKOR! ★", 16, TextAnchor.MiddleCenter, new Vector2(0, -20));
        newRec.gameObject.SetActive(false);

        var retryBtn = CreateButton(root.transform, "RetryButton", "TEKRAR DENE", new Vector2(0, -110));
        var menuBtn = CreateButton(root.transform, "MainMenuButton", "ANA MENÜ", new Vector2(0, -170));

        Wire(ui,
            ("gameOverTitle", title),
            ("monsterCountText", monsterCount),
            ("scoreText", score),
            ("durationText", duration),
            ("highScoreText", highScore),
            ("newRecordText", newRec),
            ("retryButton", retryBtn.GetComponent<Button>()),
            ("mainMenuButton", menuBtn.GetComponent<Button>())
        );

        EditorSceneManager.SaveScene(scene);
    }

    // ---- helpers ----

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static Canvas EnsureCanvas(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            var c = existing.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null) return c;
        return go.AddComponent<T>();
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor, Vector2 anchoredPos)
    {
        var go = EnsureChild(parent, name);
        var rect = EnsureComponent<RectTransform>(go);
        rect.sizeDelta = new Vector2(600, 60);
        rect.anchoredPosition = anchoredPos;

        var t = EnsureComponent<Text>(go);
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = Color.white;
        return t;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = EnsureChild(parent, name);
        var rect = EnsureComponent<RectTransform>(go);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        var img = EnsureComponent<Image>(go);
        img.color = color;
        return img;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        var go = EnsureChild(parent, name);
        var rect = EnsureComponent<RectTransform>(go);
        rect.sizeDelta = new Vector2(280, 46);
        rect.anchoredPosition = anchoredPos;

        var img = EnsureComponent<Image>(go);
        img.color = new Color(1, 1, 1, 0.15f);

        var btn = EnsureComponent<Button>(go);

        var txt = CreateText(go.transform, "Text", label, 18, TextAnchor.MiddleCenter, Vector2.zero);
        txt.rectTransform.sizeDelta = new Vector2(280, 46);

        return go;
    }

    private static void Wire(Object target, params (string fieldName, Object value)[] bindings)
    {
        var so = new SerializedObject(target);
        foreach (var (field, val) in bindings)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[BootstrapUI] Field not found: {target.GetType().Name}.{field}");
                continue;
            }
            prop.objectReferenceValue = val;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
#endif

