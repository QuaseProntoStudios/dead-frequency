// Scripts/UI/OptionsPanel.cs
// Painel de opções reutilizado pelo menu principal e pelo menu de pausa.
// Cada alteração é aplicada na hora; "Voltar" salva em disco e emite BackRequested.
using System;
using Godot;

namespace DeadFrequency;

public partial class OptionsPanel : VBoxContainer
{
    [Signal] public delegate void BackRequestedEventHandler();

    private Button _backButton;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 12);
        CustomMinimumSize = new Vector2(620, 0);

        AddChild(UiStyle.MakeLabel("OPÇÕES", 30));
        AddChild(UiStyle.MakeLabel("ajuste do receptor", 16, color: UiStyle.PhosphorDim));

        GameSettings s = GameSettings.Instance;
        if (s == null)
        {
            AddChild(UiStyle.MakeLabel("GameSettings não carregado (autoload ausente).", 16, color: UiStyle.Danger));
        }
        else
        {
            AddSlider("Sensibilidade do mouse", 0.1, 3.0, 0.05, s.MouseSensitivity, v => s.MouseSensitivity = (float)v, v => $"{v:0.00}x");
            AddToggle("Inverter eixo Y", s.InvertY, b => s.InvertY = b);
            AddSlider("Campo de visão", 60, 100, 1, s.Fov, v => s.Fov = (float)v, v => $"{v:0}°");
            AddSlider("Volume geral", 0, 1, 0.01, s.MasterVolume, v => s.MasterVolume = (float)v, v => $"{v * 100:0}%");
            AddSlider("Música", 0, 1, 0.01, s.MusicVolume, v => s.MusicVolume = (float)v, v => $"{v * 100:0}%");
            AddSlider("Efeitos", 0, 1, 0.01, s.SfxVolume, v => s.SfxVolume = (float)v, v => $"{v * 100:0}%");
            AddChoice("Horário", new[] { "Ciclo dia/noite", "Sempre noite", "Sempre dia" }, s.TimeMode, i => s.TimeMode = i);
            AddSlider("Duração do dia", 6, 60, 1, s.DayLengthMinutes, v => s.DayLengthMinutes = (float)v, v => $"{v:0} min");
            AddToggle("Tela cheia", s.Fullscreen, b => s.Fullscreen = b);
            AddToggle("VSync", s.VSync, b => s.VSync = b);
        }

        _backButton = UiStyle.MakeButton("Voltar", OnBack, sound: UiSound.Back);
        _backButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        AddChild(_backButton);
    }

    public void FocusFirst() => _backButton?.GrabFocus();

    private void OnBack()
    {
        GameSettings.Instance?.Save();
        EmitSignal(SignalName.BackRequested);
    }

    private void AddSlider(string text, double min, double max, double step, double value, Action<double> apply, Func<double, string> format)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        Label name = UiStyle.MakeLabel(text, 18, HorizontalAlignment.Left);
        name.CustomMinimumSize = new Vector2(260, 0);

        var slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(200, 20),
        };

        Label valueLabel = UiStyle.MakeLabel(format(value), 18, HorizontalAlignment.Right);
        valueLabel.CustomMinimumSize = new Vector2(80, 0);

        slider.ValueChanged += v =>
        {
            apply(v);
            valueLabel.Text = format(v);
            GameSettings.Instance?.Apply();
        };

        row.AddChild(name);
        row.AddChild(slider);
        row.AddChild(valueLabel);
        AddChild(row);
    }

    private void AddChoice(string text, string[] options, int selected, Action<int> apply)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        Label name = UiStyle.MakeLabel(text, 18, HorizontalAlignment.Left);
        name.CustomMinimumSize = new Vector2(260, 0);
        row.AddChild(name);

        var buttons = new System.Collections.Generic.List<Button>();
        var group = new ButtonGroup();
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            var b = new Button { Text = options[i], ToggleMode = true, ButtonGroup = group, ButtonPressed = i == selected };
            b.AddThemeFontSizeOverride("font_size", 15);
            b.MouseEntered += () => AudioManager.Instance?.PlayUi(UiSound.Hover);
            b.Pressed += () =>
            {
                AudioManager.Instance?.PlayUi(UiSound.Click);
                apply(index);
                GameSettings.Instance?.Apply();
            };
            buttons.Add(b);
            row.AddChild(b);
        }
        AddChild(row);
    }

    private void AddToggle(string text, bool value, Action<bool> apply)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = value };
        toggle.AddThemeFontSizeOverride("font_size", 18);
        toggle.MouseEntered += () => AudioManager.Instance?.PlayUi(UiSound.Hover);
        toggle.Toggled += pressed =>
        {
            AudioManager.Instance?.PlayUi(UiSound.Click);
            apply(pressed);
            GameSettings.Instance?.Apply();
        };
        AddChild(toggle);
    }
}
