// ============================================================
// SceneSetup.cs — REHABIT-EL (Editor Only)
// Unity menüsünden tek tıkla 4 sahneyi otomatik kuran script.
// RehabitEL > Setup All Scenes
// ============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class SceneSetup : EditorWindow
{
    [MenuItem("RehabitEL/Setup All Scenes (Otomatik Kurulum)")]
    public static void SetupAllScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "REHABIT-EL Sahne Kurulumu",
            "Bu işlem 4 sahneyi (MainMenu, Calibration, Game, GameOver) otomatik oluşturacak.\n\nMevcut sahneler varsa üzerine yazılacak.\n\nDevam etmek istiyor musunuz?",
            "Evet, Kur!", "İptal"))
        {
            return;
        }

        string scenesDir = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesDir))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // 1. Game Sahnesi (ana oyun)
        SetupGameScene(scenesDir);

        // 2. MainMenu Sahnesi
        SetupMainMenuScene(scenesDir);

        // 3. GameOver Sahnesi
        SetupGameOverScene(scenesDir);

        // 4. Calibration Sahnesi
        SetupCalibrationScene(scenesDir);

        // Build Settings'e sahneleri ekle
        SetupBuildSettings(scenesDir);

        // Game sahnesini aç
        EditorSceneManager.OpenScene($"{scenesDir}/Game.unity");

        EditorUtility.DisplayDialog("Kurulum Tamamlandı!",
            "4 sahne başarıyla oluşturuldu:\n" +
            "• MainMenu\n• Calibration\n• Game\n• GameOver\n\n" +
            "Build Settings güncellendi.\n" +
            "Game sahnesi açıldı — Play'e basarak test edin!",
            "Tamam");
    }

    // ════════════════════════════════════════════════════════════
    // GAME SCENE
    // ════════════════════════════════════════════════════════════

    static void SetupGameScene(string scenesDir)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Kamera ──
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.15f); // Koyu lacivert
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        // ── Büyücü (WizardController ile runtime'da oluşturulur) ──
        GameObject wizardMgr = new GameObject("WizardController");
        wizardMgr.AddComponent<RehabitEL.Effects.WizardController>();

        // ── Yönetici Objeleri ──
        // GameManager
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<RehabitEL.Core.GameManager>();
        var gameUI = gmObj.AddComponent<RehabitEL.UI.GameUI>();

        // DifficultyManager
        GameObject diffObj = new GameObject("DifficultyManager");
        diffObj.AddComponent<RehabitEL.Core.DifficultyManager>();

        // MonsterSpawner
        GameObject spawnObj = new GameObject("MonsterSpawner");
        spawnObj.AddComponent<RehabitEL.Monsters.MonsterSpawner>();

        // DrawingCanvas
        GameObject canvasDrawObj = new GameObject("DrawingCanvas");
        canvasDrawObj.AddComponent<RehabitEL.Drawing.DrawingCanvas>();

        // InputManager
        GameObject inputObj = new GameObject("InputManager");
        inputObj.AddComponent<RehabitEL.Input.InputManager>();

        // SessionRecorder
        GameObject recObj = new GameObject("SessionRecorder");
        recObj.AddComponent<RehabitEL.Data.SessionRecorder>();

        // SpellEffect
        GameObject spellObj = new GameObject("SpellEffect");
        spellObj.AddComponent<RehabitEL.Effects.SpellEffect>();

        // FeedbackManager
        GameObject feedbackObj = new GameObject("FeedbackManager");
        var feedbackMgr = feedbackObj.AddComponent<RehabitEL.Effects.FeedbackManager>();

        // ParallaxBackground
        GameObject bgMgr = new GameObject("ParallaxBackground");
        bgMgr.AddComponent<RehabitEL.Effects.ParallaxBackground>();

        // ScreenShake
        GameObject shakeMgr = new GameObject("ScreenShake");
        shakeMgr.AddComponent<RehabitEL.Effects.ScreenShake>();

        // DebugOverlay
        GameObject debugObj = new GameObject("DebugOverlay");
        debugObj.AddComponent<RehabitEL.UI.DebugOverlay>();

        // ── UI Canvas ──
        GameObject canvasObj = CreateCanvas("GameCanvas");
        var canvas = canvasObj.GetComponent<Canvas>();

        // Skor Text (sol üst)
        GameObject scoreTextObj = CreateUIText(canvasObj.transform, "ScoreText",
            "PUAN: 0", 24, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(250, 40), Color.white);

        // Canavar sayısı (sol üst, altında)
        GameObject monsterCountObj = CreateUIText(canvasObj.transform, "MonsterCountText",
            "CANAVAR: 0", 20, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -60), new Vector2(250, 35), new Color(0.8f, 0.8f, 0.8f));

        // Timer Text (üst orta)
        GameObject timerTextObj = CreateUIText(canvasObj.transform, "TimerText",
            "8.0s", 36, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(150, 50), Color.white);

        // Timer Fill (üst bar)
        GameObject timerFillObj = CreateTimerBar(canvasObj.transform);

        // Şekil adı (sağ üst)
        GameObject shapeNameObj = CreateUIText(canvasObj.transform, "ShapeNameText",
            "DAİRE", 22, TextAnchor.UpperRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(200, 35), new Color(1f, 0.8f, 0.3f));

        // Şekil sembolü (sağ üst, altında)
        GameObject shapeSymbolObj = CreateUIText(canvasObj.transform, "ShapeSymbolText",
            "●", 48, TextAnchor.UpperRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-50, -55), new Vector2(80, 60), new Color(1f, 0.4f, 0.2f));

        // Bildirim text (orta)
        GameObject notifGroupObj = new GameObject("NotificationGroup");
        notifGroupObj.transform.SetParent(canvasObj.transform, false);
        RectTransform notifRT = notifGroupObj.AddComponent<RectTransform>();
        notifRT.anchorMin = new Vector2(0.5f, 0.5f);
        notifRT.anchorMax = new Vector2(0.5f, 0.5f);
        notifRT.pivot = new Vector2(0.5f, 0.5f);
        notifRT.anchoredPosition = new Vector2(0, 100);
        notifRT.sizeDelta = new Vector2(500, 60);
        CanvasGroup notifCG = notifGroupObj.AddComponent<CanvasGroup>();
        notifCG.alpha = 0f;

        GameObject notifTextObj = CreateUIText(notifGroupObj.transform, "NotificationText",
            "", 28, TextAnchor.MiddleCenter,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(1f, 1f, 0.5f));
        var notifTextRT = notifTextObj.GetComponent<RectTransform>();
        notifTextRT.offsetMin = Vector2.zero;
        notifTextRT.offsetMax = Vector2.zero;

        // Flash overlay (tam ekran, şeffaf)
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(canvasObj.transform, false);
        RectTransform flashRT = flashObj.AddComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.offsetMin = Vector2.zero;
        flashRT.offsetMax = Vector2.zero;
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(0, 0, 0, 0);
        flashImg.raycastTarget = false;

        // EventSystem
        CreateEventSystem();

        // ── Referansları bağla ──
        // GameUI referansları
        var gameUISO = new SerializedObject(gameUI);
        SetSerializedRef(gameUISO, "scoreText", scoreTextObj.GetComponent<Text>());
        SetSerializedRef(gameUISO, "monsterCountText", monsterCountObj.GetComponent<Text>());
        SetSerializedRef(gameUISO, "timerText", timerTextObj.GetComponent<Text>());
        SetSerializedRef(gameUISO, "timerFill", timerFillObj.GetComponent<Image>());
        SetSerializedRef(gameUISO, "shapeNameText", shapeNameObj.GetComponent<Text>());
        SetSerializedRef(gameUISO, "shapeSymbolText", shapeSymbolObj.GetComponent<Text>());
        SetSerializedRef(gameUISO, "notificationText", notifTextObj.GetComponent<Text>());
        SetSerializedRef(gameUISO, "notificationGroup", notifCG);
        gameUISO.ApplyModifiedPropertiesWithoutUndo();

        // FeedbackManager referansı
        var feedSO = new SerializedObject(feedbackMgr);
        SetSerializedRef(feedSO, "flashOverlay", flashImg);
        feedSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Arka plan parallax sistemi tarafından yönetiliyor ──
        // (ParallaxBackground component'i kendi arka planını oluşturur)

        EditorSceneManager.SaveScene(scene, $"{scenesDir}/Game.unity");
        Debug.Log("[SceneSetup] Game sahnesi oluşturuldu");
    }

    // ════════════════════════════════════════════════════════════
    // MAIN MENU SCENE
    // ════════════════════════════════════════════════════════════

    static void SetupMainMenuScene(string scenesDir)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Kamera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        // Canvas
        GameObject canvasObj = CreateCanvas("MenuCanvas");

        // Başlık
        CreateUIText(canvasObj.transform, "TitleText",
            "REHABIT-EL", 52, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -80), new Vector2(600, 70), new Color(0f, 0.9f, 1f));

        // Alt başlık
        CreateUIText(canvasObj.transform, "SubtitleText",
            "Büyücü vs Canavarlar\nRehabilitasyon Oyunu", 22, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -160), new Vector2(500, 60), new Color(0.7f, 0.7f, 0.8f));

        // Başla butonu
        GameObject startBtn = CreateButton(canvasObj.transform, "StartButton",
            "BAŞLA", new Vector2(0, -30), new Vector2(280, 55),
            new Color(0.15f, 0.6f, 0.3f), Color.white);

        // Kalibrasyon butonu
        GameObject calibBtn = CreateButton(canvasObj.transform, "CalibrationButton",
            "KALİBRASYON", new Vector2(0, -100), new Vector2(280, 55),
            new Color(0.2f, 0.3f, 0.6f), Color.white);

        // Yüksek skor
        GameObject highScoreObj = CreateUIText(canvasObj.transform, "HighScoreText",
            "Henüz skor yok", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 80), new Vector2(400, 30), new Color(0.6f, 0.6f, 0.7f));

        GameObject highMonsterObj = CreateUIText(canvasObj.transform, "HighMonsterText",
            "", 16, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 50), new Vector2(400, 30), new Color(0.5f, 0.5f, 0.6f));

        // MainMenuUI
        GameObject menuMgr = new GameObject("MainMenuManager");
        var menuUI = menuMgr.AddComponent<RehabitEL.UI.MainMenuUI>();

        var menuSO = new SerializedObject(menuUI);
        SetSerializedRef(menuSO, "titleText", canvasObj.transform.Find("TitleText").GetComponent<Text>());
        SetSerializedRef(menuSO, "subtitleText", canvasObj.transform.Find("SubtitleText").GetComponent<Text>());
        SetSerializedRef(menuSO, "startButton", startBtn.GetComponent<Button>());
        SetSerializedRef(menuSO, "calibrationButton", calibBtn.GetComponent<Button>());
        SetSerializedRef(menuSO, "highScoreText", highScoreObj.GetComponent<Text>());
        SetSerializedRef(menuSO, "highMonsterText", highMonsterObj.GetComponent<Text>());
        menuSO.ApplyModifiedPropertiesWithoutUndo();

        // Büyücü (WizardController kullanılacak)
        // CreateWizard(); // Eski statik büyücü kaldırıldı

        CreateEventSystem();
        CreateBackground(cam);

        EditorSceneManager.SaveScene(scene, $"{scenesDir}/MainMenu.unity");
        Debug.Log("[SceneSetup] MainMenu sahnesi oluşturuldu");
    }

    // ════════════════════════════════════════════════════════════
    // GAME OVER SCENE
    // ════════════════════════════════════════════════════════════

    static void SetupGameOverScene(string scenesDir)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Kamera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.12f, 0.02f, 0.02f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        // Canvas
        GameObject canvasObj = CreateCanvas("GameOverCanvas");

        // Başlık
        GameObject titleObj = CreateUIText(canvasObj.transform, "GameOverTitle",
            "OYUN BİTTİ!", 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -60), new Vector2(500, 65), new Color(1f, 0.3f, 0.3f));

        // Canavar sayısı
        GameObject monsterObj = CreateUIText(canvasObj.transform, "MonsterCountText",
            "Öldürülen Canavar: 0", 26, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 60), new Vector2(400, 40), Color.white);

        // Skor
        GameObject scoreObj = CreateUIText(canvasObj.transform, "ScoreText",
            "Toplam Puan: 0", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 15), new Vector2(400, 45), new Color(1f, 0.9f, 0.3f));

        // Süre
        GameObject durationObj = CreateUIText(canvasObj.transform, "DurationText",
            "Süre: 00:00", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -25), new Vector2(300, 30), new Color(0.7f, 0.7f, 0.8f));

        // Yüksek skor
        GameObject highObj = CreateUIText(canvasObj.transform, "HighScoreText",
            "En Yüksek Skor: 0", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -60), new Vector2(300, 28), new Color(0.5f, 0.5f, 0.6f));

        // Yeni rekor
        GameObject newRecObj = CreateUIText(canvasObj.transform, "NewRecordText",
            "★ YENİ REKOR! ★", 32, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 110), new Vector2(400, 45), new Color(1f, 0.85f, 0f));

        // Butonlar
        GameObject retryBtn = CreateButton(canvasObj.transform, "RetryButton",
            "TEKRAR DENE", new Vector2(-100, -120), new Vector2(220, 50),
            new Color(0.15f, 0.5f, 0.3f), Color.white);

        GameObject menuBtn = CreateButton(canvasObj.transform, "MainMenuButton",
            "ANA MENÜ", new Vector2(100, -120), new Vector2(220, 50),
            new Color(0.3f, 0.3f, 0.5f), Color.white);

        // GameOverUI
        GameObject goMgr = new GameObject("GameOverManager");
        var goUI = goMgr.AddComponent<RehabitEL.UI.GameOverUI>();

        var goSO = new SerializedObject(goUI);
        SetSerializedRef(goSO, "gameOverTitle", titleObj.GetComponent<Text>());
        SetSerializedRef(goSO, "monsterCountText", monsterObj.GetComponent<Text>());
        SetSerializedRef(goSO, "scoreText", scoreObj.GetComponent<Text>());
        SetSerializedRef(goSO, "durationText", durationObj.GetComponent<Text>());
        SetSerializedRef(goSO, "highScoreText", highObj.GetComponent<Text>());
        SetSerializedRef(goSO, "newRecordText", newRecObj.GetComponent<Text>());
        SetSerializedRef(goSO, "retryButton", retryBtn.GetComponent<Button>());
        SetSerializedRef(goSO, "mainMenuButton", menuBtn.GetComponent<Button>());
        goSO.ApplyModifiedPropertiesWithoutUndo();

        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, $"{scenesDir}/GameOver.unity");
        Debug.Log("[SceneSetup] GameOver sahnesi oluşturuldu");
    }

    // ════════════════════════════════════════════════════════════
    // CALIBRATION SCENE
    // ════════════════════════════════════════════════════════════

    static void SetupCalibrationScene(string scenesDir)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Kamera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        // CalibrationManager (DontDestroyOnLoad)
        GameObject calibMgrObj = new GameObject("CalibrationManager");
        calibMgrObj.AddComponent<RehabitEL.Calibration.CalibrationManager>();

        // InputManager
        GameObject inputObj = new GameObject("InputManager");
        inputObj.AddComponent<RehabitEL.Input.InputManager>();

        // Canvas
        GameObject canvasObj = CreateCanvas("CalibrationCanvas");

        // Başlık
        GameObject titleObj = CreateUIText(canvasObj.transform, "TitleText",
            "KALİBRASYON", 38, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -40), new Vector2(500, 55), new Color(0f, 0.9f, 1f));

        // Talimat
        GameObject instrObj = CreateUIText(canvasObj.transform, "InstructionText",
            "Kalibrasyona başlamak için\naşağıdaki butonları kullanın.", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -110), new Vector2(500, 60), Color.white);

        // Durum
        GameObject statusObj = CreateUIText(canvasObj.transform, "StatusText",
            "", 16, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 50), new Vector2(400, 50), new Color(0.7f, 0.9f, 0.7f));

        // Ham değer
        GameObject rawObj = CreateUIText(canvasObj.transform, "RawValueText",
            "Ham: 0.000 | Filtreli: 0.000", 14, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 10), new Vector2(400, 25), new Color(0.6f, 0.6f, 0.7f));

        // Progress bar
        GameObject progObj = CreateProgressBar(canvasObj.transform, "ProgressFill",
            new Vector2(0, -30), new Vector2(350, 20));

        // Butonlar
        GameObject minBtn = CreateButton(canvasObj.transform, "MinButton",
            "DÜZ TUT (Min)", new Vector2(-130, -90), new Vector2(220, 45),
            new Color(0.2f, 0.5f, 0.3f), Color.white);

        GameObject maxBtn = CreateButton(canvasObj.transform, "MaxButton",
            "MAX BÜK (Max)", new Vector2(130, -90), new Vector2(220, 45),
            new Color(0.5f, 0.3f, 0.2f), Color.white);

        GameObject resetBtn = CreateButton(canvasObj.transform, "ResetButton",
            "SIFIRLA", new Vector2(-130, -150), new Vector2(220, 45),
            new Color(0.4f, 0.4f, 0.4f), Color.white);

        GameObject doneBtn = CreateButton(canvasObj.transform, "DoneButton",
            "TAMAM → OYUN", new Vector2(130, -150), new Vector2(220, 45),
            new Color(0.15f, 0.5f, 0.7f), Color.white);

        GameObject backBtn = CreateButton(canvasObj.transform, "BackButton",
            "← GERİ", new Vector2(0, -210), new Vector2(180, 40),
            new Color(0.3f, 0.3f, 0.3f), Color.white);

        // CalibrationUI
        GameObject calibUIObj = new GameObject("CalibrationUI");
        var calibUI = calibUIObj.AddComponent<RehabitEL.UI.CalibrationUI>();

        var calibSO = new SerializedObject(calibUI);
        SetSerializedRef(calibSO, "titleText", titleObj.GetComponent<Text>());
        SetSerializedRef(calibSO, "instructionText", instrObj.GetComponent<Text>());
        SetSerializedRef(calibSO, "statusText", statusObj.GetComponent<Text>());
        SetSerializedRef(calibSO, "rawValueText", rawObj.GetComponent<Text>());
        SetSerializedRef(calibSO, "progressFill", progObj.GetComponent<Image>());
        SetSerializedRef(calibSO, "minButton", minBtn.GetComponent<Button>());
        SetSerializedRef(calibSO, "maxButton", maxBtn.GetComponent<Button>());
        SetSerializedRef(calibSO, "resetButton", resetBtn.GetComponent<Button>());
        SetSerializedRef(calibSO, "doneButton", doneBtn.GetComponent<Button>());
        SetSerializedRef(calibSO, "backButton", backBtn.GetComponent<Button>());
        calibSO.ApplyModifiedPropertiesWithoutUndo();

        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, $"{scenesDir}/Calibration.unity");
        Debug.Log("[SceneSetup] Calibration sahnesi oluşturuldu");
    }

    // ════════════════════════════════════════════════════════════
    // BUILD SETTINGS
    // ════════════════════════════════════════════════════════════

    static void SetupBuildSettings(string scenesDir)
    {
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene($"{scenesDir}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{scenesDir}/Calibration.unity", true),
            new EditorBuildSettingsScene($"{scenesDir}/Game.unity", true),
            new EditorBuildSettingsScene($"{scenesDir}/GameOver.unity", true)
        };
        Debug.Log("[SceneSetup] Build Settings güncellendi");
    }

    // ════════════════════════════════════════════════════════════
    // YARDIMCI METOTLAR
    // ════════════════════════════════════════════════════════════

    static GameObject CreateCanvas(string name)
    {
        GameObject canvasObj = new GameObject(name);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    static GameObject CreateUIText(Transform parent, string name, string text,
        int fontSize, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = color;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        // Outline (okunabilirlik)
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        return obj;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Color btnColor, Color textColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = btnObj.AddComponent<Image>();
        img.color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = btnColor;
        colors.highlightedColor = btnColor * 1.2f;
        colors.pressedColor = btnColor * 0.8f;
        btn.colors = colors;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        Text t = textObj.AddComponent<Text>();
        t.text = label;
        t.fontSize = 20;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = textColor;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;

        return btnObj;
    }

    static GameObject CreateTimerBar(Transform parent)
    {
        // Background
        GameObject bgObj = new GameObject("TimerBarBG");
        bgObj.transform.SetParent(parent, false);
        RectTransform bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.15f, 1);
        bgRT.anchorMax = new Vector2(0.85f, 1);
        bgRT.pivot = new Vector2(0.5f, 1);
        bgRT.anchoredPosition = new Vector2(0, -65);
        bgRT.sizeDelta = new Vector2(0, 12);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        // Fill
        GameObject fillObj = new GameObject("TimerFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.9f, 0.3f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        return fillObj;
    }

    static GameObject CreateProgressBar(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        // Background
        GameObject bgObj = new GameObject(name + "_BG");
        bgObj.transform.SetParent(parent, false);
        RectTransform bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = anchoredPos;
        bgRT.sizeDelta = sizeDelta;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.3f);

        // Fill
        GameObject fillObj = new GameObject(name);
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.8f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;

        return fillObj;
    }

    static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    static GameObject CreateWizard()
    {
        // Ana büyücü objesi
        GameObject wizard = new GameObject("Wizard");
        wizard.transform.position = new Vector3(-5.5f, -0.5f, 0f);

        // Gövde (mor dikdörtgen)
        GameObject body = new GameObject("Body");
        body.transform.SetParent(wizard.transform);
        body.transform.localPosition = Vector3.zero;
        SpriteRenderer bodySR = body.AddComponent<SpriteRenderer>();
        bodySR.sprite = CreateColoredSprite(32, 48, new Color(0.4f, 0.2f, 0.7f));
        bodySR.sortingOrder = 3;

        // Kafa (daire)
        GameObject head = new GameObject("Head");
        head.transform.SetParent(wizard.transform);
        head.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        SpriteRenderer headSR = head.AddComponent<SpriteRenderer>();
        headSR.sprite = CreateCircleSprite(24, new Color(0.9f, 0.75f, 0.6f));
        headSR.sortingOrder = 4;
        head.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        // Şapka (üçgen → basit kare şapka)
        GameObject hat = new GameObject("Hat");
        hat.transform.SetParent(wizard.transform);
        hat.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        SpriteRenderer hatSR = hat.AddComponent<SpriteRenderer>();
        hatSR.sprite = CreateColoredSprite(20, 32, new Color(0.2f, 0.1f, 0.5f));
        hatSR.sortingOrder = 5;
        hat.transform.localScale = new Vector3(0.5f, 0.7f, 1f);

        // Asa (ince dikdörtgen)
        GameObject staff = new GameObject("Staff");
        staff.transform.SetParent(wizard.transform);
        staff.transform.localPosition = new Vector3(0.4f, 0.2f, 0f);
        SpriteRenderer staffSR = staff.AddComponent<SpriteRenderer>();
        staffSR.sprite = CreateColoredSprite(4, 64, new Color(0.55f, 0.35f, 0.15f));
        staffSR.sortingOrder = 2;
        staff.transform.localScale = new Vector3(0.3f, 1f, 1f);

        // Asa ucu (parlak topuz)
        GameObject orb = new GameObject("StaffOrb");
        orb.transform.SetParent(staff.transform);
        orb.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        SpriteRenderer orbSR = orb.AddComponent<SpriteRenderer>();
        orbSR.sprite = CreateCircleSprite(16, new Color(0f, 0.9f, 1f));
        orbSR.sortingOrder = 6;
        orb.transform.localScale = new Vector3(1.5f, 0.4f, 1f);

        return wizard;
    }

    static Sprite CreateColoredSprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    static Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    static void CreateBackground(Camera cam)
    {
        // Basit gradient arka plan — düz renk kamera bg yeterli
        // İstenirse bir quad eklenebilir
    }

    static void SetSerializedRef(SerializedObject so, string fieldName, Object reference)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = reference;
        }
        else
        {
            Debug.LogWarning($"[SceneSetup] Field bulunamadı: {fieldName}");
        }
    }
}
#endif
