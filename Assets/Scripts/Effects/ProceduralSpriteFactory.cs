// ============================================================
// ProceduralSpriteFactory.cs — REHABIT-EL
// Runtime'da pixel art stil sprite üretici.
// Büyücü, canavar, şekil ve arka plan sprite'ları üretir.
// ============================================================
using UnityEngine;

namespace RehabitEL.Effects
{
    /// <summary>
    /// Tüm runtime sprite üretiminden sorumlu merkezi fabrika sınıfı.
    /// Prefab/asset dosyası gerektirmeden kaliteli pixel art sprite'lar oluşturur.
    /// </summary>
    public static class ProceduralSpriteFactory
    {
        // ── Sabitler ──
        private const int WIZARD_WIDTH = 64;
        private const int WIZARD_HEIGHT = 96;
        private const int MONSTER_SIZE = 64;
        private const int SHAPE_ICON_SIZE = 32;
        private const int BG_WIDTH = 512;
        private const int BG_HEIGHT = 256;

        // ════════════════════════════════════════════
        // Büyücü Sprite'ı
        // ════════════════════════════════════════════

        /// <summary>
        /// Detaylı büyücü karakter sprite'ı oluşturur (pixel art).
        /// Cüppe, şapka, asa ve glow efektli.
        /// </summary>
        public static Sprite CreateWizardSprite()
        {
            Texture2D tex = new Texture2D(WIZARD_WIDTH, WIZARD_HEIGHT);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[WIZARD_WIDTH * WIZARD_HEIGHT];

            // Şeffaf arka plan
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Renk paleti
            Color robeMain = new Color(0.25f, 0.12f, 0.55f);       // Koyu mor cüppe
            Color robeHighlight = new Color(0.40f, 0.20f, 0.75f);  // Açık mor vurgu
            Color robeShadow = new Color(0.15f, 0.08f, 0.35f);     // Gölge
            Color skin = new Color(0.93f, 0.78f, 0.65f);           // Ten rengi
            Color hatColor = new Color(0.20f, 0.08f, 0.50f);       // Şapka
            Color hatBand = new Color(0.85f, 0.65f, 0.15f);        // Şapka bandı (altın)
            Color staffWood = new Color(0.45f, 0.28f, 0.12f);      // Asa gövdesi
            Color staffGem = new Color(0.3f, 0.9f, 1.0f);          // Asa kristali (cyan)
            Color staffGlow = new Color(0.3f, 0.9f, 1.0f, 0.5f);   // Kristal parıltısı
            Color belt = new Color(0.75f, 0.55f, 0.10f);           // Kemer (altın)
            Color eyeColor = Color.white;
            Color pupil = new Color(0.15f, 0.05f, 0.40f);          // Göz bebeği

            // Büyücü çizimi (aşağıdan yukarıya)
            // Ayaklar (y: 2-8)
            FillRect(pixels, WIZARD_WIDTH, 22, 2, 8, 8, robeShadow);    // Sol ayak
            FillRect(pixels, WIZARD_WIDTH, 34, 2, 8, 8, robeShadow);    // Sağ ayak

            // Cüppe alt kısmı (y: 8-32) — geniş etek
            for (int y = 8; y < 32; y++)
            {
                float t = (float)(y - 8) / 24f;
                int halfWidth = (int)Mathf.Lerp(22, 14, t);
                int cx = WIZARD_WIDTH / 2;
                Color robeColor = (y % 4 < 2) ? robeMain : robeHighlight;
                FillRect(pixels, WIZARD_WIDTH, cx - halfWidth, y, halfWidth * 2, 1, robeColor);
                // Cüppe kenarı gölgeleme
                SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, cx - halfWidth, y, robeShadow);
                SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, cx + halfWidth - 1, y, robeShadow);
            }

            // Kemer (y: 32-34)
            FillRect(pixels, WIZARD_WIDTH, 18, 32, 28, 3, belt);
            // Kemer tokası
            FillRect(pixels, WIZARD_WIDTH, 30, 32, 4, 3, new Color(1f, 0.85f, 0.2f));

            // Cüppe üst gövde (y: 34-52)
            for (int y = 34; y < 52; y++)
            {
                float t = (float)(y - 34) / 18f;
                int halfWidth = (int)Mathf.Lerp(14, 10, t);
                int cx = WIZARD_WIDTH / 2;
                Color robeColor = (y % 3 == 0) ? robeHighlight : robeMain;
                FillRect(pixels, WIZARD_WIDTH, cx - halfWidth, y, halfWidth * 2, 1, robeColor);
            }

            // Kollar (y: 38-50)
            // Sol kol
            for (int y = 38; y < 50; y++)
            {
                FillRect(pixels, WIZARD_WIDTH, 14, y, 5, 1, robeMain);
            }
            // Sol el (ten)
            FillRect(pixels, WIZARD_WIDTH, 12, 38, 4, 4, skin);

            // Sağ kol (asa tutan)
            for (int y = 38; y < 50; y++)
            {
                FillRect(pixels, WIZARD_WIDTH, 45, y, 5, 1, robeMain);
            }
            // Sağ el (ten)
            FillRect(pixels, WIZARD_WIDTH, 48, 46, 4, 4, skin);

            // Asa (x: 50-52, y: 30-85)
            FillRect(pixels, WIZARD_WIDTH, 50, 30, 3, 55, staffWood);
            // Asa kristali
            DrawDiamond(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 51, 88, 5, staffGem);
            // Kristal parıltı
            DrawDiamond(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 51, 88, 7, staffGlow);

            // Boyun / yüz (y: 52-62)
            FillRect(pixels, WIZARD_WIDTH, 27, 52, 10, 2, skin); // Boyun
            // Yüz
            FillEllipse(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 32, 59, 7, 7, skin);

            // Gözler
            FillRect(pixels, WIZARD_WIDTH, 27, 59, 3, 3, eyeColor);    // Sol göz
            FillRect(pixels, WIZARD_WIDTH, 34, 59, 3, 3, eyeColor);    // Sağ göz  
            SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 28, 59, pupil); // Sol göz bebeği
            SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 35, 59, pupil); // Sağ göz bebeği

            // Ağız
            FillRect(pixels, WIZARD_WIDTH, 30, 55, 4, 1, new Color(0.7f, 0.35f, 0.30f));

            // Sakal (beyaz)
            Color beard = new Color(0.9f, 0.9f, 0.92f);
            for (int y = 52; y < 42; y--)
            {
                // Sakal aşağı uzuyor (bu range ters, sakal yüzün altında olacak)
            }
            // Sakal — yüzün altından aşağı
            for (int y = 52; y < 48; y--)
            {
            }
            FillRect(pixels, WIZARD_WIDTH, 28, 53, 8, 2, beard);
            // Sakal uzantısı
            for (int y = 48; y >= 42; y--)
            {
                int w = 8 - (52 - y);
                if (w < 2) w = 2;
                FillRect(pixels, WIZARD_WIDTH, 32 - w / 2, y, w, 1, beard);
            }

            // Sivri büyücü şapkası (y: 64-92)
            for (int y = 64; y < 92; y++)
            {
                float t = (float)(y - 64) / 28f;
                int halfWidth = (int)Mathf.Lerp(10, 1, t);
                int cx = WIZARD_WIDTH / 2;
                FillRect(pixels, WIZARD_WIDTH, cx - halfWidth, y, halfWidth * 2, 1, hatColor);
            }
            // Şapka bandı
            FillRect(pixels, WIZARD_WIDTH, 21, 66, 22, 3, hatBand);
            // Şapka kenarı (geniş brim)
            FillRect(pixels, WIZARD_WIDTH, 17, 63, 30, 2, hatColor);

            // Şapka yıldızı
            SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 32, 78, hatBand);
            SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 31, 77, hatBand);
            SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 33, 77, hatBand);
            SetPixelSafe(pixels, WIZARD_WIDTH, WIZARD_HEIGHT, 32, 76, hatBand);

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex,
                new Rect(0, 0, WIZARD_WIDTH, WIZARD_HEIGHT),
                new Vector2(0.5f, 0.2f), // Pivot alt ortada
                32f); // 32 pixels per unit
        }

        // ════════════════════════════════════════════
        // Canavar Sprite'ları
        // ════════════════════════════════════════════

        /// <summary>
        /// Şekil türüne göre canavar sprite'ı oluşturur.
        /// Her canavar türünün kendine özgü görsel tasarımı vardır.
        /// </summary>
        public static Sprite CreateMonsterSprite(ShapeType shape)
        {
            switch (shape)
            {
                case ShapeType.Circle:
                    return CreateCircleMonster();
                case ShapeType.Square:
                    return CreateSquareMonster();
                case ShapeType.Triangle:
                    return CreateTriangleMonster();
                default:
                    return CreateCircleMonster();
            }
        }

        /// <summary>Daire canavar — Ateş elementli, turuncu-kırmızı yuvarlak yaratık.</summary>
        private static Sprite CreateCircleMonster()
        {
            Texture2D tex = new Texture2D(MONSTER_SIZE, MONSTER_SIZE);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[MONSTER_SIZE * MONSTER_SIZE];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color bodyMain = new Color(1.0f, 0.35f, 0.15f);   // Turuncu
            Color bodyDark = new Color(0.8f, 0.2f, 0.1f);     // Koyu turuncu
            Color bodyLight = new Color(1.0f, 0.6f, 0.3f);    // Açık turuncu
            Color eyeWhite = Color.white;
            Color pupilColor = new Color(0.1f, 0.0f, 0.0f);
            Color mouth = new Color(0.3f, 0.05f, 0.0f);
            Color tooth = Color.white;

            int cx = MONSTER_SIZE / 2;
            int cy = MONSTER_SIZE / 2;

            // Gövde (daire)
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx, cy, 28, 26, bodyMain);
            // Üst vurgu
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx, cy + 6, 20, 16, bodyLight);
            // Alt gölge
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx, cy - 8, 22, 12, bodyDark);

            // Gözler — büyük ve ifadeli
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 9, cy + 6, 7, 8, eyeWhite);
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 9, cy + 6, 7, 8, eyeWhite);
            // Göz bebekleri
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 7, cy + 6, 4, 5, pupilColor);
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 11, cy + 6, 4, 5, pupilColor);
            // Göz parıltısı
            SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 8, cy + 8, Color.white);
            SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 10, cy + 8, Color.white);

            // Kızgın ağız
            for (int x = cx - 10; x <= cx + 10; x++)
            {
                int mouthY = cy - 6 - (int)(3f * Mathf.Sin((x - cx + 10) * Mathf.PI / 20f));
                SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, x, mouthY, mouth);
                SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, x, mouthY - 1, mouth);
            }
            // Dişler
            for (int i = 0; i < 4; i++)
            {
                int tx = cx - 8 + i * 5;
                FillRect(pixels, MONSTER_SIZE, tx, cy - 8, 2, 4, tooth);
            }

            // Ateş alevi detayları (üstte)
            Color flame = new Color(1f, 0.85f, 0.1f);
            for (int i = 0; i < 5; i++)
            {
                int fx = cx - 12 + i * 6;
                int fh = 4 + (i % 2 == 0 ? 6 : 3);
                for (int y = 0; y < fh; y++)
                {
                    int fw = (int)Mathf.Lerp(3, 1, (float)y / fh);
                    FillRect(pixels, MONSTER_SIZE, fx - fw / 2, cy + 24 + y, fw, 1, flame);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, MONSTER_SIZE, MONSTER_SIZE),
                new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>Kare canavar — Elektrik elementli, mavi kübik golem.</summary>
        private static Sprite CreateSquareMonster()
        {
            Texture2D tex = new Texture2D(MONSTER_SIZE, MONSTER_SIZE);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[MONSTER_SIZE * MONSTER_SIZE];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color bodyMain = new Color(0.25f, 0.45f, 0.95f);  // Mavi
            Color bodyDark = new Color(0.15f, 0.30f, 0.70f);  // Koyu mavi
            Color bodyLight = new Color(0.45f, 0.65f, 1.0f);  // Açık mavi
            Color eyeWhite = Color.white;
            Color pupilColor = new Color(0.0f, 0.0f, 0.2f);
            Color mouth = new Color(0.1f, 0.1f, 0.3f);
            Color tooth = Color.white;
            Color bolt = new Color(0.8f, 0.95f, 1.0f);       // Elektrik kıvılcım

            int cx = MONSTER_SIZE / 2;
            int cy = MONSTER_SIZE / 2;

            // Gövde (kare)
            FillRect(pixels, MONSTER_SIZE, 6, 4, 52, 52, bodyMain);
            // Üst vurgu
            FillRect(pixels, MONSTER_SIZE, 8, 30, 48, 24, bodyLight);
            // Alt gölge
            FillRect(pixels, MONSTER_SIZE, 8, 4, 48, 16, bodyDark);
            // Kenar çerçeve
            DrawRectOutline(pixels, MONSTER_SIZE, MONSTER_SIZE, 6, 4, 52, 52, new Color(0.1f, 0.2f, 0.5f));

            // Gözler — angüler, kızgın
            FillRect(pixels, MONSTER_SIZE, 14, 34, 12, 10, eyeWhite);
            FillRect(pixels, MONSTER_SIZE, 38, 34, 12, 10, eyeWhite);
            // Göz bebekleri
            FillRect(pixels, MONSTER_SIZE, 18, 35, 6, 7, pupilColor);
            FillRect(pixels, MONSTER_SIZE, 42, 35, 6, 7, pupilColor);
            // Kızgın bakış (üst göz kenarı çizgisi)
            for (int x = 14; x < 26; x++) SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, x, 44 - (x - 14) / 3, bodyDark);
            for (int x = 38; x < 50; x++) SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, x, 41 + (x - 38) / 3, bodyDark);

            // Ağız (ızgara dişler)
            FillRect(pixels, MONSTER_SIZE, 18, 14, 28, 10, mouth);
            for (int i = 0; i < 5; i++)
            {
                FillRect(pixels, MONSTER_SIZE, 20 + i * 6, 20, 3, 4, tooth);
                FillRect(pixels, MONSTER_SIZE, 20 + i * 6, 14, 3, 4, tooth);
            }

            // Elektrik çatlakları
            DrawLightningBolt(pixels, MONSTER_SIZE, MONSTER_SIZE, 4, 50, 4, 30, bolt);
            DrawLightningBolt(pixels, MONSTER_SIZE, MONSTER_SIZE, 58, 50, 58, 30, bolt);

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, MONSTER_SIZE, MONSTER_SIZE),
                new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>Üçgen canavar — Zehir elementli, yeşil-mor dikenli yaratık.</summary>
        private static Sprite CreateTriangleMonster()
        {
            Texture2D tex = new Texture2D(MONSTER_SIZE, MONSTER_SIZE);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[MONSTER_SIZE * MONSTER_SIZE];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color bodyMain = new Color(0.30f, 0.85f, 0.25f);  // Yeşil
            Color bodyDark = new Color(0.20f, 0.55f, 0.15f);  // Koyu yeşil
            Color bodyLight = new Color(0.50f, 1.0f, 0.40f);  // Açık yeşil
            Color eyeWhite = new Color(1f, 1f, 0.8f);
            Color pupilColor = new Color(0.2f, 0.0f, 0.4f);   // Mor göz bebeği
            Color mouth = new Color(0.3f, 0.0f, 0.5f);
            Color tooth = Color.white;
            Color spike = new Color(0.5f, 0.0f, 0.8f);        // Mor dikenler

            int cx = MONSTER_SIZE / 2;

            // Gövde (üçgen — yukarı sivri)
            for (int y = 4; y < 52; y++)
            {
                float t = (float)(y - 4) / 48f;
                int halfWidth = (int)Mathf.Lerp(26, 2, t);
                Color bodyColor = (y > 30) ? bodyLight : ((y > 15) ? bodyMain : bodyDark);
                FillRect(pixels, MONSTER_SIZE, cx - halfWidth, y, halfWidth * 2, 1, bodyColor);
            }

            // Gözler (üçgen içinde)
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 8, 28, 6, 7, eyeWhite);
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 8, 28, 6, 7, eyeWhite);
            // Göz bebekleri (dikey yarık — yılan gözü)
            FillRect(pixels, MONSTER_SIZE, cx - 9, 26, 2, 6, pupilColor);
            FillRect(pixels, MONSTER_SIZE, cx + 7, 26, 2, 6, pupilColor);

            // Ağız (üçgenin alt kısmı)
            for (int x = cx - 12; x <= cx + 12; x++)
            {
                int mouthY = 14 + (int)(4f * Mathf.Sin((x - cx + 12) * Mathf.PI / 24f));
                SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, x, mouthY, mouth);
                SetPixelSafe(pixels, MONSTER_SIZE, MONSTER_SIZE, x, mouthY - 1, mouth);
            }
            // Sivri dişler (üçgen dişler)
            for (int i = 0; i < 3; i++)
            {
                int tx = cx - 8 + i * 8;
                for (int dy = 0; dy < 5; dy++)
                {
                    int tw = 4 - dy;
                    if (tw < 1) tw = 1;
                    FillRect(pixels, MONSTER_SIZE, tx - tw / 2, 12 - dy, tw, 1, tooth);
                }
            }

            // Dikenler (tepede ve yanlarda)
            DrawSpike(pixels, MONSTER_SIZE, MONSTER_SIZE, cx, 52, 4, 8, spike);
            DrawSpike(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 14, 38, 3, 6, spike);
            DrawSpike(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 14, 38, 3, 6, spike);
            DrawSpike(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 8, 46, 3, 5, spike);
            DrawSpike(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 8, 46, 3, 5, spike);

            // Zehir damlacıkları
            Color poison = new Color(0.6f, 0.0f, 1.0f, 0.7f);
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx - 20, 8, 3, 4, poison);
            FillEllipse(pixels, MONSTER_SIZE, MONSTER_SIZE, cx + 20, 12, 2, 3, poison);

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, MONSTER_SIZE, MONSTER_SIZE),
                new Vector2(0.5f, 0.5f), 32f);
        }

        // ════════════════════════════════════════════
        // Şekil İkonu Sprite'ları
        // ════════════════════════════════════════════

        /// <summary>
        /// Canavar üzerinde gösterilecek şekil ikonu sprite'ı üretir.
        /// </summary>
        public static Sprite CreateShapeIconSprite(ShapeType shape)
        {
            Texture2D tex = new Texture2D(SHAPE_ICON_SIZE, SHAPE_ICON_SIZE);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[SHAPE_ICON_SIZE * SHAPE_ICON_SIZE];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color white = Color.white;
            int cx = SHAPE_ICON_SIZE / 2;
            int cy = SHAPE_ICON_SIZE / 2;

            switch (shape)
            {
                case ShapeType.Circle:
                    DrawCircleOutline(pixels, SHAPE_ICON_SIZE, SHAPE_ICON_SIZE, cx, cy, 12, white, 2);
                    break;
                case ShapeType.Square:
                    DrawRectOutline(pixels, SHAPE_ICON_SIZE, SHAPE_ICON_SIZE, 4, 4, 24, 24, white);
                    DrawRectOutline(pixels, SHAPE_ICON_SIZE, SHAPE_ICON_SIZE, 5, 5, 22, 22, white);
                    break;
                case ShapeType.Triangle:
                    DrawTriangleOutline(pixels, SHAPE_ICON_SIZE, SHAPE_ICON_SIZE, cx, 28, 24, white, 2);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SHAPE_ICON_SIZE, SHAPE_ICON_SIZE),
                new Vector2(0.5f, 0.5f), 32f);
        }

        // ════════════════════════════════════════════
        // Arka Plan Katmanları
        // ════════════════════════════════════════════

        /// <summary>
        /// Gökyüzü gradient arka planı. En arka katman.
        /// </summary>
        public static Sprite CreateSkyBackground()
        {
            Texture2D tex = new Texture2D(BG_WIDTH, BG_HEIGHT);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] pixels = new Color[BG_WIDTH * BG_HEIGHT];

            Color topColor = new Color(0.05f, 0.02f, 0.15f);    // Koyu gece moru
            Color midColor = new Color(0.10f, 0.05f, 0.25f);    // Orta mor
            Color bottomColor = new Color(0.15f, 0.08f, 0.20f); // Altın ufuk

            for (int y = 0; y < BG_HEIGHT; y++)
            {
                float t = (float)y / BG_HEIGHT;
                Color lineColor;
                if (t > 0.5f)
                    lineColor = Color.Lerp(midColor, topColor, (t - 0.5f) * 2f);
                else
                    lineColor = Color.Lerp(bottomColor, midColor, t * 2f);

                for (int x = 0; x < BG_WIDTH; x++)
                {
                    pixels[y * BG_WIDTH + x] = lineColor;
                }
            }

            // Yıldızlar
            System.Random rng = new System.Random(42);
            for (int i = 0; i < 80; i++)
            {
                int sx = rng.Next(BG_WIDTH);
                int sy = BG_HEIGHT / 2 + rng.Next(BG_HEIGHT / 2);
                float brightness = 0.5f + (float)rng.NextDouble() * 0.5f;
                Color starColor = new Color(brightness, brightness, brightness * 1.1f, brightness);
                SetPixelSafe(pixels, BG_WIDTH, BG_HEIGHT, sx, sy, starColor);
                // Büyük yıldızlar
                if (i % 5 == 0)
                {
                    SetPixelSafe(pixels, BG_WIDTH, BG_HEIGHT, sx + 1, sy, starColor * 0.7f);
                    SetPixelSafe(pixels, BG_WIDTH, BG_HEIGHT, sx - 1, sy, starColor * 0.7f);
                    SetPixelSafe(pixels, BG_WIDTH, BG_HEIGHT, sx, sy + 1, starColor * 0.7f);
                    SetPixelSafe(pixels, BG_WIDTH, BG_HEIGHT, sx, sy - 1, starColor * 0.7f);
                }
            }

            // Ay
            FillEllipse(pixels, BG_WIDTH, BG_HEIGHT, BG_WIDTH - 80, BG_HEIGHT - 40, 20, 20,
                new Color(0.95f, 0.92f, 0.80f, 0.9f));
            // Ay krateri gölgesi
            FillEllipse(pixels, BG_WIDTH, BG_HEIGHT, BG_WIDTH - 74, BG_HEIGHT - 38, 16, 16,
                new Color(0.05f, 0.02f, 0.15f, 0.3f));

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, BG_WIDTH, BG_HEIGHT),
                new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>
        /// Uzak dağlar silüeti (2. katman).
        /// </summary>
        public static Sprite CreateMountainLayer()
        {
            int w = BG_WIDTH;
            int h = BG_HEIGHT / 2;
            Texture2D tex = new Texture2D(w, h);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] pixels = new Color[w * h];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color mountain = new Color(0.08f, 0.04f, 0.18f);
            Color mountainLight = new Color(0.12f, 0.06f, 0.25f);

            // Dağ silüetleri (Perlin noise benzeri)
            System.Random rng = new System.Random(123);
            float[] heights = new float[w];
            float frequency = 0.008f;
            for (int x = 0; x < w; x++)
            {
                heights[x] = h * 0.3f +
                    Mathf.Sin(x * frequency * Mathf.PI) * h * 0.25f +
                    Mathf.Sin(x * frequency * 2.7f * Mathf.PI) * h * 0.15f +
                    Mathf.Sin(x * frequency * 5.3f * Mathf.PI) * h * 0.05f;
            }

            for (int x = 0; x < w; x++)
            {
                int topY = (int)heights[x];
                for (int y = 0; y < Mathf.Min(topY, h); y++)
                {
                    float t = (float)y / topY;
                    pixels[y * w + x] = Color.Lerp(mountain, mountainLight, t);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0f), 32f);
        }

        /// <summary>
        /// Orman ağaçları silüeti (3. katman — orta).
        /// </summary>
        public static Sprite CreateForestLayer()
        {
            int w = BG_WIDTH;
            int h = BG_HEIGHT / 2;
            Texture2D tex = new Texture2D(w, h);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] pixels = new Color[w * h];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color treeDark = new Color(0.04f, 0.12f, 0.06f);
            Color treeLight = new Color(0.08f, 0.22f, 0.10f);

            // Ağaç silüetleri
            System.Random rng = new System.Random(456);
            for (int i = 0; i < 30; i++)
            {
                int tx = rng.Next(w);
                int treeHeight = 30 + rng.Next(50);
                int trunkWidth = 3 + rng.Next(3);
                Color treeColor = (i % 2 == 0) ? treeDark : treeLight;

                // Gövde
                FillRect(pixels, w, tx - trunkWidth / 2, 0, trunkWidth, treeHeight / 3, new Color(0.15f, 0.08f, 0.04f));

                // Yapraklar (üçgen)
                for (int y = treeHeight / 3; y < treeHeight; y++)
                {
                    float t = (float)(y - treeHeight / 3) / (treeHeight * 2f / 3f);
                    int leafWidth = (int)Mathf.Lerp(treeHeight / 3, 1, t);
                    FillRect(pixels, w, tx - leafWidth / 2, y, leafWidth, 1, treeColor);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0f), 32f);
        }

        /// <summary>
        /// Ön plan bitki örtüsü (4. katman — en yakın).
        /// </summary>
        public static Sprite CreateForegroundLayer()
        {
            int w = BG_WIDTH;
            int h = BG_HEIGHT / 4;
            Texture2D tex = new Texture2D(w, h);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] pixels = new Color[w * h];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color grass = new Color(0.02f, 0.08f, 0.03f);
            Color grassLight = new Color(0.05f, 0.15f, 0.06f);

            // Çimenlik arazi
            for (int x = 0; x < w; x++)
            {
                int groundHeight = 8 + (int)(4f * Mathf.Sin(x * 0.03f));
                for (int y = 0; y < groundHeight && y < h; y++)
                {
                    pixels[y * w + x] = grass;
                }
                // Çimen tepeleri
                if (x % 3 == 0)
                {
                    int grassH = groundHeight + 2 + (x % 7);
                    for (int y = groundHeight; y < grassH && y < h; y++)
                    {
                        SetPixelSafe(pixels, w, h, x, y, grassLight);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0f), 32f);
        }

        // ════════════════════════════════════════════
        // Çizim Yardımcıları
        // ════════════════════════════════════════════

        private static void SetPixelSafe(Color[] pixels, int w, int h, int x, int y, Color c)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                int idx = y * w + x;
                if (c.a < 1f && pixels[idx].a > 0f)
                {
                    // Alpha blending
                    pixels[idx] = Color.Lerp(pixels[idx], c, c.a);
                }
                else
                {
                    pixels[idx] = c;
                }
            }
        }

        private static void FillRect(Color[] pixels, int texWidth, int x, int y, int w, int h, Color c)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < texWidth && py >= 0)
                    {
                        int idx = py * texWidth + px;
                        if (idx >= 0 && idx < pixels.Length)
                            pixels[idx] = c;
                    }
                }
            }
        }

        private static void FillEllipse(Color[] pixels, int texW, int texH, int cx, int cy, int rx, int ry, Color c)
        {
            for (int y = -ry; y <= ry; y++)
            {
                for (int x = -rx; x <= rx; x++)
                {
                    float nx = (float)x / rx;
                    float ny = (float)y / ry;
                    if (nx * nx + ny * ny <= 1.0f)
                    {
                        SetPixelSafe(pixels, texW, texH, cx + x, cy + y, c);
                    }
                }
            }
        }

        private static void DrawRectOutline(Color[] pixels, int texW, int texH, int x, int y, int w, int h, Color c)
        {
            for (int i = 0; i < w; i++)
            {
                SetPixelSafe(pixels, texW, texH, x + i, y, c);         // Bottom
                SetPixelSafe(pixels, texW, texH, x + i, y + h - 1, c); // Top
            }
            for (int i = 0; i < h; i++)
            {
                SetPixelSafe(pixels, texW, texH, x, y + i, c);         // Left
                SetPixelSafe(pixels, texW, texH, x + w - 1, y + i, c); // Right
            }
        }

        private static void DrawCircleOutline(Color[] pixels, int texW, int texH, int cx, int cy, int r, Color c, int thickness)
        {
            for (int angle = 0; angle < 360; angle++)
            {
                float rad = angle * Mathf.Deg2Rad;
                for (int t = 0; t < thickness; t++)
                {
                    int x = cx + (int)((r - t) * Mathf.Cos(rad));
                    int y = cy + (int)((r - t) * Mathf.Sin(rad));
                    SetPixelSafe(pixels, texW, texH, x, y, c);
                }
            }
        }

        private static void DrawTriangleOutline(Color[] pixels, int texW, int texH, int cx, int topY, int baseW, Color c, int thickness)
        {
            int bottomY = topY - baseW;
            Vector2 top = new Vector2(cx, topY);
            Vector2 bottomLeft = new Vector2(cx - baseW / 2, bottomY);
            Vector2 bottomRight = new Vector2(cx + baseW / 2, bottomY);

            DrawLine(pixels, texW, texH, top, bottomLeft, c, thickness);
            DrawLine(pixels, texW, texH, bottomLeft, bottomRight, c, thickness);
            DrawLine(pixels, texW, texH, bottomRight, top, c, thickness);
        }

        private static void DrawLine(Color[] pixels, int texW, int texH, Vector2 a, Vector2 b, Color c, int thickness)
        {
            int steps = (int)(Vector2.Distance(a, b) * 2);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / Mathf.Max(steps, 1);
                Vector2 p = Vector2.Lerp(a, b, t);
                for (int tx = 0; tx < thickness; tx++)
                {
                    for (int ty = 0; ty < thickness; ty++)
                    {
                        SetPixelSafe(pixels, texW, texH, (int)p.x + tx, (int)p.y + ty, c);
                    }
                }
            }
        }

        private static void DrawDiamond(Color[] pixels, int texW, int texH, int cx, int cy, int size, Color c)
        {
            for (int y = -size; y <= size; y++)
            {
                int hw = size - Mathf.Abs(y);
                for (int x = -hw; x <= hw; x++)
                {
                    SetPixelSafe(pixels, texW, texH, cx + x, cy + y, c);
                }
            }
        }

        private static void DrawSpike(Color[] pixels, int texW, int texH, int cx, int baseY, int baseW, int height, Color c)
        {
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                int hw = (int)Mathf.Lerp(baseW, 0, t);
                if (hw < 1) hw = 1;
                FillRect(pixels, texW, cx - hw / 2, baseY + y, hw, 1, c);
            }
        }

        private static void DrawLightningBolt(Color[] pixels, int texW, int texH, int x1, int y1, int x2, int y2, Color c)
        {
            int segments = 6;
            int prevX = x1, prevY = y1;
            System.Random rng = new System.Random(x1 * 100 + y1);

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                int nextX = (int)Mathf.Lerp(x1, x2, t) + rng.Next(-4, 5);
                int nextY = (int)Mathf.Lerp(y1, y2, t);

                DrawLine(pixels, texW, texH, new Vector2(prevX, prevY), new Vector2(nextX, nextY), c, 1);
                prevX = nextX;
                prevY = nextY;
            }
        }
    }
}
