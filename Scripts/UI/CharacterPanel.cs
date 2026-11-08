// Scripts/UI/CharacterPanel.cs
// Menu "Personagem" (v0.9.6): escolher homem ou mulher e as cores da pele, do cabelo, da camisa e da calça,
// com o boneco girando na prévia. Salvo nas opções e mostrado para todos no online.
using Action = System.Action;
using Godot;

namespace DeadFrequency;

public partial class CharacterPanel : VBoxContainer
{
    public event Action BackRequested;

    private SubViewport _viewport;
    private CharacterModel _preview;
    private Label _summary;
    private Button _first;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        AddChild(UiStyle.MakeLabel("PERSONAGEM", 30));

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        AddChild(columns);

        var viewportContainer = new SubViewportContainer { CustomMinimumSize = new Vector2(260, 330), Stretch = true };
        columns.AddChild(viewportContainer);
        _viewport = new SubViewport { OwnWorld3D = true, TransparentBg = true, Size = new Vector2I(260, 330), RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible };
        viewportContainer.AddChild(_viewport);
        _viewport.AddChild(new Camera3D { Position = new Vector3(0f, 1.05f, -2.7f), RotationDegrees = new Vector3(-3f, 180f, 0f), Fov = 42f, Current = true });
        _viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-35f, 150f, 0f), LightEnergy = 1.4f });
        _viewport.AddChild(new WorldEnvironment
        {
            Environment = new Environment { BackgroundMode = Environment.BGMode.ClearColor, AmbientLightSource = Environment.AmbientSource.Color, AmbientLightColor = new Color(0.55f, 0.6f, 0.58f), AmbientLightEnergy = 0.9f },
        });

        var right = new VBoxContainer();
        right.AddThemeConstantOverride("separation", 8);
        columns.AddChild(right);

        GameSettings s = GameSettings.Instance;
        var gender = new HBoxContainer();
        gender.AddThemeConstantOverride("separation", 10);
        _first = UiStyle.MakeButton("Homem", () => { s.CharWoman = false; Changed(); }, 150);
        gender.AddChild(_first);
        gender.AddChild(UiStyle.MakeButton("Mulher", () => { s.CharWoman = true; Changed(); }, 150));
        right.AddChild(gender);

        Swatches(right, "Pele", CharacterLook.Skins, i => s.CharSkin = i);
        Swatches(right, "Cabelo", CharacterLook.HairColors, i => s.CharHair = i);
        Swatches(right, "Camisa", CharacterLook.Shirts, i => s.CharShirt = i);
        Swatches(right, "Calça", CharacterLook.PantsColors, i => s.CharPants = i);
        _summary = UiStyle.MakeLabel("", 14, HorizontalAlignment.Left, UiStyle.PhosphorDim);
        right.AddChild(_summary);
        right.AddChild(UiStyle.MakeLabel("No online, os outros jogadores veem você assim\n(o nome continua na cor do jogador).", 13, HorizontalAlignment.Left, UiStyle.PhosphorDim));

        Button back = UiStyle.MakeButton("Voltar", () => BackRequested?.Invoke(), sound: UiSound.Back);
        back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(back);
        Changed(save: false);
    }

    public void FocusFirst() => _first?.GrabFocus();

    private void Swatches(VBoxContainer parent, string title, Color[] colors, System.Action<int> set)
    {
        parent.AddChild(UiStyle.MakeLabel(title, 16, HorizontalAlignment.Left));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        parent.AddChild(row);
        for (int i = 0; i < colors.Length; i++)
        {
            int idx = i;
            var b = new Button { CustomMinimumSize = new Vector2(34, 30), FocusMode = FocusModeEnum.All, TooltipText = title };
            var style = new StyleBoxFlat { BgColor = colors[i], BorderColor = UiStyle.PhosphorDim };
            style.SetBorderWidthAll(2);
            var hover = (StyleBoxFlat)style.Duplicate();
            hover.BorderColor = UiStyle.Phosphor;
            hover.SetBorderWidthAll(3);
            b.AddThemeStyleboxOverride("normal", style);
            b.AddThemeStyleboxOverride("hover", hover);
            b.AddThemeStyleboxOverride("focus", hover);
            b.AddThemeStyleboxOverride("pressed", hover);
            b.Pressed += () => { set(idx); Changed(); };
            row.AddChild(b);
        }
    }

    private void Changed(bool save = true)
    {
        GameSettings s = GameSettings.Instance;
        if (s == null)
            return;
        if (save)
            s.Save();
        _preview?.QueueFree();
        CharacterLook look = CharacterLook.Local;
        _preview = look.Build();
        _preview.Rotation = new Vector3(0f, _spin, 0f);
        _viewport.AddChild(_preview);
        _summary.Text = $"{(look.Woman ? "Mulher" : "Homem")} · cabelo {CharacterLook.HairNames[look.Hair].ToLowerInvariant()}";
    }

    private float _spin = 0.4f;

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree() || _preview == null)
            return;
        _spin += (float)delta * 0.6f;
        _preview.Rotation = new Vector3(0f, _spin, 0f);
        _preview.Animate((float)delta, 0f);
    }
}
