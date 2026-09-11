// ============================================================
// SessionRecorder.cs — REHABIT-EL
// Seans verisi toplama: her canavar sonucunu kaydeder,
// seans bitişinde DataExporter'a gönderir.
// ============================================================
using UnityEngine;
using RehabitEL.Core;
using RehabitEL.Drawing;
using RehabitEL.Input;

namespace RehabitEL.Data
{
    /// <summary>
    /// Oyun sırasında seans verilerini toplar.
    /// GameManager event'lerine abone olarak otomatik çalışır.
    /// Seans bitişinde DataExporter ile CSV/JSON dosyası oluşturur.
    /// </summary>
    public class SessionRecorder : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Export Ayarları")]
        [Tooltip("CSV dosyası oluştur")]
        [SerializeField] private bool exportCSV = true;

        [Tooltip("JSON dosyası oluştur")]
        [SerializeField] private bool exportJSON = true;

        // ── Durum ──
        private SessionData currentSession;
        private float monsterStartTime;
        private int monsterIndex = 0;
        private bool isRecording = false;

        // ── Singleton ──
        public static SessionRecorder Instance { get; private set; }

        /// <summary>Mevcut seans verisi.</summary>
        public SessionData CurrentSession => currentSession;

        // ════════════════════════════════════════════
        // Unity Lifecycle
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Start()
        {
            SubscribeEvents();
            StartNewSession();
        }

        // ════════════════════════════════════════════
        // Event Yönetimi
        // ════════════════════════════════════════════

        private void SubscribeEvents()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.OnNewShapeAssigned -= HandleNewShape;
            GameManager.Instance.OnNewShapeAssigned += HandleNewShape;

            GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
            GameManager.Instance.OnDrawingValidated += HandleDrawingResult;

            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameStateChanged += HandleStateChanged;
        }

        private void UnsubscribeEvents()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.OnNewShapeAssigned -= HandleNewShape;
            GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
        }

        // ════════════════════════════════════════════
        // Seans Yönetimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Yeni bir kayıt seansı başlatır.
        /// </summary>
        public void StartNewSession()
        {
            currentSession = new SessionData();
            currentSession.startedAt = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            monsterIndex = 0;
            isRecording = true;

            // Input modu
            if (InputManager.Instance != null)
            {
                currentSession.inputMode = InputManager.Instance.CurrentMode.ToString();
            }
            else
            {
                currentSession.inputMode = "Mouse";
            }

            Debug.Log($"[SessionRecorder] Yeni seans başladı: {currentSession.sessionId}");
        }

        /// <summary>
        /// Seansı sonlandırır ve export eder.
        /// </summary>
        public void EndSession()
        {
            if (!isRecording || currentSession == null) return;

            isRecording = false;

            currentSession.endedAt = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            currentSession.monsterCount = GameManager.Instance != null
                ? GameManager.Instance.MonsterCount : 0;
            currentSession.scoreTotal = GameManager.Instance != null
                ? GameManager.Instance.Score : 0;
            currentSession.durationSeconds = GameManager.Instance != null
                ? GameManager.Instance.SessionElapsed : 0f;
            currentSession.difficultyLevel = DifficultyManager.Instance != null
                ? DifficultyManager.Instance.CurrentLevel : 0;

            Debug.Log($"[SessionRecorder] Seans bitti. " +
                      $"N={currentSession.monsterCount}, " +
                      $"P={currentSession.scoreTotal}, " +
                      $"Süre={currentSession.durationSeconds:F1}s, " +
                      $"Kayıt sayısı={currentSession.monsters.Count}");

            // Export
            ExportSession();
        }

        // ════════════════════════════════════════════
        // Event Handler'lar
        // ════════════════════════════════════════════

        private void HandleNewShape(ShapeType shape)
        {
            // Canavar başlangıç zamanını kaydet
            monsterStartTime = Time.time;
        }

        private void HandleDrawingResult(bool success, float rmse)
        {
            if (!isRecording || currentSession == null) return;

            float timeToComplete = (Time.time - monsterStartTime) * 1000f; // ms
            float allowedTime = GameManager.Instance != null
                ? GameManager.Instance.AllowedTime * 1000f : 8000f; // ms

            string shapeName = GameManager.Instance != null
                ? GameManager.Instance.CurrentShape.ToString() : "Unknown";

            float threshold = DifficultyManager.Instance != null
                ? DifficultyManager.Instance.GetCurrentThreshold() : 0.30f;

            int pointCount = DrawingCanvas.Instance != null
                ? DrawingCanvas.Instance.DrawnPoints.Count : 0;

            MonsterRecord record = new MonsterRecord(
                monsterIndex,
                shapeName,
                success,
                timeToComplete,
                allowedTime,
                rmse >= 0 ? rmse : -1f,
                pointCount,
                threshold
            );

            currentSession.monsters.Add(record);
            monsterIndex++;

            Debug.Log($"[SessionRecorder] Kayıt #{record.monsterIndex}: " +
                      $"{record.shape}, " +
                      $"{(record.success ? "BAŞARILI" : "BAŞARISIZ")}, " +
                      $"Süre: {record.timeToCompleteMs:F0}ms, " +
                      $"RMSE: {record.errorMetric:F3}");
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                EndSession();
            }
        }

        // ════════════════════════════════════════════
        // Export
        // ════════════════════════════════════════════

        private void ExportSession()
        {
            if (currentSession == null) return;

            if (exportCSV)
            {
                string csvPath = DataExporter.ExportCSV(currentSession);
                if (!string.IsNullOrEmpty(csvPath))
                {
                    Debug.Log($"[SessionRecorder] CSV export: {csvPath}");
                }
            }

            if (exportJSON)
            {
                string jsonPath = DataExporter.ExportJSON(currentSession);
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    Debug.Log($"[SessionRecorder] JSON export: {jsonPath}");
                }
            }
        }
    }
}
