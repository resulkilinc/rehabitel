// ============================================================
// SignalFilter.cs — REHABIT-EL
// EMA (Exponential Moving Average) + Deadband filtre.
// Flex sensör gürültüsünü azaltır.
// ============================================================
using UnityEngine;

namespace RehabitEL.Calibration
{
    /// <summary>
    /// Sinyal filtresi: EMA (yumuşatma) + Deadband (küçük değişimleri yok sayma).
    /// Flex sensörden gelen analog gürültüyü azaltmak için kullanılır.
    /// </summary>
    public class SignalFilter : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("EMA (Exponential Moving Average)")]
        [Tooltip("Yumuşatma faktörü (0-1). Düşük = daha yumuşak, Yüksek = daha duyarlı")]
        [Range(0.01f, 1f)]
        [SerializeField] private float alpha = 0.3f;

        [Header("Deadband")]
        [Tooltip("Bu eşik altındaki değişimler yoksayılır (titreme önleme)")]
        [Range(0f, 0.1f)]
        [SerializeField] private float deadband = 0.02f;

        // ── Durum ──
        private float previousFiltered = 0f;
        private float lastOutputValue = 0f;
        private bool isFirstSample = true;

        // ── Properties ──
        /// <summary>Son filtreli değer.</summary>
        public float LastFilteredValue => lastOutputValue;

        /// <summary>EMA alpha parametresi.</summary>
        public float Alpha
        {
            get => alpha;
            set => alpha = Mathf.Clamp(value, 0.01f, 1f);
        }

        /// <summary>Deadband eşik değeri.</summary>
        public float Deadband
        {
            get => deadband;
            set => deadband = Mathf.Clamp(value, 0f, 0.1f);
        }

        // ════════════════════════════════════════════

        /// <summary>
        /// Filtreyi uygular ve filtreli değeri döndürür.
        /// </summary>
        /// <param name="rawValue">Ham (normalize) giriş değeri (0-1).</param>
        /// <returns>Filtrelenmiş değer (0-1).</returns>
        public float Apply(float rawValue)
        {
            // İlk örnek: doğrudan ata
            if (isFirstSample)
            {
                previousFiltered = rawValue;
                lastOutputValue = rawValue;
                isFirstSample = false;
                return rawValue;
            }

            // 1. EMA uygula
            float emaFiltered = alpha * rawValue + (1f - alpha) * previousFiltered;
            previousFiltered = emaFiltered;

            // 2. Deadband uygula
            if (Mathf.Abs(emaFiltered - lastOutputValue) > deadband)
            {
                lastOutputValue = emaFiltered;
            }

            return lastOutputValue;
        }

        /// <summary>
        /// Filtreyi sıfırlar (yeni seans veya kalibrasyon sonrası).
        /// </summary>
        public void Reset()
        {
            previousFiltered = 0f;
            lastOutputValue = 0f;
            isFirstSample = true;
        }
    }
}
