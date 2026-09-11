// ============================================================
// SerialFlexInputProvider.cs — REHABIT-EL
// ESP32 üzerinden USB Serial ile flex sensör verisi okuma.
// Ayrı thread'de okur, ana thread'e ConcurrentQueue ile aktarır.
// ============================================================
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using RehabitEL.Calibration;

namespace RehabitEL.Input
{
    /// <summary>
    /// ESP32'den USB Serial bağlantısıyla flex sensör verisini okur.
    /// Ayrı thread kullanarak ana oyun döngüsünü bloklamadan çalışır.
    /// Kalibrasyon ve filtre uygulanarak normalize değer sağlar.
    /// </summary>
    public class SerialFlexInputProvider : MonoBehaviour, IInputProvider
    {
        // ── Inspector Ayarları ──
        [Header("Serial Ayarları")]
        [Tooltip("Serial port adı (macOS: /dev/tty.usbserial-XXXX veya /dev/cu.SLAB_USBtoUART)")]
        [SerializeField] private string portName = "/dev/tty.usbserial-0001";

        [Tooltip("Baud rate (ESP32 varsayılan: 115200)")]
        [SerializeField] private int baudRate = 115200;

        [Tooltip("Okuma zaman aşımı (ms)")]
        [SerializeField] private int readTimeout = 100;

        [Header("Rail Drawing Ayarları")]
        [Tooltip("X ekseninin ilerleme hızı (birim/saniye) — çizim sırasında")]
        [SerializeField] private float xAdvanceSpeed = 1f;

        [Tooltip("Çizim alanı Y alt sınırı")]
        [SerializeField] private float yMin = -3f;

        [Tooltip("Çizim alanı Y üst sınırı")]
        [SerializeField] private float yMax = 3f;

        [Tooltip("Çizim alanı X başlangıç")]
        [SerializeField] private float xStart = -3f;

        [Tooltip("Çizim alanı X bitiş")]
        [SerializeField] private float xEnd = 3f;

        [Header("Çizim Tetikleme")]
        [Tooltip("Bu flex değerinin üzerinde çizim başlar (0-1)")]
        [SerializeField] private float drawThreshold = 0.15f;

        // ── Serial okuma ──
        #if UNITY_STANDALONE || UNITY_EDITOR
        private System.IO.Ports.SerialPort serialPort;
        #endif

        private Thread readThread;
        private ConcurrentQueue<float> valueQueue = new ConcurrentQueue<float>();
        private volatile bool isRunning = false;

        // ── Durum ──
        private float rawFlexValue = 0f;
        private float filteredFlexValue = 0f;
        private float currentX;
        private bool isSerialDrawing = false;
        private bool isConnected = false;
        private SignalFilter signalFilter;

        // ── IInputProvider ──
        public string ProviderName => "Serial (ESP32)";
        public bool IsConnected => isConnected;
        public bool IsDrawing => isSerialDrawing;

        // ════════════════════════════════════════════

        public void Initialize()
        {
            currentX = xStart;
            signalFilter = GetComponent<SignalFilter>();
            if (signalFilter == null)
            {
                signalFilter = gameObject.AddComponent<SignalFilter>();
            }

            OpenSerialPort();

            if (isConnected)
            {
                StartReadThread();
            }
        }

        public void Shutdown()
        {
            StopReadThread();
            CloseSerialPort();
            Debug.Log("[SerialFlexInputProvider] Durduruldu");
        }

        private void Update()
        {
            if (!isConnected) return;

            // Queue'dan son değeri al
            float lastValue = rawFlexValue;
            while (valueQueue.TryDequeue(out float value))
            {
                lastValue = value;
            }
            rawFlexValue = lastValue;

            // Kalibrasyon uygula
            float calibrated = rawFlexValue;
            if (CalibrationManager.Instance != null && CalibrationManager.Instance.IsCalibrated)
            {
                calibrated = CalibrationManager.Instance.NormalizeValue(rawFlexValue);
            }

            // Filtre uygula
            if (signalFilter != null)
            {
                filteredFlexValue = signalFilter.Apply(calibrated);
            }
            else
            {
                filteredFlexValue = calibrated;
            }

            // Çizim tetikleme (flex değeri eşik üzerindeyse)
            bool shouldDraw = filteredFlexValue > drawThreshold;

            if (shouldDraw && !isSerialDrawing)
            {
                isSerialDrawing = true;
                currentX = xStart;
            }
            else if (!shouldDraw && isSerialDrawing)
            {
                isSerialDrawing = false;
            }

            // X ilerle
            if (isSerialDrawing)
            {
                currentX += xAdvanceSpeed * Time.deltaTime;
                if (currentX > xEnd)
                {
                    isSerialDrawing = false;
                }
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        public Vector2 GetDrawPosition()
        {
            float y = Mathf.Lerp(yMin, yMax, filteredFlexValue);
            return new Vector2(currentX, y);
        }

        public float GetRawFlexValue()
        {
            return rawFlexValue;
        }

        public float GetFilteredFlexValue()
        {
            return filteredFlexValue;
        }

        // ════════════════════════════════════════════
        // Serial Port Yönetimi
        // ════════════════════════════════════════════

        private void OpenSerialPort()
        {
            #if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                serialPort = new System.IO.Ports.SerialPort(portName, baudRate);
                serialPort.ReadTimeout = readTimeout;
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;
                serialPort.Open();
                isConnected = true;
                Debug.Log($"[SerialFlexInputProvider] Port açıldı: {portName} @ {baudRate}bps");
            }
            catch (Exception e)
            {
                isConnected = false;
                Debug.LogWarning($"[SerialFlexInputProvider] Port açılamadı: {e.Message}");
                Debug.LogWarning("[SerialFlexInputProvider] Mouse veya Simulator moduna geçmeniz önerilir.");
            }
            #else
            Debug.LogWarning("[SerialFlexInputProvider] Serial port bu platformda desteklenmiyor.");
            isConnected = false;
            #endif
        }

        private void CloseSerialPort()
        {
            #if UNITY_STANDALONE || UNITY_EDITOR
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.Close();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SerialFlexInputProvider] Port kapatma hatası: {e.Message}");
                }
            }
            isConnected = false;
            #endif
        }

        // ════════════════════════════════════════════
        // Okuma Thread'i
        // ════════════════════════════════════════════

        private void StartReadThread()
        {
            isRunning = true;
            readThread = new Thread(ReadLoop);
            readThread.IsBackground = true;
            readThread.Start();
            Debug.Log("[SerialFlexInputProvider] Okuma thread'i başlatıldı");
        }

        private void StopReadThread()
        {
            isRunning = false;
            if (readThread != null && readThread.IsAlive)
            {
                readThread.Join(500); // 500ms bekle
            }
        }

        /// <summary>
        /// Ayrı thread'de çalışan serial okuma döngüsü.
        /// ESP32'den gelen veriyi parse edip queue'ya ekler.
        /// </summary>
        private void ReadLoop()
        {
            #if UNITY_STANDALONE || UNITY_EDITOR
            while (isRunning && serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    string line = serialPort.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        line = line.Trim();

                        // ESP32'den gelen format: "FLEX:1234" veya sadece "1234"
                        string valueStr = line;
                        if (line.StartsWith("FLEX:"))
                        {
                            valueStr = line.Substring(5);
                        }

                        if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float value))
                        {
                            // ESP32 ADC: 0-4095 → 0.0-1.0 normalize
                            if (value > 1f)
                            {
                                value = value / 4095f;
                            }

                            valueQueue.Enqueue(value);
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Zaman aşımı normal, devam et
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning($"[SerialFlexInputProvider] Okuma hatası: {e.Message}");
                    }
                }
            }
            #endif
        }
    }
}
