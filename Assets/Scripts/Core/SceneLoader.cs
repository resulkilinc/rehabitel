// ============================================================
// SceneLoader.cs — REHABIT-EL
// Sahne geçişlerini ve sahneler arası veri aktarımını yönetir.
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RehabitEL.Core
{
    /// <summary>
    /// Sahne geçişlerini merkezi olarak yöneten yardımcı sınıf.
    /// Sahneler arası skor aktarımı için static alanlar içerir.
    /// </summary>
    public static class SceneLoader
    {
        // ── Sahne adları (Build Settings'te bu sırayla eklenmelidir) ──
        public const string SCENE_MAIN_MENU   = "MainMenu";
        public const string SCENE_CALIBRATION  = "Calibration";
        public const string SCENE_GAME         = "Game";
        public const string SCENE_GAME_OVER    = "GameOver";

        // ── Sahneler arası aktarılacak veriler ──
        // GameOver sahnesinde gösterilmek üzere Game sahnesinden aktarılır.
        public static int LastMonsterCount { get; set; }
        public static int LastScore { get; set; }
        public static float LastSessionDuration { get; set; }

        /// <summary>
        /// Ana menü sahnesini yükler.
        /// </summary>
        public static void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        /// <summary>
        /// Kalibrasyon sahnesini yükler.
        /// </summary>
        public static void LoadCalibration()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SCENE_CALIBRATION);
        }

        /// <summary>
        /// Oyun sahnesini yükler ve verileri sıfırlar.
        /// </summary>
        public static void LoadGame()
        {
            LastMonsterCount = 0;
            LastScore = 0;
            LastSessionDuration = 0f;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SCENE_GAME);
        }

        /// <summary>
        /// Game Over sahnesini yükler. Skor verilerinin önceden ayarlanmış olması gerekir.
        /// </summary>
        public static void LoadGameOver(int monsterCount, int score, float duration)
        {
            LastMonsterCount = monsterCount;
            LastScore = score;
            LastSessionDuration = duration;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SCENE_GAME_OVER);
        }

        /// <summary>
        /// Oyunu yeniden başlatır (Game sahnesini yeniden yükler).
        /// </summary>
        public static void RestartGame()
        {
            LoadGame();
        }
    }
}
