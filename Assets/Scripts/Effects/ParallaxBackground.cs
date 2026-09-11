// ============================================================
// ParallaxBackground.cs — REHABIT-EL
// Çok katmanlı parallax kayan arka plan sistemi.
// Gökyüzü, dağlar, orman ve ön plan katmanları.
// ============================================================
using UnityEngine;

namespace RehabitEL.Effects
{
    /// <summary>
    /// Derinlik hissi veren çok katmanlı parallax arka plan.
    /// Her katman farklı hızda kayarak 3D benzeri bir derinlik efekti oluşturur.
    /// ProceduralSpriteFactory ile oluşturulan sprite'ları kullanır.
    /// Atmosferik parçacık efektleri (ışık tozu, yapraklar) içerir.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Parallax Hızları")]
        [Tooltip("Gökyüzü kayma hızı (en yavaş)")]
        [SerializeField] private float skySpeed = 0.02f;

        [Tooltip("Dağ katmanı kayma hızı")]
        [SerializeField] private float mountainSpeed = 0.05f;

        [Tooltip("Orman katmanı kayma hızı")]
        [SerializeField] private float forestSpeed = 0.12f;

        [Tooltip("Ön plan kayma hızı (en hızlı)")]
        [SerializeField] private float foregroundSpeed = 0.25f;

        [Header("Parçacık Efektleri")]
        [Tooltip("Atmosferik parçacık göster")]
        [SerializeField] private bool showParticles = true;

        [Tooltip("Parçacık sayısı")]
        [SerializeField] private int particleCount = 30;

        // ── Katman Referansları ──
        private SpriteRenderer skyRenderer;
        private SpriteRenderer skyRenderer2; // Tiling kopyası
        private SpriteRenderer mountainRenderer;
        private SpriteRenderer mountainRenderer2;
        private SpriteRenderer forestRenderer;
        private SpriteRenderer forestRenderer2;
        private SpriteRenderer foregroundRenderer;
        private SpriteRenderer foregroundRenderer2;

        private float skyWidth;
        private float mountainWidth;
        private float forestWidth;
        private float foregroundWidth;
        private Camera cachedMainCamera;

        // ── Parçacıklar ──
        private ParticleSystem dustParticles;

        // ── Singleton ──
        public static ParallaxBackground Instance { get; private set; }

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
        }

        private void Start()
        {
            if (!TryEnsureMainCamera()) return;
            CreateBackground();
            if (showParticles)
            {
                CreateAtmosphericParticles();
            }
        }

        private void Update()
        {
            if (!TryEnsureMainCamera()) return;
            ScrollLayer(skyRenderer, skyRenderer2, skySpeed, skyWidth);
            ScrollLayer(mountainRenderer, mountainRenderer2, mountainSpeed, mountainWidth);
            ScrollLayer(forestRenderer, forestRenderer2, forestSpeed, forestWidth);
            ScrollLayer(foregroundRenderer, foregroundRenderer2, foregroundSpeed, foregroundWidth);
        }

        // ════════════════════════════════════════════
        // Arka Plan Oluşturma
        // ════════════════════════════════════════════

        /// <summary>
        /// Tüm parallax katmanlarını oluşturur.
        /// </summary>
        private void CreateBackground()
        {
            // Kamera boyutlarını al
            if (!TryEnsureMainCamera()) return;
            Camera cam = cachedMainCamera;
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;

            // ── 1. Gökyüzü (en arka) ──
            Sprite skySpr = ProceduralSpriteFactory.CreateSkyBackground();
            skyRenderer = CreateLayerObject("BG_Sky", skySpr, -10, transform);
            skyRenderer.drawMode = SpriteDrawMode.Tiled;
            // Sprite'ı ekranı kaplaması için ölçekle
            float skyScaleX = camWidth / (skySpr.bounds.size.x) * 1.2f;
            float skyScaleY = camHeight / (skySpr.bounds.size.y) * 1.2f;
            skyRenderer.transform.localScale = new Vector3(skyScaleX, skyScaleY, 1f);
            skyRenderer2 = DuplicateLayer(skyRenderer, "BG_Sky2");
            skyWidth = skySpr.bounds.size.x * skyScaleX;

            // ── 2. Dağlar ──
            Sprite mtnSpr = ProceduralSpriteFactory.CreateMountainLayer();
            mountainRenderer = CreateLayerObject("BG_Mountains", mtnSpr, -8, transform);
            float mtnScaleX = camWidth / mtnSpr.bounds.size.x * 1.2f;
            float mtnScale = Mathf.Max(mtnScaleX, 1.5f);
            mountainRenderer.transform.localScale = new Vector3(mtnScale, mtnScale, 1f);
            mountainRenderer.transform.localPosition = new Vector3(0f, -camHeight * 0.15f, 0f);
            mountainRenderer2 = DuplicateLayer(mountainRenderer, "BG_Mountains2");
            mountainWidth = mtnSpr.bounds.size.x * mtnScale;

            // ── 3. Orman ──
            Sprite forestSpr = ProceduralSpriteFactory.CreateForestLayer();
            forestRenderer = CreateLayerObject("BG_Forest", forestSpr, -6, transform);
            float frstScaleX = camWidth / forestSpr.bounds.size.x * 1.2f;
            float frstScale = Mathf.Max(frstScaleX, 2f);
            forestRenderer.transform.localScale = new Vector3(frstScale, frstScale, 1f);
            forestRenderer.transform.localPosition = new Vector3(0f, -camHeight * 0.30f, 0f);
            forestRenderer2 = DuplicateLayer(forestRenderer, "BG_Forest2");
            forestWidth = forestSpr.bounds.size.x * frstScale;

            // ── 4. Ön plan ──
            Sprite fgSpr = ProceduralSpriteFactory.CreateForegroundLayer();
            foregroundRenderer = CreateLayerObject("BG_Foreground", fgSpr, -4, transform);
            float fgScaleX = camWidth / fgSpr.bounds.size.x * 1.2f;
            float fgScale = Mathf.Max(fgScaleX, 2.5f);
            foregroundRenderer.transform.localScale = new Vector3(fgScale, fgScale, 1f);
            foregroundRenderer.transform.localPosition = new Vector3(0f, -camHeight * 0.42f, 0f);
            foregroundRenderer2 = DuplicateLayer(foregroundRenderer, "BG_Foreground2");
            foregroundWidth = fgSpr.bounds.size.x * fgScale;
        }

        /// <summary>
        /// Atmosferik parçacık efektleri oluşturur (ışık tozu, yaprak parçacıkları).
        /// </summary>
        private void CreateAtmosphericParticles()
        {
            if (!TryEnsureMainCamera()) return;

            // Işık tozu / peri tozu efekti
            GameObject dustObj = new GameObject("AtmosphericDust");
            dustObj.transform.SetParent(transform);
            dustObj.transform.localPosition = Vector3.zero;

            dustParticles = dustObj.AddComponent<ParticleSystem>();
            var main = dustParticles.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.maxParticles = particleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.9f, 1.0f, 0.3f),
                new Color(0.5f, 1.0f, 0.6f, 0.2f)
            );

            // Emisyon
            var emission = dustParticles.emission;
            emission.rateOverTime = particleCount / 5f;

            // Şekil (geniş alan)
            Camera cam = cachedMainCamera;
            var shape = dustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(cam.orthographicSize * cam.aspect * 2f, cam.orthographicSize * 2f, 1f);

            // Boyut küçülme
            var sizeOverLife = dustParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renk fade
            var colorOverLife = dustParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.3f),
                    new GradientAlphaKey(0.5f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // Gürültü (doğal hareket)
            var noise = dustParticles.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.2f;

            // Renderer
            var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = -3;

            dustParticles.Play();
        }

        // ════════════════════════════════════════════
        // Katman Yardımcıları
        // ════════════════════════════════════════════

        private SpriteRenderer CreateLayerObject(string name, Sprite sprite, int sortingOrder, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private SpriteRenderer DuplicateLayer(SpriteRenderer original, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(original.transform.parent);
            obj.transform.localScale = original.transform.localScale;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = original.sprite;
            sr.sortingOrder = original.sortingOrder;

            // İkinci kopyayı sağa yerleştir
            float width = original.sprite.bounds.size.x * original.transform.localScale.x;
            obj.transform.localPosition = original.transform.localPosition + new Vector3(width, 0f, 0f);

            return sr;
        }

        private void ScrollLayer(SpriteRenderer layer1, SpriteRenderer layer2, float speed, float layerWidth)
        {
            if (layer1 == null || layer2 == null) return;
            if (!TryEnsureMainCamera()) return;

            float offset = speed * Time.deltaTime;

            layer1.transform.position -= new Vector3(offset, 0f, 0f);
            layer2.transform.position -= new Vector3(offset, 0f, 0f);

            // Sonsuz döngü — sol kenara çıkanı sağa taşı
            float leftEdge = -cachedMainCamera.orthographicSize * cachedMainCamera.aspect;
            if (layer1.transform.position.x + layerWidth / 2f < leftEdge)
            {
                layer1.transform.position = new Vector3(
                    layer2.transform.position.x + layerWidth,
                    layer1.transform.position.y,
                    layer1.transform.position.z);
            }

            if (layer2.transform.position.x + layerWidth / 2f < leftEdge)
            {
                layer2.transform.position = new Vector3(
                    layer1.transform.position.x + layerWidth,
                    layer2.transform.position.y,
                    layer2.transform.position.z);
            }
        }

        private bool TryEnsureMainCamera()
        {
            if (cachedMainCamera != null)
                return true;

            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null && Camera.allCamerasCount > 0)
            {
                cachedMainCamera = Camera.allCameras[0];
            }

            if (cachedMainCamera == null)
            {
                Debug.LogWarning("[ParallaxBackground] Main Camera bulunamadı, parallax güncellenmiyor.");
                return false;
            }

            return true;
        }
    }
}
