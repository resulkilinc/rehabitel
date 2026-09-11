// ============================================================
// GameOverUI.cs — REHABIT-EL
// Oyun sonu ekranı: Skor, canavar sayısı, tekrar dene, ana menü.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using RehabitEL.Core;

namespace RehabitEL.UI
{
    /// <summary>
    /// GameOver sahnesindeki sonuç ekranı ve navigasyon butonları.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Sonuç Gösterimi")]
        [SerializeField] private Text gameOverTitle;
        [SerializeField] private Text monsterCountText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text durationText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text newRecordText;

        [Header("Butonlar")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Animasyon")]
        [SerializeField] private float resultRevealDelay = 0.5f;

        // ════════════════════════════════════════════

        private void Start()
        {
            SetupButtons();
            DisplayResults();
        }

        /// <summary>
        /// Buton listener'larını ayarlar.
        /// </summary>
        private void SetupButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClick);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(OnMainMenuClick);
            }
        }

        /// <summary>
        /// SceneLoader'dan aktarılan sonuçları gösterir.
        /// </summary>
        private void DisplayResults()
        {
            int monsterCount = SceneLoader.LastMonsterCount;
            int score = SceneLoader.LastScore;
            float duration = SceneLoader.LastSessionDuration;
            bool isNewRecord = score > 0 && score >= GameManager.HighScore;

            // Başlık
            if (gameOverTitle != null)
            {
                gameOverTitle.text = "OYUN BİTTİ!";
            }

            // Canavar sayısı
            if (monsterCountText != null)
            {
                monsterCountText.text = $"Öldürülen Canavar: {monsterCount}";
            }

            // Puan
            if (scoreText != null)
            {
                scoreText.text = $"Toplam Puan: {score}";
            }

            // Süre
            if (durationText != null)
            {
                int minutes = Mathf.FloorToInt(duration / 60f);
                int seconds = Mathf.FloorToInt(duration % 60f);
                durationText.text = $"Süre: {minutes:D2}:{seconds:D2}";
            }

            // En yüksek skor
            if (highScoreText != null)
            {
                highScoreText.text = $"En Yüksek Skor: {GameManager.HighScore}";
            }

            // Yeni rekor bildirimi
            if (newRecordText != null)
            {
                if (isNewRecord && score > 0)
                {
                    newRecordText.text = "★ YENİ REKOR! ★";
                    newRecordText.gameObject.SetActive(true);
                }
                else
                {
                    newRecordText.gameObject.SetActive(false);
                }
            }

            Debug.Log($"[GameOverUI] Sonuçlar — N: {monsterCount}, P: {score}, Süre: {duration:F1}s");
        }

        // ── Buton Aksiyonları ──

        private void OnRetryClick()
        {
            Debug.Log("[GameOverUI] Tekrar Dene tıklandı");
            SceneLoader.RestartGame();
        }

        private void OnMainMenuClick()
        {
            Debug.Log("[GameOverUI] Ana Menü tıklandı");
            SceneLoader.LoadMainMenu();
        }
    }
}
