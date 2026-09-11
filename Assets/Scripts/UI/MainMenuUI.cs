// ============================================================
// MainMenuUI.cs — REHABIT-EL
// Ana menü arayüzü: Başla, Kalibrasyon, En Yüksek Skor.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using RehabitEL.Core;

namespace RehabitEL.UI
{
    /// <summary>
    /// Ana menü sahnesindeki UI elementlerini yönetir.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("UI Referansları")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button calibrationButton;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text highMonsterText;

        [Header("Başlıklar")]
        [SerializeField] private string gameTitle = "REHABIT-EL";
        [SerializeField] private string gameSubtitle = "Büyücü vs Canavarlar\nRehabilitasyon Oyunu";

        // ════════════════════════════════════════════

        private void Start()
        {
            SetupUI();
            UpdateHighScore();
        }

        /// <summary>
        /// UI elementlerini başlangıç değerleriyle ayarlar.
        /// </summary>
        private void SetupUI()
        {
            // Başlık
            if (titleText != null)
            {
                titleText.text = gameTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = gameSubtitle;
            }

            // Buton listener'ları
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartClick);
            }

            if (calibrationButton != null)
            {
                calibrationButton.onClick.RemoveAllListeners();
                calibrationButton.onClick.AddListener(OnCalibrationClick);
            }
        }

        /// <summary>
        /// En yüksek skor bilgisini günceller.
        /// </summary>
        private void UpdateHighScore()
        {
            int highScore = GameManager.HighScore;
            int highMonster = GameManager.HighMonsterCount;

            if (highScoreText != null)
            {
                if (highScore > 0)
                {
                    highScoreText.text = $"En Yüksek Skor: {highScore}";
                }
                else
                {
                    highScoreText.text = "Henüz skor yok";
                }
            }

            if (highMonsterText != null)
            {
                if (highMonster > 0)
                {
                    highMonsterText.text = $"En Çok Canavar: {highMonster}";
                }
                else
                {
                    highMonsterText.text = "";
                }
            }
        }

        // ── Buton Aksiyonları ──

        private void OnStartClick()
        {
            Debug.Log("[MainMenuUI] Oyun başlatılıyor...");
            SceneLoader.LoadGame();
        }

        private void OnCalibrationClick()
        {
            Debug.Log("[MainMenuUI] Kalibrasyon ekranına geçiliyor...");
            SceneLoader.LoadCalibration();
        }
    }
}
