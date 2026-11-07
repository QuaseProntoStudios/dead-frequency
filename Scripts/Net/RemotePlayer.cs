// Scripts/Net/RemotePlayer.cs
// Outro entregador na sala: boneco com colete na cor do jogador, nome em cima e os equipamentos
// que ele está usando. A posição chega ~10x por segundo e é suavizada aqui.
using Godot;

namespace DeadFrequency;

public partial class RemotePlayer : Node3D
{
    public int PlayerId { get; private set; }
    public string PlayerName { get; private set; } = "";

    private CharacterModel _model;
    private Label3D _nameLabel;
    private Vector3 _targetPosition;
    private Vector3 _lastPosition;
    private float _targetYaw;
    private float _targetPitch;
    private bool _hasState;
    private bool _driving;

    public void Setup(int id, string name, Color color)
    {
        PlayerId = id;
        PlayerName = name;
        Name = $"Remote_{id}";
        AddToGroup(MinimapView.MarkerGroup);
        SetMeta("map_color", color);
        SetMeta("map_kind", "player");

        _model = CharacterModel.Create(color.Darkened(0.55f), new Color(0.12f, 0.13f, 0.18f), new Color(0.76f, 0.58f, 0.44f), color);
        AddChild(_model);

        _nameLabel = new Label3D
        {
            Text = name,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 48,
            PixelSize = 0.006f,
            Modulate = color,
            OutlineSize = 8,
            Position = new Vector3(0f, 2.25f, 0f),
            NoDepthTest = true,
        };
        AddChild(_nameLabel);
    }

    public void ApplyState(Vector3 position, float yaw, float pitch, bool driving, string equipment)
    {
        _targetPosition = position;
        _targetYaw = yaw;
        _targetPitch = pitch;
        _driving = driving;
        _model.SetEquipment(equipment);
        if (!_hasState)
        {
            GlobalPosition = position;
            _lastPosition = position;
            Rotation = new Vector3(0f, yaw, 0f);
            _hasState = true;
        }
    }

    public override void _Process(double delta)
    {
        if (!_hasState)
            return;
        float dt = (float)delta;
        float t = 1f - Mathf.Exp(-12f * dt);
        GlobalPosition = GlobalPosition.Lerp(_targetPosition, t);
        Rotation = new Vector3(0f, Mathf.LerpAngle(Rotation.Y, _targetYaw, t), 0f);
        _model.SetHeadPitch(-_targetPitch);

        Vector3 moved = GlobalPosition - _lastPosition;
        moved.Y = 0f;
        _lastPosition = GlobalPosition;
        float speed = dt > 0f ? moved.Length() / dt : 0f;
        _model.Animate(dt, _driving ? 0f : speed);
        // Sentado na van: o corpo fica escondido dentro da cabine, só o nome aparece.
        _model.Visible = !_driving;
    }
}
