// ============================================================
// MouseInputProvider.cs — REHABIT-EL
// Fare girişi sağlayıcı: Sol tuş basılıyken çizim, pozisyon takibi.
// ============================================================
using UnityEngine;

namespace RehabitEL.Input
{
    /// <summary>
    /// Fare girişini IInputProvider arayüzü üzerinden sağlar.
    /// Geliştirme ve demo amaçlıdır.
    /// </summary>
    public class MouseInputProvider : MonoBehaviour, IInputProvider
    {
        // ── Durum ──
        private Camera mainCamera;
        private bool isInitialized = false;

        // ── IInputProvider ──
        public string ProviderName => "Mouse";
        public bool IsConnected => isInitialized;
        public bool IsDrawing => UnityEngine.Input.GetMouseButton(0);

        // ════════════════════════════════════════════

        public void Initialize()
        {
            mainCamera = Camera.main;
            isInitialized = true;
            Debug.Log("[MouseInputProvider] Başlatıldı");
        }

        public void Shutdown()
        {
            isInitialized = false;
            Debug.Log("[MouseInputProvider] Durduruldu");
        }

        public Vector2 GetDrawPosition()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return Vector2.zero;
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            return new Vector2(worldPos.x, worldPos.y);
        }

        /// <summary>
        /// Fare modunda flex değeri yoktur, Y pozisyonu normalize edilir.
        /// </summary>
        public float GetRawFlexValue()
        {
            if (mainCamera == null) return 0.5f;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            // Ekran Y'sini 0-1 aralığına normalize et
            float screenHeight = mainCamera.orthographicSize * 2f;
            float normalized = (worldPos.y + mainCamera.orthographicSize) / screenHeight;
            return Mathf.Clamp01(normalized);
        }

        public float GetFilteredFlexValue()
        {
            // Fare modunda filtreleme gerekmez
            return GetRawFlexValue();
        }
    }
}
