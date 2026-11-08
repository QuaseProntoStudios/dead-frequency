// Scripts/Core/UiStyle.cs
// Tema visual único da interface: fósforo verde de monitor CRT sobre preto.
// Os menus são montados em código usando estes helpers, então mudar o visual é mexer só aqui.
using System;
using Godot;

namespace DeadFrequency;

public static class UiStyle
{
    public static readonly Color Phosphor = new(0.55f, 1f, 0.62f);
    public static readonly Color PhosphorDim = new(0.28f, 0.55f, 0.33f);
    public static readonly Color Warning = new(1f, 0.72f, 0.25f);
    public static readonly Color Danger = new(1f, 0.32f, 0.28f);
    public static readonly Color PanelBackground = new(0.02f, 0.04f, 0.03f, 0.94f);

    private static Theme _theme;
    public static Theme Theme => _theme ??= BuildTheme();

    private static Theme BuildTheme()
    {
        var theme = new Theme
        {
            DefaultFontSize = 20,
            DefaultFont = new SystemFont { FontNames = new[] { "Consolas", "Lucida Console", "Courier New", "monospace" } },
        };

        // Label
        theme.SetColor("font_color", "Label", Phosphor);
        theme.SetColor("font_outline_color", "Label", Colors.Black);
        theme.SetConstant("outline_size", "Label", 4);

        // Button
        Color dark = new(0.01f, 0.03f, 0.02f);
        theme.SetColor("font_color", "Button", Phosphor);
        theme.SetColor("font_hover_color", "Button", dark);
        theme.SetColor("font_focus_color", "Button", Phosphor);
        theme.SetColor("font_pressed_color", "Button", dark);
        theme.SetColor("font_hover_pressed_color", "Button", dark);
        theme.SetStylebox("normal", "Button", Box(new Color(0f, 0f, 0f, 0.55f), PhosphorDim, 2, 10));
        theme.SetStylebox("hover", "Button", Box(Phosphor, Phosphor, 2, 10));
        theme.SetStylebox("pressed", "Button", Box(PhosphorDim, Phosphor, 2, 10));
        theme.SetStylebox("hover_pressed", "Button", Box(Phosphor, Phosphor, 2, 10));
        theme.SetStylebox("focus", "Button", Box(new Color(0f, 0f, 0f, 0f), Phosphor, 2, 10));

        // CheckButton (usa as cores de Button + ícones padrão)
        theme.SetColor("font_color", "CheckButton", Phosphor);
        theme.SetColor("font_hover_color", "CheckButton", Colors.White);
        theme.SetColor("font_focus_color", "CheckButton", Phosphor);
        theme.SetColor("font_pressed_color", "CheckButton", Phosphor);
        theme.SetColor("font_hover_pressed_color", "CheckButton", Colors.White);
        theme.SetStylebox("normal", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("hover", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("pressed", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("hover_pressed", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("focus", "CheckButton", Box(new Color(0f, 0f, 0f, 0f), PhosphorDim, 1, 2));

        // Slider
        theme.SetStylebox("slider", "HSlider", Box(new Color(0f, 0f, 0f, 0.6f), PhosphorDim, 1, 3));
        theme.SetStylebox("grabber_area", "HSlider", Box(PhosphorDim, PhosphorDim, 0, 3));
        theme.SetStylebox("grabber_area_highlight", "HSlider", Box(Phosphor, Phosphor, 0, 3));

        // Campos de texto
        theme.SetColor("font_color", "LineEdit", Phosphor);
        theme.SetColor("font_placeholder_color", "LineEdit", PhosphorDim);
        theme.SetColor("caret_color", "LineEdit", Phosphor);
        theme.SetColor("selection_color", "LineEdit", PhosphorDim);
        theme.SetStylebox("normal", "LineEdit", Box(new Color(0f, 0f, 0f, 0.7f), PhosphorDim, 2, 8));
        theme.SetStylebox("focus", "LineEdit", Box(new Color(0f, 0f, 0f, 0f), Phosphor, 2, 8));
        theme.SetStylebox("read_only", "LineEdit", Box(new Color(0f, 0f, 0f, 0.4f), PhosphorDim, 1, 8));

        // Painéis e barras
        theme.SetStylebox("panel", "PanelContainer", Box(PanelBackground, Phosphor, 2, 24));
        theme.SetStylebox("background", "ProgressBar", Box(new Color(0f, 0f, 0f, 0.6f), PhosphorDim, 1, 2));
        theme.SetStylebox("fill", "ProgressBar", Box(Phosphor, Phosphor, 0, 2));
        theme.SetColor("font_color", "ProgressBar", Colors.White);
        theme.SetColor("font_outline_color", "ProgressBar", Colors.Black);
        theme.SetConstant("outline_size", "ProgressBar", 4);

        return theme;
    }

    public static StyleBoxFlat Box(Color background, Color border, int borderWidth, int margin)
    {
        var box = new StyleBoxFlat { BgColor = background, BorderColor = border };
        box.SetBorderWidthAll(borderWidth);
        box.SetContentMarginAll(margin);
        return box;
    }

    public static Label MakeLabel(string text, int fontSize = 20, HorizontalAlignment align = HorizontalAlignment.Center, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = align,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        if (color.HasValue)
            label.AddThemeColorOverride("font_color", color.Value);
        return label;
    }

    public static Button MakeButton(string text, Action onPressed, int minWidth = 320, UiSound sound = UiSound.Click)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(minWidth, 48),
            FocusMode = Control.FocusModeEnum.All,
        };
        button.MouseEntered += () => AudioManager.Instance?.PlayUi(UiSound.Hover);
        button.Pressed += () => AudioManager.Instance?.PlayUi(sound);
        button.Pressed += onPressed;
        return button;
    }

    public const string ScanlineGroup = "ui_scanlines";

    /// <summary>Linhas de varredura + vinheta por cima de tudo (efeito CRT barato).</summary>
    public static ColorRect MakeScanlines(float strength = 0.22f)
    {
        var shader = new Shader
        {
            Code = @"
shader_type canvas_item;
uniform float strength = 0.22;
void fragment() {
    float line = step(0.5, fract(FRAGCOORD.y * 0.5));
    vec2 uv = SCREEN_UV - 0.5;
    float vignette = smoothstep(0.35, 0.85, length(uv));
    COLOR = vec4(0.0, 0.0, 0.0, line * strength + vignette * 0.55);
}",
        };
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("strength", strength);

        var rect = new ColorRect { Material = material, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = GameSettings.Instance?.Scanlines ?? true };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        rect.AddToGroup(ScanlineGroup); // Opções > Interface liga/desliga
        return rect;
    }
}
