// ============================================================
// Monster.cs — REHABIT-EL
// Canavar davranışı: sola hareket, şekil gösterimi, süre yönetimi.
// Şekle göre dinamik sprite ve animasyonlar.
// ============================================================
using UnityEngine;
using RehabitEL.Core;
using RehabitEL.Effects;

namespace RehabitEL.Monsters
{
    /// <summary>
    /// Tek bir canavarın davranışını yönetir.
    /// Sağdan sola hareket eder, üzerinde atanmış şekli gösterir.
    /// ProceduralSpriteFactory ile oluşturulan şekle özel sprite kullanır.
    /// Yürüme animasyonu (hop/salınım) ve gelişmiş ölüm efektleri içerir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Monster : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Hareket")]
        [Tooltip("Temel hareket hızı (birim/saniye)")]
        [SerializeField] private float baseSpeed = 1.5f;

        [Header("Animasyon")]
        [Tooltip("Hop (zıplama) genliği")]
        [SerializeField] private float hopAmplitude = 0.15f;

        [Tooltip("Hop frekansı (saniye başına zıplama)")]
        [SerializeField] private float hopFrequency = 3f;

        [Tooltip("Sallanma genliği (derece)")]
        [SerializeField] private float swayAmount = 8f;

        [Header("Görsel")]
        [Tooltip("Şekil göstergesi için alt nesne (TextMesh veya SpriteRenderer)")]
        [SerializeField] private TextMesh shapeLabel;

        [Tooltip("Şekil ikonu SpriteRenderer (opsiyonel - sprite ile şekil gösterimi)")]
        [SerializeField] private SpriteRenderer shapeIconRenderer;

        // ── Durum ──
        private ShapeType assignedShape;
        private bool isAlive = true;
        private bool canMove = true;
        private float speed;
        private SpriteRenderer spriteRenderer;
        private Vector3 basePosition;
        private float animTime;

        // ── Properties ──
        /// <summary>Bu canavara atanmış şekil.</summary>
        public ShapeType AssignedShape => assignedShape;

        /// <summary>Canavar hâlâ hayatta mı?</summary>
        public bool IsAlive => isAlive;

        // ════════════════════════════════════════════
        // Unity Lifecycle
        // ════════════════════════════════════════════

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (shapeLabel == null)
            {
                shapeLabel = GetComponentInChildren<TextMesh>(includeInactive: true);
            }

            if (shapeIconRenderer == null)
            {
                // If a child sprite renderer exists (other than the main one), use it as shape icon renderer.
                var srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
                foreach (var sr in srs)
                {
                    if (sr != null && sr != spriteRenderer)
                    {
                        shapeIconRenderer = sr;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (!isAlive) return;

            animTime += Time.deltaTime;

            if (canMove)
            {
                // Yatay hareket
                float dx = speed * Time.deltaTime;
                basePosition += Vector3.left * dx;
            }

            // Hop animasyonu (sinüs dalga ile zıplama)
            float hopOffset = Mathf.Abs(Mathf.Sin(animTime * hopFrequency * Mathf.PI)) * hopAmplitude;

            // Sola-sağa sallanma
            float swayAmountToUse = canMove ? swayAmount : (swayAmount * 0.2f); // Dururken hafif sallan
            float swayAngle = Mathf.Sin(animTime * hopFrequency * Mathf.PI * 0.5f) * swayAmountToUse;

            // Uygula
            transform.position = basePosition + new Vector3(0f, hopOffset, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, swayAngle);

            // Ekran dışına çıkarsa (güvenlik kontrolü)
            if (basePosition.x < -12f)
            {
                Debug.LogWarning("[Monster] Canavar ekran dışına çıktı!");
                Die(false);
            }
        }

        // ════════════════════════════════════════════
        // Başlatma
        // ════════════════════════════════════════════

        /// <summary>
        /// Canavarı belirtilen şekil ve hız ile başlatır.
        /// MonsterSpawner tarafından spawn sonrası çağrılır.
        /// </summary>
        /// <param name="shape">Atanacak şekil türü.</param>
        /// <param name="moveSpeed">Hareket hızı (birim/saniye).</param>
        public void Initialize(ShapeType shape, float moveSpeed)
        {
            assignedShape = shape;
            speed = moveSpeed;
            isAlive = true;
            canMove = true;
            basePosition = transform.position;
            animTime = Random.Range(0f, 2f); // Rastgele başlangıç fazı

            // Görsel ayarları
            ApplyVisuals();

            Debug.Log($"[Monster] Başlatıldı: {shape}, Hız: {moveSpeed:F2}");
        }

        /// <summary>
        /// Şekle göre procedural sprite ve görsel ayarları uygular.
        /// </summary>
        private void ApplyVisuals()
        {
            // Late-bind optional child renderers created at runtime (fallback monster).
            if (shapeLabel == null)
            {
                shapeLabel = GetComponentInChildren<TextMesh>(includeInactive: true);
            }

            // ── 1. Ana gövde sprite'ını şekle göre oluştur ──
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = ProceduralSpriteFactory.CreateMonsterSprite(assignedShape);
                spriteRenderer.color = Color.white; // Sprite kendi rengini taşıyor
            }

            // ── 2. Şekil ikonu (canavar üzerinde) ──
            if (shapeIconRenderer == null)
            {
                // Şekil ikonu için alt nesne oluştur
                GameObject iconObj = new GameObject("ShapeIcon");
                iconObj.transform.SetParent(transform);
                iconObj.transform.localPosition = new Vector3(0f, 0.8f, -0.1f);
                iconObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                shapeIconRenderer = iconObj.AddComponent<SpriteRenderer>();
                shapeIconRenderer.sortingOrder = 7;
            }
            shapeIconRenderer.sprite = ProceduralSpriteFactory.CreateShapeIconSprite(assignedShape);
            shapeIconRenderer.color = Color.white;

            // ── 3. TextMesh güncelle (yedek gösterim) ──
            string symbol;
            switch (assignedShape)
            {
                case ShapeType.Circle:
                    symbol = "●";
                    break;
                case ShapeType.Square:
                    symbol = "■";
                    break;
                case ShapeType.Triangle:
                    symbol = "▲";
                    break;
                default:
                    symbol = "?";
                    break;
            }

            if (shapeLabel != null)
            {
                shapeLabel.text = symbol;
                shapeLabel.color = Color.white;
                shapeLabel.fontSize = 64;
                shapeLabel.anchor = TextAnchor.MiddleCenter;
                shapeLabel.alignment = TextAlignment.Center;
            }
        }

        // ════════════════════════════════════════════
        // Ölüm / Yok Olma
        // ════════════════════════════════════════════

        /// <summary>
        /// Canavarın hedefe doğru ilerlemesini durdurur (büyü gelene kadar "donup" bekler).
        /// </summary>
        public void StopMovement()
        {
            canMove = false;
        }

        /// <summary>
        /// Canavarı öldürür.
        /// </summary>
        /// <param name="bySpell">Büyü ile mi öldü? (true = başarı efekti)</param>
        public void Die(bool bySpell)
        {
            if (!isAlive) return;
            isAlive = false;

            if (bySpell)
            {
                // Başarı: büyü efekti göster + patlama + ölüm animasyonu
                Debug.Log($"[Monster] {assignedShape} canavar büyü ile öldürüldü!");
                SpawnDeathParticles();
                StartCoroutine(DeathAnimation());
            }
            else
            {
                // Başarısızlık: direkt yok et
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Ölüm anında patlama parçacıkları oluşturur.
        /// </summary>
        private void SpawnDeathParticles()
        {
            GameObject particleObj = new GameObject("MonsterDeathFX");
            particleObj.transform.position = transform.position;

            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;

            // Şekle göre patlama rengi
            Color burstColor;
            switch (assignedShape)
            {
                case ShapeType.Circle:
                    burstColor = new Color(1f, 0.5f, 0.1f);
                    break;
                case ShapeType.Square:
                    burstColor = new Color(0.3f, 0.7f, 1f);
                    break;
                case ShapeType.Triangle:
                    burstColor = new Color(0.3f, 1f, 0.2f);
                    break;
                default:
                    burstColor = Color.white;
                    break;
            }

            main.startColor = new ParticleSystem.MinMaxGradient(burstColor, Color.white);

            // Emisyon (burst)
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 30)
            });

            // Şekil
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // Boyut küçülme
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renk fade
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(burstColor, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // Renderer
            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 25;

            ps.Play();
            Destroy(particleObj, 1.5f);
        }

        /// <summary>
        /// Ölüm animasyonu: küçülüp kaybolma + dönme.
        /// </summary>
        private System.Collections.IEnumerator DeathAnimation()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Küçül
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

                // Hızlı dönme
                transform.rotation = Quaternion.Euler(0f, 0f, t * 360f);

                // Fade out
                if (spriteRenderer != null)
                {
                    Color c = originalColor;
                    c.a = 1f - t;
                    spriteRenderer.color = c;
                }

                // Şekil ikonu da fade out
                if (shapeIconRenderer != null)
                {
                    Color ic = shapeIconRenderer.color;
                    ic.a = 1f - t;
                    shapeIconRenderer.color = ic;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        // ════════════════════════════════════════════
        // Sarsıntı Efekti (hasar alınca)
        // ════════════════════════════════════════════

        /// <summary>
        /// Canavar üzerinde kısa sarsıntı efekti.
        /// </summary>
        public void Shake()
        {
            StartCoroutine(ShakeRoutine());
        }

        private System.Collections.IEnumerator ShakeRoutine()
        {
            Vector3 originalPos = basePosition;
            float shakeDuration = 0.3f;
            float shakeAmount = 0.1f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = originalPos.x + UnityEngine.Random.Range(-shakeAmount, shakeAmount);
                float y = originalPos.y + UnityEngine.Random.Range(-shakeAmount, shakeAmount);
                basePosition = new Vector3(x, y, originalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            basePosition = originalPos;
        }
    }
}
