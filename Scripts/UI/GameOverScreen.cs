// Scripts/UI/GameOverScreen.cs
// Aparece quando todas as caixas do turno foram destruídas.
// Solo: tentar de novo ou voltar ao menu. Online: o servidor recomeça a sala sozinho em alguns segundos.
using Godot;

namespace DeadFrequency;

public partial class GameOverScreen : ModalScreen
{
    [Export] public DeliveryManager Delivery { get; set; }
    [Export(PropertyHint.File, "*.tscn")] public string MainMenuScene { get; set; } = "res://Scenes/UI/main_menu.tscn";

    private Label _stats;
    private Label _countdown;
    private Button _retry;
    private Button _menu;
    private double _timeLeft;
    private AudioStreamPlayer _sound;

    protected override bool ClosableWithEscape => false;

    protected override void BuildUi()
    {
        Layer = 12;
        PanelContainer panel = CenteredPanel(new Vector2(640, 0));
        panel.AddThemeStyleboxOverride("panel", UiStyle.Box(UiStyle.PanelBackground, UiStyle.Danger, 3, 30));
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 14);
        panel.AddChild(stack);

        stack.AddChild(UiStyle.MakeLabel("GAME OVER", 64, color: UiStyle.Danger));
        stack.AddChild(UiStyle.MakeLabel("Todas as caixas do turno foram destruídas.", 18));
        _stats = UiStyle.MakeLabel("", 16, color: UiStyle.PhosphorDim);
        stack.AddChild(_stats);
        _countdown = UiStyle.MakeLabel("", 16, color: UiStyle.Warning);
        stack.AddChild(_countdown);

        _retry = UiStyle.MakeButton("Tentar de novo", Retry, 320, UiSound.Start);
        _retry.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        stack.AddChild(_retry);
        _menu = UiStyle.MakeButton("Menu principal", GoToMenu, 320, UiSound.Back);
        _menu.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        stack.AddChild(_menu);

        _sound = new AudioStreamPlayer { Bus = AudioManager.SfxBus };
        if (ResourceLoader.Exists("res://Audio/SFX/game_over.wav"))
            _sound.Stream = GD.Load<AudioStream>("res://Audio/SFX/game_over.wav");
        AddChild(_sound);

        if (Delivery != null)
        {
            Delivery.GameOver += OnGameOver;
            Delivery.ShiftStarted += OnShiftStarted;
        }
    }

    public override void _ExitTree()
    {
        if (Delivery != null)
        {
            Delivery.GameOver -= OnGameOver;
            Delivery.ShiftStarted -= OnShiftStarted;
        }
        base._ExitTree();
    }

    private void OnGameOver(int shift, int totalDelivered)
    {
        Player?.DropHeld();
        _stats.Text = $"Chegou até o turno {shift} · {totalDelivered} entregas no total\nO dinheiro foi zerado.";
        bool online = Online;
        _retry.Visible = !online;
        _menu.Text = online ? "Sair da sala" : "Menu principal";
        _timeLeft = online ? 8.0 : 0.0;
        _countdown.Text = online ? "A sala recomeça no turno 1 em 8 s..." : "";
        Open();
        _sound.Play();
        AudioManager.Instance?.SetMuffled(true);
    }

    private void OnShiftStarted(int shift)
    {
        if (!IsOpen)
            return;
        AudioManager.Instance?.SetMuffled(false);
        Close();
    }

    public override void _Process(double delta)
    {
        if (!IsOpen || _timeLeft <= 0)
            return;
        _timeLeft -= delta;
        _countdown.Text = $"A sala recomeça no turno 1 em {Mathf.CeilToInt(Mathf.Max(0f, (float)_timeLeft))} s...";
    }

    private void Retry()
    {
        AudioManager.Instance?.SetMuffled(false);
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void GoToMenu()
    {
        NetClient.Instance?.Leave();
        AudioManager.Instance?.SetMuffled(false);
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(MainMenuScene);
    }
}
