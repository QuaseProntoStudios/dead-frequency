// Scripts/UI/ModalScreen.cs
// Base das telas que aparecem por cima do jogo (inventário, loja, game over).
// Solo: pausa o jogo. Online: o mundo continua, só os controles do jogador são travados.
using Godot;

namespace DeadFrequency;

public partial class ModalScreen : CanvasLayer
{
    [Export] public PlayerController Player { get; set; }

    public static ModalScreen Current { get; private set; }
    public static bool AnyOpen => Current != null && IsInstanceValid(Current) && Current.IsOpen;

    protected Control Root { get; private set; }
    public bool IsOpen => Root != null && Root.Visible;
    /// <summary>Esc fecha a tela (o game over não deixa).</summary>
    protected virtual bool ClosableWithEscape => true;
    protected static bool Online => NetClient.Instance?.IsOnline == true;

    public override void _Ready()
    {
        Layer = 11;
        ProcessMode = ProcessModeEnum.Always;
        Root = new Control { Theme = UiStyle.Theme, MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        Root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(Root);

        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.7f), MouseFilter = Control.MouseFilterEnum.Ignore };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        Root.AddChild(dim);
        BuildUi();
        Root.AddChild(UiStyle.MakeScanlines(0.1f));
    }

    public override void _ExitTree()
    {
        if (Current == this)
        {
            Current = null;
            if (GetTree() != null)
                GetTree().Paused = false;
        }
    }

    /// <summary>Monte a interface dentro de Root.</summary>
    protected virtual void BuildUi() { }
    protected virtual void OnOpened() { }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen || !@event.IsActionPressed("pause"))
            return;
        GetViewport().SetInputAsHandled();
        if (ClosableWithEscape)
            Close();
    }

    public void Open()
    {
        if (AnyOpen && Current != this)
            Current.Close();
        Current = this;
        Root.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (Online)
        {
            if (Player != null)
                Player.InputEnabled = false;
        }
        else
        {
            GetTree().Paused = true;
        }
        OnOpened();
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        Root.Visible = false;
        if (Current == this)
            Current = null;
        GetTree().Paused = false;
        if (Player != null)
            Player.InputEnabled = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        AudioManager.Instance?.PlayUi(UiSound.Back);
    }

    /// <summary>Painel centralizado padrão.</summary>
    protected PanelContainer CenteredPanel(Vector2 minSize)
    {
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        Root.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = minSize };
        center.AddChild(panel);
        return panel;
    }
}
