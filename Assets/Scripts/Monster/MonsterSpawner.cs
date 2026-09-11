// ============================================================
// MonsterSpawner.cs — REHABIT-EL
// Canavar spawn yönetimi: sağ kenardan spawn, şekil atama.
// ============================================================
using UnityEngine;
using RehabitEL.Core;

namespace RehabitEL.Monsters
{
    /// <summary>
    /// Canavarları spawn eder ve aktif canavarı takip eder.
    /// GameManager'ın state değişikliklerine abone olarak çalışır.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("Spawn Ayarları")]
        [Tooltip("Canavar prefab'ı (Monster component'i olmalı)")]
        [SerializeField] private GameObject monsterPrefab;

        [Tooltip("Spawn pozisyonu X (ekranın sağ kenarı)")]
        [SerializeField] private float spawnX = 7f;

        [Tooltip("Spawn pozisyonu Y")]
        [SerializeField] private float spawnY = 0f;

        [Tooltip("Hedef pozisyon X (oyuncunun konumu — canavarın ulaşacağı yer)")]
        [SerializeField] private float targetX = -5f;

        [Header("Prefab Yoksa (Fallback)")]
        [Tooltip("Prefab yoksa otomatik oluştur (geliştirme için)")]
        [SerializeField] private bool autoCreateIfNoPrefab = true;

        // ── Durum ──
        private Monster currentMonster;

        // ── Singleton ──
        public static MonsterSpawner Instance { get; private set; }

        /// <summary>Aktif canavar referansı.</summary>
        public Monster CurrentMonster => currentMonster;

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

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNewShapeAssigned += HandleNewShape;
                GameManager.Instance.OnDrawingValidated += HandleDrawingResult;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNewShapeAssigned -= HandleNewShape;
                GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
            }
        }

        private void Start()
        {
            // OnEnable çok erken çalışabilir, Start'ta tekrar bağlan
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNewShapeAssigned -= HandleNewShape;
                GameManager.Instance.OnNewShapeAssigned += HandleNewShape;
                GameManager.Instance.OnDrawingValidated -= HandleDrawingResult;
                GameManager.Instance.OnDrawingValidated += HandleDrawingResult;
            }
        }

        // ════════════════════════════════════════════
        // Event Handler'lar
        // ════════════════════════════════════════════

        /// <summary>
        /// Yeni şekil atandığında canavar spawn eder.
        /// </summary>
        private void HandleNewShape(ShapeType shape)
        {
            SpawnMonster(shape);
        }

        /// <summary>
        /// Çizim sonucu geldiğinde canavarı yok eder veya bırakır.
        /// </summary>
        private void HandleDrawingResult(bool success, float rmse)
        {
            if (currentMonster == null) return;

            if (success)
            {
                currentMonster.StopMovement(); // Büyü gelene kadar donup bekle
                // Ölüm tetiklemesi artık FeedbackManager üzerinden SpellEffect'e callback olarak gönderiliyor.
            }
            // Başarısızlıkta canavar kalır, GameManager GameOver tetikler
        }

        // ════════════════════════════════════════════
        // Spawn
        // ════════════════════════════════════════════

        /// <summary>
        /// Yeni bir canavar spawn eder.
        /// </summary>
        private void SpawnMonster(ShapeType shape)
        {
            // Önceki canavarı temizle
            if (currentMonster != null && currentMonster.gameObject != null)
            {
                Destroy(currentMonster.gameObject);
            }

            // Spawn pozisyonu
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            // Prefab'dan oluştur veya fallback
            GameObject monsterObj;
            if (monsterPrefab != null)
            {
                monsterObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
            }
            else if (autoCreateIfNoPrefab)
            {
                monsterObj = CreateFallbackMonster(spawnPos);
            }
            else
            {
                Debug.LogError("[MonsterSpawner] Monster prefab atanmamış!");
                return;
            }

            monsterObj.name = $"Monster_{shape}";

            // Monster component'ini al veya ekle
            currentMonster = monsterObj.GetComponent<Monster>();
            if (currentMonster == null)
            {
                currentMonster = monsterObj.AddComponent<Monster>();
            }

            // Hızı hesapla: mesafe / izin verilen süre
            float distance = spawnX - targetX;
            float allowedTime = GameManager.Instance != null
                ? GameManager.Instance.AllowedTime
                : 8f;
            float speed = distance / allowedTime;

            // Başlat
            currentMonster.Initialize(shape, speed);
        }

        /// <summary>
        /// Prefab yoksa basit bir canavar objesi oluşturur (geliştirme amaçlı).
        /// </summary>
        private GameObject CreateFallbackMonster(Vector3 position)
        {
            GameObject obj = new GameObject("Monster_Fallback");
            obj.transform.position = position;

            // SpriteRenderer ekle (sprite Monster.ApplyVisuals içinde ayarlanacak)
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Şekil etiketi için alt nesne
            GameObject labelObj = new GameObject("ShapeLabel");
            labelObj.transform.SetParent(obj.transform);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.fontSize = 48;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.15f;
            textMesh.color = Color.white;

            return obj;
        }
    }
}
