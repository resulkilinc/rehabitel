// ============================================================
// DifficultyManager.cs — REHABIT-EL
// Zorluk seviyesini ve süre ayarlarını yönetir.
// Her 3 canavarda süre %10 azalır, minimum 2 saniyeye kadar.
// ============================================================
using UnityEngine;

namespace RehabitEL.Core
{
    /// <summary>
    /// Oyunun zorluk parametrelerini yönetir.
    /// Canavar sayısına göre adaptif süre ve tolerans hesaplar.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Süre Ayarları")]
        [Tooltip("Başlangıç süresi (saniye) — ilk canavarın yaklaşma süresi")]
        [SerializeField] private float baseTime = 8f;

        [Tooltip("Her kaç canavarda bir zorluk artsın")]
        [SerializeField] private int monstersPerLevel = 3;

        [Tooltip("Her seviyede süre çarpanı (0.9 = %10 azalma)")]
        [SerializeField] private float timeMultiplier = 0.9f;

        [Tooltip("Minimum süre limiti (saniye) — bunun altına düşmez")]
        [SerializeField] private float minTime = 2f;

        [Header("Şekil Doğrulama Ayarları")]
        [Tooltip("Başlangıç RMSE toleransı (yüksek = kolay). Telefon/saat çizimi için 0.25 önerilir.")]
        [SerializeField] private float baseThreshold = 0.25f;

        [Tooltip("Her seviyede tolerans çarpanı (0.95 = %5 daralma)")]
        [SerializeField] private float thresholdMultiplier = 0.95f;

        [Tooltip("Minimum tolerans (bunun altına düşmez)")]
        [SerializeField] private float minThreshold = 0.12f;

        // ── Durum ──
        private int currentLevel = 0;
        private int monstersDefeatedThisLevel = 0;

        // ── Singleton ──
        public static DifficultyManager Instance { get; private set; }

        // ── Properties ──
        /// <summary>Şu anki zorluk seviyesi (0'dan başlar).</summary>
        public int CurrentLevel => currentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Zorluk durumunu sıfırlar (yeni oyun başlangıcı).
        /// </summary>
        public void ResetDifficulty()
        {
            currentLevel = 0;
            monstersDefeatedThisLevel = 0;
        }

        /// <summary>
        /// Bir canavar yenildiğinde çağrılır. Gerekirse seviye artırır.
        /// </summary>
        public void OnMonsterDefeated()
        {
            monstersDefeatedThisLevel++;

            if (monstersDefeatedThisLevel >= monstersPerLevel)
            {
                currentLevel++;
                monstersDefeatedThisLevel = 0;
                Debug.Log($"[DifficultyManager] Seviye arttı: {currentLevel}");
            }
        }

        /// <summary>
        /// Mevcut zorluk seviyesine göre canavar yaklaşma süresini hesaplar.
        /// </summary>
        /// <returns>Saniye cinsinden izin verilen süre.</returns>
        public float GetCurrentTime()
        {
            // Oyun süresini sabit 2 dakika tut.
            return 120f;
        }

        /// <summary>
        /// Mevcut zorluk seviyesine göre şekil doğrulama eşik değerini hesaplar.
        /// </summary>
        /// <returns>RMSE eşik değeri (düşük = daha kesin çizim gerekir).</returns>
        public float GetCurrentThreshold()
        {
            float threshold = baseThreshold * Mathf.Pow(thresholdMultiplier, currentLevel);
            return Mathf.Max(threshold, minThreshold);
        }

        /// <summary>
        /// Mevcut zorluk seviyesine göre canavar hareket hızını hesaplar.
        /// </summary>
        /// <returns>Birim/saniye cinsinden hız.</returns>
        public float GetCurrentMonsterSpeed(float baseSpeed)
        {
            // Süre kısaldıkça hız artmalı (mesafe / süre)
            float currentTime = GetCurrentTime();
            // Ekran genişliği referans olarak ~10 birim varsayılır
            return 10f / currentTime;
        }
    }
}
