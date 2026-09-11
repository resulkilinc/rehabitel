# OYUN YOL HARITASI (REHABIT-EL – PC)

## Proje Tanimi (MVP)

Tema: Buyucu vs canavarlar.

Her canavarin ustunde rastgele bir sekil (Daire / Kare / Ucgen) bulunur. Canavar oyuncuya ulasmadan once oyuncu ilgili sekli cizer. Basarili olursa buyucu ilgili buyuyu atar ve canavar olur. Basarisiz olursa oyun biter.

- Skor: Her canavar = 10 puan
- Final ekran: Canavar sayisi (N) ve Puan (P = N * 10)
- Platform: PC (macOS uzerinde gelistirme)
- Giris: 1 parmak flex sensor (ESP32 -> Serial/BLE; MVP’de donanim yoksa simulator)

## Klasor ve Dokuman Standardi (tek yerde her sey)

Tum oyun dokumanlari ve araclari su kok klasorde tutulur:

- `mühendislik projesi/Oyun/`
  - `roadmap/` (yol haritasi + PDF)
  - `research/` (literatur, web notlari)
  - `docs/` (GDD-lite, teknik dokuman, veri sozlesmesi)
  - `unity/` (Unity projesi buraya)
  - `assets/` (ikon, ses, VFX, UI gorselleri)
  - `tools/` (test verisi, donusum scriptleri, export araclari)

## Basari Tanimi (MVP kabul kriterleri)

MVP “basarili” sayilmasi icin su 7 maddeyi saglamalidir:

1) Oyun dongusu: Canavar -> sekil -> zaman -> basari/basarisizlik
2) 3 sekil: Daire/Kare/Ucgen (random secim)
3) Skor: Her canavar 10 puan, finalde N ve P dogru
4) En az 1 dogrulama yontemi (Path-following veya Grid/yon-sekans) ile guvenilir kabul/red
5) InputProvider mimarisi: mouse + simulator ile tam oynanis
6) Seans kaydi: CSV/JSON export (N, P, gorevler, sure/hata metrikleri)
7) Yeniden baslatma: GameOver’dan hizli “Tekrar Dene”

## Faz 0 – Hazirlik & Arastirma (1–3 gun)

Cikti hedefleri:
- Oyun dokumani (GDD-lite): mekanikler, UI, skor, fail-state
- Olcum/puanlama tanimi (sekil dogrulama kriterleri)
- Risk listesi ve alternatif plan (donanim yoksa simulator, veri yoksa sentetik)

Teslimler:
- Derin arastirma ozeti (literatur + pratik referanslar)
- Teknik kararlar: Unity surumu, proje yapisi, veri formatlari

Ek teslim (bu fazda):
- GDD-lite (1-2 sayfa): tema, core loop, UI, ses/VFX listesi
- Veri sozlesmesi taslagi: “seans” ve “canavar” kayit alanlari
  - `session_id`, `started_at`, `ended_at`, `monster_count`, `score_total`
  - `monster_index`, `shape`, `success`, `time_to_complete_ms`, `error_metric`

## Faz 1 – Unity Proje Iskeleti (2–3 gun)

Hedef: Sensorsuz bile oynanabilir “oyun dongusu”.

Isler:
- Unity projesi (2D Core) olusturma
- Sahne yapisi:
  - MainMenu
  - Game
  - GameOver
- UI:
  - Sekil etiketi (Daire/Kare/Ucgen)
  - Geri sayim (canavar yaklasma suresi)
  - Skor (P) ve canavar sayisi (N)
- Oyun dongusu:
  - Spawn canavar
  - Random sekil ata
  - Sure baslat
  - Basarida: N++ / P+=10 / yeni canavar
  - Basarisizlikta: GameOver

Kabul kriteri:
- Klavye/mouse ile “dummy basari” verilip oyun dongusu calisiyor
- Final ekranda N ve P dogru

Teslimler:
- Unity sahneleri: MainMenu/Game/GameOver
- UI layout (placeholder ile): skor, canavar sayisi, zaman, aktif sekil

## Faz 2 – Cizim Mekanigi (Input + Cizgi) (3–5 gun)

Hedef: Fare ile sekil cizme ve dogrulama.

Isler:
- Cizim alani (canvas) ve cizgi (LineRenderer veya 2D trail)
- Nokta ornekleme (sabit aralik / sabit zaman)
- Cizim baslangic-bitis tespiti
- Referans sekiller:
  - Daire: parametreli (cos/sin) ile referans nokta bulutu
  - Kare: 4 kenar / 4 koseli referans
  - Ucgen: 3 kenar / 3 koseli referans

Dogurulama (MVP secenekleri):
- A) Path-following (RMSE / ort mesafe) + normalize uzunluk
- B) Grid/yon-sekans tabanli dogrulama

Kabul kriteri:
- Fare ile cizimde dogru sekiller kabul ediliyor, yanlislar reddediliyor (tolerans ayarlanabilir)
- Zaman dolunca fail-state dogru calisiyor

Teslimler:
- Sekil dogrulama modulü + tolerans parametreleri
- “Debug overlay”: cizim noktasi sayisi, hata metriği, threshold

## Faz 3 – Flex Sensor Entegrasyonu (1 parmak) (4–7 gun)

Hedef: Sensorden gelen veriyi Unity input’a cevirmek.

Isler:
- Unity tarafinda InputProvider arayuzu:
  - MouseInputProvider
  - KeyboardSimulatorProvider
  - SerialFlexInputProvider (ESP32)
  - PlaybackProvider (CSV kaydindan oynat)
- Kalibrasyon ekrani:
  - Min (duz) – 3 sn ornek
  - Max (max buk) – 3 sn ornek
  - Normalize 0..1
- Filtre:
  - EMA (alpha parametreli)
  - deadband (kucuk degisimleri yok say)
- Mapping:
  - 1 flex ile 2D cizim icin: X zamanla ilerler, Y flex ile kontrol edilir (rail drawing)
  - Alternatif: flex “yon degistir” / “koseleri kilitle” gibi 1D sekans dogrulama

Kabul kriteri:
- Unity’de debug panelde ham/filtreli/normalize deger gorunur
- Sensor yokken simulator ile ayni akıs calisir

Teslimler:
- InputProvider implementasyonlari (Mouse/Simulator/Playback/Serial)
- Kalibrasyon ekranı (min/max yakalama) + ayar kaydi (PlayerPrefs)

## Faz 4 – Oyunlasma & UX (3–5 gun)

Hedef: Oyun hissi ve motivasyon.

Isler:
- Buyu animasyonlari:
  - Daire -> ates topu
  - Kare -> elektrik
  - Ucgen -> zehir
- Basari/basarisizlik geri bildirimi (ses + efekt)
- Zorluk parametreleri:
  - Canavar hizlanmasi (sure azalir)
  - Sekil toleransi daralir (ileri seviye)
- “Tekrar dene” akisi (GameOver -> Restart)

Kabul kriteri:
- 5+ canavar ard arda akıcı oynanis
- Fail-state “frustre etmeyecek” sekilde hizli restart

Teslimler:
- 3 buyu efekti (placeholder VFX bile olabilir)
- Ses cue’lari (basari/basarisizlik) + basit ayar (sessiz mod opsiyonel)

## Faz 5 – Veri Kaydi & Raporlanabilirlik (2–4 gun)

Hedef: Terapist paneline giden KPI’larin temeli.

Isler:
- Seans metadata:
  - tarih/saat
  - N ve P
  - her canavar: sekil, basari, tamamlanma suresi, hata metriği
- CSV/JSON export

Kabul kriteri:
- Her seans sonunda dosya cikiyor
- Tekrar calistirmada bozulmuyor

Teslimler:
- `exports/` altina otomatik dosya cikisi (CSV/JSON)
- Ornek 3 seans verisi (simulator ile)

## Faz 6 – Test Planı (surekli)

- Birim test: sekil dogrulama fonksiyonlari (ornek path’ler)
- Entegrasyon: input provider degisimi (mouse <-> simulator <-> serial)
- Performans: FPS, gecikme (input->ekran)
- Kullanilabilirlik: “ilk kez kullanan” 3 kisi ile mini test, notlar

## Faz 7 – Paketleme (Build) & Sunum (1–2 gun)

Hedef: Mac’te calisan tek paket ve demo.

- Build ayarlari (macOS IL2CPP)
- Oyun icinde “Demo modu” (kolay zorluk, hizli baslangic)
- Sunum icin 30–60 sn kayit (opsiyonel)

Kabul kriteri:
- Build aciliyor, 5 dk stabil oynaniyor
- Final ekrani dogru (N ve P)

## Riskler ve Alternatifler

- Donanim gecikmesi: PlaybackProvider + Simulator ile tum oyunu gelistir; sensör gelince sadece provider degisir.
- Sekil tanima zor: Grid/yon-sekans dogrulamaya gec (daha kararlı).
- Kullanici basarisizligi: Game over yerine “can kaybi” veya “3 hak” (opsiyonel, MVP disi).

## Sürüm Kontrolü (opsiyonel ama onerilir)

Unity projelerinde buyuk dosyalar olacagi icin:
- Git + `.gitignore` (Library/Temp/Logs vs)
- (Opsiyonel) Git LFS: buyuk asset’ler icin

## Netlestirilmesi Gereken Kararlar (Sizden Onayla)

## Oncelikli Karar Listesi (Sizden Onayla)

1) Sekil dogrulama yontemi: Path-following mi, Grid/yon-sekans mi?
2) Donanim baglanti: USB Serial mi, BLE mi?
3) Fail-state: Tek hata = game over (sizin ilk taniminiz) mi, yoksa 3 hak mi?

4) Sekil cizim arayuzu: serbest cizim mi, yoksa “yol uzerinde iz-surme” mi?
5) Zorluk: her canavarda sure azalacak mi, sabit mi?

