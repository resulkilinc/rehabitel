// ============================================================
// ShapeValidator.cs — REHABIT-EL
// Şekil doğrulama: referans şekil üretme + RMSE path-following.
// Gelişmiş kontroller: kapalılık, kapsama, aspect ratio.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace RehabitEL.Drawing
{
    /// <summary>
    /// Çizilen şekli referans şekillerle karşılaştırarak doğrulayan sınıf.
    /// Path-following RMSE algoritması + ek geometrik kontroller:
    /// 1. Çizim noktalarını normalize et (ölçek + pozisyon bağımsız)
    /// 2. Kapalılık (closure) kontrolü — başlangıç/bitiş yakınlığı
    /// 3. Kapsama (coverage) kontrolü — referans şeklin yeterince kapsanması
    /// 4. Aspect ratio kontrolü — düz çizgi eleme
    /// 5. Her çizim noktası için referanstaki en yakın noktayı bul
    /// 6. RMSE hesapla (iki yönlü, eşit ağırlıklı)
    /// 7. rmse &lt; threshold → başarı
    /// </summary>
    public static class ShapeValidator
    {
        // ── Sabitler ──
        /// <summary>Referans şekil için varsayılan örnek noktası sayısı.</summary>
        private const int DEFAULT_SAMPLE_COUNT = 64;

        /// <summary>Minimum kabul edilen çizim noktası sayısı.</summary>
        private const int MIN_DRAWN_POINTS = 15;

        /// <summary>Kapalılık kontrolü: başlangıç-bitiş mesafesi / yayılım oranı eşiği. Yüksek = daha hoşgörülü.</summary>
        private const float CLOSURE_RATIO_THRESHOLD = 0.55f;

        /// <summary>Kapsama kontrolü: referansın kaç çeyreğinin kapsanması gerektiği (4 üzerinden).</summary>
        private const int MIN_QUADRANTS_COVERED = 3;

        /// <summary>Aspect ratio minimum değeri (genişlik/yükseklik veya tersi). Düşük = üçgen/şekil köşeleri hoşgörülü.</summary>
        private const float MIN_ASPECT_RATIO = 0.28f;

        /// <summary>Kapsama kontrolü: her çeyrek için minimum kapsama mesafesi.</summary>
        private const float QUADRANT_COVERAGE_DISTANCE = 0.5f;

        // ════════════════════════════════════════════
        // Referans Şekil Üretimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Belirtilen şekil türü için referans noktalar üretir.
        /// </summary>
        /// <param name="shape">Şekil türü.</param>
        /// <param name="sampleCount">Nokta sayısı.</param>
        /// <returns>Birim daire/kare/üçgen içinde normalize edilmiş noktalar.</returns>
        public static List<Vector2> GenerateReferencePoints(ShapeType shape, int sampleCount = DEFAULT_SAMPLE_COUNT)
        {
            switch (shape)
            {
                case ShapeType.Circle:
                    return GenerateCirclePoints(sampleCount);
                case ShapeType.Square:
                    return GenerateSquarePoints(sampleCount);
                case ShapeType.Triangle:
                    return GenerateTrianglePoints(sampleCount);
                default:
                    Debug.LogWarning($"[ShapeValidator] Bilinmeyen şekil: {shape}");
                    return GenerateCirclePoints(sampleCount);
            }
        }

        /// <summary>
        /// Daire referans noktaları üretir (cos/sin).
        /// Merkez: (0,0), Yarıçap: 0.5
        /// </summary>
        public static List<Vector2> GenerateCirclePoints(int sampleCount = DEFAULT_SAMPLE_COUNT)
        {
            List<Vector2> points = new List<Vector2>(sampleCount);
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = (2f * Mathf.PI * i) / sampleCount;
                float x = 0.5f * Mathf.Cos(angle);
                float y = 0.5f * Mathf.Sin(angle);
                points.Add(new Vector2(x, y));
            }
            return points;
        }

        /// <summary>
        /// Kare referans noktaları üretir (4 kenar interpolasyon).
        /// Merkez: (0,0), Kenar uzunluğu: 1
        /// </summary>
        public static List<Vector2> GenerateSquarePoints(int sampleCount = DEFAULT_SAMPLE_COUNT)
        {
            List<Vector2> points = new List<Vector2>(sampleCount);

            // 4 köşe
            Vector2[] corners = new Vector2[]
            {
                new Vector2(-0.5f, -0.5f),  // Sol alt
                new Vector2( 0.5f, -0.5f),  // Sağ alt
                new Vector2( 0.5f,  0.5f),  // Sağ üst
                new Vector2(-0.5f,  0.5f)   // Sol üst
            };

            int pointsPerSide = sampleCount / 4;
            int remainder = sampleCount - (pointsPerSide * 4);

            for (int side = 0; side < 4; side++)
            {
                Vector2 start = corners[side];
                Vector2 end = corners[(side + 1) % 4];

                int currentSideCount = pointsPerSide + (side < remainder ? 1 : 0);

                for (int i = 0; i < currentSideCount; i++)
                {
                    float t = (float)i / currentSideCount;
                    points.Add(Vector2.Lerp(start, end, t));
                }
            }

            return points;
        }

        /// <summary>
        /// Üçgen referans noktaları üretir (eşkenar üçgen, 3 kenar interpolasyon).
        /// Merkez: (0,0)
        /// </summary>
        public static List<Vector2> GenerateTrianglePoints(int sampleCount = DEFAULT_SAMPLE_COUNT)
        {
            List<Vector2> points = new List<Vector2>(sampleCount);

            // Eşkenar üçgen köşeleri (yukarı bakan)
            float size = 0.6f;
            Vector2[] corners = new Vector2[]
            {
                new Vector2(0f, size),                                                    // Üst
                new Vector2(-size * Mathf.Sin(Mathf.PI / 3f), -size * 0.5f),             // Sol alt
                new Vector2( size * Mathf.Sin(Mathf.PI / 3f), -size * 0.5f)              // Sağ alt
            };

            int pointsPerSide = sampleCount / 3;
            int remainder = sampleCount - (pointsPerSide * 3);

            for (int side = 0; side < 3; side++)
            {
                Vector2 start = corners[side];
                Vector2 end = corners[(side + 1) % 3];

                int currentSideCount = pointsPerSide + (side < remainder ? 1 : 0);

                for (int i = 0; i < currentSideCount; i++)
                {
                    float t = (float)i / currentSideCount;
                    points.Add(Vector2.Lerp(start, end, t));
                }
            }

            return points;
        }

        // ════════════════════════════════════════════
        // Doğrulama (Validation)
        // ════════════════════════════════════════════

        /// <summary>
        /// Çizilen noktaları belirtilen şekille karşılaştırır.
        /// Gelişmiş kontroller: kapalılık + kapsama + aspect ratio + RMSE.
        /// </summary>
        /// <param name="drawnPoints">Oyuncunun çizdiği noktalar.</param>
        /// <param name="targetShape">Hedef şekil türü.</param>
        /// <param name="threshold">RMSE eşik değeri (bu değerin altı = başarılı).</param>
        /// <param name="rmse">Hesaplanan RMSE değeri (out parametresi).</param>
        /// <returns>true = şekil doğrulandı, false = doğrulanamadı.</returns>
        public static bool Validate(List<Vector2> drawnPoints, ShapeType targetShape,
                                     float threshold, out float rmse)
        {
            rmse = float.MaxValue;

            // ── 1. Minimum nokta kontrolü ──
            if (drawnPoints == null || drawnPoints.Count < MIN_DRAWN_POINTS)
            {
                Debug.Log($"[ShapeValidator] Yetersiz nokta: {drawnPoints?.Count ?? 0} < {MIN_DRAWN_POINTS}");
                return false;
            }

            // Noktaları normalize et
            List<Vector2> normalizedDrawn = NormalizePoints(drawnPoints);

            // ── 2. Aspect Ratio kontrolü (düz çizgi eleme) ──
            if (!CheckAspectRatio(normalizedDrawn))
            {
                Debug.Log("[ShapeValidator] BAŞARISIZ: Aspect ratio düşük (düz çizgi benzeri çizim)");
                rmse = 1.0f;
                return false;
            }

            // ── 3. Kapalılık (Closure) kontrolü ──
            if (!CheckClosure(normalizedDrawn))
            {
                Debug.Log("[ShapeValidator] BAŞARISIZ: Çizim kapalı değil (başlangıç-bitiş çok uzak)");
                rmse = 0.9f;
                return false;
            }

            // ── 4. Kapsama (Coverage) kontrolü ──
            if (!CheckCoverage(normalizedDrawn))
            {
                Debug.Log("[ShapeValidator] BAŞARISIZ: Çizim referans şekli yeterince kapsamıyor");
                rmse = 0.8f;
                return false;
            }

            // ── 5. Referans noktaları üret ve RMSE hesapla ──
            List<Vector2> referencePoints = GenerateReferencePoints(targetShape);
            List<Vector2> normalizedRef = NormalizePoints(referencePoints);

            rmse = CalculateRMSE(normalizedDrawn, normalizedRef);

            Debug.Log($"[ShapeValidator] Şekil: {targetShape}, " +
                      $"RMSE: {rmse:F4}, Eşik: {threshold:F4}, " +
                      $"Nokta: {drawnPoints.Count}, " +
                      $"Sonuç: {(rmse < threshold ? "BAŞARILI" : "BAŞARISIZ")}");

            return rmse < threshold;
        }

        // ════════════════════════════════════════════
        // Geometrik Kontroller
        // ════════════════════════════════════════════

        /// <summary>
        /// Kapalılık kontrolü: çizimin başlangıç ve bitiş noktası
        /// birbirine yeterince yakın mı? Kapalı şekiller (daire, kare, üçgen)
        /// için başlangıç ve bitiş noktası birbirine yakın olmalıdır.
        /// </summary>
        private static bool CheckClosure(List<Vector2> normalizedPoints)
        {
            if (normalizedPoints.Count < 2) return false;

            Vector2 start = normalizedPoints[0];
            Vector2 end = normalizedPoints[normalizedPoints.Count - 1];

            // Çizimin toplam yayılımını hesapla (bounding box)
            float spread = CalculateSpread(normalizedPoints);
            if (spread < 0.01f) return false;

            // Başlangıç-bitiş mesafesi / toplam yayılım oranı
            float closureDistance = Vector2.Distance(start, end);
            float closureRatio = closureDistance / spread;

            Debug.Log($"[ShapeValidator] Kapalılık: mesafe={closureDistance:F3}, yayılım={spread:F3}, oran={closureRatio:F3}");

            return closureRatio < CLOSURE_RATIO_THRESHOLD;
        }

        /// <summary>
        /// Aspect ratio kontrolü: çizimin genişlik/yükseklik oranını kontrol eder.
        /// Düz çizgiler (çok dar veya çok uzun) elenir.
        /// </summary>
        private static bool CheckAspectRatio(List<Vector2> normalizedPoints)
        {
            if (normalizedPoints.Count < 2) return false;

            // Bounding box hesapla
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (Vector2 p in normalizedPoints)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            // Sıfıra çok yakın genişlik veya yükseklik → düz çizgi
            if (width < 0.01f || height < 0.01f) return false;

            float aspectRatio = Mathf.Min(width / height, height / width);

            Debug.Log($"[ShapeValidator] Aspect ratio: {aspectRatio:F3} (min: {MIN_ASPECT_RATIO})");

            return aspectRatio >= MIN_ASPECT_RATIO;
        }

        /// <summary>
        /// Kapsama kontrolü: çizimin 4 çeyreği (sol-üst, sağ-üst, sol-alt, sağ-alt)
        /// yeterince kapsayıp kapsamadığını kontrol eder.
        /// Bir şekil, en az 3 çeyrekte nokta içermelidir.
        /// </summary>
        private static bool CheckCoverage(List<Vector2> normalizedPoints)
        {
            if (normalizedPoints.Count < 4) return false;

            // Centroid zaten (0,0) normalize edilmiş olmalı
            // Her çeyrek için en az bir nokta var mı kontrol et
            bool hasTopLeft = false, hasTopRight = false;
            bool hasBottomLeft = false, hasBottomRight = false;

            foreach (Vector2 p in normalizedPoints)
            {
                // Merkeze çok yakın noktaları atla (belirsiz çeyrek)
                if (Mathf.Abs(p.x) < 0.05f && Mathf.Abs(p.y) < 0.05f) continue;

                if (p.x <= 0 && p.y >= 0) hasTopLeft = true;
                if (p.x >= 0 && p.y >= 0) hasTopRight = true;
                if (p.x <= 0 && p.y <= 0) hasBottomLeft = true;
                if (p.x >= 0 && p.y <= 0) hasBottomRight = true;
            }

            int quadrantsCovered = 0;
            if (hasTopLeft) quadrantsCovered++;
            if (hasTopRight) quadrantsCovered++;
            if (hasBottomLeft) quadrantsCovered++;
            if (hasBottomRight) quadrantsCovered++;

            Debug.Log($"[ShapeValidator] Kapsama: {quadrantsCovered}/4 çeyrek " +
                      $"(TL:{hasTopLeft}, TR:{hasTopRight}, BL:{hasBottomLeft}, BR:{hasBottomRight})");

            return quadrantsCovered >= MIN_QUADRANTS_COVERED;
        }

        // ════════════════════════════════════════════
        // Yardımcı Metotlar
        // ════════════════════════════════════════════

        /// <summary>
        /// Nokta kümesinin toplam yayılımını (bounding box diyagonali) hesaplar.
        /// </summary>
        private static float CalculateSpread(List<Vector2> points)
        {
            if (points.Count < 2) return 0f;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (Vector2 p in points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            return Mathf.Sqrt(width * width + height * height);
        }

        /// <summary>
        /// Noktaları normalize eder:
        /// 1. Merkeze taşı (centroid = 0,0)
        /// 2. Ölçekle (max uzaklık = 1.0)
        /// </summary>
        private static List<Vector2> NormalizePoints(List<Vector2> points)
        {
            if (points.Count == 0) return points;

            // 1. Ağırlık merkezi (centroid) hesapla
            Vector2 centroid = Vector2.zero;
            foreach (Vector2 p in points)
            {
                centroid += p;
            }
            centroid /= points.Count;

            // 2. Merkeze taşı
            List<Vector2> centered = new List<Vector2>(points.Count);
            float maxDistance = 0f;

            foreach (Vector2 p in points)
            {
                Vector2 cp = p - centroid;
                centered.Add(cp);
                float dist = cp.magnitude;
                if (dist > maxDistance) maxDistance = dist;
            }

            // 3. Ölçekle (maxDistance = 1.0)
            if (maxDistance < 0.001f)
            {
                // Tüm noktalar aynı yerde (çok küçük çizim)
                return centered;
            }

            List<Vector2> normalized = new List<Vector2>(centered.Count);
            foreach (Vector2 p in centered)
            {
                normalized.Add(p / maxDistance);
            }

            return normalized;
        }

        /// <summary>
        /// İki nokta kümesi arasındaki RMSE (Root Mean Square Error) hesaplar.
        /// Her çizim noktası için referanstaki en yakın noktanın mesafesini kullanır.
        /// İki yönlü, eşit ağırlıklı hesaplama ile eksik çizim de yakalanır.
        /// </summary>
        private static float CalculateRMSE(List<Vector2> drawnPoints, List<Vector2> referencePoints)
        {
            if (drawnPoints.Count == 0 || referencePoints.Count == 0)
                return float.MaxValue;

            float sumSquaredError = 0f;

            // İleri yön: her çizim noktası için referanstaki en yakın noktayı bul
            foreach (Vector2 drawnPoint in drawnPoints)
            {
                float minDistSq = float.MaxValue;

                foreach (Vector2 refPoint in referencePoints)
                {
                    float distSq = (drawnPoint - refPoint).sqrMagnitude;
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                    }
                }

                sumSquaredError += minDistSq;
            }

            // Geri yön: referans noktaları da çizim noktalarını kapsıyor mu?
            // (Eksik çizim tespiti — ağırlığı artırıldı)
            float sumSquaredErrorReverse = 0f;
            foreach (Vector2 refPoint in referencePoints)
            {
                float minDistSq = float.MaxValue;

                foreach (Vector2 drawnPoint in drawnPoints)
                {
                    float distSq = (refPoint - drawnPoint).sqrMagnitude;
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                    }
                }

                sumSquaredErrorReverse += minDistSq;
            }

            // İki yönlü ortalama (Hausdorff-benzeri) — eşit ağırlıklı
            float avgForward = sumSquaredError / drawnPoints.Count;
            float avgReverse = sumSquaredErrorReverse / referencePoints.Count;

            // Eşit ağırlıklı ortalama (eksik çizimi daha iyi yakalar)
            float combinedMean = (avgForward * 0.5f + avgReverse * 0.5f);

            return Mathf.Sqrt(combinedMean);
        }

        /// <summary>
        /// Belirtilen şekil için referans noktaları dünya koordinatlarında üretir.
        /// Çizim alanı içinde gösterim için kullanılır.
        /// </summary>
        /// <param name="shape">Şekil türü.</param>
        /// <param name="center">Merkez pozisyon.</param>
        /// <param name="size">Boyut.</param>
        /// <param name="sampleCount">Nokta sayısı.</param>
        /// <returns>Dünya koordinatlarında referans noktalar.</returns>
        public static List<Vector2> GenerateWorldReferencePoints(
            ShapeType shape, Vector2 center, float size, int sampleCount = DEFAULT_SAMPLE_COUNT)
        {
            List<Vector2> localPoints = GenerateReferencePoints(shape, sampleCount);
            List<Vector2> worldPoints = new List<Vector2>(localPoints.Count);

            foreach (Vector2 p in localPoints)
            {
                worldPoints.Add(center + p * size);
            }

            return worldPoints;
        }
    }
}
