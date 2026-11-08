// Scripts/Audio/RadioStation.cs
// Rádio Frequência 96.6 FM: a programação é a mesma para todo mundo porque sai do relógio (unix time).
// Cada bloco = vinheta + música. Dois jogadores (ou a van e o escritório) ouvem o mesmo trecho.
// Músicas originais geradas por código (Audio/synth_radio.py.txt), sem direitos autorais de terceiros.
using Godot;

namespace DeadFrequency;

public static class RadioStation
{
    public const string Frequency = "96.6";
    public const string Name = "Rádio Frequência 96.6 FM";
    public const string JinglePath = "res://Audio/Radio/radio_jingle.ogg";
    public const float JingleLength = 2.6f;

    public readonly record struct Track(string Title, string Artist, string Path, float Length);

    public static readonly Track[] Playlist =
    {
        new("Frequência Morta", "Os Sintonizados", "res://Audio/Radio/radio_01_frequencia_morta.ogg", 99.0f),
        new("Bossa da Madrugada", "Trio Estática", "res://Audio/Radio/radio_02_bossa_da_madrugada.ogg", 86.8f),
        new("Asfalto Molhado", "Lo-Fi do Viaduto", "res://Audio/Radio/radio_03_asfalto_molhado.ogg", 94.4f),
        new("Forró do Entregador", "Zé da Van e Banda", "res://Audio/Radio/radio_04_forro_do_entregador.ogg", 88.1f),
        new("Neon na Rodovia", "Cartucho 16", "res://Audio/Radio/radio_05_neon_na_rodovia.ogg", 93.8f),
    };

    public static float CycleLength
    {
        get
        {
            float total = 0f;
            foreach (Track t in Playlist)
                total += JingleLength + t.Length;
            return total;
        }
    }

    /// <summary>O que está no ar agora: índice da música, se é a vinheta, e quantos segundos já tocaram.</summary>
    public static (int Index, bool Jingle, float Offset) Now()
    {
        double pos = Time.GetUnixTimeFromSystem() % CycleLength;
        for (int i = 0; i < Playlist.Length; i++)
        {
            if (pos < JingleLength)
                return (i, true, (float)pos);
            pos -= JingleLength;
            if (pos < Playlist[i].Length)
                return (i, false, (float)pos);
            pos -= Playlist[i].Length;
        }
        return (0, true, 0f);
    }

    public static string NowPlayingText()
    {
        (int i, bool _, float _) = Now();
        Track t = Playlist[i];
        return $"{Frequency} FM · {t.Title} — {t.Artist}";
    }
}
