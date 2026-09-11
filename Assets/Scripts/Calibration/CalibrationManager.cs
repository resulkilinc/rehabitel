// ============================================================
// CalibrationManager.cs — REHABIT-EL
// Flex sensör kalibrasyonu: min/max yakalama, normalize, kaydetme.
// ============================================================
using UnityEngine;

namespace RehabitEL.Calibration
{
    /// <summary>
    /// Flex sensör kalibrasyonu yöneticisi.
    /// 1. "Parmağınızı düz tutun" → 3 sn min değer ortalaması
    /// 2. "Parmağınızı max bükün" → 3 sn max değer ortalaması
    /// 3. Normalize: (raw - min) / (max - min) → 0..1
    /// Kalibrasyon değerleri PlayerPrefs'te saklanır.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        // ── Singleton ──
        public static CalibrationManager Instance { get; private set; }

        // ── Inspector Ayarları ──
        [Header("Kalibrasyon Ayarları")]
        [Tooltip("Örnek alma süresi (saniye)")]
        [SerializeField] private float sampleDuration = 3f;

        // ── Kalibrasyon Durumları ──
        public enum CalibrationState
        {
            Idle,           // Bekleme
            SamplingMin,    // Min değer toplama (düz parmak)
            SamplingMax,    // Max değer toplama (bükülü parmak)
            Completed       // Kalibrasyon tamamlandı
        }

        // ── Durum ──
        private CalibrationState state = CalibrationState.Idle;
        private float minValue = 0f;
        private float maxValue = 1f;
        private float sampleSum = 0f;
        private int sampleCount = 0;
        private float sampleTimer = 0f;
        private bool isCalibrated = false;

        // ── PlayerPrefs Anahtarları ──
        private const string PREF_MIN = "RehabitEL_CalibMin";
        private const string PREF_MAX = "RehabitEL_CalibMax";
        private const string PREF_CALIBRATED = "RehabitEL_IsCalibrated";

        // ── Properties ──
        /// <summary>Mevcut kalibrasyon durumu.</summary>
        public CalibrationState State => state;

        /// <summary>Kalibrasyon tamamlandı mı?</summary>
        public bool IsCalibrated => isCalibrated;

        /// <summary>Kalibre edilmiş min değer.</summary>
        public float MinValue => minValue;

        /// <summary>Kalibre edilmiş max değer.</summary>
        public float MaxValue => maxValue;

        /// <summary>Örnek alma ilerleme yüzdesi (0-1).</summary>
        public float SampleProgress => sampleDuration > 0 ? sampleTimer / sampleDuration : 0f;

        // ── Events ──
        public event System.Action<CalibrationState> OnStateChanged;
        public event System.Action<float> OnProgressUpdated;
        public event System.Action OnCalibrationCompleted;

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
            DontDestroyOnLoad(gameObject);

            // Önceki kalibrasyonu yükle
            LoadCalibration();
        }

        private void Update()
        {
            if (state == CalibrationState.SamplingMin || state == CalibrationState.SamplingMax)
            {
                UpdateSampling();
            }
        }

        // ════════════════════════════════════════════
        // Kalibrasyon Akışı
        // ════════════════════════════════════════════

        /// <summary>
        /// Min değer kalibrasyonunu başlatır ("Parmağınızı düz tutun").
        /// </summary>
        public void StartMinCalibration()
        {
            ResetSampling();
            state = CalibrationState.SamplingMin;
            OnStateChanged?.Invoke(state);
            Debug.Log("[CalibrationManager] Min kalibrasyon başladı — Parmağınızı düz tutun");
        }

        /// <summary>
        /// Max değer kalibrasyonunu başlatır ("Parmağınızı max bükün").
        /// </summary>
        public void StartMaxCalibration()
        {
            ResetSampling();
            state = CalibrationState.SamplingMax;
            OnStateChanged?.Invoke(state);
            Debug.Log("[CalibrationManager] Max kalibrasyon başladı — Parmağınızı max bükün");
        }

        /// <summary>
        /// Örnek toplama sürecini günceller.
        /// </summary>
        private void UpdateSampling()
        {
            sampleTimer += Time.deltaTime;
            OnProgressUpdated?.Invoke(SampleProgress);

            // Input provider'dan ham değer al
            float rawValue = 0f;
            if (RehabitEL.Input.InputManager.Instance != null)
            {
                rawValue = RehabitEL.Input.InputManager.Instance.GetRawFlexValue();
            }

            sampleSum += rawValue;
            sampleCount++;

            // Süre doldu mu?
            if (sampleTimer >= sampleDuration)
            {
                float averageValue = sampleCount > 0 ? sampleSum / sampleCount : rawValue;

                if (state == CalibrationState.SamplingMin)
                {
                    minValue = averageValue;
                    Debug.Log($"[CalibrationManager] Min değer kaydedildi: {minValue:F4}");
                    state = CalibrationState.Idle;
                    OnStateChanged?.Invoke(state);
                }
                else if (state == CalibrationState.SamplingMax)
                {
                    maxValue = averageValue;
                    Debug.Log($"[CalibrationManager] Max değer kaydedildi: {maxValue:F4}");

                    // Her iki değer de alındıysa kalibrasyon tamamlandı
                    if (maxValue > minValue)
                    {
                        isCalibrated = true;
                        state = CalibrationState.Completed;
                        SaveCalibration();
                        OnStateChanged?.Invoke(state);
                        OnCalibrationCompleted?.Invoke();
                        Debug.Log($"[CalibrationManager] Kalibrasyon tamamlandı! " +
                                  $"Min: {minValue:F4}, Max: {maxValue:F4}, " +
                                  $"Aralık: {(maxValue - minValue):F4}");
                    }
                    else
                    {
                        Debug.LogWarning("[CalibrationManager] Max ≤ Min! Tekrar deneyin.");
                        state = CalibrationState.Idle;
                        OnStateChanged?.Invoke(state);
                    }
                }
            }
        }

        /// <summary>
        /// Örnek toplama değişkenlerini sıfırlar.
        /// </summary>
        private void ResetSampling()
        {
            sampleSum = 0f;
            sampleCount = 0;
            sampleTimer = 0f;
        }

        // ════════════════════════════════════════════
        // Normalizasyon
        // ════════════════════════════════════════════

        /// <summary>
        /// Ham sensör değerini kalibre edilmiş aralığa göre normalize eder.
        /// </summary>
        /// <param name="rawValue">Ham sensör değeri.</param>
        /// <returns>Normalize değer (0.0 - 1.0).</returns>
        public float NormalizeValue(float rawValue)
        {
            if (!isCalibrated || Mathf.Approximately(maxValue, minValue))
            {
                return rawValue;
            }

            float normalized = (rawValue - minValue) / (maxValue - minValue);
            return Mathf.Clamp01(normalized);
        }

        // ════════════════════════════════════════════
        // Kalıcı Kayıt (PlayerPrefs)
        // ════════════════════════════════════════════

        /// <summary>
        /// Kalibrasyon değerlerini PlayerPrefs'e kaydeder.
        /// </summary>
        private void SaveCalibration()
        {
            PlayerPrefs.SetFloat(PREF_MIN, minValue);
            PlayerPrefs.SetFloat(PREF_MAX, maxValue);
            PlayerPrefs.SetInt(PREF_CALIBRATED, 1);
            PlayerPrefs.Save();
            Debug.Log("[CalibrationManager] Kalibrasyon kaydedildi (PlayerPrefs)");
        }

        /// <summary>
        /// Önceki kalibrasyon değerlerini PlayerPrefs'ten yükler.
        /// </summary>
        private void LoadCalibration()
        {
            if (PlayerPrefs.GetInt(PREF_CALIBRATED, 0) == 1)
            {
                minValue = PlayerPrefs.GetFloat(PREF_MIN, 0f);
                maxValue = PlayerPrefs.GetFloat(PREF_MAX, 1f);
                isCalibrated = true;
                state = CalibrationState.Completed;
                Debug.Log($"[CalibrationManager] Önceki kalibrasyon yüklendi — " +
                          $"Min: {minValue:F4}, Max: {maxValue:F4}");
            }
        }

        /// <summary>
        /// Kalibrasyonu sıfırlar.
        /// </summary>
        public void ResetCalibration()
        {
            minValue = 0f;
            maxValue = 1f;
            isCalibrated = false;
            state = CalibrationState.Idle;
            PlayerPrefs.DeleteKey(PREF_MIN);
            PlayerPrefs.DeleteKey(PREF_MAX);
            PlayerPrefs.DeleteKey(PREF_CALIBRATED);
            PlayerPrefs.Save();
            OnStateChanged?.Invoke(state);
            Debug.Log("[CalibrationManager] Kalibrasyon sıfırlandı");
        }
    }
}
