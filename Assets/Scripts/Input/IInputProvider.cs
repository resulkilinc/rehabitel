// ============================================================
// IInputProvider.cs — REHABIT-EL
// Tüm giriş kaynaklarının uygulaması gereken arayüz.
// Mouse, Simulator, Serial, Playback aynı arayüzü kullanır.
// ============================================================
using UnityEngine;

namespace RehabitEL.Input
{
    /// <summary>
    /// Input provider arayüzü. Tüm giriş kaynakları bu arayüzü uygular.
    /// Bu sayede oyun mantığı, giriş kaynağından bağımsız çalışır.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>Provider'ın okunabilir adı.</summary>
        string ProviderName { get; }

        /// <summary>Provider aktif ve veri alıyor mu?</summary>
        bool IsConnected { get; }

        /// <summary>Oyuncu şu anda çizim yapıyor mu?</summary>
        bool IsDrawing { get; }

        /// <summary>Anlık çizim pozisyonunu döndürür (dünya koordinatları).</summary>
        Vector2 GetDrawPosition();

        /// <summary>Ham flex sensör değerini döndürür (0.0 - 1.0).</summary>
        float GetRawFlexValue();

        /// <summary>Normalize ve filtreli flex değerini döndürür (0.0 - 1.0).</summary>
        float GetFilteredFlexValue();

        /// <summary>Provider'ı başlatır.</summary>
        void Initialize();

        /// <summary>Provider'ı durdurur ve kaynakları serbest bırakır.</summary>
        void Shutdown();
    }
}
