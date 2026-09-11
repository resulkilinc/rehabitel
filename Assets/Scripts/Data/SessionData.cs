// ============================================================
// SessionData.cs — REHABIT-EL
// Seans ve canavar kayıt veri modelleri (serializable).
// JSON/CSV export için kullanılır.
// ============================================================
using System;
using System.Collections.Generic;

namespace RehabitEL.Data
{
    /// <summary>
    /// Bir oyun seansının tüm verilerini temsil eder.
    /// JSON serializasyonu ve CSV export için kullanılır.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        /// <summary>Benzersiz seans kimliği (GUID).</summary>
        public string sessionId;

        /// <summary>Seans başlangıç zamanı (ISO 8601).</summary>
        public string startedAt;

        /// <summary>Seans bitiş zamanı (ISO 8601).</summary>
        public string endedAt;

        /// <summary>Toplam öldürülen canavar sayısı (N).</summary>
        public int monsterCount;

        /// <summary>Toplam puan (P = N × 10).</summary>
        public int scoreTotal;

        /// <summary>Seans süresi (saniye).</summary>
        public float durationSeconds;

        /// <summary>Kullanılan giriş modu (Mouse/Serial/Simulator/Playback).</summary>
        public string inputMode;

        /// <summary>Ulaşılan zorluk seviyesi.</summary>
        public int difficultyLevel;

        /// <summary>Her canavar için detaylı kayıtlar.</summary>
        public List<MonsterRecord> monsters = new List<MonsterRecord>();

        /// <summary>
        /// Yeni bir SessionData oluşturur ve benzersiz ID atar.
        /// </summary>
        public SessionData()
        {
            sessionId = Guid.NewGuid().ToString();
            monsters = new List<MonsterRecord>();
        }
    }

    /// <summary>
    /// Tek bir canavar karşılaşmasının detaylı kaydı.
    /// </summary>
    [Serializable]
    public class MonsterRecord
    {
        /// <summary>Seans içindeki canavar sırası (0'dan başlar).</summary>
        public int monsterIndex;

        /// <summary>Atanan şekil adı (Circle/Square/Triangle).</summary>
        public string shape;

        /// <summary>Başarılı mı?</summary>
        public bool success;

        /// <summary>Tamamlama süresi (milisaniye).</summary>
        public float timeToCompleteMs;

        /// <summary>İzin verilen süre (milisaniye).</summary>
        public float timeAllowedMs;

        /// <summary>RMSE hata değeri (normalize edilmiş).</summary>
        public float errorMetric;

        /// <summary>Çizilen nokta sayısı.</summary>
        public int drawnPointCount;

        /// <summary>Kullanılan kabul eşiği.</summary>
        public float thresholdUsed;

        public MonsterRecord() { }

        /// <summary>
        /// Yeni bir MonsterRecord oluşturur.
        /// </summary>
        public MonsterRecord(int index, string shapeName, bool isSuccess,
                             float timeMs, float allowedMs, float rmse,
                             int pointCount, float threshold)
        {
            monsterIndex = index;
            shape = shapeName;
            success = isSuccess;
            timeToCompleteMs = timeMs;
            timeAllowedMs = allowedMs;
            errorMetric = rmse;
            drawnPointCount = pointCount;
            thresholdUsed = threshold;
        }
    }
}
