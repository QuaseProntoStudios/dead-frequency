// Scripts/World/CarModels.cs
// Modelos de carro low-poly montados com caixas e cilindros (estilo PS1).
// Quatro tipos: sedã, hatch, picape e perua/kombi. Frente do carro = -Z, origem no chão.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public enum CarType
{
    Sedan,
    Hatch,
    Pickup,
    Kombi,
}

public static class CarModels
{
    private static BoxMesh _box;
    private static CylinderMesh _wheel;
    private static StandardMaterial3D _glass, _rubber, _chrome, _head, _tail, _plate, _interior, _grille, _blinker;
    private static readonly Dictionary<Color, StandardMaterial3D> Paint = new();

    private static void EnsureResources()
    {
        if (_box != null)
            return;
        _box = new BoxMesh { Size = Vector3.One };
        _wheel = new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f, RadialSegments = 10, Rings = 1 };
        _glass = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.11f, 0.13f), Roughness = 0.1f, Metallic = 0.2f };
        _rubber = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.05f, 0.05f), Roughness = 1f };
        _chrome = new StandardMaterial3D { AlbedoColor = new Color(0.6f, 0.6f, 0.62f), Roughness = 0.3f, Metallic = 0.8f };
        _interior = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.1f, 0.09f) };
        _head = Emissive(new Color(1f, 0.95f, 0.8f), 1.2f);
        _tail = Emissive(new Color(0.8f, 0.05f, 0.04f), 0.8f);
        _plate = new StandardMaterial3D { AlbedoColor = new Color(0.85f, 0.85f, 0.8f) };
        _grille = new StandardMaterial3D { AlbedoColor = new Color(0.04f, 0.04f, 0.045f), Roughness = 0.9f };
        _blinker = Emissive(new Color(1f, 0.55f, 0.1f), 0.5f);
    }

    private static StandardMaterial3D Emissive(Color c, float energy) =>
        new() { AlbedoColor = c, EmissionEnabled = true, Emission = c, EmissionEnergyMultiplier = energy };

    public static StandardMaterial3D PaintFor(Color color)
    {
        if (!Paint.TryGetValue(color, out StandardMaterial3D mat))
        {
            mat = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.35f, Metallic = 0.35f };
            Paint[color] = mat;
        }
        return mat;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 pos, Vector3 size, Material mat, float tiltX = 0f)
    {
        EnsureResources();
        var mesh = new MeshInstance3D { Mesh = _box, Position = pos, Scale = size, MaterialOverride = mat, Rotation = new Vector3(tiltX, 0f, 0f) };
        parent.AddChild(mesh);
        return mesh;
    }

    private static void Wheel(Node3D parent, Vector3 pos, float radius, float width)
    {
        var tire = new MeshInstance3D
        {
            Mesh = _wheel,
            Position = pos,
            Scale = new Vector3(radius * 2f, width, radius * 2f),
            Rotation = new Vector3(0f, 0f, Mathf.Pi * 0.5f),
            MaterialOverride = _rubber,
        };
        parent.AddChild(tire);
        var hub = new MeshInstance3D
        {
            Mesh = _wheel,
            Position = pos + new Vector3(Mathf.Sign(pos.X) * width * 0.52f, 0f, 0f),
            Scale = new Vector3(radius * 1.1f, 0.02f, radius * 1.1f),
            Rotation = new Vector3(0f, 0f, Mathf.Pi * 0.5f),
            MaterialOverride = _chrome,
        };
        parent.AddChild(hub);
    }

    /// <summary>Faróis, lanternas, para-choques, grade e placa, comuns a todos.
    /// Cada peça fica numa "camada" própria à frente da carroceria (nenhuma face coincide com outra),
    /// senão as duas superfícies brigam pelo mesmo pixel e a grade pisca (z-fighting).</summary>
    private static void Details(Node3D car, float width, float length, float lightY, float bumperY)
    {
        float hz = length * 0.5f;
        // Para-choques: saem 9 cm da carroceria.
        Box(car, new Vector3(0f, bumperY, -hz - 0.03f), new Vector3(width + 0.06f, 0.18f, 0.12f), _rubber);
        Box(car, new Vector3(0f, bumperY, hz + 0.03f), new Vector3(width + 0.06f, 0.18f, 0.12f), _rubber);

        // Grade: fundo escuro rente à frente + 3 frisos cromados mais à frente.
        float grilleW = width * 0.36f;
        Box(car, new Vector3(0f, lightY, -hz - 0.015f), new Vector3(grilleW, 0.16f, 0.03f), _grille);
        for (int i = -1; i <= 1; i++)
            Box(car, new Vector3(0f, lightY + i * 0.05f, -hz - 0.04f), new Vector3(grilleW + 0.02f, 0.018f, 0.02f), _chrome);

        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * width * 0.34f;
            // Farol: moldura cromada + lente acesa um pouco à frente.
            Box(car, new Vector3(x, lightY, -hz - 0.012f), new Vector3(0.4f, 0.17f, 0.024f), _chrome);
            Box(car, new Vector3(x, lightY, -hz - 0.035f), new Vector3(0.34f, 0.12f, 0.02f), _head);
            // Pisca laranja ao lado do farol.
            Box(car, new Vector3(side * (width * 0.5f - 0.07f), lightY, -hz - 0.02f), new Vector3(0.1f, 0.1f, 0.03f), _blinker);
            // Lanterna traseira.
            Box(car, new Vector3(x, lightY, hz + 0.02f), new Vector3(0.34f, 0.14f, 0.04f), _tail);
            // Retrovisor.
            Box(car, new Vector3(side * (width * 0.5f + 0.08f), lightY + 0.4f, -hz * 0.36f), new Vector3(0.1f, 0.09f, 0.14f), _rubber);
        }
        // Placas (frente no para-choque, trás acima dele).
        Box(car, new Vector3(0f, bumperY, -hz - 0.1f), new Vector3(0.46f, 0.12f, 0.02f), _plate);
        Box(car, new Vector3(0f, bumperY + 0.19f, hz + 0.02f), new Vector3(0.46f, 0.12f, 0.03f), _plate);
        // Maçanetas.
        foreach (float side in new[] { -1f, 1f })
            Box(car, new Vector3(side * (width * 0.5f + 0.01f), lightY + 0.12f, -0.1f), new Vector3(0.03f, 0.04f, 0.16f), _chrome);
    }

    /// <summary>Monta o carro como filho de parent. Retorna o tamanho (largura, altura, comprimento) para colisão.</summary>
    public static Vector3 Build(Node3D parent, CarType type, Color color)
    {
        EnsureResources();
        StandardMaterial3D paint = PaintFor(color);
        switch (type)
        {
            case CarType.Hatch:
            {
                const float w = 1.7f, l = 3.8f;
                Box(parent, new Vector3(0f, 0.55f, 0f), new Vector3(w, 0.55f, l), paint);
                Box(parent, new Vector3(0f, 0.86f, -1.35f), new Vector3(w - 0.04f, 0.1f, 1.0f), paint);
                Box(parent, new Vector3(0f, 1.1f, 0.35f), new Vector3(w - 0.14f, 0.46f, 2.1f), _glass);
                Box(parent, new Vector3(0f, 1.36f, 0.4f), new Vector3(w - 0.1f, 0.07f, 1.95f), paint);
                Box(parent, new Vector3(0f, 1.115f, -0.79f), new Vector3(w - 0.16f, 0.05f, 0.56f), _glass, -0.92f); // para-brisa (sobe do capô até o teto)
                Box(parent, new Vector3(0f, 1.0f, 1.42f), new Vector3(w - 0.06f, 0.55f, 0.12f), paint); // traseira reta
                Details(parent, w, l, 0.68f, 0.4f);
                foreach (float x in new[] { -0.8f, 0.8f })
                    foreach (float z in new[] { -1.2f, 1.2f })
                        Wheel(parent, new Vector3(x, 0.31f, z), 0.31f, 0.22f);
                return new Vector3(w, 1.4f, l);
            }
            case CarType.Pickup:
            {
                const float w = 1.85f, l = 4.9f;
                Box(parent, new Vector3(0f, 0.62f, 0f), new Vector3(w, 0.6f, l), paint);
                Box(parent, new Vector3(0f, 0.95f, -1.8f), new Vector3(w - 0.04f, 0.1f, 1.2f), paint);
                Box(parent, new Vector3(0f, 1.2f, -0.55f), new Vector3(w - 0.12f, 0.5f, 1.3f), _glass);
                Box(parent, new Vector3(0f, 1.48f, -0.5f), new Vector3(w - 0.08f, 0.07f, 1.25f), paint);
                Box(parent, new Vector3(0f, 1.225f, -1.2f), new Vector3(w - 0.14f, 0.05f, 0.52f), _glass, -1.06f); // para-brisa
                // Caçamba
                Box(parent, new Vector3(0f, 0.95f, 1.25f), new Vector3(w - 0.2f, 0.06f, 2.1f), _interior);
                Box(parent, new Vector3(w * 0.5f - 0.05f, 1.1f, 1.25f), new Vector3(0.1f, 0.35f, 2.2f), paint);
                Box(parent, new Vector3(-w * 0.5f + 0.05f, 1.1f, 1.25f), new Vector3(0.1f, 0.35f, 2.2f), paint);
                Box(parent, new Vector3(0f, 1.1f, 2.4f), new Vector3(w, 0.35f, 0.1f), paint);
                Details(parent, w, l, 0.78f, 0.45f);
                foreach (float x in new[] { -0.88f, 0.88f })
                    foreach (float z in new[] { -1.55f, 1.5f })
                        Wheel(parent, new Vector3(x, 0.36f, z), 0.36f, 0.26f);
                return new Vector3(w, 1.55f, l);
            }
            case CarType.Kombi:
            {
                const float w = 1.8f, l = 4.3f;
                Box(parent, new Vector3(0f, 0.75f, 0f), new Vector3(w, 0.9f, l), paint);
                Box(parent, new Vector3(0f, 1.62f, 0f), new Vector3(w, 0.85f, l - 0.2f), PaintFor(new Color(0.88f, 0.86f, 0.8f)));
                Box(parent, new Vector3(0f, 1.6f, 0.1f), new Vector3(w + 0.02f, 0.42f, 3.0f), _glass);
                Box(parent, new Vector3(0f, 1.62f, -2.07f), new Vector3(w - 0.2f, 0.45f, 0.04f), _glass);
                Box(parent, new Vector3(0f, 1.1f, -2.165f), new Vector3(0.9f, 0.42f, 0.03f), PaintFor(new Color(0.88f, 0.86f, 0.8f))); // "V" frontal
                Details(parent, w, l, 0.75f, 0.38f);
                foreach (float x in new[] { -0.82f, 0.82f })
                    foreach (float z in new[] { -1.4f, 1.4f })
                        Wheel(parent, new Vector3(x, 0.33f, z), 0.33f, 0.22f);
                return new Vector3(w, 2.05f, l);
            }
            default: // Sedan
            {
                const float w = 1.8f, l = 4.5f;
                Box(parent, new Vector3(0f, 0.56f, 0f), new Vector3(w, 0.52f, l), paint);
                Box(parent, new Vector3(0f, 0.86f, -1.5f), new Vector3(w - 0.04f, 0.1f, 1.3f), paint); // capô
                Box(parent, new Vector3(0f, 0.86f, 1.7f), new Vector3(w - 0.04f, 0.1f, 0.95f), paint); // porta-malas
                Box(parent, new Vector3(0f, 1.1f, 0.15f), new Vector3(w - 0.16f, 0.44f, 1.75f), _glass);
                Box(parent, new Vector3(0f, 1.35f, 0.15f), new Vector3(w - 0.12f, 0.07f, 1.6f), paint); // teto
                Box(parent, new Vector3(0f, 1.12f, -0.86f), new Vector3(w - 0.18f, 0.05f, 0.62f), _glass, -0.83f); // para-brisa (do capô ao teto)
                Box(parent, new Vector3(0f, 1.125f, 1.1f), new Vector3(w - 0.18f, 0.05f, 0.54f), _glass, 0.96f); // vidro traseiro (do teto ao porta-malas)
                Details(parent, w, l, 0.66f, 0.4f);
                foreach (float x in new[] { -0.84f, 0.84f })
                    foreach (float z in new[] { -1.4f, 1.4f })
                        Wheel(parent, new Vector3(x, 0.32f, z), 0.32f, 0.22f);
                return new Vector3(w, 1.4f, l);
            }
        }
    }
}
