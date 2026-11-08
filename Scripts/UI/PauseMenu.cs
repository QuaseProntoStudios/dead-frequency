// Scripts/UI/PauseMenu.cs
// Menu de pausa (Esc / P). No solo pausa a árvore inteira; online o mundo continua rodando
// (só os controles do jogador são desligados) e aparece o código da sala para convidar amigos.
// A interface é montada em código no _Ready (ver UiStyle).
using System;
using Godot;

namespace DeadFrequency;

public partial class PauseMenu : CanvasLayer
{
    [Export(PropertyHint.File, "*.tscn")]
    public string MainMenuScene { get; set; } = "res://Scenes/UI/main_menu.tscn";

    /// <summary>Pausa sozinho quando a janela do jogo perde o foco (Alt+Tab).</summary>
    [Export] public bool PauseOnFocusLoss { get; set; } = true;
    /// <summary>Usado no multiplayer para travar os controles sem pausar o jogo.</summary>
    [Export] public PlayerController Player { get; set; }

    private static bool Online => NetClient.Instance?.IsOnline == true;

    public bool IsOpen => _root != null && _root.Visible;

    private Control _root;
    private VBoxContainer _mainList;
    private OptionsPanel _options;
    private Button _resumeButton;
    private Button _restartButton;
    private Button _menuButton;
    private VBoxContainer _roomBox;
    private Label _roomCode;
    private Label _roomPlayers;
    private Label _feedback;
    private RichTextLabel _playerList;
    private Label _waitLabel;
    private double _refresh;
    private Button _profButton;
    private VBoxContainer _profList;

    public override void _Ready()
    {
        Layer = 10;
        ProcessMode = ProcessModeEnum.Always;

        _root = new Control { Theme = UiStyle.Theme, MouseFilter = Control.MouseFilterEnum.Stop };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.65f), MouseFilter = Control.MouseFilterEnum.Ignore };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);

        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var stack = new VBoxContainer();
        panel.AddChild(stack);

        _mainList = new VBoxContainer();
        _mainList.AddThemeConstantOverride("separation", 12);
        stack.AddChild(_mainList);

        _mainList.AddChild(UiStyle.MakeLabel("— PAUSADO —", 34));
        BuildRoomBox();
        _waitLabel = UiStyle.MakeLabel("sinal em espera", 16, color: UiStyle.PhosphorDim);
        _mainList.AddChild(_waitLabel);

        _resumeButton = UiStyle.MakeButton("Continuar", Resume, sound: UiSound.Back);
        _mainList.AddChild(_resumeButton);
        _mainList.AddChild(UiStyle.MakeButton("Opções", ShowOptions));
        _profButton = UiStyle.MakeButton("Trocar profissão", ShowProfessions);
        _mainList.AddChild(_profButton);
        _restartButton = UiStyle.MakeButton("Reiniciar turno", Restart);
        _mainList.AddChild(_restartButton);
        _menuButton = UiStyle.MakeButton("Menu principal", GoToMainMenu);
        _mainList.AddChild(_menuButton);
        _mainList.AddChild(UiStyle.MakeButton("Sair do jogo", Quit));

        _mainList.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        _mainList.AddChild(UiStyle.MakeLabel("WASD andar · Shift correr · E usar · Clique arremessar · F lanterna · I inventário · N visão noturna", 14, color: UiStyle.PhosphorDim));
        _mainList.AddChild(UiStyle.MakeLabel("Van: W/S acelerar · A/D virar · Espaço freio de mão · R rádio 96.6 · F3 debug", 14, color: UiStyle.PhosphorDim));

        _profList = new VBoxContainer { Visible = false };
        _profList.AddThemeConstantOverride("separation", 8);
        stack.AddChild(_profList);

        _options = new OptionsPanel { Visible = false };
        _options.BackRequested += ShowMainList;
        stack.AddChild(_options);

        _root.AddChild(UiStyle.MakeScanlines(0.12f));

        _root.Visible = false;
    }

    private void BuildRoomBox()
    {
        _roomBox = new VBoxContainer { Visible = false };
        _roomBox.AddThemeConstantOverride("separation", 8);
        _mainList.AddChild(_roomBox);

        _roomBox.AddChild(UiStyle.MakeLabel("código da sala", 15, color: UiStyle.PhosphorDim));
        _roomCode = UiStyle.MakeLabel("-----", 36);
        _roomBox.AddChild(_roomCode);
        _roomPlayers = UiStyle.MakeLabel("", 15, color: UiStyle.PhosphorDim);
        _roomBox.AddChild(_roomPlayers);
        // Quem está na sala: cor, nome e profissão.
        _playerList = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, CustomMinimumSize = new Vector2(560, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _playerList.AddThemeFontSizeOverride("normal_font_size", 16);
        _roomBox.AddChild(_playerList);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(UiStyle.MakeButton("Copiar código", CopyCode, 170));
        row.AddChild(UiStyle.MakeButton("Copiar convite", CopyInvite, 170));
        row.AddChild(UiStyle.MakeButton("WhatsApp", SendWhatsApp, 150));
        _roomBox.AddChild(row);

        _feedback = UiStyle.MakeLabel("", 14, color: UiStyle.Warning);
        _roomBox.AddChild(_feedback);
    }

    private void CopyCode()
    {
        DisplayServer.ClipboardSet(NetClient.Instance.RoomCode);
        _feedback.Text = "Código copiado!";
    }

    private void CopyInvite()
    {
        DisplayServer.ClipboardSet(NetClient.Instance.InviteText());
        _feedback.Text = "Convite copiado — cole no Discord, WhatsApp...";
    }

    private void SendWhatsApp()
    {
        OS.ShellOpen("https://wa.me/?text=" + Uri.EscapeDataString(NetClient.Instance.InviteText()));
        _feedback.Text = "Abrindo o WhatsApp...";
    }

    private static string PlayerLine(Color color, string name, string prof, bool host)
    {
        Professions.Def def = Professions.Get(prof);
        string safe = name.Replace("[", "(").Replace("]", ")");
        return $"[color=#{color.ToHtml(false)}]●[/color] {safe}  [color=#{def.Color.ToHtml(false)}]{def.Name}[/color]" + (host ? "  [color=#ffb840]★ anfitrião[/color]" : "");
    }

    private void RefreshRoomBox()
    {
        bool online = Online;
        _roomBox.Visible = online;
        _waitLabel.Visible = !online;
        _restartButton.Visible = !online;
        _menuButton.Text = online ? "Sair da sala" : "Menu principal";
        if (!online)
            return;
        NetClient net = NetClient.Instance;
        _roomCode.Text = net.RoomCode;
        _roomPlayers.Text = $"{net.PlayerCount} jogador(es) online · ping {net.PingMs:0} ms" + (net.IsHost ? " · você é o anfitrião" : "");
        var sb = new System.Text.StringBuilder("[center]");
        sb.Append(PlayerLine(net.MyColor, net.PlayerName + " (você)", Professions.Current, net.IsHost));
        foreach (NetPlayerInfo p in net.Players.Values)
            sb.Append('\n').Append(PlayerLine(p.Color, p.Name, p.Profession, p.Id == net.HostId));
        sb.Append("[/center]");
        _playerList.Text = sb.ToString();
        _feedback.Text = "";
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("pause") || ModalScreen.AnyOpen)
            return;

        GetViewport().SetInputAsHandled();

        if (!IsOpen)
            Open();
        else if (_options.Visible)
            ShowMainList();
        else
            Resume();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && PauseOnFocusLoss && !IsOpen && IsInsideTree())
            Open();
    }

    public override void _Process(double delta)
    {
        if (!IsOpen || !Online || !_mainList.Visible)
            return;
        _refresh -= delta;
        if (_refresh > 0)
            return;
        _refresh = 1.0;
        string fb = _feedback.Text;
        RefreshRoomBox();
        _feedback.Text = fb;
    }

    public void Open()
    {
        // Online o mundo não pode parar: os outros jogadores continuam jogando.
        if (Online)
        {
            if (Player != null)
                Player.InputEnabled = false;
        }
        else
        {
            GetTree().Paused = true;
            AudioManager.Instance?.SetMuffled(true);
        }
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _root.Visible = true;
        RefreshRoomBox();
        AudioManager.Instance?.PlayUi(UiSound.Pause);
        ShowMainList();
    }

    public void Resume()
    {
        _root.Visible = false;
        GetTree().Paused = false;
        if (Player != null)
            Player.InputEnabled = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        AudioManager.Instance?.SetMuffled(false);
    }

    private void ShowProfessions()
    {
        _mainList.Visible = false;
        _profList.Visible = true;
        foreach (Node c in _profList.GetChildren())
            c.QueueFree();
        _profList.AddChild(UiStyle.MakeLabel("TROCAR PROFISSÃO", 30));
        Button first = null;
        foreach (Professions.Def def in Professions.All)
        {
            if (!Professions.AvailableIn(def.Id, Professions.CurrentLevel))
                continue;
            string id = def.Id;
            Button b = UiStyle.MakeButton((id == Professions.Current ? "► " : "") + def.Name, () =>
            {
                ProfessionManager.Instance?.SetProfession(id);
                Resume();
            });
            b.AddThemeColorOverride("font_color", def.Color);
            _profList.AddChild(b);
            first ??= b;
        }
        _profList.AddChild(UiStyle.MakeButton("Voltar", ShowMainList, sound: UiSound.Back));
        first?.GrabFocus();
    }

    private void ShowMainList()
    {
        _profList.Visible = false;
        if (_profButton != null)
        {
            bool locked = Online && NetClient.Instance.AutoProfession;
            _profButton.Disabled = locked;
            _profButton.Text = locked ? "Profissão: a sala escolhe" : $"Trocar profissão ({Professions.Get(Professions.Current).Name})";
        }
        _options.Visible = false;
        _mainList.Visible = true;
        _resumeButton.GrabFocus();
    }

    private void ShowOptions()
    {
        _mainList.Visible = false;
        _options.Visible = true;
        _options.FocusFirst();
    }

    private void Restart()
    {
        AudioManager.Instance?.SetMuffled(false);
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void GoToMainMenu()
    {
        NetClient.Instance?.Leave();
        AudioManager.Instance?.SetMuffled(false);
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(MainMenuScene);
    }

    private void Quit() => GetTree().Quit();
}
