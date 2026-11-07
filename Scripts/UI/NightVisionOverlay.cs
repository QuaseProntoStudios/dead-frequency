// Scripts/UI/NightVisionOverlay.cs
// Tela verde dos óculos de visão noturna (liga com N quando os óculos estão equipados).
using Godot;

namespace DeadFrequency;

public partial class NightVisionOverlay : CanvasLayer
{
    [Export] public PlayerController Player { get; set; }

    private ColorRect _rect;
    private AudioStreamPlayer _sound;

    public override void _Ready()
    {
        Layer = 0; // abaixo do HUD, acima do 3D
        _rect = new ColorRect
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/night_vision.gdshader") },
        };
        _rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rect);

        _sound = new AudioStreamPlayer { Bus = AudioManager.SfxBus, VolumeDb = -6f };
        if (ResourceLoader.Exists("res://Audio/SFX/nightvision_on.wav"))
            _sound.Stream = GD.Load<AudioStream>("res://Audio/SFX/nightvision_on.wav");
        AddChild(_sound);

        if (Player != null)
            Player.NightVisionToggled += OnToggled;
    }

    public override void _ExitTree()
    {
        if (Player != null)
            Player.NightVisionToggled -= OnToggled;
    }

    private void OnToggled(bool on)
    {
        _rect.Visible = on;
        if (on && _sound.Stream != null)
            _sound.Play();
    }
}
