// Scripts/Audio/LevelAudio.cs
// Coloque um nó com este script em cada fase: toca a música e o ambiente ao entrar.
// Com o ciclo dia/noite, troca para a música do dia quando clareia (e volta à noite).
using Godot;

namespace DeadFrequency;

public partial class LevelAudio : Node
{
    [Export(PropertyHint.File, "*.wav,*.ogg")] public string MusicPath { get; set; } = "res://Audio/Music/music_world.wav";
    [Export(PropertyHint.File, "*.wav,*.ogg")] public string DayMusicPath { get; set; } = "res://Audio/Music/music_day.wav";
    [Export(PropertyHint.File, "*.wav,*.ogg")] public string AmbiencePath { get; set; } = "res://Audio/Music/ambience_city.wav";

    private bool? _dayMusic;
    private double _check;

    public override void _Ready()
    {
        AudioManager.Instance?.SetMuffled(false);
        AudioManager.Instance?.PlayAmbience(AmbiencePath, 3f);
        // A primeira música sai no fim do frame, depois do DayNightController calcular a hora.
        Callable.From(() => UpdateMusic(3f)).CallDeferred();
    }

    public override void _Process(double delta)
    {
        _check -= delta;
        if (_check > 0)
            return;
        _check = 2.0;
        UpdateMusic(8f);
    }

    private void UpdateMusic(float fade)
    {
        float night = DayNightController.Night;
        // Histerese: só troca quando passa bem do meio, para não ficar alternando no crepúsculo.
        bool wantDay = _dayMusic switch
        {
            true => night < 0.65f,
            false => night < 0.3f,
            null => night < 0.5f,
        };
        if (_dayMusic == wantDay)
            return;
        _dayMusic = wantDay;
        string path = wantDay && ResourceLoader.Exists(DayMusicPath) ? DayMusicPath : MusicPath;
        AudioManager.Instance?.PlayMusic(path, fade);
    }
}
