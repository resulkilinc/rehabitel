// ============================================================
// SpellEffect.cs — REHABIT-EL
// Büyü efekt sistemi: büyücüden canavara uçan projectile'lar.
// Ateş topu, elektrik, zehir — şekle göre farklı efektler.
// ============================================================
using System.Collections;
using UnityEngine;
using RehabitEL.Core;

namespace RehabitEL.Effects
{
    /// <summary>
    /// Büyü efekt türleri — her şekle bir büyü tipi atanır.
    /// </summary>
    public enum SpellType
    {
        Fireball,    // Daire → Ateş topu
        Lightning,   // Kare → Elektrik/yıldırım
        Poison       // Üçgen → Zehir
    }

    /// <summary>
    /// Büyü efektlerini yöneten sistem.
    /// Büyücüden canavara doğru projectile fırlatır, varışta patlama efekti oluşturur.
    /// Her şekil türü farklı büyü efektine sahiptir.
    /// </summary>
    public class SpellEffect : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Projectile")]
        [Tooltip("Projectile uçuş hızı")]
        [SerializeField] private float projectileSpeed = 8f;

        [Tooltip("Projectile boyutu")]
        [SerializeField] private float projectileSize = 0.3f;

        // ── Singleton ──
        public static SpellEffect Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ════════════════════════════════════════════
        // Genel Arayüz
        // ════════════════════════════════════════════

        /// <summary>
        /// Şekle göre büyü efekti oluşturur.
        /// Büyücüden hedef canavara doğru projectile fırlatır.
        /// </summary>
        /// <param name="shape">Şekil türü (büyü tipini belirler).</param>
        /// <param name="targetPosition">Hedef konum (canavar pozisyonu).</param>
        public void CastSpell(ShapeType shape, Vector3 targetPosition, System.Action onHit = null)
        {
            // Büyücüden başlangıç noktası al
            Vector3 startPos;
            if (WizardController.Instance != null)
            {
                startPos = WizardController.Instance.GetStaffTipPosition();
            }
            else
            {
                startPos = new Vector3(-4f, 1f, 0f); // Varsayılan
            }

            SpellType spellType;
            switch (shape)
            {
                case ShapeType.Circle:
                    spellType = SpellType.Fireball;
                    break;
                case ShapeType.Square:
                    spellType = SpellType.Lightning;
                    break;
                case ShapeType.Triangle:
                    spellType = SpellType.Poison;
                    break;
                default:
                    spellType = SpellType.Fireball;
                    break;
            }

            StartCoroutine(SpellProjectileRoutine(spellType, startPos, targetPosition, onHit));
        }

        // ════════════════════════════════════════════
        // Projectile Routines
        // ════════════════════════════════════════════

        private IEnumerator SpellProjectileRoutine(SpellType type, Vector3 start, Vector3 target, System.Action onHit)
        {
            // Büyücü animasyonu tetikle
            if (WizardController.Instance != null)
            {
                ShapeType shape = (type == SpellType.Fireball) ? ShapeType.Circle :
                                  (type == SpellType.Lightning) ? ShapeType.Square : ShapeType.Triangle;
                WizardController.Instance.PlayCastAnimation(shape);
            }

            // Kısa gecikme (büyücü power-up animasyonu için)
            yield return new WaitForSeconds(0.3f);

            switch (type)
            {
                case SpellType.Fireball:
                    yield return FireballRoutine(start, target, onHit);
                    break;
                case SpellType.Lightning:
                    yield return LightningRoutine(start, target, onHit);
                    break;
                case SpellType.Poison:
                    yield return PoisonRoutine(start, target, onHit);
                    break;
            }
        }

        // ────────────────────────────────────────────
        // ATEŞ TOPU (Fireball)
        // ────────────────────────────────────────────

        private IEnumerator FireballRoutine(Vector3 start, Vector3 target, System.Action onHit)
        {
            // Projectile objesi oluştur
            GameObject projectile = new GameObject("Fireball");
            SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFireballSprite();
            sr.sortingOrder = 20;
            projectile.transform.position = start;
            projectile.transform.localScale = Vector3.one * projectileSize;

            // Trail efekti
            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.6f, 0.1f, 0.8f);
            trail.endColor = new Color(1f, 0.2f, 0f, 0f);
            trail.sortingOrder = 19;

            // Uçuş
            float journeyTime = Vector3.Distance(start, target) / projectileSpeed;
            float elapsed = 0f;

            while (elapsed < journeyTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / journeyTime;

                // Eğimli yol (hafif yukarı ark)
                Vector3 pos = Vector3.Lerp(start, target, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 0.8f; // Ark efekti

                projectile.transform.position = pos;

                // Döndür
                projectile.transform.Rotate(0, 0, -500f * Time.deltaTime);

                // Boyut büyümesi
                float scale = projectileSize * (1f + t * 0.3f);
                projectile.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            // Patlama
            SpawnImpactExplosion(target, new Color(1f, 0.5f, 0.1f), new Color(1f, 0.9f, 0.3f));
            Destroy(projectile);

            // Ekran sarsıntısı
            if (ScreenShake.Instance != null)
                ScreenShake.Instance.ShakeMedium();

            onHit?.Invoke();
        }

        // ────────────────────────────────────────────
        // ELEKTRİK (Lightning)
        // ────────────────────────────────────────────

        private IEnumerator LightningRoutine(Vector3 start, Vector3 target, System.Action onHit)
        {
            // Elektrik çizgisi (LineRenderer)
            GameObject boltObj = new GameObject("LightningBolt");
            LineRenderer lr = boltObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.5f, 0.8f, 1f, 1f);
            lr.endColor = new Color(0.3f, 0.5f, 1f, 0.5f);
            lr.startWidth = 0.12f;
            lr.endWidth = 0.06f;
            lr.sortingOrder = 20;
            lr.useWorldSpace = true;

            // Zigzag noktaları oluştur
            int segments = 12;
            lr.positionCount = segments + 1;

            // Anlık flash efekti — birkaç kez yanıp sönme
            for (int flash = 0; flash < 3; flash++)
            {
                // Her flash'ta farklı zigzag deseni
                Vector3[] points = GenerateLightningPath(start, target, segments);
                lr.SetPositions(points);

                // Parlama
                lr.startWidth = 0.15f - flash * 0.02f;
                lr.startColor = new Color(0.7f, 0.9f, 1f, 1f - flash * 0.2f);

                // Kıvılcım parçacıkları
                if (flash == 0)
                {
                    SpawnSparks(target, new Color(0.5f, 0.8f, 1f));
                }

                yield return new WaitForSeconds(0.06f);
            }

            // Patlama
            SpawnImpactExplosion(target, new Color(0.3f, 0.6f, 1f), new Color(0.8f, 0.95f, 1f));

            // Fade out
            float fadeDuration = 0.2f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                lr.startColor = new Color(0.5f, 0.8f, 1f, alpha);
                lr.endColor = new Color(0.3f, 0.5f, 1f, alpha * 0.5f);
                lr.startWidth = 0.15f * alpha;
                lr.endWidth = 0.06f * alpha;
                yield return null;
            }

            Destroy(boltObj);

            if (ScreenShake.Instance != null)
                ScreenShake.Instance.ShakeMedium();

            onHit?.Invoke();
        }

        private Vector3[] GenerateLightningPath(Vector3 start, Vector3 end, int segments)
        {
            Vector3[] points = new Vector3[segments + 1];
            points[0] = start;
            points[segments] = end;

            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector3 basePoint = Vector3.Lerp(start, end, t);

                // Rastgele sapma
                float deviation = Random.Range(-0.4f, 0.4f);
                Vector3 perpendicular = Vector3.Cross(end - start, Vector3.forward).normalized;
                points[i] = basePoint + perpendicular * deviation;
            }

            return points;
        }

        // ────────────────────────────────────────────
        // ZEHİR (Poison)
        // ────────────────────────────────────────────

        private IEnumerator PoisonRoutine(Vector3 start, Vector3 target, System.Action onHit)
        {
            // Zehir bulutu projectile
            GameObject projectile = new GameObject("PoisonCloud");
            SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePoisonSprite();
            sr.sortingOrder = 20;
            projectile.transform.position = start;
            projectile.transform.localScale = Vector3.one * projectileSize * 0.8f;

            // Trail efekti
            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(0.3f, 0.9f, 0.2f, 0.6f);
            trail.endColor = new Color(0.5f, 0f, 0.8f, 0f);
            trail.sortingOrder = 19;

            // Dalga şeklinde uçuş
            float journeyTime = Vector3.Distance(start, target) / (projectileSpeed * 0.8f);
            float elapsed = 0f;

            while (elapsed < journeyTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / journeyTime;

                Vector3 pos = Vector3.Lerp(start, target, t);
                // Dalga hareketi
                pos.y += Mathf.Sin(t * Mathf.PI * 4f) * 0.3f;

                projectile.transform.position = pos;

                // Boyut pulsasyonu
                float pulsate = 1f + Mathf.Sin(elapsed * 15f) * 0.15f;
                projectile.transform.localScale = Vector3.one * projectileSize * 0.8f * pulsate;

                // Renk değişimi
                float hue = Mathf.Lerp(0.33f, 0.80f, Mathf.PingPong(elapsed * 3f, 1f));
                sr.color = Color.HSVToRGB(hue, 0.8f, 1f);

                yield return null;
            }

            // Zehir bulutu patlaması
            SpawnPoisonCloud(target);
            Destroy(projectile);

            if (ScreenShake.Instance != null)
                ScreenShake.Instance.ShakeLight();

            onHit?.Invoke();
        }

        // ════════════════════════════════════════════
        // Efekt Yardımcıları
        // ════════════════════════════════════════════

        private void SpawnImpactExplosion(Vector3 position, Color mainColor, Color secondaryColor)
        {
            GameObject fxObj = new GameObject("SpellImpact");
            fxObj.transform.position = position;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;
            main.startColor = new ParticleSystem.MinMaxGradient(mainColor, secondaryColor);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 40)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(mainColor, 0f),
                    new GradientColorKey(secondaryColor, 0.5f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = fxObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 22;

            ps.Play();
            Destroy(fxObj, 1.5f);
        }

        private void SpawnSparks(Vector3 position, Color sparkColor)
        {
            GameObject fxObj = new GameObject("Sparks");
            fxObj.transform.position = position;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.2f;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = 0.05f;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;
            main.startColor = sparkColor;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 15)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var renderer = fxObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 21;

            ps.Play();
            Destroy(fxObj, 1f);
        }

        private void SpawnPoisonCloud(Vector3 position)
        {
            GameObject fxObj = new GameObject("PoisonCloud");
            fxObj.transform.position = position;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.3f, 0.9f, 0.2f, 0.7f),
                new Color(0.5f, 0.0f, 0.8f, 0.5f)
            );
            main.gravityModifier = -0.3f; // Yukarı yüzme efekti

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 25)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.3f, 0.9f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.5f, 0f, 0.8f), 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.1f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 2f;

            var renderer = fxObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 22;

            ps.Play();
            Destroy(fxObj, 2.5f);
        }

        // ════════════════════════════════════════════
        // Sprite Oluşturma
        // ════════════════════════════════════════════

        private Sprite CreateFireballSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[size * size];
            int cx = size / 2, cy = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float maxR = size / 2f;
                    if (dist < maxR)
                    {
                        float t = dist / maxR;
                        Color c = Color.Lerp(
                            new Color(1f, 1f, 0.5f, 1f),  // İç: parlak sarı
                            new Color(1f, 0.3f, 0f, 0.5f), // Dış: turuncu-kırmızı
                            t);
                        pixels[y * size + x] = c;
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreatePoisonSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[size * size];
            int cx = size / 2, cy = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float maxR = size / 2f;
                    if (dist < maxR)
                    {
                        float t = dist / maxR;
                        Color c = Color.Lerp(
                            new Color(0.5f, 1f, 0.3f, 0.9f),  // İç: parlak yeşil
                            new Color(0.4f, 0f, 0.7f, 0.3f),   // Dış: mor
                            t);
                        pixels[y * size + x] = c;
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
