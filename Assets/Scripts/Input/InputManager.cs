// ============================================================
// InputManager.cs — REHABIT-EL
// Input provider seçimi ve yönetimi. Runtime'da provider değiştirilebilir.
// ============================================================
using UnityEngine;

namespace RehabitEL.Input
{
    /// <summary>
    /// Kullanılabilir giriş modları.
    /// </summary>
    public enum InputMode
    {
        Mouse,       // Fare girişi (geliştirme/demo)
        Simulator,   // Klavye simülatörü (sensörsüz test)
        Serial,      // ESP32 USB Serial (gerçek sensör)
        Playback,    // CSV oynatma (kayıt tekrarı)
        WatchHttp    // Apple Watch HTTP (accelerometer)
    }

    /// <summary>
    /// Aktif input provider'ı yöneten singleton.
    /// Runtime'da farklı provider'lar arasında geçiş yapılabilir.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // ── Singleton ──
        public static InputManager Instance { get; private set; }

        // ── Inspector Ayarları ──
        [Header("Giriş Modu")]
        [Tooltip("Aktif giriş modu")]
        [SerializeField] private InputMode currentMode = InputMode.WatchHttp;

        [Header("Provider Referansları (opsiyonel — yoksa otomatik oluşturulur)")]
        [SerializeField] private MouseInputProvider mouseProvider;
        [SerializeField] private SimulatorInputProvider simulatorProvider;
        [SerializeField] private SerialFlexInputProvider serialProvider;
        [SerializeField] private PlaybackInputProvider playbackProvider;
        [SerializeField] private WatchHttpInputProvider watchHttpProvider;

        // ── Durum ──
        private IInputProvider activeProvider;

        // ── Properties ──
        /// <summary>Aktif input provider.</summary>
        public IInputProvider ActiveProvider => activeProvider;

        /// <summary>Aktif giriş modu.</summary>
        public InputMode CurrentMode => currentMode;

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

            EnsureProviders();
        }

        private void Start()
        {
            // Bu proje akışında watch ile test öncelikli olduğu için başlangıçta WatchHttp'a zorla.
            SetInputMode(InputMode.WatchHttp);
        }

        private void OnDestroy()
        {
            if (activeProvider != null)
            {
                activeProvider.Shutdown();
            }
        }

        // ════════════════════════════════════════════
        // Provider Yönetimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Provider component'lerinin varlığını kontrol eder, yoksa oluşturur.
        /// </summary>
        private void EnsureProviders()
        {
            if (mouseProvider == null)
                mouseProvider = GetComponent<MouseInputProvider>() ?? gameObject.AddComponent<MouseInputProvider>();

            if (simulatorProvider == null)
                simulatorProvider = GetComponent<SimulatorInputProvider>() ?? gameObject.AddComponent<SimulatorInputProvider>();

            if (serialProvider == null)
                serialProvider = GetComponent<SerialFlexInputProvider>() ?? gameObject.AddComponent<SerialFlexInputProvider>();

            if (playbackProvider == null)
                playbackProvider = GetComponent<PlaybackInputProvider>() ?? gameObject.AddComponent<PlaybackInputProvider>();

            if (watchHttpProvider == null)
                watchHttpProvider = GetComponent<WatchHttpInputProvider>() ?? gameObject.AddComponent<WatchHttpInputProvider>();
        }

        /// <summary>
        /// Giriş modunu değiştirir. Eski provider durdurulur, yeni başlatılır.
        /// </summary>
        /// <param name="mode">Yeni giriş modu.</param>
        public void SetInputMode(InputMode mode)
        {
            // Eski provider'ı durdur
            if (activeProvider != null)
            {
                activeProvider.Shutdown();
            }

            currentMode = mode;

            // Yeni provider'ı seç
            switch (mode)
            {
                case InputMode.Mouse:
                    activeProvider = mouseProvider;
                    break;
                case InputMode.Simulator:
                    activeProvider = simulatorProvider;
                    break;
                case InputMode.Serial:
                    activeProvider = serialProvider;
                    break;
                case InputMode.Playback:
                    activeProvider = playbackProvider;
                    break;
                case InputMode.WatchHttp:
                    activeProvider = watchHttpProvider;
                    break;
                default:
                    activeProvider = mouseProvider;
                    break;
            }

            if (activeProvider == null)
            {
                Debug.LogWarning($"[InputManager] {mode} provider bulunamadı, Mouse provider'a düşülüyor.");
                activeProvider = mouseProvider ?? GetComponent<MouseInputProvider>() ?? gameObject.AddComponent<MouseInputProvider>();
                currentMode = InputMode.Mouse;
            }

            // Başlat
            activeProvider.Initialize();
            Debug.Log($"[InputManager] Giriş modu değiştirildi: {currentMode} ({activeProvider.ProviderName})");
        }

        /// <summary>
        /// Bir sonraki giriş moduna geçer (döngüsel).
        /// Debug amaçlı, runtime'da F1 ile kullanılabilir.
        /// </summary>
        public void CycleInputMode()
        {
            int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(InputMode)).Length;
            SetInputMode((InputMode)nextMode);
        }

        // ════════════════════════════════════════════
        // Kolay Erişim Metotları
        // ════════════════════════════════════════════

        /// <summary>Aktif provider'dan çizim durumu.</summary>
        public bool IsDrawing => activeProvider?.IsDrawing ?? false;

        /// <summary>Aktif provider'dan çizim pozisyonu.</summary>
        public Vector2 GetDrawPosition()
        {
            return activeProvider?.GetDrawPosition() ?? Vector2.zero;
        }

        /// <summary>Aktif provider'dan ham flex değeri.</summary>
        public float GetRawFlexValue()
        {
            return activeProvider?.GetRawFlexValue() ?? 0f;
        }

        /// <summary>Aktif provider'dan filtreli flex değeri.</summary>
        public float GetFilteredFlexValue()
        {
            return activeProvider?.GetFilteredFlexValue() ?? 0f;
        }

        /// <summary>Aktif provider bağlı mı?</summary>
        public bool IsProviderConnected()
        {
            return activeProvider?.IsConnected ?? false;
        }
    }
}
