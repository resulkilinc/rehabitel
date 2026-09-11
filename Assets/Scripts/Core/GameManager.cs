// ============================================================
// GameManager.cs — REHABIT-EL
// Oyun döngüsünü ve state machine'i yöneten merkezi yönetici.
// Spawn → Çizim → Doğrulama → Skor → Spawn döngüsünü kontrol eder.
// ============================================================
using System;
using UnityEngine;

namespace RehabitEL.Core
{
    /// <summary>
    /// Oyunun tüm durumlarını temsil eden state machine.
    /// </summary>
    public enum GameState
    {
        Idle,              // Oyun henüz başlamadı
        Spawning,          // Canavar spawn ediliyor
        WaitingForDraw,    // Oyuncu çizim yapmasını bekliyor
        Drawing,           // Oyuncu aktif olarak çiziyor
        Validating,        // Çizim doğrulanıyor
        SpellCasting,      // Büyü animasyonu oynatılıyor
        GameOver           // Oyun bitti
    }

    /// <summary>
    /// Oyun döngüsünü merkezi olarak yöneten singleton sınıf.
    /// Tüm alt sistemler (MonsterSpawner, DrawingCanvas, ShapeValidator, UI)
    /// bu sınıf üzerinden koordine edilir.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──
        public static GameManager Instance { get; private set; }

        // ── Events (diğer sistemler bu event'lere abone olur) ──
        /// <summary>Oyun durumu değiştiğinde tetiklenir.</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>Skor güncellendiğinde tetiklenir (monsterCount, score).</summary>
        public event Action<int, int> OnScoreUpdated;

        /// <summary>Geri sayım güncellendiğinde tetiklenir (kalan saniye).</summary>
        public event Action<float> OnTimerUpdated;

        /// <summary>Yeni bir şekil görevi atandığında tetiklenir.</summary>
        public event Action<ShapeType> OnNewShapeAssigned;

        /// <summary>Çizim doğrulandığında tetiklenir (başarılı mı, RMSE değeri).</summary>
        public event Action<bool, float> OnDrawingValidated;

        // ── Durum ──
        private GameState currentState = GameState.Idle;
        private int monsterCount = 0;   // Öldürülen canavar sayısı (N)
        private int score = 0;          // Toplam puan (P = N * 10)
        private float sessionStartTime;
        private float currentTimer;
        private float allowedTime;
        private ShapeType currentShape;

        // ── Sabitler ──
        private const int POINTS_PER_MONSTER = 10;
        private const float SPELL_CAST_DURATION = 1.2f; // Büyü animasyon süresi

        // ── Spell cast zamanlayıcı ──
        private float spellCastTimer;

        // ── Properties ──
        /// <summary>Mevcut oyun durumu.</summary>
        public GameState CurrentState => currentState;

        /// <summary>Toplam öldürülen canavar sayısı.</summary>
        public int MonsterCount => monsterCount;

        /// <summary>Toplam puan.</summary>
        public int Score => score;

        /// <summary>Aktif şekil görevi.</summary>
        public ShapeType CurrentShape => currentShape;

        /// <summary>Kalan süre (saniye).</summary>
        public float CurrentTimer => currentTimer;

        /// <summary>İzin verilen süre (saniye).</summary>
        public float AllowedTime => allowedTime;

        /// <summary>Seans başlangıcından geçen süre.</summary>
        public float SessionElapsed => Time.time - sessionStartTime;

        // ── En yüksek skor (PlayerPrefs) ──
        private const string HIGH_SCORE_KEY = "RehabitEL_HighScore";
        private const string HIGH_MONSTER_KEY = "RehabitEL_HighMonster";

        public static int HighScore
        {
            get => PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
            private set => PlayerPrefs.SetInt(HIGH_SCORE_KEY, value);
        }

        public static int HighMonsterCount
        {
            get => PlayerPrefs.GetInt(HIGH_MONSTER_KEY, 0);
            private set => PlayerPrefs.SetInt(HIGH_MONSTER_KEY, value);
        }

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

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            switch (currentState)
            {
                case GameState.WaitingForDraw:
                case GameState.Drawing:
                    UpdateTimer();
                    break;

                case GameState.SpellCasting:
                    UpdateSpellCast();
                    break;
            }
        }

        // ════════════════════════════════════════════
        // Oyun Akışı
        // ════════════════════════════════════════════

        /// <summary>
        /// Yeni bir oyun başlatır. Tüm değerleri sıfırlar.
        /// </summary>
        public void StartGame()
        {
            monsterCount = 0;
            score = 0;
            sessionStartTime = Time.time;

            // Zorluk yöneticisini sıfırla
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.ResetDifficulty();
            }

            OnScoreUpdated?.Invoke(monsterCount, score);
            ChangeState(GameState.Spawning);

            // İlk canavarı spawn et
            SpawnNextMonster();
        }

        /// <summary>
        /// Yeni bir canavar spawn eder ve şekil görevi atar.
        /// </summary>
        private void SpawnNextMonster()
        {
            // Rastgele şekil seç
            ShapeType[] shapes = (ShapeType[])Enum.GetValues(typeof(ShapeType));
            currentShape = shapes[UnityEngine.Random.Range(0, shapes.Length)];

            // Zorluk seviyesine göre süreyi al
            if (DifficultyManager.Instance != null)
            {
                allowedTime = DifficultyManager.Instance.GetCurrentTime();
            }
            else
            {
                allowedTime = 120f; // Varsayılan (2 dakika)
            }

            currentTimer = allowedTime;

            // Event'leri tetikle
            OnNewShapeAssigned?.Invoke(currentShape);
            OnTimerUpdated?.Invoke(currentTimer);

            Debug.Log($"[GameManager] Yeni canavar: {currentShape}, Süre: {allowedTime:F1}s");

            ChangeState(GameState.WaitingForDraw);
        }

        /// <summary>
        /// Geri sayım zamanlayıcısını günceller.
        /// Süre dolarsa oyunu bitirir.
        /// </summary>
        private void UpdateTimer()
        {
            currentTimer -= Time.deltaTime;
            OnTimerUpdated?.Invoke(Mathf.Max(0f, currentTimer));

            if (currentTimer <= 0f)
            {
                Debug.Log("[GameManager] Süre doldu! Game Over.");
                OnDrawingValidated?.Invoke(false, -1f);
                TriggerGameOver();
            }
        }

        /// <summary>
        /// Büyü animasyonu süresini yönetir.
        /// </summary>
        private void UpdateSpellCast()
        {
            spellCastTimer -= Time.deltaTime;
            if (spellCastTimer <= 0f)
            {
                // Animasyon bitti, yeni canavar spawn et
                ChangeState(GameState.Spawning);
                SpawnNextMonster();
            }
        }

        // ════════════════════════════════════════════
        // Dış Sistemlerden Çağrılan Metodlar
        // ════════════════════════════════════════════

        /// <summary>
        /// Oyuncu çizime başladığında DrawingCanvas tarafından çağrılır.
        /// </summary>
        public void OnDrawingStarted()
        {
            if (currentState == GameState.WaitingForDraw)
            {
                ChangeState(GameState.Drawing);
            }
        }

        /// <summary>
        /// Oyuncu çizimi bitirdiğinde DrawingCanvas tarafından çağrılır.
        /// ShapeValidator bu metoda RMSE sonucunu iletir.
        /// </summary>
        /// <param name="isSuccess">Şekil doğrulandı mı?</param>
        /// <param name="rmseValue">RMSE hata değeri (0 = mükemmel).</param>
        public void OnDrawingCompleted(bool isSuccess, float rmseValue)
        {
            if (currentState != GameState.Drawing && currentState != GameState.WaitingForDraw)
                return;

            ChangeState(GameState.Validating);

            Debug.Log($"[GameManager] Çizim sonucu: {(isSuccess ? "BAŞARILI" : "BAŞARISIZ")}, RMSE: {rmseValue:F3}");

            OnDrawingValidated?.Invoke(isSuccess, rmseValue);

            if (isSuccess)
            {
                HandleSuccess();
            }
            else
            {
                // BAŞARISIZ: Oyunu bitirme, oyuncuya tekrar deneme şansı ver.
                HandleFailTryAgain();
            }
        }

        /// <summary>
        /// Oyuncu yanlış çizdiğinde oyunu bitirmeden önce tekrar çizim hakkı tanır.
        /// </summary>
        private void HandleFailTryAgain()
        {
            // Zaman dolmadıysa tekrar çizim yapma durumuna geç
            ChangeState(GameState.WaitingForDraw);
            
            // OnDrawingValidated false gidecek (FeedbackManager sadece küçük sarsıntı yapar)
        }

        /// <summary>
        /// Başarılı çizim sonrası skor güncelleme ve büyü animasyonu.
        /// </summary>
        private void HandleSuccess()
        {
            monsterCount++;
            score = monsterCount * POINTS_PER_MONSTER;

            OnScoreUpdated?.Invoke(monsterCount, score);

            // Zorluk seviyesini güncelle
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnMonsterDefeated();
            }

            Debug.Log($"[GameManager] Skor: N={monsterCount}, P={score}");

            // Büyü animasyonu başlat
            spellCastTimer = SPELL_CAST_DURATION;
            ChangeState(GameState.SpellCasting);
        }

        /// <summary>
        /// Oyunu bitirir ve GameOver sahnesine geçer.
        /// </summary>
        private void TriggerGameOver()
        {
            ChangeState(GameState.GameOver);

            // Bitiş sarsıntısı ve flash'ı
            if (RehabitEL.Effects.FeedbackManager.Instance != null)
            {
                RehabitEL.Effects.FeedbackManager.Instance.PlayGameOverFeedback();
            }

            // En yüksek skoru güncelle
            if (score > HighScore)
            {
                HighScore = score;
                HighMonsterCount = monsterCount;
                PlayerPrefs.Save();
                Debug.Log($"[GameManager] Yeni rekor! N={monsterCount}, P={score}");
            }

            // Kısa gecikme sonrası GameOver sahnesine geç
            Invoke(nameof(TransitionToGameOver), 1.5f);
        }

        /// <summary>
        /// GameOver sahnesine geçiş yapar.
        /// </summary>
        private void TransitionToGameOver()
        {
            float duration = Time.time - sessionStartTime;
            SceneLoader.LoadGameOver(monsterCount, score, duration);
        }

        // ════════════════════════════════════════════
        // State Machine
        // ════════════════════════════════════════════

        /// <summary>
        /// Oyun durumunu değiştirir ve ilgili event'i tetikler.
        /// </summary>
        private void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] State: {previousState} → {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        // ════════════════════════════════════════════
        // Debug & Test
        // ════════════════════════════════════════════

        /// <summary>
        /// Debug: Space tuşu ile başarı simüle et (geliştirme için).
        /// </summary>
        public void DebugForceSuccess()
        {
            if (currentState == GameState.WaitingForDraw || currentState == GameState.Drawing)
            {
                OnDrawingCompleted(true, 0.05f);
            }
        }

        /// <summary>
        /// Debug: Escape tuşu ile başarısızlık simüle et.
        /// </summary>
        public void DebugForceFail()
        {
            if (currentState == GameState.WaitingForDraw || currentState == GameState.Drawing)
            {
                OnDrawingCompleted(false, 1.0f);
            }
        }
    }
}
