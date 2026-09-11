# REHABIT-EL Oyun — Game Design Document (Lite)

## 1. Genel Bakış

- **Proje adı:** REHABIT-EL Rehabilitasyon Oyunu
- **Platform:** PC (macOS geliştirme, Windows build opsiyonel)
- **Motor:** Unity 2022 LTS (2D)
- **Tema:** Büyücü vs Canavarlar
- **Hedef kullanıcı:** Üst ekstremite rehabilitasyonu gören hastalar
- **Kontrol:** 1 parmak flex sensör (ESP32 Serial) veya fare (geliştirme/demo)

## 2. Core Loop (Oyun Döngüsü)

```
Canavar Spawn → Şekil Göster → Oyuncu Çizer → Doğrula → Büyü At / Game Over
     ↑                                                        |
     └────────── Başarı: N++, P+=10, Yeni Canavar ────────────┘
```

1. Ekranın sağ tarafından bir canavar spawn olur
2. Canavar, üzerindeki şekille birlikte sola (oyuncuya) doğru hareket eder
3. Oyuncu, canavar ulaşmadan önce ilgili şekli çizer
4. Şekil doğrulanır (Path-following RMSE)
5. **Başarı:** Büyü animasyonu oynar, canavar ölür, skor artar, yeni canavar gelir
6. **Başarısızlık:** Şekil yanlış veya süre doldu → Game Over

## 3. Şekiller & Büyüler

| Şekil   | Büyü       | Renk            | Efekt Tipi    |
|---------|------------|-----------------|---------------|
| Daire   | Ateş Topu  | Turuncu/Kırmızı | Patlama       |
| Kare    | Elektrik   | Mavi/Beyaz      | Çarpma        |
| Üçgen   | Zehir      | Yeşil/Mor       | Yayılma       |

## 4. Zorluk Sistemi

- **Başlangıç süresi:** 8 saniye
- **Her 3 canavarda:** Süre %10 azalır
- **Minimum süre:** 2 saniye
- **İleri seviye (opsiyonel):** Şekil toleransı daralır

## 5. Skor Sistemi

- Her öldürülen canavar: **10 puan**
- Final ekranında gösterilecek:
  - Canavar sayısı (N)
  - Toplam puan (P = N × 10)
  - En yüksek skor (PlayerPrefs)

## 6. Sahneler

### 6.1 MainMenu
- Proje başlığı ("REHABIT-EL")
- "Başla" butonu
- "Kalibrasyon" butonu (sensör bağlıysa)
- En yüksek skor gösterimi

### 6.2 Calibration (opsiyonel)
- "Parmağınızı düz tutun" → 3 sn min değer kaydı
- "Parmağınızı maksimum bükün" → 3 sn max değer kaydı
- Progress bar + onay butonu

### 6.3 Game
- Sol: Büyücü (statik)
- Sağ: Canavarlar yaklaşır
- Orta: Çizim alanı + referans şekil
- Üst: HUD (skor, canavar sayısı, süre, aktif şekil)

### 6.4 GameOver
- "Oyun Bitti!" başlığı
- Sonuçlar: N ve P
- "Tekrar Dene" butonu → Game
- "Ana Menü" butonu → MainMenu

## 7. UI Elementleri

- **HUD:** Skor (P), Canavar (N), Süre (geri sayım), Aktif şekil ikonu
- **Çizim alanı:** Yarı saydam arka plan, referans şekil silüeti
- **Geri bildirim:** Başarı = yeşil flash, Başarısızlık = kırmızı flash

## 8. Ses & VFX Listesi (MVP)

### Ses
- Başarı: Kısa "ding" / "whoosh"
- Başarısızlık: Kısa "buzz" / "thud"
- Büyü: Şekle göre farklı (ateş/elektrik/zehir)
- Arka plan: Basit ambient müzik (opsiyonel)

### VFX
- Büyü partikül efektleri (3 farklı)
- Canavar ölüm animasyonu (fade out veya patlama)
- Çizim çizgisi glow efekti
- Ekran sarsıntısı (başarısızlıkta)

## 9. Veri Kayıt

Her seans sonunda otomatik CSV/JSON export:
- Seans ID, başlangıç/bitiş zamanı
- Toplam canavar ve skor
- Her canavar için: şekil, başarı durumu, tamamlama süresi, RMSE hata değeri
