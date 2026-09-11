// ============================================================
// ScreenShake.cs — REHABIT-EL
// Gelişmiş kamera sarsıntı sistemi (Perlin noise tabanlı).
// ============================================================
using System.Collections;
using UnityEngine;

namespace RehabitEL.Effects
{
    /// <summary>
    /// Perlin noise tabanlı doğal kamera sarsıntı yöneticisi.
    /// Farklı şiddet seviyeleri ve yumuşak geçişler sağlar.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        // ── Singleton ──
        public static ScreenShake Instance { get; private set; }

        // ── Durum ──
        private Camera mainCamera;
        private Vector3 originalPosition;
        private bool isShaking = false;
        private float currentShakeAmount;
        private float currentShakeDuration;
        private float shakeElapsed;
        private float noiseSeed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalPosition = mainCamera.transform.position;
            }
            noiseSeed = Random.Range(0f, 100f);
        }

        private void LateUpdate()
        {
            if (!isShaking || mainCamera == null) return;

            shakeElapsed += Time.unscaledDeltaTime;

            if (shakeElapsed >= currentShakeDuration)
            {
                isShaking = false;
                mainCamera.transform.position = originalPosition;
                return;
            }

            // Fade-out: sarsıntı şiddeti zamanla azalır
            float decay = 1f - (shakeElapsed / currentShakeDuration);
            float amount = currentShakeAmount * decay;

            // Perlin noise ile doğal sarsıntı
            float speed = 25f;
            float offsetX = (Mathf.PerlinNoise(noiseSeed, Time.unscaledTime * speed) - 0.5f) * 2f * amount;
            float offsetY = (Mathf.PerlinNoise(noiseSeed + 100f, Time.unscaledTime * speed) - 0.5f) * 2f * amount;

            mainCamera.transform.position = originalPosition + new Vector3(offsetX, offsetY, 0f);
        }

        // ════════════════════════════════════════════
        // Genel Arayüz
        // ════════════════════════════════════════════

        /// <summary>Hafif sarsıntı (başarı geri bildirimi).</summary>
        public void ShakeLight()
        {
            Shake(0.08f, 0.2f);
        }

        /// <summary>Orta sarsıntı (büyü çarpması).</summary>
        public void ShakeMedium()
        {
            Shake(0.15f, 0.3f);
        }

        /// <summary>Güçlü sarsıntı (başarısızlık / game over).</summary>
        public void ShakeHeavy()
        {
            Shake(0.25f, 0.5f);
        }

        /// <summary>
        /// Özel şiddet ve süre ile sarsıntı başlatır.
        /// </summary>
        /// <param name="amount">Sarsıntı şiddeti (birim cinsinden).</param>
        /// <param name="duration">Sarsıntı süresi (saniye).</param>
        public void Shake(float amount, float duration)
        {
            if (mainCamera == null) return;

            // Zaten sarsılıyorsa, daha güçlü olanı uygula
            if (isShaking && amount <= currentShakeAmount) return;

            if (!isShaking)
            {
                originalPosition = mainCamera.transform.position;
            }

            currentShakeAmount = amount;
            currentShakeDuration = duration;
            shakeElapsed = 0f;
            isShaking = true;
            noiseSeed = Random.Range(0f, 100f);
        }

        /// <summary>Sarsıntıyı anında durdurur.</summary>
        public void StopShake()
        {
            if (mainCamera != null)
            {
                mainCamera.transform.position = originalPosition;
            }
            isShaking = false;
        }
    }
}
