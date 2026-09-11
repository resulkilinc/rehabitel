// ============================================================
// FeedbackManager.cs — REHABIT-EL
// Görsel ve işitsel geri bildirim: Flash, sarsıntı, ses.
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RehabitEL.Core;
using RehabitEL.Monsters;

namespace RehabitEL.Effects
{
    /// <summary>
    /// Başarı/başarısızlık durumlarında görsel ve işitsel geri bildirim sağlar.
    /// - Ekran flash efekti (yeşil/kırmızı)
    /// - Kamera sarsıntısı
    /// - Büyü efekti tetikleme
    /// - Ses efektleri
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Ekran Flash")]
        [Tooltip("Flash overlay Image (Canvas üzerinde tam ekran)")]
        [SerializeField] private Image flashOverlay;

        [Tooltip("Flash süresi")]
        [SerializeField] private float flashDuration = 0.3f;

        [Header("Kamera Sarsıntısı")]
        [Tooltip("Sarsıntı şiddeti")]
        [SerializeField] private float shakeAmount = 0.15f;

        [Tooltip("Sarsıntı süresi")]
        [SerializeField] private float shakeDuration = 0.3f;

        [Header("Ses Efektleri")]
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failSound;
        [SerializeField] private AudioClip spellFireSound;
        [SerializeField] private AudioClip spellLightningSound;
        [SerializeField] private AudioClip spellPoisonSound;

        [Header("Ses Ayarları")]
        [SerializeField] private float sfxVolume = 0.7f;

        // ── Component Referansları ──
        private AudioSource audioSource;
        private Camera mainCamera;
        private Vector3 originalCameraPos;

        // ── Singleton ──
        public static FeedbackManager Instance { get; private set; }

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

            // AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;

            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCameraPos = mainCamera.transform.position;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingValidated += HandleDrawingResult;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
            }
        }

        private void Start()
        {
            // Flash overlay başlangıçta görünmez
            if (flashOverlay != null)
            {
                Color c = flashOverlay.color;
                c.a = 0f;
                flashOverlay.color = c;
            }

            // Event aboneliği güvence
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
                GameManager.Instance.OnDrawingValidated += HandleDrawingResult;
            }
        }

        // ════════════════════════════════════════════
        // Event Handler
        // ════════════════════════════════════════════

        private void HandleDrawingResult(bool success, float rmse)
        {
            if (success)
            {
                PlaySuccessFeedback();
            }
            else
            {
                // Artık sadece hata yaptık, oyun bitmediği için Mistake feedback ver
                PlayMistakeFeedback();
            }
        }

        // ════════════════════════════════════════════
        // Başarı Geri Bildirimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Başarılı çizim geri bildirimi: yeşil flash + büyü efekti + ses.
        /// </summary>
        public void PlaySuccessFeedback()
        {
            // Yeşil flash
            StartCoroutine(FlashScreen(new Color(0.2f, 1f, 0.3f, 0.4f)));

            // Büyü projectile efekti (büyücüden canavara)
            if (SpellEffect.Instance != null && MonsterSpawner.Instance != null)
            {
                Monster currentMonster = MonsterSpawner.Instance.CurrentMonster;
                if (currentMonster != null)
                {
                    SpellEffect.Instance.CastSpell(
                        currentMonster.AssignedShape,
                        currentMonster.transform.position,
                        () =>
                        {
                            // Büyü hedefe vardığında (onHit) canavarı patlat
                            if (currentMonster != null)
                            {
                                currentMonster.Die(true);
                            }
                        });
                }
            }

            // Ses
            PlaySound(successSound);

            // Şekle göre büyü sesi
            if (GameManager.Instance != null)
            {
                ShapeType shape = GameManager.Instance.CurrentShape;
                switch (shape)
                {
                    case ShapeType.Circle:
                        PlaySound(spellFireSound);
                        break;
                    case ShapeType.Square:
                        PlaySound(spellLightningSound);
                        break;
                    case ShapeType.Triangle:
                        PlaySound(spellPoisonSound);
                        break;
                }
            }
        }

        // ════════════════════════════════════════════
        // Başarısızlık Geri Bildirimi
        // ════════════════════════════════════════════

        /// <summary>
        /// Yanlış çizim yapıldığında (hata): hafif kırmızı flash + küçük sarsıntı.
        /// </summary>
        public void PlayMistakeFeedback()
        {
            // Hafif kırmızı flash
            StartCoroutine(FlashScreen(new Color(1f, 0.2f, 0.2f, 0.25f)));

            // Hafif sarsıntı
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.ShakeLight();
            }

            // Hata sesi
            PlaySound(failSound);
        }

        /// <summary>
        /// Süre bittiğinde (Game Over) yani canavar büyücüye çarptığında: büyük flash + ağır sarsıntı.
        /// </summary>
        public void PlayGameOverFeedback()
        {
            // Güçlü kırmızı flash
            StartCoroutine(FlashScreen(new Color(1f, 0.1f, 0.1f, 0.6f)));

            // Ağır sarsıntı
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.ShakeHeavy();
            }
            else
            {
                StartCoroutine(CameraShake());
            }

            // Patlama / fail sesi
            PlaySound(failSound);
        }

        // ════════════════════════════════════════════
        // Efekt Coroutine'leri
        // ════════════════════════════════════════════

        /// <summary>
        /// Ekran flash efekti (overlay fade-in/fade-out).
        /// </summary>
        private IEnumerator FlashScreen(Color flashColor)
        {
            if (flashOverlay == null) yield break;

            float halfDuration = flashDuration / 2f;

            // Fade in
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                Color c = flashColor;
                c.a = Mathf.Lerp(0f, flashColor.a, t);
                flashOverlay.color = c;
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                Color c = flashColor;
                c.a = Mathf.Lerp(flashColor.a, 0f, t);
                flashOverlay.color = c;
                yield return null;
            }

            // Tamamen şeffaf
            Color final_c = flashColor;
            final_c.a = 0f;
            flashOverlay.color = final_c;
        }

        /// <summary>
        /// Kamera sarsıntı efekti.
        /// </summary>
        private IEnumerator CameraShake()
        {
            if (mainCamera == null) yield break;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                float x = originalCameraPos.x + Random.Range(-shakeAmount, shakeAmount);
                float y = originalCameraPos.y + Random.Range(-shakeAmount, shakeAmount);
                mainCamera.transform.position = new Vector3(x, y, originalCameraPos.z);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            mainCamera.transform.position = originalCameraPos;
        }

        // ════════════════════════════════════════════
        // Ses
        // ════════════════════════════════════════════

        /// <summary>
        /// Ses efekti çalar (clip varsa).
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, sfxVolume);
            }
        }
    }
}
