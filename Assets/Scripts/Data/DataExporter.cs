// ============================================================
// DataExporter.cs — REHABIT-EL
// CSV ve JSON formatında seans verisi export.
// ============================================================
using System.IO;
using System.Text;
using UnityEngine;

namespace RehabitEL.Data
{
    /// <summary>
    /// SessionData'yı CSV ve JSON formatında dosyaya yazar.
    /// Editor modunda Assets/Exports/, Build modunda persistentDataPath kullanır.
    /// </summary>
    public static class DataExporter
    {
        /// <summary>Export klasör adı.</summary>
        private const string EXPORT_FOLDER = "Exports";

        // ════════════════════════════════════════════
        // CSV Export
        // ════════════════════════════════════════════

        /// <summary>
        /// Seans verisini CSV formatında dosyaya yazar.
        /// Her canavar kaydı bir satırdır, seans bilgileri her satırda tekrarlanır.
        /// </summary>
        /// <param name="session">Seans verisi.</param>
        /// <returns>Oluşturulan dosya yolu, hata durumunda null.</returns>
        public static string ExportCSV(SessionData session)
        {
            if (session == null) return null;

            try
            {
                string directory = GetExportDirectory();
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string shortId = session.sessionId.Length > 8
                    ? session.sessionId.Substring(0, 8)
                    : session.sessionId;
                string fileName = $"session_{shortId}_{timestamp}.csv";
                string filePath = Path.Combine(directory, fileName);

                StringBuilder sb = new StringBuilder();

                // Başlık satırı
                sb.AppendLine("session_id,started_at,ended_at,monster_count,score_total," +
                              "duration_seconds,input_mode,difficulty_level," +
                              "monster_index,shape,success,time_to_complete_ms," +
                              "time_allowed_ms,error_metric,drawn_point_count,threshold_used");

                // Veri satırları
                if (session.monsters.Count > 0)
                {
                    foreach (var m in session.monsters)
                    {
                        sb.AppendLine(
                            $"{session.sessionId}," +
                            $"{session.startedAt}," +
                            $"{session.endedAt}," +
                            $"{session.monsterCount}," +
                            $"{session.scoreTotal}," +
                            $"{session.durationSeconds:F1}," +
                            $"{session.inputMode}," +
                            $"{session.difficultyLevel}," +
                            $"{m.monsterIndex}," +
                            $"{m.shape}," +
                            $"{m.success.ToString().ToLower()}," +
                            $"{m.timeToCompleteMs:F1}," +
                            $"{m.timeAllowedMs:F1}," +
                            $"{m.errorMetric:F4}," +
                            $"{m.drawnPointCount}," +
                            $"{m.thresholdUsed:F4}"
                        );
                    }
                }
                else
                {
                    // Canavar kaydı yoksa (0 skor ile biten seans)
                    sb.AppendLine(
                        $"{session.sessionId}," +
                        $"{session.startedAt}," +
                        $"{session.endedAt}," +
                        $"{session.monsterCount}," +
                        $"{session.scoreTotal}," +
                        $"{session.durationSeconds:F1}," +
                        $"{session.inputMode}," +
                        $"{session.difficultyLevel}," +
                        "0,None,false,0.0,0.0,0.0000,0,0.0000"
                    );
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[DataExporter] CSV dosyası oluşturuldu: {filePath}");
                return filePath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataExporter] CSV export hatası: {e.Message}");
                return null;
            }
        }

        // ════════════════════════════════════════════
        // JSON Export
        // ════════════════════════════════════════════

        /// <summary>
        /// Seans verisini JSON formatında dosyaya yazar.
        /// </summary>
        /// <param name="session">Seans verisi.</param>
        /// <returns>Oluşturulan dosya yolu, hata durumunda null.</returns>
        public static string ExportJSON(SessionData session)
        {
            if (session == null) return null;

            try
            {
                string directory = GetExportDirectory();
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string shortId = session.sessionId.Length > 8
                    ? session.sessionId.Substring(0, 8)
                    : session.sessionId;
                string fileName = $"session_{shortId}_{timestamp}.json";
                string filePath = Path.Combine(directory, fileName);

                // Unity'nin JsonUtility'si ile serializasyon
                string json = JsonUtility.ToJson(session, true);

                File.WriteAllText(filePath, json, Encoding.UTF8);
                Debug.Log($"[DataExporter] JSON dosyası oluşturuldu: {filePath}");
                return filePath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataExporter] JSON export hatası: {e.Message}");
                return null;
            }
        }

        // ════════════════════════════════════════════
        // Yardımcı
        // ════════════════════════════════════════════

        /// <summary>
        /// Export klasörünün yolunu döndürür. Yoksa oluşturur.
        /// </summary>
        private static string GetExportDirectory()
        {
            string basePath;

            #if UNITY_EDITOR
            // Editor modunda Assets/Exports/
            basePath = Path.Combine(Application.dataPath, EXPORT_FOLDER);
            #else
            // Build modunda persistentDataPath/Exports/
            basePath = Path.Combine(Application.persistentDataPath, EXPORT_FOLDER);
            #endif

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                Debug.Log($"[DataExporter] Export klasörü oluşturuldu: {basePath}");
            }

            return basePath;
        }

        /// <summary>
        /// Export klasöründeki dosya sayısını döndürür.
        /// </summary>
        public static int GetExportFileCount()
        {
            string dir = GetExportDirectory();
            if (Directory.Exists(dir))
            {
                return Directory.GetFiles(dir).Length;
            }
            return 0;
        }
    }
}
