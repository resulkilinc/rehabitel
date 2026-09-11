# REHABIT-EL — Veri Sözleşmesi (Data Contract)

Bu doküman, oyun tarafından üretilecek seans verilerinin yapısını tanımlar.
Terapist paneli ve veri analizi bu formata göre çalışacaktır.

## 1. Seans Verisi (Session)

| Alan             | Tip      | Açıklama                              | Örnek                                  |
|------------------|----------|---------------------------------------|----------------------------------------|
| session_id       | string   | Benzersiz seans kimliği (GUID)        | "a1b2c3d4-e5f6-7890-abcd-ef1234567890" |
| started_at       | string   | Seans başlangıç zamanı (ISO 8601)     | "2026-04-22T14:30:00+03:00"            |
| ended_at         | string   | Seans bitiş zamanı (ISO 8601)         | "2026-04-22T14:32:45+03:00"            |
| monster_count    | int      | Toplam öldürülen canavar sayısı (N)   | 12                                     |
| score_total      | int      | Toplam puan (P = N × 10)             | 120                                    |
| duration_seconds | float    | Seans süresi (saniye)                 | 165.3                                  |
| input_mode       | string   | Kullanılan giriş modu                 | "Mouse" / "Serial" / "Simulator"       |
| difficulty_level | int      | Ulaşılan zorluk seviyesi              | 4                                      |

## 2. Canavar Kaydı (MonsterRecord)

Her seansta öldürülen/kaçırılan her canavar için bir kayıt tutulur.

| Alan                | Tip    | Açıklama                              | Örnek       |
|---------------------|--------|---------------------------------------|-------------|
| monster_index       | int    | Seans içindeki canavar sırası (0'dan) | 0           |
| shape               | string | Atanan şekil                          | "Circle"    |
| success             | bool   | Başarılı mı?                          | true        |
| time_to_complete_ms | float  | Tamamlama süresi (milisaniye)         | 3450.5      |
| time_allowed_ms     | float  | İzin verilen süre (milisaniye)        | 8000.0      |
| error_metric        | float  | RMSE hata değeri (normalize)          | 0.12        |
| drawn_point_count   | int    | Çizilen nokta sayısı                  | 47          |
| threshold_used      | float  | Kullanılan kabul eşiği                | 0.25        |

## 3. CSV Format

### Dosya adı formatı:
```
session_{session_id}_{YYYYMMDD_HHmmss}.csv
```

### Başlık satırı:
```csv
session_id,started_at,ended_at,monster_count,score_total,monster_index,shape,success,time_to_complete_ms,time_allowed_ms,error_metric,drawn_point_count,threshold_used
```

### Örnek satır:
```csv
a1b2c3d4,2026-04-22T14:30:00,2026-04-22T14:32:45,12,120,0,Circle,true,3450.5,8000.0,0.12,47,0.25
a1b2c3d4,2026-04-22T14:30:00,2026-04-22T14:32:45,12,120,1,Square,true,4200.0,8000.0,0.18,52,0.25
a1b2c3d4,2026-04-22T14:30:00,2026-04-22T14:32:45,12,120,2,Triangle,false,8000.0,8000.0,0.45,23,0.25
```

## 4. JSON Format

### Dosya adı formatı:
```
session_{session_id}_{YYYYMMDD_HHmmss}.json
```

### Yapı:
```json
{
  "session_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "started_at": "2026-04-22T14:30:00+03:00",
  "ended_at": "2026-04-22T14:32:45+03:00",
  "monster_count": 12,
  "score_total": 120,
  "duration_seconds": 165.3,
  "input_mode": "Mouse",
  "difficulty_level": 4,
  "monsters": [
    {
      "monster_index": 0,
      "shape": "Circle",
      "success": true,
      "time_to_complete_ms": 3450.5,
      "time_allowed_ms": 8000.0,
      "error_metric": 0.12,
      "drawn_point_count": 47,
      "threshold_used": 0.25
    }
  ]
}
```

## 5. Çıktı Konumu

- Unity Editor modunda: `Assets/Exports/`
- Build modunda: `Application.persistentDataPath + "/Exports/"`
- Her seans sonunda otomatik yazılır (üzerine yazmaz, ek dosya oluşturur)
