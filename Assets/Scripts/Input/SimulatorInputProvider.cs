// ============================================================
// SimulatorInputProvider.cs — REHABIT-EL
// Klavye simülatörü: Ok tuşları ile flex sensor simülasyonu.
// X otomatik ilerler, Y ok tuşlarıyla kontrol edilir.
// ============================================================
using UnityEngine;

namespace RehabitEL.Input
{
    /// <summary>
    /// Klavye ile flex sensör simülasyonu sağlar.
    /// Sensör donanımı olmadan oyun test etmek için kullanılır.
    /// Yukarı/Aşağı ok tuşları → flex değeri (0-1)
    /// X ekseni zamanla otomatik ilerler (rail drawing).
    /// </summary>
    public class SimulatorInputProvider : MonoBehaviour, IInputProvider
    {
        // ── Inspector Ayarları ──
        [Header("Simülasyon Ayarları")]
        [Tooltip("Flex değerinin değişim hızı (birim/saniye)")]
        [SerializeField] private float flexChangeSpeed = 2f;

        [Tooltip("X ekseninin ilerleme hızı (birim/saniye)")]
        [SerializeField] private float xAdvanceSpeed = 1f;

        [Tooltip("Çizim alanı Y alt sınırı")]
        [SerializeField] private float yMin = -3f;

        [Tooltip("Çizim alanı Y üst sınırı")]
        [SerializeField] private float yMax = 3f;

        [Tooltip("Çizim alanı X başlangıç")]
        [SerializeField] private float xStart = -3f;

        [Tooltip("Çizim alanı X bitiş")]
        [SerializeField] private float xEnd = 3f;

        // ── Durum ──
        private float currentFlexValue = 0.5f;
        private float currentX;
        private bool isSimDrawing = false;
        private bool isInitialized = false;

        // ── IInputProvider ──
        public string ProviderName => "Simulator";
        public bool IsConnected => isInitialized;
        public bool IsDrawing => isSimDrawing;

        // ════════════════════════════════════════════

        public void Initialize()
        {
            currentFlexValue = 0.5f;
            currentX = xStart;
            isSimDrawing = false;
            isInitialized = true;
            Debug.Log("[SimulatorInputProvider] Başlatıldı");
        }

        public void Shutdown()
        {
            isInitialized = false;
            Debug.Log("[SimulatorInputProvider] Durduruldu");
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Yukarı/Aşağı ok tuşları ile flex değeri kontrol
            float input = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow))
                input = 1f;
            else if (UnityEngine.Input.GetKey(KeyCode.DownArrow))
                input = -1f;

            currentFlexValue += input * flexChangeSpeed * Time.deltaTime;
            currentFlexValue = Mathf.Clamp01(currentFlexValue);

            // Space tuşu ile çizim başlat/durdur
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                isSimDrawing = !isSimDrawing;
                if (isSimDrawing)
                {
                    currentX = xStart;
                    Debug.Log("[SimulatorInputProvider] Simülasyon çizimi başladı");
                }
                else
                {
                    Debug.Log("[SimulatorInputProvider] Simülasyon çizimi durdu");
                }
            }

            // X otomatik ilerler
            if (isSimDrawing)
            {
                currentX += xAdvanceSpeed * Time.deltaTime;
                if (currentX > xEnd)
                {
                    isSimDrawing = false; // Çizim bitti
                    Debug.Log("[SimulatorInputProvider] X sınırına ulaşıldı, çizim bitti");
                }
            }
        }

        public Vector2 GetDrawPosition()
        {
            float y = Mathf.Lerp(yMin, yMax, currentFlexValue);
            return new Vector2(currentX, y);
        }

        public float GetRawFlexValue()
        {
            return currentFlexValue;
        }

        public float GetFilteredFlexValue()
        {
            return currentFlexValue; // Simülatörde gürültü yok, filtre gereksiz
        }
    }
}
