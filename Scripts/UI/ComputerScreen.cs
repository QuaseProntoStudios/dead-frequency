// Scripts/UI/ComputerScreen.cs
// Tela cheia do PC do escritório: o "monitor CRT" ocupa a tela com o relatório de ganhos
// e o histórico de entregas. TAB ou E troca a página, ESC sai.
// Criada na hora pelo OfficeComputer (não precisa estar na cena).
using Godot;

namespace DeadFrequency;

public partial class ComputerScreen : ModalScreen
{
    private Label _text;
    private int _page;
    private double _blink;
    private double _refresh;

    public static void OpenFor(Node from, PlayerController player)
    {
        Node scene = from.GetTree().CurrentScene;
        var screen = scene.GetNodeOrNull<ComputerScreen>("ComputerScreen");
        if (screen == null)
        {
            screen = new ComputerScreen { Name = "ComputerScreen", Player = player };
            scene.AddChild(screen);
        }
        screen.Player = player;
        screen.Open();
    }

    protected override void BuildUi()
    {
        PanelContainer panel = CenteredPanel(new Vector2(900, 560));
        // Moldura bege de monitor anos 90 com a tela escura dentro.
        panel.AddThemeStyleboxOverride("panel", UiStyle.Box(new Color(0.01f, 0.05f, 0.02f, 0.98f), new Color(0.78f, 0.74f, 0.64f), 18, 28));
        _text = UiStyle.MakeLabel("", 20, HorizontalAlignment.Left);
        _text.VerticalAlignment = VerticalAlignment.Top;
        panel.AddChild(_text);
    }

    protected override void OnOpened()
    {
        _refresh = 0;
        Update();
    }

    public override void _Process(double delta)
    {
        if (!IsOpen)
            return;
        _blink += delta;
        _refresh -= delta;
        if (_refresh <= 0)
            Update();
    }

    public override void _Input(InputEvent @event)
    {
        if (IsOpen && @event is InputEventKey { Pressed: true, Echo: false } key
            && (key.Keycode == Key.Tab || @event.IsActionPressed("interact") || @event.IsActionPressed("inventory")))
        {
            _page = (_page + 1) % 2;
            AudioManager.Instance?.PlayUi(UiSound.Click);
            Update();
            GetViewport().SetInputAsHandled();
            return;
        }
        base._Input(@event);
    }

    private void Update()
    {
        _refresh = 0.5;
        _text.Text = OfficeComputer.Report(_page, (int)(_blink * 2) % 2 == 0);
    }
}
