// Scripts/UI/MinimapView.cs
// Minimapa do radar de pulso. Gira com o jogador (frente = para cima) e mostra ruas, depósito,
// entregas, barracas, ônibus e outros jogadores. Tudo desenhado em _Draw, sem texturas.
//
// Quem quiser aparecer no mapa entra no grupo "map_marker" e define a meta "map_color"
// (Color) e opcionalmente "map_kind" ("vendor", "bus", "player").
using Godot;

namespace DeadFrequency;

public partial class MinimapView : Control
{
    public const string MarkerGroup = "map_marker";

    public PlayerController Player { get; set; }
    public CityGenerator City { get; set; }
    public DeliveryManager Delivery { get; set; }
    /// <summary>Pixels por metro.</summary>
    public float PixelsPerMeter { get; set; } = 0.75f;

    private float _yaw;
    private Vector2 _origin;
    private float _time;

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    private Vector2 ToMap(Vector3 world)
    {
        float dx = world.X - _origin.X;
        float dz = world.Z - _origin.Y;
        float c = Mathf.Cos(_yaw), s = Mathf.Sin(_yaw);
        var rotated = new Vector2(dx * c - dz * s, dx * s + dz * c);
        return Size * 0.5f + rotated * PixelsPerMeter;
    }

    /// <summary>Mantém o ponto dentro do mapa (seta na borda quando está longe).</summary>
    private Vector2 ClampToEdge(Vector2 p, out bool clamped)
    {
        Vector2 center = Size * 0.5f;
        Vector2 d = p - center;
        float limit = Mathf.Min(Size.X, Size.Y) * 0.5f - 8f;
        float m = Mathf.Max(Mathf.Abs(d.X), Mathf.Abs(d.Y));
        clamped = m > limit;
        return clamped ? center + d / m * limit : p;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.01f, 0.03f, 0.02f, 0.88f));
        if (Player == null || !IsInstanceValid(Player))
            return;

        Vector3 me = Player.GlobalPosition;
        _origin = new Vector2(me.X, me.Z);
        _yaw = Player.Camera != null ? Player.Camera.GlobalRotation.Y : Player.GlobalRotation.Y;

        DrawRoads();

        // Depósito
        Vector2 depot = ClampToEdge(ToMap(Vector3.Zero), out bool depotFar);
        DrawRect(new Rect2(depot - new Vector2(6, 6), new Vector2(12, 12)), new Color(1f, 0.6f, 0.15f), !depotFar);
        if (depotFar)
            DrawRect(new Rect2(depot - new Vector2(6, 6), new Vector2(12, 12)), new Color(1f, 0.6f, 0.15f), false, 2f);

        // Entregas
        if (Delivery != null)
        {
            float pulse = 4f + Mathf.Sin(_time * 5f) * 1.5f;
            foreach (DeliveryJob job in Delivery.Jobs)
            {
                Vector2 p = ClampToEdge(ToMap(job.Point.Position), out bool far);
                DrawCircle(p, far ? 4f : pulse, UiStyle.Phosphor);
            }
        }

        // Outros marcadores (barraca, ônibus, jogadores)
        foreach (Node node in GetTree().GetNodesInGroup(MarkerGroup))
        {
            if (node is not Node3D marker || !marker.IsVisibleInTree())
                continue;
            Color color = marker.HasMeta("map_color") ? marker.GetMeta("map_color").AsColor() : Colors.White;
            string kind = marker.HasMeta("map_kind") ? marker.GetMeta("map_kind").AsString() : "";
            Vector2 p = ClampToEdge(ToMap(marker.GlobalPosition), out bool far);
            switch (kind)
            {
                case "bus":
                    if (!far)
                        DrawRect(new Rect2(p - new Vector2(4, 4), new Vector2(8, 8)), color);
                    break;
                case "poi":
                {
                    // Prédios importantes (bombeiros, polícia): quadrado com letra.
                    string letter = marker.HasMeta("map_label") ? marker.GetMeta("map_label").AsString() : "?";
                    DrawRect(new Rect2(p - new Vector2(7, 7), new Vector2(14, 14)), color);
                    DrawString(ThemeDB.FallbackFont, p + new Vector2(-4, 5), letter, HorizontalAlignment.Left, -1, 12, Colors.White);
                    break;
                }
                case "phone":
                    if (!far)
                        DrawCircle(p, 2.5f, color);
                    break;
                case "vendor":
                    DrawCircle(p, 5f, color);
                    DrawString(ThemeDB.FallbackFont, p + new Vector2(-4, 5), "$", HorizontalAlignment.Left, -1, 12, Colors.Black);
                    break;
                default:
                    DrawCircle(p, 3.5f, color);
                    break;
            }
        }

        // Eu (triângulo apontando para frente = cima)
        Vector2 c = Size * 0.5f;
        DrawColoredPolygon(new[] { c + new Vector2(0, -8), c + new Vector2(6, 6), c + new Vector2(-6, 6) }, Colors.White);

        DrawRect(new Rect2(Vector2.Zero, Size), UiStyle.PhosphorDim, false, 2f);
        DrawString(ThemeDB.FallbackFont, new Vector2(8, Size.Y - 8), "RADAR", HorizontalAlignment.Left, -1, 12, UiStyle.PhosphorDim);
    }

    private void DrawRoads()
    {
        if (City == null)
            return;
        if (City.MapRoads.Count > 0)
        {
            // Rancho: estradas de terra soltas.
            var dirt = new Color(0.36f, 0.3f, 0.2f);
            foreach ((Vector2 a, Vector2 b, float w) in City.MapRoads)
                DrawLine(ToMap(new Vector3(a.X, 0f, a.Y)), ToMap(new Vector3(b.X, 0f, b.Y)), dirt, Mathf.Max(2f, w * PixelsPerMeter));
            for (int i = 0; i < City.MapRoads.Count && i < City.MapRoadNames.Count; i++)
            {
                (Vector2 a, Vector2 b, _) = City.MapRoads[i];
                StreetLabel(new Vector3(a.X, 0f, a.Y), new Vector3(b.X, 0f, b.Y), City.MapRoadNames[i]);
            }
            return;
        }
        float period = City.Period;
        float extent = (City.Radius + 1) * period;
        float width = Mathf.Max(2f, City.RoadWidth * PixelsPerMeter);
        var color = new Color(0.25f, 0.3f, 0.27f);
        for (int k = -City.Radius - 1; k <= City.Radius; k++)
        {
            float road = k * period + period * 0.5f;
            DrawLine(ToMap(new Vector3(road, 0, -extent)), ToMap(new Vector3(road, 0, extent)), color, width);
            DrawLine(ToMap(new Vector3(-extent, 0, road)), ToMap(new Vector3(extent, 0, road)), color, width);
        }
        // Nomes das ruas por cima (só as que aparecem perto de você).
        for (int k = -City.Radius - 1; k <= City.Radius; k++)
        {
            float road = k * period + period * 0.5f;
            StreetLabel(new Vector3(road, 0, -extent), new Vector3(road, 0, extent), City.StreetNameX(k));
            StreetLabel(new Vector3(-extent, 0, road), new Vector3(extent, 0, road), City.StreetNameZ(k));
        }
    }

    /// <summary>Escreve o nome da rua ao longo dela, num ponto à frente do jogador, sem ficar de cabeça para baixo.</summary>
    private void StreetLabel(Vector3 a, Vector3 b, string name)
    {
        if (string.IsNullOrEmpty(name))
            return;
        var me = new Vector3(_origin.X, 0f, _origin.Y);
        Vector3 ab = b - a;
        float t = Mathf.Clamp((me - a).Dot(ab) / Mathf.Max(ab.LengthSquared(), 0.001f), 0f, 1f);
        Vector3 closest = a + ab * t;
        if (closest.DistanceTo(me) > 90f)
            return;
        // Um pouco adiante na rua para não ficar embaixo da setinha do jogador.
        Vector3 dir = ab.Normalized();
        Vector3 spot = closest + dir * 28f;
        if ((spot - a).Dot(dir) > ab.Length() || (spot - a).Dot(dir) < 0f)
            spot = closest - dir * 28f;
        Vector2 p0 = ToMap(spot);
        Vector2 p1 = ToMap(spot + dir);
        float angle = (p1 - p0).Angle();
        if (angle > Mathf.Pi * 0.5f || angle < -Mathf.Pi * 0.5f)
            angle += Mathf.Pi; // sempre legível da esquerda para a direita
        Vector2 c = Size * 0.5f;
        if ((p0 - c).Length() > Mathf.Min(Size.X, Size.Y) * 0.5f - 12f)
            return;
        Font font = ThemeDB.FallbackFont;
        const int size = 10;
        float w = font.GetStringSize(name, HorizontalAlignment.Left, -1, size).X;
        DrawSetTransform(p0, angle, Vector2.One);
        DrawString(font, new Vector2(-w * 0.5f + 1f, 4f), name, HorizontalAlignment.Left, -1, size, new Color(0f, 0f, 0f, 0.8f));
        DrawString(font, new Vector2(-w * 0.5f, 3f), name, HorizontalAlignment.Left, -1, size, new Color(0.75f, 0.95f, 0.8f));
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }
}
