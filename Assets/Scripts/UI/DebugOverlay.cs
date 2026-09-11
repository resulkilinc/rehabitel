// ============================================================
// DebugOverlay.cs — REHABIT-EL
// Debug bilgi paneli: FPS, input değerleri, çizim metrikleri.
// F12 ile açılır/kapanır.
// ============================================================
using UnityEngine;
using RehabitEL.Core;
using RehabitEL.Input;
using RehabitEL.Drawing;
using RehabitEL.Calibration;

namespace RehabitEL.UI
{
    /// <summary>
    /// Ekran üzerinde debug bilgi paneli gösterir.
    /// F12 tuşu ile açılır/kapanır.
    /// Geliştirme ve test sırasında kullanılır.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Görünüm")]
        [Tooltip("Panel başlangıçta açık mı?")]
        [SerializeField] private bool showOnStart = false;

        [Tooltip("Panel arka plan rengi")]
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.75f);

        [Tooltip("Metin rengi")]
        [SerializeField] private Color textColor = Color.white;

        [Tooltip("Panel genişliği")]
        [SerializeField] private float panelWidth = 320f;

        // ── Durum ──
        private bool isVisible;
        private float fps;
        private float fpsUpdateTimer;
        private int frameCount;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private Texture2D bgTexture;

        // ════════════════════════════════════════════

        private void Start()
        {
            isVisible = showOnStart;
            CreateStyles();
        }

        private void Update()
        {
            // FPS hesapla
            frameCount++;
            fpsUpdateTimer += Time.unscaledDeltaTime;
            if (fpsUpdateTimer >= 0.5f)
            {
                fps = frameCount / fpsUpdateTimer;
                frameCount = 0;
                fpsUpdateTimer = 0f;
            }

            // F12 ile toggle
            if (UnityEngine.Input.GetKeyDown(KeyCode.F12))
            {
                isVisible = !isVisible;
            }

            // F1 ile input mode değiştir
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.CycleInputMode();
                }
            }
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            float x = Screen.width - panelWidth - 10f;
            float y = 10f;
            float lineHeight = 20f;
            float padding = 8f;

            // Arka plan
            float totalHeight = lineHeight * 20 + padding * 2;
            GUI.DrawTexture(new Rect(x - padding, y - padding, panelWidth + padding * 2, totalHeight), bgTexture);

            // ── Başlık ──
            GUI.Label(new Rect(x, y, panelWidth, lineHeight), "═══ DEBUG PANEL (F12) ═══", headerStyle);
            y += lineHeight + 4;

            // ── FPS ──
            Color fpsColor = fps >= 55 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
            labelStyle.normal.textColor = fpsColor;
            GUI.Label(new Rect(x, y, panelWidth, lineHeight), $"FPS: {fps:F0}", labelStyle);
            y += lineHeight;
            labelStyle.normal.textColor = textColor;

            // ── Game State ──
            string gameState = GameManager.Instance != null
                ? GameManager.Instance.CurrentState.ToString() : "N/A";
            GUI.Label(new Rect(x, y, panelWidth, lineHeight), $"State: {gameState}", labelStyle);
            y += lineHeight;

            // ── Skor ──
            if (GameManager.Instance != null)
            {
                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"N: {GameManager.Instance.MonsterCount}  P: {GameManager.Instance.Score}", labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Şekil: {GameManager.Instance.CurrentShape}  " +
                    $"Süre: {GameManager.Instance.CurrentTimer:F1}s", labelStyle);
                y += lineHeight;
            }

            y += 4;
            GUI.Label(new Rect(x, y, panelWidth, lineHeight), "── Input ──", headerStyle);
            y += lineHeight;

            // ── Input Provider ──
            if (InputManager.Instance != null)
            {
                var provider = InputManager.Instance.ActiveProvider;
                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Mode: {InputManager.Instance.CurrentMode} [F1 değiştir]", labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Bağlı: {(provider?.IsConnected ?? false)}  " +
                    $"Çizim: {(provider?.IsDrawing ?? false)}", labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Ham: {InputManager.Instance.GetRawFlexValue():F3}  " +
                    $"Filtreli: {InputManager.Instance.GetFilteredFlexValue():F3}", labelStyle);
                y += lineHeight;
            }

            // ── Kalibrasyon ──
            if (CalibrationManager.Instance != null)
            {
                y += 4;
                GUI.Label(new Rect(x, y, panelWidth, lineHeight), "── Kalibrasyon ──", headerStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Kalibre: {CalibrationManager.Instance.IsCalibrated}  " +
                    $"Min: {CalibrationManager.Instance.MinValue:F3}  " +
                    $"Max: {CalibrationManager.Instance.MaxValue:F3}", labelStyle);
                y += lineHeight;
            }

            // ── Çizim ──
            if (DrawingCanvas.Instance != null)
            {
                y += 4;
                GUI.Label(new Rect(x, y, panelWidth, lineHeight), "── Çizim ──", headerStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Çiziyor: {DrawingCanvas.Instance.IsDrawing}  " +
                    $"Noktalar: {DrawingCanvas.Instance.DrawnPoints.Count}", labelStyle);
                y += lineHeight;
            }

            // ── Zorluk ──
            if (DifficultyManager.Instance != null)
            {
                y += 4;
                GUI.Label(new Rect(x, y, panelWidth, lineHeight), "── Zorluk ──", headerStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    $"Seviye: {DifficultyManager.Instance.CurrentLevel}  " +
                    $"Süre: {DifficultyManager.Instance.GetCurrentTime():F1}s  " +
                    $"Eşik: {DifficultyManager.Instance.GetCurrentThreshold():F3}", labelStyle);
                y += lineHeight;
            }
        }

        // ════════════════════════════════════════════

        private void CreateStyles()
        {
            // Arka plan texture
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, backgroundColor);
            bgTexture.Apply();

            // Label style
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.normal.textColor = textColor;
            labelStyle.fontStyle = FontStyle.Normal;

            // Header style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 13;
            headerStyle.normal.textColor = new Color(0f, 0.9f, 1f);
            headerStyle.fontStyle = FontStyle.Bold;
        }
    }
}
