// Scripts/Audio/RadioMediaInteractable.cs
// Disco (música da 96.6) ou fita (comercial/vinheta gravado) na discoteca da rádio (v0.10.1).
// Mire e aperte E: toca uma prévia ali mesmo; E de novo para; pegar outro troca. Só um toca por vez.
using Godot;

namespace DeadFrequency;

public partial class RadioMediaInteractable : StaticBody3D, IInteractable
{
    private static RadioMediaInteractable _current;

    /// <summary>"disco" ou "fita" (aparece no texto).</summary>
    public string Kind { get; set; } = "disco";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string AudioPath { get; set; } = "";
    /// <summary>Quanto tocar, em segundos (0 = inteiro).</summary>
    public float PreviewSeconds { get; set; }

    private AudioStreamPlayer3D _player;
    private SceneTreeTimer _timer;

    public bool IsPlaying => _player != null && _player.Playing;

    public override void _Ready()
    {
        _player = new AudioStreamPlayer3D
        {
            Bus = AudioManager.MusicBus,
            UnitSize = 3f,
            MaxDistance = 14f,
            VolumeDb = -4f,
            AttenuationFilterCutoffHz = 5000f,
        };
        AddChild(_player);
        _player.Finished += () => { if (_current == this) _current = null; };
    }

    public override void _ExitTree()
    {
        if (_current == this)
            _current = null;
    }

    public string GetInteractionPrompt(PlayerController player)
    {
        string what = Kind == "fita" ? "a fita" : "o disco";
        string name = string.IsNullOrEmpty(Artist) ? Title : $"{Title} — {Artist}";
        return IsPlaying ? $"[E] Parar {what}" : $"[E] Ouvir {what}: {name}";
    }

    public void Interact(PlayerController player)
    {
        if (IsPlaying)
        {
            Stop();
            return;
        }
        if (_current != null && IsInstanceValid(_current))
            _current.Stop();
        if (!ResourceLoader.Exists(AudioPath) || GD.Load<AudioStream>(AudioPath) is not AudioStream stream)
        {
            GameHud.Toast(this, Kind == "fita" ? "Fita sem som (está em branco)." : "Disco riscado, não toca.");
            return;
        }
        if (stream is AudioStreamOggVorbis ogg)
            ogg.Loop = false;
        _player.Stream = stream;
        _player.Play();
        _current = this;
        GameHud.Toast(this, "♪ " + (string.IsNullOrEmpty(Artist) ? Title : $"{Title} — {Artist}"));
        if (PreviewSeconds > 0.5f)
        {
            _timer = GetTree().CreateTimer(PreviewSeconds);
            SceneTreeTimer mine = _timer;
            mine.Timeout += () =>
            {
                if (_timer == mine && IsInstanceValid(this))
                    Stop();
            };
        }
    }

    private void Stop()
    {
        _timer = null;
        _player?.Stop();
        if (_current == this)
            _current = null;
    }
}
