// ============================================================
// DrawingLine.cs — REHABIT-EL
// LineRenderer wrapper: çizgi oluşturma, güncelleme, renk/kalınlık.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace RehabitEL.Drawing
{
    /// <summary>
    /// LineRenderer component'ini yönetir.
    /// Gerçek zamanlı çizgi çizme ve görsel ayarlardan sorumludur.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class DrawingLine : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Çizgi Görünümü")]
        [Tooltip("Çizgi kalınlığı")]
        [SerializeField] private float lineWidth = 0.08f;

        [Tooltip("Çizgi rengi (başlangıç)")]
        [SerializeField] private Color lineColor = new Color(0f, 0.8f, 1f, 1f); // Cyan

        [Tooltip("Çizgi renk geçişi (son nokta)")]
        [SerializeField] private Color lineEndColor = new Color(0.5f, 0f, 1f, 1f); // Mor

        [Header("Materyal")]
        [Tooltip("Çizgi materyali (boşsa varsayılan Sprites/Default kullanılır)")]
        [SerializeField] private Material lineMaterial;

        // ── Durum ──
        private LineRenderer lineRenderer;
        private List<Vector3> points = new List<Vector3>();

        /// <summary>Çizgideki toplam nokta sayısı.</summary>
        public int PointCount => points.Count;

        /// <summary>Çizim noktalarının listesi.</summary>
        public List<Vector3> Points => points;

        // ════════════════════════════════════════════
        // Unity Lifecycle
        // ════════════════════════════════════════════

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        /// <summary>
        /// LineRenderer'ı varsayılan ayarlarla yapılandırır.
        /// </summary>
        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null) return;

            // Materyal
            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                // Varsayılan unlit materyal
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            // Kalınlık
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            // Renk
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(lineColor, 0f),
                    new GradientColorKey(lineEndColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            lineRenderer.colorGradient = gradient;

            // Diğer ayarlar
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 10;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;
            lineRenderer.positionCount = 0;
        }

        // ════════════════════════════════════════════
        // Çizim İşlemleri
        // ════════════════════════════════════════════

        /// <summary>
        /// Çizgiye yeni bir nokta ekler.
        /// </summary>
        /// <param name="point">Dünya koordinatlarında nokta.</param>
        public void AddPoint(Vector3 point)
        {
            if (!EnsureLineRenderer()) return;

            // Z koordinatını sıfırla (2D)
            point.z = 0f;

            points.Add(point);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, point);
        }

        /// <summary>
        /// Son noktayı günceller (gerçek zamanlı takip için).
        /// </summary>
        /// <param name="point">Güncel pozisyon.</param>
        public void UpdateLastPoint(Vector3 point)
        {
            if (!EnsureLineRenderer()) return;
            if (points.Count == 0) return;

            point.z = 0f;
            points[points.Count - 1] = point;
            lineRenderer.SetPosition(points.Count - 1, point);
        }

        /// <summary>
        /// Çizgiyi tamamen temizler.
        /// </summary>
        public void Clear()
        {
            if (!EnsureLineRenderer()) return;
            points.Clear();
            lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// Çizgi rengini değiştirir (başarı/başarısızlık geri bildirimi).
        /// </summary>
        public void SetColor(Color startColor, Color endColor)
        {
            if (!EnsureLineRenderer()) return;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            lineRenderer.colorGradient = gradient;
        }

        /// <summary>
        /// Başarı durumu için yeşil renk.
        /// </summary>
        public void SetSuccessColor()
        {
            SetColor(new Color(0.2f, 1f, 0.3f), new Color(0f, 0.8f, 0.2f));
        }

        /// <summary>
        /// Başarısızlık durumu için kırmızı renk.
        /// </summary>
        public void SetFailColor()
        {
            SetColor(new Color(1f, 0.2f, 0.2f), new Color(0.8f, 0f, 0f));
        }

        /// <summary>
        /// Varsayılan renge geri döner.
        /// </summary>
        public void ResetColor()
        {
            SetColor(lineColor, lineEndColor);
        }

        /// <summary>
        /// Çizgi noktalarını Vector2 listesi olarak döndürür (doğrulama için).
        /// </summary>
        public List<Vector2> GetPoints2D()
        {
            List<Vector2> points2D = new List<Vector2>(points.Count);
            foreach (Vector3 p in points)
            {
                points2D.Add(new Vector2(p.x, p.y));
            }
            return points2D;
        }

        private bool EnsureLineRenderer()
        {
            if (lineRenderer != null) return true;

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogWarning("[DrawingLine] LineRenderer bulunamadı, çizim güncellenemedi.");
                return false;
            }

            ConfigureLineRenderer();
            return true;
        }
    }
}
