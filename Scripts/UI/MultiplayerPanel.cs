// Scripts/UI/MultiplayerPanel.cs
// Tela de multiplayer do menu principal. Três jeitos de jogar:
//   1. Servidor oficial (padrão) — o servidor do projeto na Cloudflare; o endereço nem aparece.
//   2. Meu servidor — você informa o endereço do seu próprio Worker (ou de um amigo que hospeda).
//   3. Hospedar no meu PC — o jogo vira o servidor (LocalServer) e gera um link público com o
//      Cloudflare Tunnel (TunnelService), tipo ngrok, para os amigos entrarem de qualquer lugar.
using System;
using System.Text.RegularExpressions;
using Godot;

namespace DeadFrequency;

public partial class MultiplayerPanel : VBoxContainer
{
    [Signal] public delegate void BackRequestedEventHandler();
    /// <summary>Entrou numa sala: o menu troca para a fase.</summary>
    [Signal] public delegate void JoinedRoomEventHandler();

    private static readonly string[] ModeNames = { "Servidor oficial", "Meu servidor", "Hospedar no meu PC" };
    private static readonly Regex UrlInText = new(@"(https?|wss?)://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private int _mode;
    private string _level = "city";
    private LineEdit _name;
    private LineEdit _server;
    private LineEdit _code;
    private Label _status;
    private Label _modeHint;
    private Control _serverRow;
    private Control _joinBlock;
    private Button _createButton;
    private Button _joinButton;
    private readonly Button[] _modeButtons = new Button[3];

    // Hospedar no PC
    private VBoxContainer _hostBox;
    private Label _hostInfo;
    private Button _hostStart;
    private Button _hostCopy;
    private Button _hostEnter;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        CustomMinimumSize = new Vector2(660, 0);

        AddChild(UiStyle.MakeLabel("MULTIPLAYER", 30));

        GameSettings s = GameSettings.Instance;
        _mode = Mathf.Clamp(s?.ServerMode ?? 0, 0, 2);

        _name = AddField("Seu apelido", s?.PlayerName ?? "Entregador", "Entregador");
        _name.MaxLength = 16;

        // Onde jogar.
        var modeRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        modeRow.AddThemeConstantOverride("separation", 8);
        var modeGroup = new ButtonGroup();
        for (int i = 0; i < 3; i++)
        {
            int mode = i;
            var b = new Button { Text = ModeNames[i], ToggleMode = true, ButtonGroup = modeGroup, ButtonPressed = i == _mode, CustomMinimumSize = new Vector2(200, 40) };
            b.AddThemeFontSizeOverride("font_size", 16);
            b.MouseEntered += () => AudioManager.Instance?.PlayUi(UiSound.Hover);
            b.Pressed += () => SetMode(mode);
            _modeButtons[i] = b;
            modeRow.AddChild(b);
        }
        AddChild(modeRow);
        _modeHint = UiStyle.MakeLabel("", 14, color: UiStyle.PhosphorDim);
        _modeHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _modeHint.CustomMinimumSize = new Vector2(640, 0);
        AddChild(_modeHint);

        _server = AddField("Endereço", s?.ServerUrl ?? "", "https://meu-servidor.workers.dev");
        _serverRow = _server.GetParent<Control>();

        // Fase da sala nova (quem entra pelo código carrega a fase escolhida pelo criador).
        var levelRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        levelRow.AddThemeConstantOverride("separation", 10);
        var levelGroup = new ButtonGroup();
        foreach (GameLevels.Level level in GameLevels.All)
        {
            string id = level.Id;
            var b = new Button { Text = level.Name, ToggleMode = true, ButtonGroup = levelGroup, ButtonPressed = id == _level, CustomMinimumSize = new Vector2(220, 40) };
            b.MouseEntered += () => AudioManager.Instance?.PlayUi(UiSound.Hover);
            b.Pressed += () =>
            {
                _level = id;
                AudioManager.Instance?.PlayUi(UiSound.Click);
            };
            levelRow.AddChild(b);
        }
        AddChild(levelRow);

        _createButton = UiStyle.MakeButton("Criar sala", OnCreatePressed, sound: UiSound.Start);
        _createButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(_createButton);

        BuildHostBox();

        // Entrar na sala de alguém.
        var join = new VBoxContainer();
        join.AddThemeConstantOverride("separation", 8);
        join.AddChild(UiStyle.MakeLabel("— ou entre na sala de um amigo —", 15, color: UiStyle.PhosphorDim));
        var joinRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        joinRow.AddThemeConstantOverride("separation", 10);
        _code = new LineEdit
        {
            PlaceholderText = "CÓDIGO",
            MaxLength = 5,
            CustomMinimumSize = new Vector2(150, 48),
            Alignment = HorizontalAlignment.Center,
        };
        _code.AddThemeFontSizeOverride("font_size", 26);
        _code.TextChanged += text =>
        {
            int caret = _code.CaretColumn;
            _code.Text = text.ToUpperInvariant();
            _code.CaretColumn = caret;
        };
        _code.TextSubmitted += _ => OnJoinPressed();
        joinRow.AddChild(_code);
        joinRow.AddChild(UiStyle.MakeButton("Colar convite", PasteInvite, 170));
        _joinButton = UiStyle.MakeButton("Entrar", OnJoinPressed, 150, UiSound.Start);
        joinRow.AddChild(_joinButton);
        join.AddChild(joinRow);
        _joinBlock = join;
        AddChild(join);

        _status = UiStyle.MakeLabel("", 16, color: UiStyle.Warning);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new Vector2(640, 0);
        AddChild(_status);

        Button back = UiStyle.MakeButton("Voltar", OnBack, sound: UiSound.Back);
        back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(back);

        if (NetClient.Instance != null)
        {
            NetClient.Instance.StatusMessage += OnStatus;
            NetClient.Instance.Joined += OnJoined;
            NetClient.Instance.Disconnected += OnFailed;
        }
        SetMode(_mode, false);
    }

    public override void _ExitTree()
    {
        if (TunnelService.Instance != null)
            TunnelService.Instance.Changed -= RefreshHostBox;
        if (NetClient.Instance == null)
            return;
        NetClient.Instance.StatusMessage -= OnStatus;
        NetClient.Instance.Joined -= OnJoined;
        NetClient.Instance.Disconnected -= OnFailed;
    }

    public override void _Process(double delta)
    {
        // Atualiza a porcentagem do download do cloudflared.
        if (_mode == 2 && TunnelService.Instance?.State == TunnelService.TunnelState.Downloading)
            RefreshHostBox();
    }

    public void FocusFirst() => (_mode == 2 ? _hostStart : _createButton)?.GrabFocus();

    // ---------------- Modo ----------------

    private void SetMode(int mode, bool sound = true)
    {
        _mode = mode;
        if (sound)
            AudioManager.Instance?.PlayUi(UiSound.Click);
        _modeButtons[mode].ButtonPressed = true;
        _serverRow.Visible = mode == 1;
        _createButton.Visible = mode != 2;
        _hostBox.Visible = mode == 2;
        _joinBlock.Visible = mode != 2;
        _modeHint.Text = mode switch
        {
            0 => "Servidor do projeto na nuvem. É só criar a sala ou digitar o código do amigo.",
            1 => "Use o endereço do seu servidor (o Worker da pasta server/) ou o link que um amigo gerou ao hospedar.",
            _ => "Seu PC vira o servidor. O jogo gera um link público grátis (Cloudflare Tunnel) para os amigos entrarem de qualquer lugar.",
        };
        _status.Text = "";
        RefreshHostBox();
    }

    private string CurrentServer() => _mode == 1 ? _server.Text.Trim() : NetClient.OfficialServer;

    // ---------------- Hospedar no PC ----------------

    private void BuildHostBox()
    {
        _hostBox = new VBoxContainer { Visible = false };
        _hostBox.AddThemeConstantOverride("separation", 8);
        _hostInfo = UiStyle.MakeLabel("", 16);
        _hostInfo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _hostInfo.CustomMinimumSize = new Vector2(640, 0);

        _hostStart = UiStyle.MakeButton("Hospedar sala", OnHostPressed, sound: UiSound.Start);
        _hostStart.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 10);
        _hostCopy = UiStyle.MakeButton("Copiar convite", CopyHostInvite, 220);
        _hostEnter = UiStyle.MakeButton("Entrar no jogo", EnterHostedRoom, 220, UiSound.Start);
        row.AddChild(_hostCopy);
        row.AddChild(_hostEnter);

        _hostBox.AddChild(_hostStart);
        _hostBox.AddChild(_hostInfo);
        _hostBox.AddChild(row);
        AddChild(_hostBox);
        TunnelService.Ensure().Changed += RefreshHostBox;
    }

    private void OnHostPressed()
    {
        if (!SaveFields())
            return;
        LocalServer server = LocalServer.Ensure();
        Error err = server.Start(_level);
        if (err != Error.Ok)
        {
            _status.Text = $"Não consegui abrir a porta {LocalServer.DefaultPort} ({err}). Feche outro jogo/servidor que esteja usando ela.";
            return;
        }
        TunnelService.Ensure().Start(server.Port);
        RefreshHostBox();
    }

    private void RefreshHostBox()
    {
        if (_hostBox == null || !IsInstanceValid(_hostBox))
            return;
        LocalServer server = LocalServer.Instance;
        TunnelService tunnel = TunnelService.Instance;
        bool running = server != null && server.Running;
        _hostStart.Visible = !running;
        _hostCopy.Visible = running;
        _hostEnter.Visible = running;

        if (!running)
        {
            _hostInfo.Text = TunnelService.IsInstalled
                ? "Pronto para hospedar. O Windows pode pedir para liberar o jogo no firewall: aceite."
                : "Na primeira vez o jogo baixa o cloudflared (programa oficial e gratuito da Cloudflare) para criar o link público.";
            return;
        }

        string lan = string.Join("  ou  ", LocalServer.LanAddresses().ConvertAll(ip => $"http://{ip}:{server.Port}"));
        string tunnelLine = tunnel?.State switch
        {
            TunnelService.TunnelState.Ready => $"Link público: {tunnel.PublicUrl}",
            TunnelService.TunnelState.Downloading => $"Baixando o cloudflared... {tunnel.DownloadProgress * 100f:0}%",
            TunnelService.TunnelState.Starting => "Gerando o link público (uns 10 segundos)...",
            TunnelService.TunnelState.Failed => "Link público indisponível: " + tunnel.ErrorMessage + "  (amigos na mesma rede ainda podem entrar)",
            _ => "Link público desligado.",
        };
        _hostInfo.Text = $"SALA {server.Code} no seu PC · {GameLevels.Get(_level).Name}\n{tunnelLine}" +
                         (lan.Length > 0 ? $"\nMesma rede (Wi-Fi/cabo): {lan}" : "") +
                         "\nO amigo clica em \"Colar convite\" e entra.";
    }

    private string HostInviteText()
    {
        LocalServer server = LocalServer.Instance;
        TunnelService tunnel = TunnelService.Instance;
        var lan = LocalServer.LanAddresses();
        string address = tunnel?.State == TunnelService.TunnelState.Ready
            ? tunnel.PublicUrl
            : $"http://{(lan.Count > 0 ? lan[0] : "127.0.0.1")}:{server?.Port ?? LocalServer.DefaultPort}";
        return $"Bora entregar comigo no Dead Frequency! Código da sala: {server?.Code}\nServidor: {address}";
    }

    private void CopyHostInvite()
    {
        DisplayServer.ClipboardSet(HostInviteText());
        _status.Text = "Convite copiado! Mande no Discord ou WhatsApp.";
    }

    private void EnterHostedRoom()
    {
        LocalServer server = LocalServer.Instance;
        if (server == null || !server.Running || NetClient.Instance == null)
            return;
        SetBusy(true);
        NetClient.Instance.JoinRoom($"http://127.0.0.1:{server.Port}", server.Code, PlayerName());
        // O convite do menu de pausa usa o link público, não o 127.0.0.1.
        TunnelService tunnel = TunnelService.Instance;
        NetClient.Instance.PublicServerUrl = tunnel?.State == TunnelService.TunnelState.Ready ? tunnel.PublicUrl : "";
    }

    private void OnBack()
    {
        // Voltar sem entrar no jogo desliga a sala hospedada.
        LocalServer.Instance?.Stop();
        TunnelService.Instance?.Stop();
        RefreshHostBox();
        EmitSignal(SignalName.BackRequested);
    }

    // ---------------- Campos ----------------

    private LineEdit AddField(string label, string value, string placeholder)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        Label name = UiStyle.MakeLabel(label, 18, HorizontalAlignment.Left);
        name.CustomMinimumSize = new Vector2(170, 0);
        var edit = new LineEdit
        {
            Text = value,
            PlaceholderText = placeholder,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 40),
        };
        row.AddChild(name);
        row.AddChild(edit);
        AddChild(row);
        return edit;
    }

    private string PlayerName()
    {
        string n = _name.Text.Trim();
        return n.Length == 0 ? "Entregador" : n;
    }

    private bool SaveFields()
    {
        if (_mode == 1 && string.IsNullOrEmpty(_server.Text.Trim()))
        {
            _status.Text = "Informe o endereço do seu servidor (ou cole o convite do amigo).";
            return false;
        }
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ServerMode = _mode;
            GameSettings.Instance.ServerUrl = _server.Text.Trim();
            GameSettings.Instance.PlayerName = PlayerName();
            GameSettings.Instance.Save();
        }
        return true;
    }

    private void OnCreatePressed()
    {
        if (NetClient.Instance == null || !SaveFields())
            return;
        SetBusy(true);
        NetClient.Instance.CreateRoom(CurrentServer(), PlayerName(), _level);
    }

    private void OnJoinPressed()
    {
        if (NetClient.Instance == null || !SaveFields())
            return;
        string code = ExtractCode(_code.Text);
        if (code.Length != 5)
        {
            _status.Text = "O código da sala tem 5 letras/números.";
            return;
        }
        SetBusy(true);
        NetClient.Instance.JoinRoom(CurrentServer(), code, PlayerName());
    }

    /// <summary>Cola o convite: pega o código e, se tiver endereço de servidor, troca para "Meu servidor".</summary>
    private void PasteInvite()
    {
        string text = DisplayServer.ClipboardGet() ?? "";
        _code.Text = ExtractCode(text);
        Match url = UrlInText.Match(text);
        if (url.Success && NetClient.NormalizeServer(url.Value) != NetClient.NormalizeServer(NetClient.OfficialServer))
        {
            _server.Text = url.Value.TrimEnd('.', ',', ')');
            SetMode(1);
            _status.Text = "Convite colado: servidor do amigo + código. É só clicar em Entrar.";
        }
        else if (_code.Text.Length == 5)
        {
            if (_mode == 2)
                SetMode(0);
            _status.Text = "Código colado.";
        }
    }

    private void OnStatus(string message) => _status.Text = message;

    private void OnJoined()
    {
        SetBusy(false);
        EmitSignal(SignalName.JoinedRoom);
    }

    private void OnFailed(string message)
    {
        SetBusy(false);
        _status.Text = message;
    }

    private void SetBusy(bool busy)
    {
        _createButton.Disabled = busy;
        _joinButton.Disabled = busy;
        _hostEnter.Disabled = busy;
    }

    /// <summary>Aceita o código puro ou o texto inteiro do convite colado.</summary>
    private static string ExtractCode(string text)
    {
        text = (text ?? "").ToUpperInvariant();
        int index = text.IndexOf("SALA:", StringComparison.Ordinal);
        if (index >= 0)
            text = text[(index + 5)..];
        string clean = "";
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) && c < 128)
                clean += c;
            else if (clean.Length > 0 && clean.Length < 5)
                clean = ""; // palavra curta demais, continua procurando
            if (clean.Length == 5)
                break;
        }
        return clean.Length == 5 ? clean : (text.Trim().Length <= 5 ? text.Trim() : clean);
    }
}
