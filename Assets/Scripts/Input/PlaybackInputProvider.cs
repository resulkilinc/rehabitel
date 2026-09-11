// ============================================================
// PlaybackInputProvider.cs — REHABIT-EL
// CSV dosyasından kaydedilmiş flex verisi oynatma.
// Test ve demo amaçlıdır.
// ============================================================
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RehabitEL.Input
{
    /// <summary>
    /// Daha önce kaydedilmiş CSV dosyasından flex sensör verisi oynatır.
    /// Test, demo ve analiz amaçlıdır.
    /// CSV formatı: timestamp_ms,flex_value (her satırda)
    /// </summary>
    public class PlaybackInputProvider : MonoBehaviour, IInputProvider
    {
        // ── Inspector Ayarları ──
        [Header("Dosya Ayarları")]
        [Tooltip("Oynatılacak CSV dosya yolu (Assets/... veya mutlak yol)")]
        [SerializeField] private string csvFilePath = "";

        [Header("Oynatma Ayarları")]
        [Tooltip("Oynatma hızı çarpanı (1 = gerçek zamanlı, 2 = 2x hızlı)")]
        [SerializeField] private float playbackSpeed = 1f;

        [Tooltip("Döngü modu (bittikten sonra başa dön)")]
        [SerializeField] private bool loopPlayback = false;

        [Header("Rail Drawing Ayarları")]
        [SerializeField] private float xAdvanceSpeed = 1f;
        [SerializeField] private float yMin = -3f;
        [SerializeField] private float yMax = 3f;
        [SerializeField] private float xStart = -3f;
        [SerializeField] private float xEnd = 3f;
        [SerializeField] private float drawThreshold = 0.15f;

        // ── Veri ──
        private List<PlaybackFrame> frames = new List<PlaybackFrame>();
        private int currentFrameIndex = 0;
        private float playbackTimer = 0f;

        // ── Durum ──
        private float currentFlexValue = 0f;
        private float currentX;
        private bool isPlaybackDrawing = false;
        private bool isInitialized = false;
        private bool hasData = false;

        // ── IInputProvider ──
        public string ProviderName => "Playback (CSV)";
        public bool IsConnected => isInitialized && hasData;
        public bool IsDrawing => isPlaybackDrawing;

        // ── Veri yapısı ──
        private struct PlaybackFrame
        {
            public float timestampMs;
            public float flexValue;
        }

        // ════════════════════════════════════════════

        public void Initialize()
        {
            currentX = xStart;
            currentFrameIndex = 0;
            playbackTimer = 0f;
            isInitialized = true;

            if (!string.IsNullOrEmpty(csvFilePath))
            {
                LoadCSV(csvFilePath);
            }
            else
            {
                Debug.LogWarning("[PlaybackInputProvider] CSV dosya yolu belirtilmemiş.");
                hasData = false;
            }

            Debug.Log($"[PlaybackInputProvider] Başlatıldı — {frames.Count} frame yüklendi");
        }

        public void Shutdown()
        {
            isInitialized = false;
            frames.Clear();
            Debug.Log("[PlaybackInputProvider] Durduruldu");
        }

        private void Update()
        {
            if (!isInitialized || !hasData) return;

            // Zaman ilerlet
            playbackTimer += Time.deltaTime * 1000f * playbackSpeed; // ms cinsinden

            // Mevcut frame'i bul
            while (currentFrameIndex < frames.Count - 1 &&
                   frames[currentFrameIndex + 1].timestampMs <= playbackTimer)
            {
                currentFrameIndex++;
            }

            // Değeri al
            if (currentFrameIndex < frames.Count)
            {
                currentFlexValue = frames[currentFrameIndex].flexValue;
            }

            // Son frame'e ulaşıldı mı?
            if (currentFrameIndex >= frames.Count - 1)
            {
                if (loopPlayback)
                {
                    currentFrameIndex = 0;
                    playbackTimer = 0f;
                    Debug.Log("[PlaybackInputProvider] Döngü: başa dönüldü");
                }
            }

            // Çizim tetikleme
            bool shouldDraw = currentFlexValue > drawThreshold;
            if (shouldDraw && !isPlaybackDrawing)
            {
                isPlaybackDrawing = true;
                currentX = xStart;
            }
            else if (!shouldDraw && isPlaybackDrawing)
            {
                isPlaybackDrawing = false;
            }

            if (isPlaybackDrawing)
            {
                currentX += xAdvanceSpeed * Time.deltaTime;
                if (currentX > xEnd) isPlaybackDrawing = false;
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
            return currentFlexValue; // Kayıtlı veri zaten filtrelenmiş olabilir
        }

        // ════════════════════════════════════════════
        // CSV Yükleme
        // ════════════════════════════════════════════

        /// <summary>
        /// CSV dosyasını yükler.
        /// Format: timestamp_ms,flex_value (başlık satırı opsiyonel)
        /// </summary>
        private void LoadCSV(string path)
        {
            frames.Clear();

            try
            {
                // Mutlak veya göreceli yol kontrolü
                string fullPath;
                if (Path.IsPathRooted(path))
                {
                    fullPath = path;
                }
                else
                {
                    fullPath = Path.Combine(Application.dataPath, path);
                }

                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"[PlaybackInputProvider] Dosya bulunamadı: {fullPath}");
                    hasData = false;
                    return;
                }

                string[] lines = File.ReadAllLines(fullPath);

                foreach (string line in lines)
                {
                    // Başlık satırını atla
                    if (line.StartsWith("timestamp") || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Trim().Split(',');
                    if (parts.Length >= 2)
                    {
                        if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float ts) &&
                            float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float val))
                        {
                            frames.Add(new PlaybackFrame
                            {
                                timestampMs = ts,
                                flexValue = Mathf.Clamp01(val)
                            });
                        }
                    }
                }

                hasData = frames.Count > 0;
                Debug.Log($"[PlaybackInputProvider] {frames.Count} frame yüklendi: {fullPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlaybackInputProvider] CSV yükleme hatası: {e.Message}");
                hasData = false;
            }
        }

        /// <summary>
        /// Runtime'da farklı bir CSV dosyası yükler.
        /// </summary>
        public void LoadNewFile(string path)
        {
            csvFilePath = path;
            currentFrameIndex = 0;
            playbackTimer = 0f;
            LoadCSV(path);
        }
    }
}
