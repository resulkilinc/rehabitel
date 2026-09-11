# REHABIT-EL Oyun (PC) – MVP Yol Haritasi

Bu dokuman, 1 adet flex sensor ile PC uzerinde gelistirilecek Unity tabanli MVP oyunun gelistirme yol haritasini ve teknik kararlarini ozetler.

## 1) Hedef MVP (Oyun Mantigi)

- Oyuncu: Buyucu
- Dusman: Canavar dalgalari
- Her canavarin ustunde bir sekil: Daire / Kare / Ucgen (rastgele)
- Gorev: Canavar oyuncuya ulasmadan once ilgili sekli cizmek
- Basari: Sekil tamamlanirsa buyu atilir ve canavar olur
- Basarisizlik: Sekil tamamlanmazsa oyun biter (game over)
- Skor: Her canavar 10 puan
- Final ekran: Canavar sayisi (N) ve Puan (P = N * 10)

## 2) Kontrol (1 Flex Sensor)

MVP icin flex sensor, cizim dogrulamasinda "sekil tanima" yerine, once "sekil parcalari" mantigi icin kullanilacaktir:

- Flex degeri normalize edilir (0.0 - 1.0)
- Oyun icindeki cizim, 2 boyutlu bir imlecin hareketiyle temsil edilir
- 1 sensor oldugu icin imlec hareketi iki yoldan biriyle modellenir:
  - Secenek A (daha kolay): Imlec sadece tek eksende (Y) hareket eder, sekiller 1D sekans dogrulama ile tamamlanir
  - Secenek B (daha dogal): Imlec X konumu zamanla akar, flex Y'i kontrol eder; boylece oyuncu "yol uzerinde" cizer

Not: Gercek anlamda serbest 2D cizim icin en az 2 bagimsiz kontrol kanali gerekir. MVP'de 1 flex ile "gorev tamamlama" kurgusu korunarak ilerlenir.

## 3) Unity'de Moduller

- Veri girisi: Serial (USB) veya BLE verisini PC tarafina dusurup Unity'ye aktarma
- Kalibrasyon: min/max yakalama ve otomatik normalize
- Filtre: EMA (exponential smoothing) ile titreme azaltma
- Oyun dongusu: Spawn -> Sekil gorevi -> Dogrulama -> Skor -> Spawn
- UI: Skor, kalan sure, anlik sekil, final ekran
- Kayit: Seans sonunda N ve P degerlerini JSON/CSV olarak kaydetme (MVP)

## 4) Gelistirme Adimlari

### Adim 0 – Kurulum
- Unity Hub (kurulu)
- Unity Editor LTS (Hub icinden)
- Visual Studio Code (kurulu)

### Adim 1 – Unity Proje Skeleti
- 2D URP olmadan basit 2D sahne
- Scene 1: MainMenu (Start)
- Scene 2: Game
- Scene 3: GameOver

### Adim 2 – Oyun Mekanikleri (Sensorsuz)
- Sekil secimi (random)
- Geri sayim (canavarin gelis suresi)
- Basari / basarisizlik kosullari (dummy input ile)
- Skor hesaplama (N ve P)

### Adim 3 – Flex Verisi Entegrasyonu
- Serial okuma
- Kalibrasyon ekranı (min/max)
- Normalize + filtre
- Input -> gorev tamamlama baglantisi

### Adim 4 – Sekil Dogrulama (MVP)
- Daire / kare / ucgen icin ayri "sekans" hedefleri
- Tolerans (esik) tabanli dogrulama
- Basarisizlikta game over

### Adim 5 – Kayit ve Rapor
- Seans kimligi, tarih, N, P, tamamlanan gorev listesi
- CSV ciktisi

## 5) Unity Hub Editor Kurulumu (Oneri)

- Unity 2022 LTS veya 2023 LTS (LTS tercih)
- Modules:
  - Mac Build Support (IL2CPP) (PC hedefi macOS uzerinde calisma icin gerekli)
  - (Opsiyonel) Windows Build Support (Cross Compile) – gerekmez, ama ileride Windows'ta calistiracaksaniz

