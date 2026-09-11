// ============================================================
// DrawingCanvas.cs — REHABIT-EL
// Çizim alanı yönetimi: fare/input ile nokta toplama,
// referans şekil gösterimi, doğrulama tetikleme.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using RehabitEL.Core;
using RehabitEL.Input; // Eklenen import
using Input = UnityEngine.Input;

namespace RehabitEL.Drawing
{
    /// <summary>
    /// Çizim alanını yönetir. Oyuncu fare (veya flex sensor) ile çizim yapar,
    /// noktalar toplanır ve çizim tamamlandığında ShapeValidator'a gönderilir.
    /// </summary>
    public class DrawingCanvas : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Çizim Ayarları")]
        [Tooltip("İki nokta arası minimum mesafe. Büyük = gürültü azalır, şekil tanıma iyileşir.")]
        [SerializeField] private float minPointDistance = 0.08f;

        [Tooltip("Çizim alanı merkez pozisyonu")]
        [SerializeField] private Vector2 canvasCenter = Vector2.zero;

        [Tooltip("Çizim alanı boyutu (genişlik x yükseklik)")]
        [SerializeField] private Vector2 canvasSize = new Vector2(6f, 6f);

        [Header("Referans Şekil")]
        [Tooltip("Referans şeklin görünürlük alfa değeri")]
        [SerializeField] private float referenceAlpha = 0.15f;

        [Tooltip("Referans şekil boyutu")]
        [SerializeField] private float referenceSize = 3f;

        [Header("Debug")]
        [Tooltip("Çizim sınırlarını Gizmo olarak göster")]
        [SerializeField] private bool showGizmos = true;

        // ── Component Referansları ──
        private DrawingLine currentLine;
        private LineRenderer referenceLineRenderer;

        // ── Durum ──
        private bool isDrawing = false;
        private bool canDraw = false;
        private ShapeType currentShape;
        private Camera mainCamera;
        private List<Vector2> drawnPoints = new List<Vector2>();

        // ── Singleton ──
        public static DrawingCanvas Instance { get; private set; }

        /// <summary>Oyuncu şu anda çiziyor mu?</summary>
        public bool IsDrawing => isDrawing;

        /// <summary>Çizilen noktaların listesi.</summary>
        public List<Vector2> DrawnPoints => drawnPoints;

        // ════════════════════════════════════════════
        // Unity Lifecycle
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mainCamera = Camera.main;
            SetupReferenceLineRenderer();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnNewShapeAssigned += HandleNewShape;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnNewShapeAssigned -= HandleNewShape;
            }
        }

        private void Start()
        {
            // Event aboneliklerini güvence altına al
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnNewShapeAssigned -= HandleNewShape;
                GameManager.Instance.OnNewShapeAssigned += HandleNewShape;
            }
        }

        private void Update()
        {
            if (!canDraw) return;

            HandleDrawingInput();
        }

        // ════════════════════════════════════════════
        // Event Handler'lar
        // ════════════════════════════════════════════

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.WaitingForDraw:
                    canDraw = true;
                    break;

                case GameState.Drawing:
                    canDraw = true;
                    break;

                case GameState.SpellCasting:
                case GameState.GameOver:
                case GameState.Validating:
                    canDraw = false;
                    break;

                case GameState.Spawning:
                    canDraw = false;
                    ClearDrawing();
                    break;
            }
        }

        private void HandleNewShape(ShapeType shape)
        {
            currentShape = shape;
            ClearDrawing();
            ShowReferenceShape(shape);
        }

        // ════════════════════════════════════════════
        // Çizim Girişi
        // ════════════════════════════════════════════

        /// <summary>
        /// InputManager üzerinden girişi işler ve çizim noktalarını toplar.
        /// (Fare, Simülatör veya Seri Port - hepsi desteklenir)
        /// </summary>
        private void HandleDrawingInput()
        {
            if (InputManager.Instance == null) return;

            // InputManager üzerinden pozisyonu al (dünya koordinatında)
            Vector2 drawPos = InputManager.Instance.GetDrawPosition();
            bool isInputDrawing = InputManager.Instance.IsDrawing;

            // Çizim alanı içinde mi kontrol et
            bool inBounds = IsInBounds(drawPos);

            // ── Çizime Başla ──
            if (isInputDrawing && !isDrawing && inBounds)
            {
                StartDrawing(drawPos);
            }
            // ── Çizim Devam Ediyor ──
            else if (isInputDrawing && isDrawing)
            {
                ContinueDrawing(drawPos);
            }
            // ── Çizim Bitirildi ──
            else if (!isInputDrawing && isDrawing)
            {
                FinishDrawing();
            }

            // ── Debug kısayolları ──
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                // Space: Başarı simüle et
                if (GameManager.Instance != null)
                    GameManager.Instance.DebugForceSuccess();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                // Escape: Başarısızlık simüle et
                if (GameManager.Instance != null)
                    GameManager.Instance.DebugForceFail();
            }
        }

        /// <summary>
        /// Çizimi başlatır.
        /// </summary>
        private void StartDrawing(Vector2 position)
        {
            isDrawing = true;
            drawnPoints.Clear();

            // Yeni çizgi oluştur
            CreateNewLine();

            // İlk noktayı ekle
            AddDrawPoint(position);

            // GameManager'a bildir
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingStarted();
            }

            Debug.Log("[DrawingCanvas] Çizim başladı");
        }

        /// <summary>
        /// Çizime devam eder (fare sürüklenirken).
        /// </summary>
        private void ContinueDrawing(Vector2 position)
        {
            // Yeterli mesafe kontrolü
            if (drawnPoints.Count > 0)
            {
                float distance = Vector2.Distance(position, drawnPoints[drawnPoints.Count - 1]);
                if (distance < minPointDistance) return;
            }

            AddDrawPoint(position);
        }

        /// <summary>
        /// Çizimi bitirir ve doğrulama başlatır.
        /// </summary>
        private void FinishDrawing()
        {
            isDrawing = false;

            Debug.Log($"[DrawingCanvas] Çizim bitti. Toplam nokta: {drawnPoints.Count}");

            // Doğrulama
            ValidateDrawing();
        }

        /// <summary>
        /// Çizilen şekli doğrular ve sonucu GameManager'a bildirir.
        /// </summary>
        private void ValidateDrawing()
        {
            // Zorluk seviyesine göre eşik değeri al
            float threshold = 0.18f;
            if (DifficultyManager.Instance != null)
            {
                threshold = DifficultyManager.Instance.GetCurrentThreshold();
            }

            // Doğrula
            float rmse;
            bool isValid = ShapeValidator.Validate(drawnPoints, currentShape, threshold, out rmse);

            // Çizgi rengini güncelle (geri bildirim)
            if (currentLine != null)
            {
                if (isValid)
                    currentLine.SetSuccessColor();
                else
                    currentLine.SetFailColor();
            }

            // GameManager'a sonucu bildir
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingCompleted(isValid, rmse);
            }

            // Hatalıysa ekranı temizle (yeni çizim için hazırlan)
            if (!isValid)
            {
                // Kısa bir süre sonra çizgiyi temizleyelim ki kırmızı hatayı görsün
                Invoke(nameof(ClearDrawing), 0.3f);
            }
        }

        // ════════════════════════════════════════════
        // Çizgi Yönetimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Yeni bir DrawingLine nesnesi oluşturur.
        /// </summary>
        private void CreateNewLine()
        {
            // Eski çizgiyi temizle
            if (currentLine != null)
            {
                Destroy(currentLine.gameObject);
            }

            GameObject lineObj = new GameObject("DrawingLine");
            lineObj.transform.SetParent(transform);

            // LineRenderer ekle
            lineObj.AddComponent<LineRenderer>();
            currentLine = lineObj.AddComponent<DrawingLine>();
        }

        /// <summary>
        /// Çizime yeni bir nokta ekler.
        /// </summary>
        private void AddDrawPoint(Vector2 position)
        {
            drawnPoints.Add(position);

            if (currentLine != null)
            {
                currentLine.AddPoint(new Vector3(position.x, position.y, 0f));
            }
        }

        /// <summary>
        /// Çizimi temizler.
        /// </summary>
        public void ClearDrawing()
        {
            isDrawing = false;
            drawnPoints.Clear();

            if (currentLine != null)
            {
                Destroy(currentLine.gameObject);
                currentLine = null;
            }
        }

        // ════════════════════════════════════════════
        // Referans Şekil Gösterimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Referans LineRenderer'ı oluşturur.
        /// </summary>
        private void SetupReferenceLineRenderer()
        {
            GameObject refObj = new GameObject("ReferenceShape");
            refObj.transform.SetParent(transform);

            referenceLineRenderer = refObj.AddComponent<LineRenderer>();
            referenceLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            referenceLineRenderer.startWidth = 0.05f;
            referenceLineRenderer.endWidth = 0.05f;
            referenceLineRenderer.useWorldSpace = true;
            referenceLineRenderer.sortingOrder = 1;
            referenceLineRenderer.loop = true;
            referenceLineRenderer.numCornerVertices = 5;
            referenceLineRenderer.numCapVertices = 5;
        }

        /// <summary>
        /// Belirtilen şeklin referans çizgisini gösterir.
        /// </summary>
        private void ShowReferenceShape(ShapeType shape)
        {
            if (referenceLineRenderer == null) return;

            List<Vector2> refPoints = ShapeValidator.GenerateWorldReferencePoints(
                shape, canvasCenter, referenceSize);

            referenceLineRenderer.positionCount = refPoints.Count;
            for (int i = 0; i < refPoints.Count; i++)
            {
                referenceLineRenderer.SetPosition(i,
                    new Vector3(refPoints[i].x, refPoints[i].y, 0f));
            }

            // Şekle göre renk (yarı saydam)
            Color refColor;
            switch (shape)
            {
                case ShapeType.Circle:
                    refColor = new Color(1f, 0.4f, 0.2f, referenceAlpha);
                    break;
                case ShapeType.Square:
                    refColor = new Color(0.3f, 0.5f, 1f, referenceAlpha);
                    break;
                case ShapeType.Triangle:
                    refColor = new Color(0.4f, 0.9f, 0.3f, referenceAlpha);
                    break;
                default:
                    refColor = new Color(1f, 1f, 1f, referenceAlpha);
                    break;
            }

            referenceLineRenderer.startColor = refColor;
            referenceLineRenderer.endColor = refColor;
        }

        /// <summary>
        /// Referans şekli gizler.
        /// </summary>
        public void HideReferenceShape()
        {
            if (referenceLineRenderer != null)
            {
                referenceLineRenderer.positionCount = 0;
            }
        }

        // ════════════════════════════════════════════
        // Yardımcı
        // ════════════════════════════════════════════

        /// <summary>
        /// Pozisyonun çizim alanı sınırları içinde olup olmadığını kontrol eder.
        /// </summary>
        private bool IsInBounds(Vector2 position)
        {
            float halfW = canvasSize.x / 2f;
            float halfH = canvasSize.y / 2f;

            return position.x >= canvasCenter.x - halfW &&
                   position.x <= canvasCenter.x + halfW &&
                   position.y >= canvasCenter.y - halfH &&
                   position.y <= canvasCenter.y + halfH;
        }

        /// <summary>
        /// Editor'da çizim alanı sınırlarını gösterir.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(
                new Vector3(canvasCenter.x, canvasCenter.y, 0f),
                new Vector3(canvasSize.x, canvasSize.y, 0f));
        }
    }
}
