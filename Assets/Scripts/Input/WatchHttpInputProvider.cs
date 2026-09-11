// ============================================================
// WatchHttpInputProvider.cs — REHABIT-EL
// Apple Watch (Sensor Logger) -> HTTP POST -> hava çizimi imleci.
//
// MİMARİ (neden böyle):
//  1) IMU FÜZYONU (complementary filter): Ham yerçekimi farkı yerine, gerçek
//     EĞİM AÇILARI (roll/pitch) hesaplanır. Açı = atan2(...) olduğundan birim
//     ve doygunluk sorunu yoktur (±90° sınırlı). Jiroskop kısa vadede akıcılık
//     ve tepki, yerçekimi uzun vadede sürüklenmesizlik sağlar:
//        roll  = α(roll  + gyroX·dt) + (1-α)·atan2(gy, gz)
//        pitch = α(pitch + gyroY·dt) + (1-α)·atan2(-gx, √(gy²+gz²))
//     Füzyon listener thread'inde, örnek zaman damgalarıyla yapılır.
//  2) JITTER BUFFER: Sensor Logger ücretsiz sürümde veriyi ~1 sn'lik paketler
//     halinde yollar. Üretilen açı örnekleri zaman damgasıyla tamponlanıp
//     zamana yayılmış şekilde oynatılır -> şekil bozulmadan akıcı çizim.
//  3) AÇI -> KONUM: imleç konumu = (açı - stroke_merkez) · ölçek.
//     STROKE-BAŞINA MERKEZLEME: çizim başladığı an oryantasyon "merkez" kabul
//     edilir; böylece şekil her yöne (±) merkezden açılır, ekran kenarına
//     sıkışmaz. Gating (çiz/durdur) ayrı bir "dinlenme" referansından çalışır.
// ============================================================
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace RehabitEL.Input
{
    /// <summary>
    /// Apple Watch'tan gelen ivme + jiroskop verisini complementary filter ile
    /// füzyonlayıp eğim açılarına dayalı bir hava-çizim imleci üretir.
    /// </summary>
    public class WatchHttpInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("HTTP Ayarları")]
        [SerializeField] private int port = 8000;
        [SerializeField] private string endpointPath = "/data";
        [Tooltip("Bu süre boyunca paket gelmezse bağlantı kopmuş sayılır (sn)")]
        [SerializeField] private float disconnectTimeoutSeconds = 3f;

        [Header("IMU Füzyon (Complementary Filter)")]
        [Tooltip("Jiroskop güveni (0.95-0.99). Yüksek = akıcı/tepkili, düşük = ivme baskın")]
        [SerializeField] private float fusionAlpha = 0.97f;
        [Tooltip("Jiroskop roll ekseni işareti (+1/-1). Yön ters dönerse çevir")]
        [SerializeField] private float gyroSignRoll = 1f;
        [Tooltip("Jiroskop pitch ekseni işareti (+1/-1)")]
        [SerializeField] private float gyroSignPitch = 1f;

        [Header("Açı -> Konum Haritası")]
        [Tooltip("Radyan başına oyun birimi. ~0.75rad (43°) -> kenar için ~4")]
        [SerializeField] private float angleScale = 4f;
        [Tooltip("Bu açının altındaki titreşim yok sayılır (rad)")]
        [SerializeField] private float deadzoneRad = 0.02f;
        [Tooltip("İmlecin hedefe yumuşatma hızı. Düşük = akıcı/pürüzsüz eğri, yüksek = anlık/titreşimli.")]
        [SerializeField] private float positionFollowSpeed = 6f;

        [Header("Jitter Buffer (Akıcı Oynatma)")]
        [Tooltip("Tampondaki hedef gecikme (sn). Düşük = az gecikme. Sensor Logger batch'i 200ms ise 0.25-0.4 idealdir.")]
        [SerializeField] private float targetBufferDelay = 0.4f;
        [Tooltip("Bu kadar geri kalınırsa eski örnekler atlanır (sn)")]
        [SerializeField] private float maxBufferLag = 1.5f;

        [Header("Çizim Tetikleme (HAREKETE dayalı)")]
        [Tooltip("Bu açısal hızın üzerinde hareket -> çizim BAŞLAR (rad/sn)")]
        [SerializeField] private float startMoveSpeed = 0.15f;
        [Tooltip("Bu açısal hızın altına inince çizim durmaya başlar (rad/sn). Histerezis için start'tan küçük.")]
        [SerializeField] private float stopMoveSpeed = 0.08f;
        [Tooltip("Hız eşik altında bu kadar süre kalınca çizim BİTER (sn). Kare/üçgen köşe duraksamalarını yutar.")]
        [SerializeField] private float stopDelaySeconds = 0.9f;
        [Tooltip("Açısal hız yumuşatma katsayısı (yüksek = daha anlık tepki)")]
        [SerializeField] private float speedSmoothing = 15f;

        [Header("Canvas Sınırları")]
        [SerializeField] private float xMin = -3f;
        [SerializeField] private float xMax = 3f;
        [SerializeField] private float yMin = -3f;
        [SerializeField] private float yMax = 3f;

        [Header("Eksen Ayarı")]
        [Tooltip("Yatay ekseni ters çevir")]
        [SerializeField] private bool invertX = false;
        [Tooltip("Dikey ekseni ters çevir")]
        [SerializeField] private bool invertY = false;
        [Tooltip("Roll/pitch eksenlerini yer değiştir (yatay<->dikey)")]
        [SerializeField] private bool swapAxes = false;

        [Header("Teşhis (test sonrası kapat)")]
        [SerializeField] private bool enableDebugLog = true;
        private float debugTimer;
        private int totalPacketsReceived;
        private int totalSamplesReceived;
        private int totalGyroSamples;
        private string lastSensorSource = "(yok)";
        private bool lastGyroPresent;

        // ── Açı örneği (roll, pitch) zaman damgalı ──
        private struct AngleSample
        {
            public double timeSec;
            public float roll;   // x ekseni etrafı (rad)
            public float pitch;  // y ekseni etrafı (rad)
        }

        // ── Ham birleşik olay (füzyon girişi) ──
        private struct RawEvent
        {
            public double timeSec;
            public bool isGyro;
            public float x;
            public float y;
            public float z;
        }

        // ── Regex'ler ──
        // Açı kaynağı önceliği: gravity > accelerometeruncalibrated > accelerometer
        private static readonly Regex GravityRegex = BuildBlockRegex("gravity");
        private static readonly Regex AccelUncalibratedRegex = BuildBlockRegex("accelerometeruncalibrated");
        private static readonly Regex AccelerometerRegex = BuildBlockRegex("accelerometer");
        // Jiroskop önceliği: gyroscope > gyroscopeuncalibrated
        private static readonly Regex GyroscopeRegex = BuildBlockRegex("gyroscope");
        private static readonly Regex GyroUncalibratedRegex = BuildBlockRegex("gyroscopeuncalibrated");

        private static readonly Regex AxisXYZRegex = new Regex(
            "\"(?<axis>[xyz])\"\\s*:\\s*(?<value>[-0-9.eE]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Regex BuildBlockRegex(string sensorName)
        {
            return new Regex(
                "\"name\"\\s*:\\s*\"" + Regex.Escape(sensorName) +
                "\"\\s*,\\s*\"time\"\\s*:\\s*(?<time>\\d+)\\s*,\\s*\"values\"\\s*:\\s*\\{(?<values>[^}]*)\\}",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        // ── Ağ / thread ──
        private HttpListener httpListener;
        private Thread listenerThread;
        private volatile bool isRunning;
        private readonly ConcurrentQueue<AngleSample> netQueue = new ConcurrentQueue<AngleSample>();

        // ── Complementary filter durumu (yalnız listener thread'i kullanır) ──
        private double filterRoll;
        private double filterPitch;
        private double filterLastT;
        private bool filterInitialized;

        // ── Oynatma tamponu (ana thread) ──
        private readonly List<AngleSample> buffer = new List<AngleSample>();
        private double playbackTime;
        private bool playbackInitialized;

        // ── Durum ──
        private Vector2 baseline;       // dinlenme (rest) açıları — yalnız GATING (çiz/durdur)
        private bool baselineInitialized;
        private Vector2 strokeBaseline; // stroke başında yakalanan merkez — POZİSYON haritası için
        private Vector2 drawPosition = Vector2.zero;
        private bool isDrawing;
        private bool isConnected;
        private float stationaryTimer;
        private float tiltMagnitude;
        private Vector2 prevAngles;     // bir önceki kare açıları (hız için)
        private bool prevAnglesValid;
        private float angularSpeed;     // yumuşatılmış açısal hız (rad/sn) — gating sinyali
        private float lastReceiveRealtime = -999f;

        public string ProviderName => "Watch HTTP (IMU Fusion)";
        public bool IsConnected => isConnected;
        public bool IsDrawing => isDrawing;

        // ════════════════════════════════════════════

        public void Initialize()
        {
            buffer.Clear();
            while (netQueue.TryDequeue(out _)) { }

            filterRoll = 0;
            filterPitch = 0;
            filterLastT = 0;
            filterInitialized = false;

            playbackTime = 0;
            playbackInitialized = false;
            baseline = Vector2.zero;
            baselineInitialized = false;
            strokeBaseline = Vector2.zero;
            drawPosition = Vector2.zero;
            isDrawing = false;
            isConnected = false;
            stationaryTimer = 0f;
            tiltMagnitude = 0f;
            prevAngles = Vector2.zero;
            prevAnglesValid = false;
            angularSpeed = 0f;
            lastReceiveRealtime = -999f;

            StartListener();
        }

        public void Shutdown()
        {
            StopListener();
            isDrawing = false;
            isConnected = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            DrainNetworkQueue();

            isConnected = (Time.unscaledTime - lastReceiveRealtime) <= disconnectTimeoutSeconds
                          && lastReceiveRealtime > 0f;

            DebugTick(dt);

            if (buffer.Count == 0)
            {
                if (!isConnected) isDrawing = false;
                return;
            }

            AdvancePlayback(dt);

            Vector2 anglesNow = SampleBufferAt(playbackTime); // (roll, pitch) rad

            // Dinlenme referansı: yalnız çizim yokken güncellenir (stroke boyunca sabit).
            if (!baselineInitialized)
            {
                baseline = anglesNow;
                baselineInitialized = true;
            }
            else if (!isDrawing)
            {
                float baseLerp = 1f - Mathf.Exp(-1.0f * dt);
                // Roll'u en kısa yoldan takip et (wrap-aware EMA).
                baseline.x = (float)WrapPi(baseline.x + baseLerp * WrapPi(anglesNow.x - baseline.x));
                baseline.y = Mathf.Lerp(baseline.y, anglesNow.y, baseLerp);
            }

            // Teşhis için dinlenme pozundan sapma (gating'de KULLANILMIYOR).
            Vector2 restDelta = new Vector2(
                (float)WrapPi(anglesNow.x - baseline.x),
                anglesNow.y - baseline.y);
            tiltMagnitude = restDelta.magnitude;

            // GATING sinyali: AÇISAL HIZ. Telefon hareket ediyorsa çiz, sabit durunca dur.
            // (Mutlak eğim değil; böylece eğik tutunca "sürekli çiziyor" takılması olmaz.)
            if (prevAnglesValid && dt > 0f)
            {
                float rollRate = (float)WrapPi(anglesNow.x - prevAngles.x) / dt;
                float pitchRate = (anglesNow.y - prevAngles.y) / dt;
                float inst = Mathf.Sqrt(rollRate * rollRate + pitchRate * pitchRate);
                float k = 1f - Mathf.Exp(-speedSmoothing * dt);
                angularSpeed = Mathf.Lerp(angularSpeed, inst, k);
            }
            prevAngles = anglesNow;
            prevAnglesValid = true;

            bool wasDrawing = isDrawing;
            UpdateDrawingState(dt);

            // Yeni stroke başlangıcı (yükselen kenar): merkezi BURADA yeniden tanımla.
            // Böylece şekil her yönde merkezden çizilir, ekran kenarına sıkışmaz.
            if (isDrawing && !wasDrawing)
            {
                strokeBaseline = anglesNow;
                drawPosition = Vector2.zero;
            }

            if (isDrawing)
            {
                // POZİSYON: stroke merkezinden sapma (her yöne simetrik).
                float sRoll = (float)WrapPi(anglesNow.x - strokeBaseline.x);
                float sPitch = anglesNow.y - strokeBaseline.y;

                float dRoll = ApplyDeadzone(sRoll, deadzoneRad);
                float dPitch = ApplyDeadzone(sPitch, deadzoneRad);

                Vector2 mapped = swapAxes
                    ? new Vector2(dPitch, dRoll) * angleScale
                    : new Vector2(dRoll, dPitch) * angleScale;

                if (invertX) mapped.x = -mapped.x;
                if (invertY) mapped.y = -mapped.y;

                float followT = 1f - Mathf.Exp(-positionFollowSpeed * dt);
                drawPosition = Vector2.Lerp(drawPosition, mapped, followT);
                drawPosition.x = Mathf.Clamp(drawPosition.x, xMin, xMax);
                drawPosition.y = Mathf.Clamp(drawPosition.y, yMin, yMax);
            }

            TrimBuffer();
        }

        private static float ApplyDeadzone(float v, float dz)
        {
            if (Mathf.Abs(v) <= dz) return 0f;
            return v - Mathf.Sign(v) * dz;
        }

        private void UpdateDrawingState(float dt)
        {
            if (angularSpeed >= startMoveSpeed)
            {
                isDrawing = true;
                stationaryTimer = 0f;
            }
            else if (isDrawing)
            {
                if (angularSpeed <= stopMoveSpeed)
                {
                    stationaryTimer += dt;
                    if (stationaryTimer >= stopDelaySeconds)
                    {
                        isDrawing = false;
                    }
                }
                else
                {
                    stationaryTimer = 0f;
                }
            }
        }

        // ════════════════════════════════════════════
        // Jitter buffer (açı örnekleri)
        // ════════════════════════════════════════════

        private void DrainNetworkQueue()
        {
            bool got = false;
            while (netQueue.TryDequeue(out AngleSample s))
            {
                if (buffer.Count == 0 || s.timeSec >= buffer[buffer.Count - 1].timeSec)
                {
                    buffer.Add(s);
                }
                else
                {
                    int idx = buffer.FindLastIndex(b => b.timeSec <= s.timeSec);
                    buffer.Insert(idx + 1, s);
                }
                got = true;
            }

            if (got) lastReceiveRealtime = Time.unscaledTime;
        }

        private void AdvancePlayback(double dt)
        {
            double lastT = buffer[buffer.Count - 1].timeSec;

            if (!playbackInitialized)
            {
                playbackTime = buffer[0].timeSec;
                playbackInitialized = true;
                return;
            }

            double backlog = lastT - playbackTime;

            if (backlog > maxBufferLag)
            {
                playbackTime = lastT - targetBufferDelay;
                return;
            }

            double speed = 1.0;
            if (backlog > targetBufferDelay + 0.5) speed = 1.3;
            else if (backlog < targetBufferDelay * 0.5) speed = 0.9;

            playbackTime += dt * speed;
            if (playbackTime > lastT) playbackTime = lastT;
        }

        private Vector2 SampleBufferAt(double t)
        {
            int count = buffer.Count;
            if (count == 1) return new Vector2(buffer[0].roll, buffer[0].pitch);

            if (t <= buffer[0].timeSec)
                return new Vector2(buffer[0].roll, buffer[0].pitch);
            if (t >= buffer[count - 1].timeSec)
                return new Vector2(buffer[count - 1].roll, buffer[count - 1].pitch);

            for (int i = count - 1; i >= 0; i--)
            {
                if (buffer[i].timeSec <= t)
                {
                    AngleSample a = buffer[i];
                    AngleSample b = buffer[i + 1];
                    double span = b.timeSec - a.timeSec;
                    float u = span > 1e-9 ? (float)((t - a.timeSec) / span) : 0f;
                    // Roll'u en kısa yoldan interpole et (±π süreksizliği için).
                    float rollI = (float)WrapPi(a.roll + u * WrapPi(b.roll - a.roll));
                    return new Vector2(rollI, Mathf.Lerp(a.pitch, b.pitch, u));
                }
            }

            return new Vector2(buffer[0].roll, buffer[0].pitch);
        }

        private void TrimBuffer()
        {
            double cutoff = playbackTime - 0.5;
            int removeCount = 0;
            while (removeCount < buffer.Count - 1 && buffer[removeCount].timeSec < cutoff)
            {
                removeCount++;
            }
            if (removeCount > 0) buffer.RemoveRange(0, removeCount);
        }

        // ════════════════════════════════════════════
        // IInputProvider
        // ════════════════════════════════════════════

        public Vector2 GetDrawPosition() => drawPosition;
        public float GetRawFlexValue() => Mathf.Clamp01(tiltMagnitude);
        public float GetFilteredFlexValue() => Mathf.Clamp01(tiltMagnitude);

        private void OnDestroy() => Shutdown();

        // ════════════════════════════════════════════
        // HTTP Listener
        // ════════════════════════════════════════════

        private void StartListener()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://*:{port}/");
                httpListener.Start();

                isRunning = true;
                listenerThread = new Thread(ListenLoop) { IsBackground = true };
                listenerThread.Start();

                Debug.Log($"[WatchHttpInputProvider] Dinleniyor: http://*:{port}{endpointPath}");
            }
            catch (Exception e)
            {
                isConnected = false;
                Debug.LogError($"[WatchHttpInputProvider] HTTP listener başlatılamadı: {e.Message}");
            }
        }

        private void StopListener()
        {
            isRunning = false;

            if (httpListener != null)
            {
                try
                {
                    httpListener.Stop();
                    httpListener.Close();
                }
                catch { }
                httpListener = null;
            }

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(500);
            }
        }

        private void ListenLoop()
        {
            while (isRunning && httpListener != null)
            {
                try
                {
                    HttpListenerContext context = httpListener.GetContext();
                    HandleRequest(context);
                }
                catch (HttpListenerException)
                {
                    if (!isRunning) return;
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning($"[WatchHttpInputProvider] Listener hatası: {e.Message}");
                    }
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            string path = context.Request.Url != null ? context.Request.Url.AbsolutePath : string.Empty;
            string normalizedPath = path.TrimEnd('/');
            string normalizedEndpoint = endpointPath.TrimEnd('/');
            bool isEndpoint = string.Equals(normalizedPath, normalizedEndpoint, StringComparison.OrdinalIgnoreCase);

            // #region agent log
            WriteDebugFile("C", "WatchHttpInputProvider.HandleRequest:entry",
                "{\"method\":\"" + JsonEscape(context.Request.HttpMethod) + "\"" +
                ",\"path\":\"" + JsonEscape(path) + "\"" +
                ",\"remote\":\"" + JsonEscape(context.Request.RemoteEndPoint != null ? context.Request.RemoteEndPoint.ToString() : "?") + "\"}");
            // #endregion

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !isEndpoint)
            {
                context.Response.StatusCode = 404;
                WriteResponse(context.Response, "not-found");
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            int added = ProcessBody(body);

            // #region agent log
            WriteDebugFile("D", "WatchHttpInputProvider.HandleRequest:parsed",
                "{\"emitted\":" + added + ",\"angleSrc\":\"" + JsonEscape(lastSensorSource) +
                "\",\"gyro\":" + (lastGyroPresent ? "true" : "false") + "}");
            // #endregion

            if (added > 0)
            {
                totalPacketsReceived++;
                totalSamplesReceived += added;
            }

            context.Response.StatusCode = 200;
            WriteResponse(context.Response, added > 0 ? $"success:{added}" : "accepted-no-motion");
        }

        private static void WriteResponse(HttpListenerResponse response, string text)
        {
            byte[] payload = Encoding.UTF8.GetBytes(text);
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = payload.Length;
            response.OutputStream.Write(payload, 0, payload.Length);
            response.OutputStream.Close();
        }

        // ════════════════════════════════════════════
        // Parsing + Complementary Filter füzyonu (listener thread)
        // ════════════════════════════════════════════

        /// <summary>
        /// Body'den ivme/yerçekimi + jiroskop örneklerini ayıklar, zaman sırasına
        /// göre complementary filter ile (roll,pitch) açılarına çevirir, queue'ya ekler.
        /// </summary>
        private int ProcessBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return 0;

            // 1) Açı kaynağı (yerçekimi öncelikli)
            List<RawEvent> angleSrc = ParseSensorXYZ(body, GravityRegex, false);
            if (angleSrc.Count > 0) lastSensorSource = "gravity";
            else
            {
                angleSrc = ParseSensorXYZ(body, AccelUncalibratedRegex, false);
                if (angleSrc.Count > 0) lastSensorSource = "accelerometeruncalibrated";
                else
                {
                    angleSrc = ParseSensorXYZ(body, AccelerometerRegex, false);
                    if (angleSrc.Count > 0) lastSensorSource = "accelerometer(lineer)";
                }
            }

            // 2) Jiroskop
            List<RawEvent> gyro = ParseSensorXYZ(body, GyroscopeRegex, true);
            if (gyro.Count == 0) gyro = ParseSensorXYZ(body, GyroUncalibratedRegex, true);
            lastGyroPresent = gyro.Count > 0;

            if (angleSrc.Count == 0 && gyro.Count == 0) return 0;

            // 3) Birleştir + zaman sırala
            List<RawEvent> events = new List<RawEvent>(angleSrc.Count + gyro.Count);
            events.AddRange(angleSrc);
            events.AddRange(gyro);
            events.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));

            // 4) Complementary filter -> açı örnekleri
            int emitted = 0;
            for (int i = 0; i < events.Count; i++)
            {
                RawEvent e = events[i];

                if (!filterInitialized)
                {
                    if (!e.isGyro)
                    {
                        ComputeAccelAngles(e, out double r0, out double p0);
                        filterRoll = r0;
                        filterPitch = p0;
                    }
                    else
                    {
                        filterRoll = 0;
                        filterPitch = 0;
                    }
                    filterLastT = e.timeSec;
                    filterInitialized = true;
                    EnqueueAngle(e.timeSec);
                    emitted++;
                    continue;
                }

                // Zaman geriye giderse (paket sırası bozulursa) atla — monotonluğu koru.
                if (e.timeSec <= filterLastT) continue;

                double dt = e.timeSec - filterLastT;
                filterLastT = e.timeSec;
                if (dt > 0.1) dt = 0.1; // büyük boşlukta entegrasyonu sınırla

                if (e.isGyro)
                {
                    // Roll ±π'de sarmalanır; pitch fizik gereği ±π/2 sınırlı.
                    filterRoll = WrapPi(filterRoll + gyroSignRoll * e.x * dt);
                    filterPitch += gyroSignPitch * e.y * dt;
                }
                else
                {
                    ComputeAccelAngles(e, out double aRoll, out double aPitch);
                    // Wrap-aware blend: roll'u en kısa yoldan ivme açısına çek.
                    filterRoll = WrapPi(filterRoll + (1.0 - fusionAlpha) * WrapPi(aRoll - filterRoll));
                    filterPitch = fusionAlpha * filterPitch + (1.0 - fusionAlpha) * aPitch;
                }

                EnqueueAngle(e.timeSec);
                emitted++;
            }

            return emitted;
        }

        private void EnqueueAngle(double t)
        {
            netQueue.Enqueue(new AngleSample
            {
                timeSec = t,
                roll = (float)filterRoll,
                pitch = (float)filterPitch
            });
        }

        // Yerçekimi vektöründen mutlak roll/pitch (ölçek bağımsız — atan2).
        private static void ComputeAccelAngles(RawEvent e, out double roll, out double pitch)
        {
            roll = Math.Atan2(e.y, e.z);
            pitch = Math.Atan2(-e.x, Math.Sqrt(e.y * e.y + e.z * e.z));
        }

        // Açıyı (-π, π] aralığına sarmalar. ±180° süreksizliğini doğru ele almak için.
        private static double WrapPi(double a)
        {
            const double TwoPi = 2.0 * Math.PI;
            a %= TwoPi;
            if (a > Math.PI) a -= TwoPi;
            else if (a < -Math.PI) a += TwoPi;
            return a;
        }

        private static List<RawEvent> ParseSensorXYZ(string body, Regex blockRegex, bool isGyro)
        {
            var list = new List<RawEvent>();
            MatchCollection blocks = blockRegex.Matches(body);
            for (int i = 0; i < blocks.Count; i++)
            {
                Match block = blocks[i];
                if (!double.TryParse(block.Groups["time"].Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double timeNs))
                {
                    continue;
                }

                if (!TryParseXYZ(block.Groups["values"].Value, out float x, out float y, out float z))
                    continue;

                list.Add(new RawEvent
                {
                    timeSec = timeNs / 1e9,
                    isGyro = isGyro,
                    x = x,
                    y = y,
                    z = z
                });
            }
            return list;
        }

        private static bool TryParseXYZ(string valuesBlock, out float x, out float y, out float z)
        {
            x = 0f; y = 0f; z = 0f;
            bool hasX = false, hasY = false, hasZ = false;

            MatchCollection axes = AxisXYZRegex.Matches(valuesBlock);
            for (int i = 0; i < axes.Count; i++)
            {
                if (!float.TryParse(axes[i].Groups["value"].Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float v))
                {
                    continue;
                }

                switch (axes[i].Groups["axis"].Value.ToLowerInvariant())
                {
                    case "x": x = v; hasX = true; break;
                    case "y": y = v; hasY = true; break;
                    case "z": z = v; hasZ = true; break;
                }
            }

            return hasX && hasY && hasZ;
        }

        // ════════════════════════════════════════════
        // Teşhis
        // ════════════════════════════════════════════

        private void DebugTick(float dt)
        {
            if (!enableDebugLog) return;

            debugTimer += dt;
            if (debugTimer < 1f) return;
            debugTimer = 0f;

            Debug.Log(
                $"[WatchDebug] bağlı={isConnected} açıKaynak={lastSensorSource} jiro={lastGyroPresent} " +
                $"paket={totalPacketsReceived} örnek={totalSamplesReceived} tampon={buffer.Count} " +
                $"hız={angularSpeed:F3}rad/sn (başla={startMoveSpeed:F2}/dur={stopMoveSpeed:F2}) " +
                $"çiziyor={isDrawing} pos=({drawPosition.x:F2},{drawPosition.y:F2})");

            // #region agent log
            WriteDebugFile("E", "WatchHttpInputProvider.DebugTick:state",
                "{\"connected\":" + (isConnected ? "true" : "false") +
                ",\"packets\":" + totalPacketsReceived +
                ",\"samples\":" + totalSamplesReceived +
                ",\"buffer\":" + buffer.Count +
                ",\"angleSrc\":\"" + JsonEscape(lastSensorSource) + "\"" +
                ",\"gyro\":" + (lastGyroPresent ? "true" : "false") +
                ",\"angSpeed\":" + angularSpeed.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"dev\":" + tiltMagnitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"drawing\":" + (isDrawing ? "true" : "false") +
                ",\"posX\":" + drawPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"posY\":" + drawPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "}");
            // #endregion
        }

        // #region agent log
        private static readonly object DebugFileLock = new object();
        private const string DebugLogPath = "/Users/resulkilinc/Desktop/mühendislik projesi/.cursor/debug-d833e8.log";

        private static void WriteDebugFile(string hypothesisId, string location, string dataJson)
        {
            try
            {
                long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                string line = "{\"sessionId\":\"d833e8\",\"hypothesisId\":\"" + hypothesisId +
                    "\",\"location\":\"" + location + "\",\"data\":" + dataJson +
                    ",\"timestamp\":" + ts + "}";
                lock (DebugFileLock)
                {
                    File.AppendAllText(DebugLogPath, line + "\n");
                }
            }
            catch { }
        }

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
        }
        // #endregion
    }
}
