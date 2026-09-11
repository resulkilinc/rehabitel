// ============================================================
// WizardController.cs — REHABIT-EL
// Büyücü karakterin görsel yönetimi: idle animasyonu, büyü atma,
// glow/aura efektleri.
// ============================================================
using System.Collections;
using UnityEngine;
using RehabitEL.Core;

namespace RehabitEL.Effects
{
    /// <summary>
    /// Büyücü karakterin görsellerini ve animasyonlarını yönetir.
    /// ProceduralSpriteFactory ile oluşturulan sprite'ı kullanır.
    /// Idle sırasında hafif salınım, büyü atarken glow efekti oluşturur.
    /// </summary>
    public class WizardController : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Pozisyon")]
        [Tooltip("Büyücü pozisyonu")]
        [SerializeField] private Vector3 wizardPosition = new Vector3(-5f, -0.5f, 0f);

        [Header("Idle Animasyon")]
        [Tooltip("Yukarı-aşağı salınım genliği")]
        [SerializeField] private float bobAmplitude = 0.08f;

        [Tooltip("Salınım hızı")]
        [SerializeField] private float bobFrequency = 1.5f;

        [Header("Büyü Efekti")]
        [Tooltip("Büyü atarken parlama rengi")]
        [SerializeField] private Color castGlowColor = new Color(0.3f, 0.8f, 1f, 0.5f);

        [Tooltip("Büyü atma süresi")]
        [SerializeField] private float castDuration = 0.6f;

        // ── Bileşenler ──
        private SpriteRenderer wizardRenderer;
        private SpriteRenderer auraRenderer;
        private GameObject wizardObj;
        private Vector3 basePosition;
        private bool isCasting = false;
        private Color originalColor;

        // ── Singleton ──
        public static WizardController Instance { get; private set; }

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
            CreateWizard();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!isCasting)
            {
                AnimateIdle();
            }
        }

        // ════════════════════════════════════════════
        // Oluşturma
        // ════════════════════════════════════════════

        private void CreateWizard()
        {
            // Ana büyücü objesi
            wizardObj = new GameObject("Wizard");
            wizardObj.transform.SetParent(transform);
            wizardObj.transform.position = wizardPosition;
            basePosition = wizardPosition;

            // Sprite
            wizardRenderer = wizardObj.AddComponent<SpriteRenderer>();
            wizardRenderer.sprite = ProceduralSpriteFactory.CreateWizardSprite();
            wizardRenderer.sortingOrder = 5;
            originalColor = Color.white;

            // Aura (glow efekti — büyücünün arkasında)
            GameObject auraObj = new GameObject("WizardAura");
            auraObj.transform.SetParent(wizardObj.transform);
            auraObj.transform.localPosition = new Vector3(0f, 0.5f, 0.1f);
            auraObj.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

            auraRenderer = auraObj.AddComponent<SpriteRenderer>();
            // Basit dairesel glow sprite oluştur
            auraRenderer.sprite = CreateGlowSprite();
            auraRenderer.sortingOrder = 4;
            auraRenderer.color = new Color(0.2f, 0.4f, 0.8f, 0.1f); // Çok hafif mavi glow
        }

        private Sprite CreateGlowSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];

            int cx = size / 2;
            int cy = size / 2;
            float maxR = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(1f - (dist / maxR));
                    alpha = alpha * alpha; // Smooth falloff
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * 0.5f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        // ════════════════════════════════════════════
        // Animasyonlar
        // ════════════════════════════════════════════

        /// <summary>
        /// Idle animasyonu: hafif yukarı-aşağı salınım + cüppe dalgalanması.
        /// </summary>
        private void AnimateIdle()
        {
            if (wizardObj == null) return;

            float bobOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            wizardObj.transform.position = basePosition + new Vector3(0f, bobOffset, 0f);

            // Aura pulsasyon
            if (auraRenderer != null)
            {
                float pulse = 0.08f + Mathf.Sin(Time.time * 2f) * 0.04f;
                Color auraColor = auraRenderer.color;
                auraColor.a = pulse;
                auraRenderer.color = auraColor;
            }
        }

        /// <summary>
        /// Büyü atma animasyonu: parıltı + büyüme + glow.
        /// </summary>
        public void PlayCastAnimation(ShapeType shape)
        {
            if (isCasting) return;
            StartCoroutine(CastAnimationRoutine(shape));
        }

        private IEnumerator CastAnimationRoutine(ShapeType shape)
        {
            isCasting = true;

            // Şekle göre büyü rengi
            Color spellColor;
            switch (shape)
            {
                case ShapeType.Circle:
                    spellColor = new Color(1f, 0.5f, 0.1f);      // Ateş — turuncu
                    break;
                case ShapeType.Square:
                    spellColor = new Color(0.3f, 0.7f, 1.0f);    // Elektrik — mavi
                    break;
                case ShapeType.Triangle:
                    spellColor = new Color(0.3f, 1f, 0.2f);      // Zehir — yeşil
                    break;
                default:
                    spellColor = Color.white;
                    break;
            }

            float halfDuration = castDuration / 2f;

            // ── Faz 1: Power up ──
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;

                // Wizard parlama
                if (wizardRenderer != null)
                {
                    wizardRenderer.color = Color.Lerp(originalColor, spellColor, t * 0.5f);
                }

                // Aura büyüme + renk değişimi
                if (auraRenderer != null)
                {
                    auraRenderer.color = new Color(spellColor.r, spellColor.g, spellColor.b, t * 0.4f);
                    auraRenderer.transform.localScale = Vector3.Lerp(
                        new Vector3(2.5f, 2.5f, 1f),
                        new Vector3(3.5f, 3.5f, 1f), t);
                }

                // Wizard hafif geri çekilme (asa kaldırma efekti)
                if (wizardObj != null)
                {
                    wizardObj.transform.position = basePosition + new Vector3(-0.1f * t, 0.15f * t, 0f);
                }

                yield return null;
            }

            // ── Faz 2: Release ──
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;

                // Wizard normale dön
                if (wizardRenderer != null)
                {
                    wizardRenderer.color = Color.Lerp(spellColor * 0.5f + originalColor * 0.5f, originalColor, t);
                }

                // Aura geri küçülme
                if (auraRenderer != null)
                {
                    auraRenderer.color = new Color(spellColor.r, spellColor.g, spellColor.b, (1f - t) * 0.4f);
                    auraRenderer.transform.localScale = Vector3.Lerp(
                        new Vector3(3.5f, 3.5f, 1f),
                        new Vector3(2.5f, 2.5f, 1f), t);
                }

                // Pozisyon normale dön
                if (wizardObj != null)
                {
                    wizardObj.transform.position = Vector3.Lerp(
                        basePosition + new Vector3(-0.1f, 0.15f, 0f),
                        basePosition, t);
                }

                yield return null;
            }

            // Reset
            if (wizardRenderer != null) wizardRenderer.color = originalColor;
            if (auraRenderer != null)
            {
                auraRenderer.color = new Color(0.2f, 0.4f, 0.8f, 0.1f);
                auraRenderer.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            }

            isCasting = false;
        }

        // ════════════════════════════════════════════
        // Event Abonelikleri
        // ════════════════════════════════════════════

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
                GameManager.Instance.OnDrawingValidated += HandleDrawingResult;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
            }
        }

        private void HandleDrawingResult(bool success, float rmse)
        {
            if (success && GameManager.Instance != null)
            {
                PlayCastAnimation(GameManager.Instance.CurrentShape);
            }
        }

        /// <summary>Büyücünün dünya pozisyonunu döndürür (büyü spawn noktası olarak).</summary>
        public Vector3 GetStaffTipPosition()
        {
            if (wizardObj == null) return wizardPosition;
            // Asa ucunun yaklaşık pozisyonu (büyücünün sağ üstü)
            return wizardObj.transform.position + new Vector3(0.6f, 1.5f, 0f);
        }
    }
}
