# REHABIT-EL

Unity tabanlı rehabilitasyon oyunu. Oyuncu bir büyücü; canavarlar üzerindeki şekilleri (daire / kare / üçgen) zamanında çizerek büyüyü tamamlar.

Flex sensör (ESP32 Serial), saat/HTTP girişi veya fare ile oynanabilir. Seans verisi JSON/CSV olarak dışa aktarılabilir.

## Özellikler

- Core loop: spawn → şekil görevi → çizim doğrulama → büyü / game over
- Şekil doğrulama (path-following)
- Zorluk: süre kademeli kısalır
- Kalibrasyon ve sinyal filtreleme (EMA)
- Girdi katmanı: Serial flex, HTTP watch, simülatör, fare
- Seans kaydı (`Assets/Exports/` yerel kalır; repoya eklenmez)

## Gereksinimler

- Unity Hub
- Unity Editor **6000.4.3f1** (Unity 6)
- C# IDE (Visual Studio, Rider veya VS Code)

## Kurulum

1. Bu depoyu klonla.
2. Unity Hub → **Open** → proje kökünü seç.
3. `Assets/Scenes/MainMenu.unity` sahnesini aç.
4. Play.

## Proje yapısı

```
Assets/
├── Scripts/
│   ├── Core/          # GameManager, sahne, zorluk
│   ├── Drawing/       # tuval, çizgi, şekil doğrulama
│   ├── Input/         # flex / HTTP / fare / simülatör
│   ├── Monster/       # spawn ve canavar
│   ├── Calibration/   # kalibrasyon + filtre
│   ├── UI/            # menü, oyun, game over
│   ├── Effects/       # büyü, sarsıntı, parallax
│   └── Data/          # seans kaydı / export
├── Scenes/
├── Editor/
└── Plugins/
docs/                  # GDD lite ve yol haritası özeti
```

## Notlar

- `Library/`, `Logs/`, `Temp/`, `UserSettings/` ve seans export’ları bilerek dışarıda bırakılmıştır.
- Rapor PDF/DOCX ve üretım scriptleri bu depoda yoktur.
