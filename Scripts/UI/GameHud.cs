// Scripts/UI/GameHud.cs
// HUD mínimo de protótipo: mira, texto de interação, caixa na mão, velocímetro e avisos.
// (No jogo final boa parte disso migra para o painel físico da van — UI diegética.)
// F3 liga/desliga o overlay de debug.
using Godot;

namespace DeadFrequency;

public partial class GameHud : CanvasLayer
{
    [Export] public PlayerController Player { get; set; }
    [Export] public DeliveryManager Delivery { get; set; }

    private ColorRect _crosshair;
    private Label _prompt;
    private PanelContainer _cargoPanel;
    private Label _cargoName;
    private ProgressBar _cargoBar;
    private Label _cargoDetails;
    private Label _speed;
    private Label _toast;
    private PanelContainer _toastPanel;
    private Label _clock;
    private Label _debug;
    private PanelContainer _jobPanel;
    private Label _jobTitle;
    private Label _jobBody;
    private Label _money;
    private MinimapView _minimap;
    private CompassArrow _compass;
    private Label _status;

    [Export] public CityGenerator City { get; set; }
    private double _toastTimeLeft;

    public override void _Ready()
    {
        Layer = 1;

        var root = new Control { Theme = UiStyle.Theme, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // Mira
        _crosshair = new ColorRect { Color = new Color(1f, 1f, 1f, 0.7f), MouseFilter = Control.MouseFilterEnum.Ignore };
        Place(_crosshair, 0.5f, 0.5f, 0.5f, 0.5f, -2, -2, 2, 2);
        root.AddChild(_crosshair);

        // Texto de interação
        _prompt = UiStyle.MakeLabel("", 20);
        Place(_prompt, 0.5f, 0.5f, 0.5f, 0.5f, -400, 22, 400, 52);
        root.AddChild(_prompt);

        // Caixa na mão (canto inferior esquerdo)
        _cargoPanel = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _cargoPanel.AddThemeStyleboxOverride("panel", UiStyle.Box(UiStyle.PanelBackground, UiStyle.PhosphorDim, 2, 12));
        Place(_cargoPanel, 0f, 1f, 0f, 1f, 20, -130, 360, -20);
        root.AddChild(_cargoPanel);

        var cargoStack = new VBoxContainer();
        _cargoPanel.AddChild(cargoStack);
        _cargoName = UiStyle.MakeLabel("", 20, HorizontalAlignment.Left);
        _cargoBar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 100, CustomMinimumSize = new Vector2(0, 22), MouseFilter = Control.MouseFilterEnum.Ignore };
        _cargoDetails = UiStyle.MakeLabel("", 15, HorizontalAlignment.Left, UiStyle.PhosphorDim);
        cargoStack.AddChild(_cargoName);
        cargoStack.AddChild(_cargoBar);
        cargoStack.AddChild(_cargoDetails);

        // Velocímetro (canto inferior direito)
        _speed = UiStyle.MakeLabel("", 40, HorizontalAlignment.Right);
        Place(_speed, 1f, 1f, 1f, 1f, -320, -80, -24, -24);
        root.AddChild(_speed);

        // Avisos: abaixo da bússola, em caixa com fundo e quebra de linha,
        // para não ficarem por trás do painel de pedido (canto superior direito).
        _toastPanel = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _toastPanel.AddThemeStyleboxOverride("panel", UiStyle.Box(new Color(0f, 0f, 0f, 0.72f), UiStyle.PhosphorDim, 2, 10));
        Place(_toastPanel, 0.5f, 0f, 0.5f, 0f, -320, 168, 320, 168);
        _toastPanel.GrowVertical = Control.GrowDirection.End;
        _toast = UiStyle.MakeLabel("", 19);
        _toast.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _toast.CustomMinimumSize = new Vector2(600, 0);
        _toastPanel.AddChild(_toast);
        root.AddChild(_toastPanel);

        // Relógio do mundo (topo, acima da bússola).
        _clock = UiStyle.MakeLabel("", 22);
        Place(_clock, 0.5f, 0f, 0.5f, 0f, -160, 18, 160, 52);
        root.AddChild(_clock);

        // Pedido atual (canto superior direito)
        _jobPanel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _jobPanel.AddThemeStyleboxOverride("panel", UiStyle.Box(UiStyle.PanelBackground, UiStyle.PhosphorDim, 2, 12));
        Place(_jobPanel, 1f, 0f, 1f, 0f, -520, 20, -20, 150);
        root.AddChild(_jobPanel);
        var jobStack = new VBoxContainer();
        _jobPanel.AddChild(jobStack);
        _jobTitle = UiStyle.MakeLabel("", 16, HorizontalAlignment.Left, UiStyle.PhosphorDim);
        _jobBody = UiStyle.MakeLabel("", 18, HorizontalAlignment.Left);
        _jobBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _money = UiStyle.MakeLabel("", 22, HorizontalAlignment.Right);
        jobStack.AddChild(_jobTitle);
        jobStack.AddChild(_jobBody);
        jobStack.AddChild(_money);

        // Debug (F3)
        _debug = UiStyle.MakeLabel("", 14, HorizontalAlignment.Left);
        _debug.Visible = false;
        Place(_debug, 0f, 0f, 0f, 0f, 12, 12, 700, 120);
        root.AddChild(_debug);

        // Minimapa (só com o radar equipado), canto superior esquerdo.
        _minimap = new MinimapView { Player = Player, City = City, Delivery = Delivery, Visible = false };
        Place(_minimap, 0f, 0f, 0f, 0f, 20, 20, 240, 240);
        root.AddChild(_minimap);

        // Seta para o depósito.
        _compass = new CompassArrow { Player = Player, Target = Vector3.Zero };
        Place(_compass, 0.5f, 0f, 0.5f, 0f, -200, 60, 200, 130);
        root.AddChild(_compass);

        // Efeitos ativos (energético, visão noturna) no rodapé.
        _status = UiStyle.MakeLabel("", 15, HorizontalAlignment.Center, UiStyle.PhosphorDim);
        Place(_status, 0.5f, 1f, 0.5f, 1f, -300, -40, 300, -14);
        root.AddChild(_status);

        root.AddChild(UiStyle.MakeScanlines(0.06f));

        if (Player == null)
            GD.PushWarning("GameHud: campo Player não atribuído no Inspector.");

        if (Delivery != null)
        {
            Delivery.DeliveryCompleted += (name, payment) => ShowToast($"{name} entregue! +{payment}", UiStyle.Phosphor, 3.5);
            Delivery.CargoLost += name => ShowToast($"{name} perdida — sem pagamento", UiStyle.Danger, 3.5);
            Delivery.ShiftStarted += shift => ShowToast($"TURNO {shift} — pegue as caixas no depósito", UiStyle.Phosphor, 4.0);
            Delivery.ShiftCompleted += (shift, earnings) => ShowToast($"Turno {shift} concluído · ganhos {earnings}", UiStyle.Warning, 4.0);
        }

        if (NetClient.Instance != null)
        {
            NetClient.Instance.MessageReceived += OnNetMessage;
            NetClient.Instance.Disconnected += OnNetDisconnected;
        }

        foreach (Node node in GetTree().GetNodesInGroup(CargoItem.GroupName))
            WatchCargo(node);
        GetTree().NodeAdded += WatchCargo;
    }

    public override void _ExitTree()
    {
        // O SceneTree sobrevive à troca de cena: precisa desconectar.
        GetTree().NodeAdded -= WatchCargo;
        if (NetClient.Instance != null)
        {
            NetClient.Instance.MessageReceived -= OnNetMessage;
            NetClient.Instance.Disconnected -= OnNetDisconnected;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F3 })
            _debug.Visible = !_debug.Visible;
    }

    public override void _Process(double delta)
    {
        if (_toastTimeLeft > 0)
        {
            _toastTimeLeft -= delta;
            _toastPanel.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp((float)_toastTimeLeft, 0f, 1f));
            if (_toastTimeLeft <= 0)
                _toastPanel.Visible = false;
        }

        _clock.Text = DayNightController.ClockText();

        if (Player == null || !IsInstanceValid(Player))
            return;

        _prompt.Text = Player.CurrentPrompt;

        bool hasTarget = Player.HasTarget;
        _crosshair.Color = hasTarget ? UiStyle.Phosphor : new Color(1f, 1f, 1f, 0.7f);
        float half = hasTarget ? 4f : 2f;
        Place(_crosshair, 0.5f, 0.5f, 0.5f, 0.5f, -half, -half, half, half);
        _crosshair.Visible = Player.State == PlayerState.Walking || hasTarget;

        CargoItem held = Player.HeldItem;
        _cargoPanel.Visible = held != null;
        if (held != null)
        {
            _cargoName.Text = held.Data?.CargoName ?? held.Name;
            _cargoBar.Value = held.Integrity * 100f;
            _cargoDetails.Text = $"{held.WeightKg:0.#} kg · pagamento {held.GetCurrentPayment()}";
        }

        VanController van = Player.CurrentVehicle;
        _speed.Visible = van != null;
        if (van != null)
            _speed.Text = $"{van.SpeedKmh:000} km/h";

        UpdateJobPanel();
        UpdateItems();
        UpdateCompass();

        if (_debug.Visible)
        {
            _debug.Text = $"FPS {Engine.GetFramesPerSecond()}\n" +
                          $"Estado: {Player.State}\n" +
                          $"Mirando: {Player.DebugLookingAt}\n" +
                          $"Prompt: {(string.IsNullOrEmpty(Player.CurrentPrompt) ? "-" : Player.CurrentPrompt)}";
        }
    }

    /// <summary>A seta aponta para o lugar de trabalho da profissão (ou para o depósito).</summary>
    private void UpdateCompass()
    {
        ProfessionManager pm = ProfessionManager.Instance;
        if (pm == null || !pm.TryGetTarget(out Vector3 target, out string label))
            return;
        _compass.Target = target;
        _compass.Label = label;
        bool depot = label == "DEPÓSITO";
        _compass.HideDistance = depot ? 30f : 4f;
        _compass.Color = depot ? new Color(1f, 0.6f, 0.15f) : Professions.Get(pm.Current).Color;
    }

    private void UpdateItems()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        bool radar = inv?.IsEquipped(ItemCatalog.Radar) == true;
        _minimap.Visible = radar;
        // Com o minimapa aberto, o debug desce um pouco para não ficar por cima.
        _debug.OffsetTop = radar ? 250 : 12;
        _debug.OffsetBottom = _debug.OffsetTop + 108;

        var parts = new System.Collections.Generic.List<string>();
        if (Player.SpeedBoostLeft > 0f)
            parts.Add($"ENERGÉTICO {Player.SpeedBoostLeft:0}s");
        if (inv?.IsEquipped(ItemCatalog.NightVision) == true)
            parts.Add(Player.NightVisionOn ? "VISÃO NOTURNA [N]" : "[N] visão noturna");
        if (inv?.IsEquipped(ItemCatalog.Gloves) == true)
            parts.Add($"LUVAS · até {Player.MaxCarryWeightKg:0} kg");
        parts.Add("[I] inventário");
        _status.Text = string.Join("   ", parts);
    }

    private void UpdateJobPanel()
    {
        _jobPanel.Visible = Delivery != null;
        if (Delivery == null)
            return;

        NetClient net = NetClient.Instance;
        string room = net != null && net.IsOnline ? $"SALA {net.RoomCode} · {net.PlayerCount} online · " : "";
        ProfessionManager pm = ProfessionManager.Instance;
        if (pm != null && pm.Current != Professions.Entregador)
        {
            _jobTitle.Text = room + pm.PanelTitle();
            _jobTitle.AddThemeColorOverride("font_color", Professions.Get(pm.Current).Color);
            _jobBody.Text = pm.PanelText();
            _money.Text = $"$ {Delivery.Money}";
            return;
        }
        _jobTitle.RemoveThemeColorOverride("font_color");
        _jobTitle.AddThemeColorOverride("font_color", UiStyle.PhosphorDim);
        _jobTitle.Text = $"{room}TURNO {Delivery.Shift} · {Delivery.ShiftDelivered}/{Delivery.ShiftTotal}";
        _money.Text = $"$ {Delivery.Money}";

        if (Delivery.Jobs.Count == 0)
        {
            _jobBody.Text = "Aguardando novo turno...";
            return;
        }

        var lines = new System.Text.StringBuilder();
        CargoItem held = Player.HeldItem;
        int shown = 0;
        foreach (DeliveryJob job in Delivery.Jobs)
        {
            if (shown++ >= 5)
            {
                lines.Append($"+{Delivery.Jobs.Count - 5} pedidos...");
                break;
            }
            string marker = job.Cargo == held && held != null ? "► " : "· ";
            lines.Append($"{marker}{job.CargoName} → {job.Point.Address} ({Delivery.DistanceTo(job):0} m)\n");
        }
        _jobBody.Text = lines.ToString().TrimEnd();
    }

    private void OnNetMessage(System.Text.Json.JsonElement m)
    {
        string type = m.GetProperty("t").GetString();
        switch (type)
        {
            case "join":
                ShowToast($"{m.GetProperty("player").GetProperty("name").GetString()} entrou na sala", UiStyle.Phosphor);
                break;
            case "leave":
                // O NetClient já removeu da lista; mostramos só o aviso genérico.
                ShowToast("Um entregador saiu da sala", UiStyle.PhosphorDim);
                break;
            case "chat":
                ShowToast($"{m.GetProperty("name").GetString()}: {m.GetProperty("msg").GetString()}", Colors.White, 5.0);
                break;
            case "claimFail":
                ShowToast("Outro entregador está usando isso", UiStyle.Warning);
                break;
        }
    }

    private void OnNetDisconnected(string message) => ShowToast(message + " Voltando ao menu...", UiStyle.Danger, 5.0);

    public const string GroupName = "game_hud";

    public override void _EnterTree() => AddToGroup(GroupName);

    /// <summary>Atalho para qualquer script mostrar um aviso no topo da tela.</summary>
    public static void Toast(Node from, string text, Color? color = null, double seconds = 3.0)
    {
        if (from?.GetTree()?.GetFirstNodeInGroup(GroupName) is GameHud hud)
            hud.ShowToast(text, color, seconds);
    }

    /// <summary>Tela escurece e volta (dormir, elevador).</summary>
    public static void FadeBlack(Node from, double seconds = 1.0)
    {
        if (from?.GetTree()?.GetFirstNodeInGroup(GroupName) is not GameHud hud)
            return;
        var rect = new ColorRect { Color = new Color(0f, 0f, 0f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        hud.AddChild(rect);
        Tween tw = rect.CreateTween();
        tw.TweenProperty(rect, "color:a", 1.0, seconds * 0.4);
        tw.TweenInterval(seconds * 0.2);
        tw.TweenProperty(rect, "color:a", 0.0, seconds * 0.4);
        tw.TweenCallback(Callable.From(rect.QueueFree));
    }

    public void ShowToast(string text, Color? color = null, double seconds = 3.0)
    {
        _toast.Text = text;
        _toast.AddThemeColorOverride("font_color", color ?? UiStyle.Phosphor);
        _toastTimeLeft = seconds;
        _toastPanel.Visible = true;
        _toastPanel.Modulate = Colors.White;
        _toastPanel.ResetSize();
    }

    private void WatchCargo(Node node)
    {
        if (node is not CargoItem cargo)
            return;

        cargo.IntegrityChanged += integrity =>
        {
            string name = cargo.Data?.CargoName ?? cargo.Name;
            Color color = integrity > 0.6f ? UiStyle.Phosphor : integrity > 0.3f ? UiStyle.Warning : UiStyle.Danger;
            ShowToast($"{name}: {integrity:P0}", color);
        };
        cargo.CargoDestroyed += () => ShowToast($"{cargo.Data?.CargoName ?? cargo.Name} DESTRUÍDO", UiStyle.Danger, 4.0);
    }

    private static void Place(Control control, float anchorLeft, float anchorTop, float anchorRight, float anchorBottom,
        float left, float top, float right, float bottom)
    {
        control.AnchorLeft = anchorLeft;
        control.AnchorTop = anchorTop;
        control.AnchorRight = anchorRight;
        control.AnchorBottom = anchorBottom;
        control.OffsetLeft = left;
        control.OffsetTop = top;
        control.OffsetRight = right;
        control.OffsetBottom = bottom;
    }
}
