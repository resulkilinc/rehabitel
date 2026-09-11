# REHABIT-EL Unity Sahne Kurulum Talimatları

Bu doküman, oluşturulan C# scriptlerini Unity Editor'da sahne üzerinde nasıl bağlayacağınızı adım adım anlatır.

## Ön Gereksinimler

1. **Unity Hub** kurulu olmalı
2. **Unity 2022 LTS** (veya 2023 LTS) Editor yüklenmeli
3. Unity Hub'dan **New Project → 2D (Core)** seçerek `RehabitEL` adıyla proje oluşturun
4. Oluşturulan Unity projesinin `Assets/` klasörüne, bu repodaki `Assets/Scripts/` klasörünü kopyalayın

> **NOT:** Unity projesini Hub üzerinden oluşturup sonra scriptleri kopyalamak en güvenli yoldur.
> Alternatif olarak, mevcut `Oyun/unity/RehabitEL/` klasörünü doğrudan Unity Hub'da açabilirsiniz
> ancak bu durumda `ProjectSettings` klasörü otomatik oluşturulacaktır.

---

## Adım 1: Sahneleri Oluşturma

### Build Settings'e sahne ekleme sırası:
```
0: MainMenu
1: Calibration
2: Game
3: GameOver
```

### 1.1 MainMenu Sahnesi

**File → New Scene → Save As: `MainMenu`**

Sahneye eklenecek objeler:

| GameObject         | Component(ler)              | Ayarlar                           |
|--------------------|-----------------------------|-----------------------------------|
| Main Camera        | (varsayılan)                | Orthographic, Size: 5             |
| Canvas             | Canvas (Screen Space - Overlay) | —                             |
| ├── Title          | Text                        | "REHABIT-EL", Font Size: 48      |
| ├── Subtitle       | Text                        | "Büyücü vs Canavarlar", Size: 24 |
| ├── StartButton    | Button + Text               | "BAŞLA"                           |
| ├── CalibButton    | Button + Text               | "KALİBRASYON"                     |
| ├── HighScoreText  | Text                        | "En Yüksek Skor: 0"              |
| └── HighMonsterText| Text                        | ""                                |
| EventSystem        | EventSystem                 | (otomatik eklenir)               |
| MainMenuManager    | **MainMenuUI.cs**           | UI referanslarını sürükle-bırak  |

**MainMenuUI ayarları:**
- `titleText` → Title objesini sürükle
- `subtitleText` → Subtitle objesini sürükle
- `startButton` → StartButton objesini sürükle
- `calibrationButton` → CalibButton objesini sürükle
- `highScoreText` → HighScoreText objesini sürükle
- `highMonsterText` → HighMonsterText objesini sürükle

---

### 1.2 Calibration Sahnesi

**File → New Scene → Save As: `Calibration`**

| GameObject         | Component(ler)           | Ayarlar                          |
|--------------------|--------------------------|----------------------------------|
| Main Camera        | (varsayılan)             | Orthographic, Size: 5            |
| Canvas             | Canvas (Screen Space - Overlay) | —                          |
| ├── Title          | Text                     | "KALİBRASYON", Size: 36         |
| ├── Instruction    | Text                     | Talimat metni, Size: 20         |
| ├── Status         | Text                     | Min/Max değerler, Size: 16      |
| ├── RawValue       | Text                     | Ham değer gösterimi, Size: 14   |
| ├── ProgressBar    | Image (Filled)           | FillMethod: Horizontal          |
| ├── MinButton      | Button + Text            | "DÜZ TUT (Min)"                 |
| ├── MaxButton      | Button + Text            | "MAX BÜK (Max)"                 |
| ├── ResetButton    | Button + Text            | "SIFIRLA"                        |
| ├── DoneButton     | Button + Text            | "TAMAM"                          |
| └── BackButton     | Button + Text            | "GERİ"                           |
| EventSystem        | EventSystem              |                                  |
| CalibrationMgr     | **CalibrationManager.cs**|                                  |
| InputMgr           | **InputManager.cs**      | Current Mode: Serial (veya Mouse)|
| CalibrationUI_Obj  | **CalibrationUI.cs**     | UI referanslarını bağla          |

---

### 1.3 Game Sahnesi (ANA SAHNE)

**File → New Scene → Save As: `Game`**

#### Yönetici Objeleri (Boş GameObject):

| GameObject        | Component(ler)                    | Notlar                            |
|-------------------|-----------------------------------|-----------------------------------|
| GameManager       | **GameManager.cs**                | Oyun döngüsü                     |
| DifficultyManager | **DifficultyManager.cs**          | baseTime: 8, monstersPerLevel: 3 |
| MonsterSpawner    | **MonsterSpawner.cs**             | spawnX: 7, spawnY: 0, targetX: -5|
| DrawingCanvas     | **DrawingCanvas.cs**              | canvasCenter: (0,0), canvasSize: (6,6)|
| InputManager      | **InputManager.cs**               | currentMode: Mouse                |
| SessionRecorder   | **SessionRecorder.cs**            | exportCSV: true, exportJSON: true |
| SpellEffect       | **SpellEffect.cs**                |                                    |
| FeedbackManager   | **FeedbackManager.cs**            | flashOverlay: FlashImage'i bağla |
| DebugOverlay      | **DebugOverlay.cs**               | showOnStart: false                |

#### Görsel Objeler:

| GameObject     | Component(ler)    | Ayarlar                                |
|----------------|-------------------|----------------------------------------|
| Main Camera    | Camera            | Orthographic, Size: 5, BG: koyu renk  |
| Wizard         | SpriteRenderer    | Pozisyon: (-5, 0, 0) — Büyücü sprite  |

#### UI (Canvas):

| GameObject       | Component(ler)     | Ayarlar                              |
|------------------|--------------------|--------------------------------------|
| Canvas           | Canvas (Overlay)   |                                      |
| ├── ScoreText    | Text               | Anchor: Sol Üst, "PUAN: 0"          |
| ├── MonsterCount | Text               | Anchor: Sol Üst, "CANAVAR: 0"       |
| ├── TimerText    | Text               | Anchor: Üst Orta, "8.0s"            |
| ├── TimerFill    | Image (Filled)     | Anchor: Üst, FillMethod: Horizontal |
| ├── ShapeName    | Text               | Anchor: Sağ Üst, "DAİRE"            |
| ├── ShapeSymbol  | Text               | Anchor: Sağ Üst, "●", Size: 36     |
| ├── Notification | Text               | Anchor: Orta                         |
| ├── NotifGroup   | CanvasGroup        | Notification'ın parent'ı            |
| └── FlashImage   | Image              | Tam ekran, renk: şeffaf, Raycast: off|
| EventSystem      | EventSystem        |                                      |

**GameUI bağlantıları:** GameManager objesine **GameUI.cs** de ekleyin veya ayrı bir obje yapıp:
- `scoreText` → ScoreText
- `monsterCountText` → MonsterCount
- `timerText` → TimerText
- `timerFill` → TimerFill
- `shapeNameText` → ShapeName
- `shapeSymbolText` → ShapeSymbol
- `notificationText` → Notification
- `notificationGroup` → NotifGroup

**FeedbackManager bağlantıları:**
- `flashOverlay` → FlashImage

---

### 1.4 GameOver Sahnesi

**File → New Scene → Save As: `GameOver`**

| GameObject        | Component(ler)     | Ayarlar                             |
|-------------------|--------------------|-------------------------------------|
| Main Camera       | (varsayılan)       | Orthographic, Size: 5               |
| Canvas            | Canvas (Overlay)   |                                     |
| ├── GameOverTitle | Text               | "OYUN BİTTİ!", Size: 42            |
| ├── MonsterCount  | Text               | "Öldürülen Canavar: 0", Size: 24   |
| ├── ScoreText     | Text               | "Toplam Puan: 0", Size: 28         |
| ├── DurationText  | Text               | "Süre: 00:00", Size: 20            |
| ├── HighScore     | Text               | "En Yüksek Skor: 0", Size: 18     |
| ├── NewRecordText | Text               | "★ YENİ REKOR! ★", Size: 30       |
| ├── RetryButton   | Button + Text      | "TEKRAR DENE"                       |
| └── MenuButton    | Button + Text      | "ANA MENÜ"                          |
| EventSystem       | EventSystem        |                                     |
| GameOverUI_Obj    | **GameOverUI.cs**  | Tüm UI referanslarını bağla        |

---

## Adım 2: Build Settings

**File → Build Settings:**
1. "Add Open Scenes" ile tüm sahneleri ekleyin
2. Sıralama:
   - `Scenes/MainMenu` (index 0)
   - `Scenes/Calibration` (index 1)
   - `Scenes/Game` (index 2)
   - `Scenes/GameOver` (index 3)
3. Platform: PC, Mac & Linux Standalone
4. Architecture: Intel 64-bit + Apple Silicon

---

## Adım 3: Test

1. **MainMenu** sahnesini açın ve Play'e basın
2. "BAŞLA" butonuna tıklayın → Game sahnesine geçmeli
3. Fare ile ekranda şekil çizin (sol tuş basılı sürükle)
4. **Space** tuşu ile başarı simüle edin (debug)
5. **Escape** tuşu ile başarısızlık simüle edin
6. **F12** ile debug paneli açın
7. **F1** ile input modu değiştirin
8. GameOver sahnesinde "TEKRAR DENE" ve "ANA MENÜ" butonlarını test edin
9. `Assets/Exports/` klasöründe CSV/JSON dosyalarının oluştuğunu kontrol edin

---

## Kısayol Özetli

| Tuş    | İşlev                          |
|--------|--------------------------------|
| F1     | Input modu değiştir (döngüsel)|
| F12    | Debug panel aç/kapat           |
| Space  | Başarı simüle et (debug)       |
| Escape | Başarısızlık simüle et (debug)|
| Enter  | Simulator modda çizim başlat   |
| ↑ / ↓  | Simulator modda flex kontrol   |
