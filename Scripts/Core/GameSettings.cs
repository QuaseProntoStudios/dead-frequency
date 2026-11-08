// Scripts/Core/GameSettings.cs
// Autoload (registrado no project.godot como "GameSettings").
// Guarda as opções do jogador em user://settings.cfg e as aplica (áudio, janela, vsync).
// Quem depende delas (Player, menus) escuta o sinal SettingsChanged.
using Godot;

namespace DeadFrequency;

public partial class GameSettings : Node
{
    [Signal] public delegate void SettingsChangedEventHandler();

    public static GameSettings Instance { get; private set; }

    private const string FilePath = "user://settings.cfg";

    /// <summary>Multiplicador sobre a sensibilidade base do PlayerController.</summary>
    public float MouseSensitivity { get; set; } = 1.0f;
    public bool InvertY { get; set; }
    public float Fov { get; set; } = 75f;
    /// <summary>0..1 linear.</summary>
    public float MasterVolume { get; set; } = 0.8f;
    public float MusicVolume { get; set; } = 0.7f;
    public float SfxVolume { get; set; } = 0.9f;
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;
    /// <summary>0 = ciclo dia/noite com o tempo passando, 1 = sempre noite, 2 = sempre dia.</summary>
    public int TimeMode { get; set; }
    /// <summary>Minutos reais para um dia inteiro no jogo (24 h).</summary>
    public float DayLengthMinutes { get; set; } = 24f;
    /// <summary>Última versão cujas novidades o jogador já viu (para mostrar o changelog só uma vez).</summary>
    public string LastSeenVersion { get; set; } = "";
    /// <summary>Multiplayer: 0 = servidor oficial do projeto, 1 = meu servidor (ServerUrl), 2 = hospedar no meu PC.</summary>
    public int ServerMode { get; set; }
    /// <summary>Endereço do "meu servidor" (só usado no modo 1).</summary>
    public string ServerUrl { get; set; } = "";
    public string PlayerName { get; set; } = "Entregador";
    /// <summary>Última profissão escolhida (Professions.*).</summary>
    public string Profession { get; set; } = "entregador";
    /// <summary>Ao criar sala: o servidor escolhe a profissão de cada um (equilibra as vagas).</summary>
    public bool AutoProfession { get; set; }
    /// <summary>Ao criar sala: aparece na lista de salas online.</summary>
    public bool PublicRoom { get; set; } = true;

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
        Load();
        Apply();
    }

    /// <summary>Aplica tudo e avisa os ouvintes. Chamado pelos menus a cada alteração.</summary>
    public void Apply()
    {
        SetBusVolume("Master", MasterVolume);
        SetBusVolume("Music", MusicVolume);
        SetBusVolume("SFX", SfxVolume);
        SetBusVolume("UI", SfxVolume);

        // Só mexe na janela quando muda: rodando incorporado no editor, trocar o modo gera aviso.
        DisplayServer.WindowMode wantedMode = Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        DisplayServer.WindowMode currentMode = DisplayServer.WindowGetMode();
        bool isFullscreen = currentMode is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
        if (isFullscreen != Fullscreen && !Engine.IsEmbeddedInEditor())
            DisplayServer.WindowSetMode(wantedMode);

        DisplayServer.VSyncMode wantedVsync = VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled;
        if (DisplayServer.WindowGetVsyncMode() != wantedVsync)
            DisplayServer.WindowSetVsyncMode(wantedVsync);

        EmitSignal(SignalName.SettingsChanged);
    }

    private static void SetBusVolume(string busName, float linear)
    {
        int bus = AudioServer.GetBusIndex(busName);
        if (bus < 0)
            return;
        AudioServer.SetBusMute(bus, linear <= 0.001f);
        AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(Mathf.Max(linear, 0.0001f)));
    }

    public void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("controls", "mouse_sensitivity", MouseSensitivity);
        cfg.SetValue("controls", "invert_y", InvertY);
        cfg.SetValue("video", "fov", Fov);
        cfg.SetValue("video", "fullscreen", Fullscreen);
        cfg.SetValue("video", "vsync", VSync);
        cfg.SetValue("video", "time_mode", TimeMode);
        cfg.SetValue("video", "day_length_minutes", DayLengthMinutes);
        cfg.SetValue("meta", "last_seen_version", LastSeenVersion);
        cfg.SetValue("audio", "master_volume", MasterVolume);
        cfg.SetValue("audio", "music_volume", MusicVolume);
        cfg.SetValue("audio", "sfx_volume", SfxVolume);
        cfg.SetValue("network", "server_mode", ServerMode);
        cfg.SetValue("network", "server_url", ServerUrl);
        cfg.SetValue("network", "player_name", PlayerName);
        cfg.SetValue("game", "profession", Profession);
        cfg.SetValue("network", "auto_profession", AutoProfession);
        cfg.SetValue("network", "public_room", PublicRoom);

        Error err = cfg.Save(FilePath);
        if (err != Error.Ok)
            GD.PushWarning($"GameSettings: não foi possível salvar ({err}).");
    }

    public void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(FilePath) != Error.Ok)
            return; // primeira execução: usa os padrões

        MouseSensitivity = cfg.GetValue("controls", "mouse_sensitivity", MouseSensitivity).AsSingle();
        InvertY = cfg.GetValue("controls", "invert_y", InvertY).AsBool();
        Fov = cfg.GetValue("video", "fov", Fov).AsSingle();
        Fullscreen = cfg.GetValue("video", "fullscreen", Fullscreen).AsBool();
        VSync = cfg.GetValue("video", "vsync", VSync).AsBool();
        // Versões antigas tinham só "Modo dia" (liga/desliga).
        bool oldDay = cfg.GetValue("video", "day_mode", false).AsBool();
        TimeMode = cfg.GetValue("video", "time_mode", oldDay ? 2 : 0).AsInt32();
        DayLengthMinutes = cfg.GetValue("video", "day_length_minutes", DayLengthMinutes).AsSingle();
        LastSeenVersion = cfg.GetValue("meta", "last_seen_version", LastSeenVersion).AsString();
        MasterVolume = cfg.GetValue("audio", "master_volume", MasterVolume).AsSingle();
        MusicVolume = cfg.GetValue("audio", "music_volume", MusicVolume).AsSingle();
        SfxVolume = cfg.GetValue("audio", "sfx_volume", SfxVolume).AsSingle();
        ServerUrl = cfg.GetValue("network", "server_url", ServerUrl).AsString();
        Profession = cfg.GetValue("game", "profession", Profession).AsString();
        AutoProfession = cfg.GetValue("network", "auto_profession", AutoProfession).AsBool();
        PublicRoom = cfg.GetValue("network", "public_room", PublicRoom).AsBool();
        // Versões antigas guardavam o endereço oficial no campo: vira o modo "oficial".
        if (NetClient.NormalizeServer(ServerUrl) == NetClient.NormalizeServer(NetClient.OfficialServer))
            ServerUrl = "";
        ServerMode = cfg.GetValue("network", "server_mode", string.IsNullOrWhiteSpace(ServerUrl) ? 0 : 1).AsInt32();
        PlayerName = cfg.GetValue("network", "player_name", PlayerName).AsString();
    }
}
