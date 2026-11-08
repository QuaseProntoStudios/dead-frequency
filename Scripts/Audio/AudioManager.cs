// Scripts/Audio/AudioManager.cs
// Autoload (registrado no project.godot como "AudioManager").
// Música com crossfade, som ambiente em loop e sons de interface. Funciona com o jogo pausado.
// Buses (default_bus_layout.tres): Music (com filtro passa-baixa ligado na pausa), SFX e UI.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public enum UiSound
{
    Hover,
    Click,
    Back,
    Start,
    Pause,
}

public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    public const string MusicBus = "Music";
    public const string SfxBus = "SFX";
    public const string UiBus = "UI";
    /// <summary>Bus só da trilha (envia para Music). Abaixa quando o rádio da van está ligado.</summary>
    public const string BgmBus = "Bgm";

    /// <summary>Volume base das faixas (os sliders das Opções controlam os buses).</summary>
    [Export] public float MusicVolumeDb { get; set; } = -9f;
    [Export] public float AmbienceVolumeDb { get; set; } = -12f;

    private static readonly Dictionary<UiSound, string> UiPaths = new()
    {
        { UiSound.Hover, "res://Audio/UI/ui_hover.wav" },
        { UiSound.Click, "res://Audio/UI/ui_click.wav" },
        { UiSound.Back, "res://Audio/UI/ui_back.wav" },
        { UiSound.Start, "res://Audio/UI/ui_start.wav" },
        { UiSound.Pause, "res://Audio/UI/ui_pause.wav" },
    };

    private readonly Dictionary<UiSound, AudioStream> _uiStreams = new();
    private readonly List<AudioStreamPlayer> _uiPool = new();
    private int _uiNext;

    private AudioStreamPlayer _musicA;
    private AudioStreamPlayer _musicB;
    private AudioStreamPlayer _activeMusic;
    private AudioStreamPlayer _ambience;
    private string _currentMusicPath = "";
    private string _currentAmbiencePath = "";

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        EnsureBus(BgmBus, MusicBus);
        _musicA = MakePlayer("MusicA", BgmBus);
        _musicB = MakePlayer("MusicB", BgmBus);
        _ambience = MakePlayer("Ambience", SfxBus);

        for (int i = 0; i < 4; i++)
            _uiPool.Add(MakePlayer($"Ui{i}", UiBus));

        foreach ((UiSound key, string path) in UiPaths)
        {
            if (ResourceLoader.Exists(path))
                _uiStreams[key] = GD.Load<AudioStream>(path);
        }
    }

    private AudioStreamPlayer MakePlayer(string name, string bus)
    {
        var player = new AudioStreamPlayer { Name = name, Bus = bus };
        AddChild(player);
        return player;
    }

    // ---------------- Interface ----------------

    public void PlayUi(UiSound sound)
    {
        if (!_uiStreams.TryGetValue(sound, out AudioStream stream))
            return;

        AudioStreamPlayer player = _uiPool[_uiNext];
        _uiNext = (_uiNext + 1) % _uiPool.Count;
        player.Stream = stream;
        player.PitchScale = 1f + (float)GD.RandRange(-0.03, 0.03);
        player.VolumeDb = sound == UiSound.Hover ? -10f : -4f;
        player.Play();
    }

    // ---------------- Música ----------------

    public void PlayMusic(string path, float fadeSeconds = 2f)
    {
        if (path == _currentMusicPath && _activeMusic != null && _activeMusic.Playing)
            return;

        AudioStream stream = LoadLooping(path);
        if (stream == null)
            return;

        _currentMusicPath = path;
        AudioStreamPlayer next = _activeMusic == _musicA ? _musicB : _musicA;
        AudioStreamPlayer previous = _activeMusic;
        _activeMusic = next;

        next.Stream = stream;
        next.VolumeDb = -60f;
        next.Play();

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(next, "volume_db", MusicVolumeDb, fadeSeconds);
        if (previous != null && previous.Playing)
        {
            tween.TweenProperty(previous, "volume_db", -60f, fadeSeconds);
            tween.Chain().TweenCallback(Callable.From(previous.Stop));
        }
    }

    public void StopMusic(float fadeSeconds = 1.5f)
    {
        _currentMusicPath = "";
        if (_activeMusic == null || !_activeMusic.Playing)
            return;
        AudioStreamPlayer player = _activeMusic;
        Tween tween = CreateTween();
        tween.TweenProperty(player, "volume_db", -60f, fadeSeconds);
        tween.TweenCallback(Callable.From(player.Stop));
    }

    public void PlayAmbience(string path, float fadeSeconds = 2f)
    {
        if (path == _currentAmbiencePath && _ambience.Playing)
            return;
        AudioStream stream = LoadLooping(path);
        if (stream == null)
            return;
        _currentAmbiencePath = path;
        _ambience.Stream = stream;
        _ambience.VolumeDb = -60f;
        _ambience.Play();
        CreateTween().TweenProperty(_ambience, "volume_db", AmbienceVolumeDb, fadeSeconds);
    }

    public void StopAmbience(float fadeSeconds = 1f)
    {
        _currentAmbiencePath = "";
        if (!_ambience.Playing)
            return;
        Tween tween = CreateTween();
        tween.TweenProperty(_ambience, "volume_db", -60f, fadeSeconds);
        tween.TweenCallback(Callable.From(_ambience.Stop));
    }

    /// <summary>Abafa a música (passa-baixa) enquanto o jogo está pausado.</summary>
    public void SetMuffled(bool muffled)
    {
        int bus = AudioServer.GetBusIndex(MusicBus);
        if (bus >= 0 && AudioServer.GetBusEffectCount(bus) > 0)
            AudioServer.SetBusEffectEnabled(bus, 0, muffled);
    }

    /// <summary>Abaixa a trilha (rádio ligado na van). Contador para vários rádios ao mesmo tempo.</summary>
    public void SetMusicDucked(bool ducked)
    {
        int bus = AudioServer.GetBusIndex(BgmBus);
        if (bus < 0)
            return;
        float from = AudioServer.GetBusVolumeDb(bus);
        float to = ducked ? -24f : 0f;
        CreateTween().TweenMethod(Callable.From<float>(v => AudioServer.SetBusVolumeDb(bus, v)), from, to, 0.8f);
    }

    private static void EnsureBus(string name, string sendTo)
    {
        if (AudioServer.GetBusIndex(name) >= 0)
            return;
        AudioServer.AddBus();
        int index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, name);
        AudioServer.SetBusSend(index, AudioServer.GetBusIndex(sendTo) >= 0 ? sendTo : "Master");
    }

    // ---------------- Utilitários ----------------

    public static AudioStream LoadLooping(string path)
    {
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
        {
            GD.PushWarning($"AudioManager: áudio não encontrado: {path}");
            return null;
        }
        return MakeLooping(GD.Load<AudioStream>(path));
    }

    /// <summary>Liga o loop de um WAV importado sem precisar mexer nas opções de importação.</summary>
    public static AudioStream MakeLooping(AudioStream stream)
    {
        if (stream is AudioStreamWav wav && wav.LoopMode == AudioStreamWav.LoopModeEnum.Disabled)
        {
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            wav.LoopBegin = 0;
            wav.LoopEnd = Mathf.RoundToInt(wav.GetLength() * wav.MixRate);
        }
        else if (stream is AudioStreamOggVorbis ogg)
            ogg.Loop = true;
        return stream;
    }
}
