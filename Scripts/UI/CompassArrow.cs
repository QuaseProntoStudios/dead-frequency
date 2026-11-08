// Scripts/UI/CompassArrow.cs
// Seta no topo da tela apontando para um lugar do mundo (o depósito), com a distância.
// Some quando você está perto.
using Godot;

namespace DeadFrequency;

public partial class CompassArrow : Control
{
    public PlayerController Player { get; set; }
    public Vector3 Target { get; set; } = Vector3.Zero;
    public string Label { get; set; } = "DEPÓSITO";
    public float HideDistance { get; set; } = 30f;
    public Color Color { get; set; } = new(1f, 0.6f, 0.15f);

    private float _angle;
    private float _distance;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Process(double delta)
    {
        if (Player == null || !IsInstanceValid(Player) || Player.Camera == null)
        {
            Visible = false;
            return;
        }
        Vector3 to = Target - Player.GlobalPosition;
        to.Y = 0f;
        _distance = to.Length();
        Visible = _distance > HideDistance && (GameSettings.Instance?.ShowCompass ?? true);
        if (!Visible)
            return;

        // Ângulo entre a frente da câmera e a direção do alvo (0 = em frente).
        Vector3 forward = -Player.Camera.GlobalTransform.Basis.Z;
        forward.Y = 0f;
        _angle = new Vector2(forward.X, forward.Z).AngleTo(new Vector2(to.X, to.Z));
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 c = new(Size.X * 0.5f, 22f);
        var points = new[] { new Vector2(0, -16), new Vector2(11, 10), new Vector2(0, 4), new Vector2(-11, 10) };
        var transformed = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
            transformed[i] = c + points[i].Rotated(_angle);
        DrawColoredPolygon(transformed, Color);
        DrawPolyline(new[] { transformed[0], transformed[1], transformed[2], transformed[3], transformed[0] }, Colors.Black, 1.5f);
        string text = $"{Label} · {_distance:0} m";
        Font font = ThemeDB.FallbackFont;
        float w = font.GetStringSize(text, HorizontalAlignment.Left, -1, 16).X;
        DrawString(font, new Vector2(c.X - w * 0.5f + 1, 59), text, HorizontalAlignment.Left, -1, 16, Colors.Black);
        DrawString(font, new Vector2(c.X - w * 0.5f, 58), text, HorizontalAlignment.Left, -1, 16, Color);
    }
}
