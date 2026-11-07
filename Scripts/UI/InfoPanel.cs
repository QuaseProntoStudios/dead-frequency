// Scripts/UI/InfoPanel.cs
// Painel de texto rolável do menu principal (Novidades e Créditos).
using Godot;

namespace DeadFrequency;

public partial class InfoPanel : VBoxContainer
{
    [Signal] public delegate void BackRequestedEventHandler();

    private Label _title;
    private Label _subtitle;
    private RichTextLabel _text;
    private Button _back;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        CustomMinimumSize = new Vector2(760, 0);

        _title = UiStyle.MakeLabel("", 30);
        _subtitle = UiStyle.MakeLabel("", 16, color: UiStyle.PhosphorDim);
        AddChild(_title);
        AddChild(_subtitle);

        _text = new RichTextLabel
        {
            BbcodeEnabled = true,
            CustomMinimumSize = new Vector2(740, 330),
            ScrollActive = true,
            FitContent = false,
            SelectionEnabled = false,
        };
        _text.AddThemeColorOverride("default_color", UiStyle.Phosphor);
        _text.AddThemeFontSizeOverride("normal_font_size", 17);
        _text.AddThemeFontSizeOverride("bold_font_size", 19);
        _text.AddThemeStyleboxOverride("normal", UiStyle.Box(new Color(0f, 0f, 0f, 0.5f), UiStyle.PhosphorDim, 1, 12));
        AddChild(_text);

        _back = UiStyle.MakeButton("Voltar", () => EmitSignal(SignalName.BackRequested), sound: UiSound.Back);
        _back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(_back);
    }

    public void Display(string title, string subtitle, string bbcode)
    {
        _title.Text = title;
        _subtitle.Text = subtitle;
        _text.Text = bbcode;
        _text.ScrollToLine(0);
        _back.GrabFocus();
    }
}
