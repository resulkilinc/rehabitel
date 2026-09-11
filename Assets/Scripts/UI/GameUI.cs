// ============================================================
// GameUI.cs — REHABIT-EL
// Oyun içi HUD: Skor, canavar sayısı, geri sayım, aktif şekil.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using RehabitEL.Core;

namespace RehabitEL.UI
{
    /// <summary>
    /// Oyun sahnesindeki HUD (Heads-Up Display) yönetimi.
    /// GameManager event'lerine abone olarak güncel bilgileri gösterir.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Skor")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text monsterCountText;

        [Header("Zamanlayıcı")]
        [SerializeField] private Text timerText;
        [SerializeField] private Image timerFill;  // Progress bar fill image

        [Header("Şekil Göstergesi")]
        [SerializeField] private Text shapeNameText;
        [SerializeField] private Text shapeSymbolText;
        [SerializeField] private Image shapeBackground;

        [Header("Bildirim")]
        [SerializeField] private Text notificationText;
        [SerializeField] private CanvasGroup notificationGroup;

        [Header("Renk Ayarları")]
        [SerializeField] private Color timerSafeColor = new Color(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color timerDangerColor = new Color(1f, 0.3f, 0.2f);

        // ── Durum ──
        private float maxTime = 8f;
        private float notificationFadeTimer;
        private const float NOTIFICATION_DURATION = 1.5f;

        // ── Şekil adları ve sembolleri (Türkçe) ──
        private static readonly string[] SHAPE_NAMES = { "DAİRE", "KARE", "ÜÇGEN" };
        private static readonly string[] SHAPE_SYMBOLS = { "●", "■", "▲" };
        private static readonly Color[] SHAPE_COLORS =
        {
            new Color(1.0f, 0.4f, 0.2f), // Daire: Turuncu
            new Color(0.3f, 0.5f, 1.0f), // Kare: Mavi
            new Color(0.4f, 0.9f, 0.3f)  // Üçgen: Yeşil
        };

        // ════════════════════════════════════════════
        // Unity Lifecycle
        // ════════════════════════════════════════════

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            // Event'lere bir kez daha abone ol (sıralama güvencesi)
            SubscribeToEvents();
            InitializeUI();
        }

        private void Update()
        {
            // Bildirim fade-out
            if (notificationFadeTimer > 0f)
            {
                notificationFadeTimer -= Time.deltaTime;
                if (notificationGroup != null)
                {
                    notificationGroup.alpha = Mathf.Clamp01(notificationFadeTimer / 0.5f);
                }
            }
        }

        // ════════════════════════════════════════════
        // Event Abonelik
        // ════════════════════════════════════════════

        private void SubscribeToEvents()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.OnScoreUpdated -= UpdateScore;
            GameManager.Instance.OnScoreUpdated += UpdateScore;

            GameManager.Instance.OnTimerUpdated -= UpdateTimer;
            GameManager.Instance.OnTimerUpdated += UpdateTimer;

            GameManager.Instance.OnNewShapeAssigned -= UpdateShape;
            GameManager.Instance.OnNewShapeAssigned += UpdateShape;

            GameManager.Instance.OnDrawingValidated -= ShowDrawingResult;
            GameManager.Instance.OnDrawingValidated += ShowDrawingResult;
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.OnScoreUpdated -= UpdateScore;
            GameManager.Instance.OnTimerUpdated -= UpdateTimer;
            GameManager.Instance.OnNewShapeAssigned -= UpdateShape;
            GameManager.Instance.OnDrawingValidated -= ShowDrawingResult;
        }

        // ════════════════════════════════════════════
        // UI Güncelleme
        // ════════════════════════════════════════════

        private void InitializeUI()
        {
            UpdateScore(0, 0);
            if (notificationGroup != null) notificationGroup.alpha = 0f;
        }

        /// <summary>Skor ve canavar sayısını günceller.</summary>
        private void UpdateScore(int monsterCount, int score)
        {
            if (scoreText != null)
                scoreText.text = $"PUAN: {score}";

            if (monsterCountText != null)
                monsterCountText.text = $"CANAVAR: {monsterCount}";
        }

        /// <summary>Geri sayım zamanlayıcısını günceller.</summary>
        private void UpdateTimer(float remainingTime)
        {
            if (timerText != null)
            {
                timerText.text = $"{remainingTime:F1}s";
            }

            // Progress bar güncelle
            if (timerFill != null && maxTime > 0f)
            {
                float ratio = remainingTime / maxTime;
                timerFill.fillAmount = ratio;

                // Renk geçişi: güvenli → uyarı → tehlike
                if (ratio > 0.5f)
                    timerFill.color = timerSafeColor;
                else if (ratio > 0.25f)
                    timerFill.color = timerWarningColor;
                else
                    timerFill.color = timerDangerColor;
            }

            // Zamanlayıcı text rengi
            if (timerText != null)
            {
                float ratio = maxTime > 0 ? remainingTime / maxTime : 0;
                if (ratio > 0.5f)
                    timerText.color = Color.white;
                else if (ratio > 0.25f)
                    timerText.color = timerWarningColor;
                else
                    timerText.color = timerDangerColor;
            }
        }

        /// <summary>Aktif şekli günceller.</summary>
        private void UpdateShape(ShapeType shape)
        {
            int index = (int)shape;

            // Süreyi güncelle
            if (GameManager.Instance != null)
            {
                maxTime = GameManager.Instance.AllowedTime;
            }

            if (shapeNameText != null)
                shapeNameText.text = SHAPE_NAMES[index];

            if (shapeSymbolText != null)
            {
                shapeSymbolText.text = SHAPE_SYMBOLS[index];
                shapeSymbolText.color = SHAPE_COLORS[index];
            }

            if (shapeBackground != null)
            {
                Color bgColor = SHAPE_COLORS[index];
                bgColor.a = 0.2f;
                shapeBackground.color = bgColor;
            }

            // Bildirim göster
            ShowNotification($"Çiz: {SHAPE_NAMES[index]} {SHAPE_SYMBOLS[index]}");
        }

        /// <summary>Çizim sonucunu bildirim olarak gösterir.</summary>
        private void ShowDrawingResult(bool success, float rmse)
        {
            if (success)
            {
                ShowNotification($"BAŞARILI! (Hata: {rmse:F2})");
            }
            else
            {
                if (rmse < 0)
                    ShowNotification("SÜRE DOLDU!");
                else
                    ShowNotification($"BAŞARISIZ (Hata: {rmse:F2})");
            }
        }

        /// <summary>Geçici bildirim gösterir.</summary>
        private void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            if (notificationGroup != null)
            {
                notificationGroup.alpha = 1f;
            }

            notificationFadeTimer = NOTIFICATION_DURATION;
        }
    }
}
