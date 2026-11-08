// Scripts/World/PedestrianSignal.cs
// Semáforo de pedestre nas esquinas (v0.9.3), no lugar da placa "faixa de pedestre".
// Fica VERDE (bonequinho andando) quando não vem nenhum veículo, e VERMELHO quando o ônibus, a van,
// um caminhão ou um carro do posto está passando perto. Pisca o verde quando um veículo se aproxima.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public partial class PedestrianSignal : StaticBody3D
{
    /// <summary>Grupo dos veículos que andam pela cidade (ônibus, van, caminhões, carros do posto).</summary>
    public const string TrafficGroup = "traffic";

    private static StandardMaterial3D _redOn, _greenOn, _off;
    private readonly List<MeshInstance3D> _red = new();
    private readonly List<MeshInstance3D> _green = new();
    private double _timer;
    private readonly Dictionary<ulong, Vector3> _last = new();

    public override void _Ready()
    {
        CollisionLayer = 1;
        CollisionMask = 0;
        EnsureMaterials();
        var metal = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.12f, 0.13f), Roughness = 0.6f };
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.14f, 2.9f, 0.14f) }, Position = new Vector3(0f, 1.45f, 0f) });
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.06f, Height = 2.9f, RadialSegments = 8 }, Position = new Vector3(0f, 1.45f, 0f), MaterialOverride = metal });
        // Duas caixas: uma para cada faixa da esquina (viradas para -X e para -Z, para quem está do outro lado).
        foreach (float yaw in new[] { -Mathf.Pi * 0.5f, Mathf.Pi })
        {
            var head = new Node3D { Position = new Vector3(0f, 2.45f, 0f), Rotation = new Vector3(0f, yaw, 0f) };
            AddChild(head);
            head.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.32f, 0.62f, 0.22f) }, Position = new Vector3(0f, 0f, 0.16f), MaterialOverride = metal });
            var red = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(0.24f, 0.24f) }, Position = new Vector3(0f, 0.14f, 0.272f), MaterialOverride = _off };
            var green = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(0.24f, 0.24f) }, Position = new Vector3(0f, -0.14f, 0.272f), MaterialOverride = _off };
            // Viseiras.
            head.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.02f, 0.12f) }, Position = new Vector3(0f, 0.28f, 0.33f), MaterialOverride = metal });
            head.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.02f, 0.12f) }, Position = new Vector3(0f, 0.0f, 0.33f), MaterialOverride = metal });
            head.AddChild(red);
            head.AddChild(green);
            _red.Add(red);
            _green.Add(green);
        }
        _timer = GD.Randf() * 0.3;
    }

    private static void EnsureMaterials()
    {
        if (_off != null)
            return;
        StandardMaterial3D Lamp(string path, float energy)
        {
            var tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
            return new StandardMaterial3D { AlbedoTexture = tex, EmissionEnabled = energy > 0f, EmissionTexture = tex, EmissionEnergyMultiplier = energy, AlbedoColor = energy > 0f ? Colors.White : new Color(0.25f, 0.25f, 0.25f) };
        }
        _redOn = Lamp("res://Assets/Textures/ped_red.png", 2.5f);
        _greenOn = Lamp("res://Assets/Textures/ped_green.png", 2.5f);
        _off = new StandardMaterial3D { AlbedoColor = new Color(0.06f, 0.06f, 0.06f) };
    }

    public override void _Process(double delta)
    {
        _timer -= delta;
        if (_timer > 0)
            return;
        double step = 0.25;
        _timer = step;
        // Distância do veículo mais perto que está andando (e se ele vem chegando).
        float danger = float.MaxValue;
        bool approaching = false;
        Vector3 me = GlobalPosition;
        foreach (Node n in GetTree().GetNodesInGroup(TrafficGroup))
        {
            if (n is not Node3D v || !v.IsVisibleInTree())
                continue;
            ulong id = v.GetInstanceId();
            Vector3 pos = v.GlobalPosition;
            Vector3 vel = _last.TryGetValue(id, out Vector3 prev) ? (pos - prev) / (float)step : Vector3.Zero;
            _last[id] = pos;
            float d = new Vector2(pos.X - me.X, pos.Z - me.Z).Length();
            if (d > 45f || vel.Length() < 1f)
                continue;
            if (d < danger)
                danger = d;
            Vector3 to = me - pos;
            if (d < 45f && vel.Dot(to) > 0f)
                approaching = true;
        }
        bool red = danger < 22f;
        bool blink = !red && approaching && danger < 45f;
        bool greenOn = !red && (!blink || Mathf.PosMod(Time.GetTicksMsec() / 250, 2) == 0);
        if (blink)
            _timer = 0.25;
        foreach (MeshInstance3D m in _red)
            m.MaterialOverride = red ? _redOn : _off;
        foreach (MeshInstance3D m in _green)
            m.MaterialOverride = greenOn ? _greenOn : _off;
    }
}
