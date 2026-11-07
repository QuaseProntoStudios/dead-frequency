// Scripts/Vehicle/VanDetails.cs
// Acabamento da van montado em código (a cena van.tscn tem só o volume básico):
// para-choques, grade, piscas, retrovisores, placas, faixa lateral com o nome da empresa,
// para-barros e giroflex no teto. Frente da van = +Z. Só visual, sem colisão.
using Godot;

namespace DeadFrequency;

public static class VanDetails
{
    public static void Build(VanController van)
    {
        var root = new Node3D { Name = "Details" };
        van.AddChild(root);

        var dark = Mat(new Color(0.06f, 0.06f, 0.06f), 0.9f);
        var chrome = new StandardMaterial3D { AlbedoColor = new Color(0.62f, 0.62f, 0.64f), Metallic = 0.85f, Roughness = 0.3f };
        var stripe = Mat(new Color(0.95f, 0.5f, 0.08f), 0.6f);
        var plate = Mat(new Color(0.85f, 0.86f, 0.82f), 0.7f);
        var blinker = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.55f, 0.1f), EmissionEnabled = true, Emission = new Color(1f, 0.5f, 0.05f), EmissionEnergyMultiplier = 0.6f,
        };
        var beacon = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.55f, 0.1f, 0.85f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true, Emission = new Color(1f, 0.45f, 0.05f), EmissionEnergyMultiplier = 1.2f,
        };

        // Para-choques (saem da carroceria, que vai de z -2,3 a 2,3).
        Box(root, new Vector3(0f, 0.56f, 2.37f), new Vector3(2.0f, 0.2f, 0.14f), dark);
        Box(root, new Vector3(0f, 0.56f, -2.37f), new Vector3(2.0f, 0.2f, 0.14f), dark);
        // Grade com frisos cromados entre os faróis.
        Box(root, new Vector3(0f, 0.86f, 2.315f), new Vector3(0.8f, 0.24f, 0.03f), dark);
        for (int i = -1; i <= 1; i++)
            Box(root, new Vector3(0f, 0.86f + i * 0.07f, 2.34f), new Vector3(0.82f, 0.025f, 0.02f), chrome);
        // Molduras dos faróis e piscas.
        foreach (float side in new[] { -1f, 1f })
        {
            Box(root, new Vector3(side * 0.65f, 1f, 2.3f + 0.012f), new Vector3(0.36f, 0.21f, 0.02f), chrome);
            Box(root, new Vector3(side * 0.88f, 1f, 2.32f), new Vector3(0.1f, 0.12f, 0.03f), blinker);
            Box(root, new Vector3(side * 0.9f, 0.85f, -2.32f), new Vector3(0.08f, 0.12f, 0.03f), blinker);
            // Retrovisores com braço.
            Box(root, new Vector3(side * 1.0f, 1.75f, 2.2f), new Vector3(0.12f, 0.03f, 0.03f), dark);
            Box(root, new Vector3(side * 1.1f, 1.78f, 2.2f), new Vector3(0.06f, 0.22f, 0.15f), dark);
            // Faixa laranja ao longo da lateral.
            Box(root, new Vector3(side * 0.962f, 0.86f, 0f), new Vector3(0.02f, 0.08f, 4.4f), stripe);
            // Para-barros atrás das rodas traseiras.
            Box(root, new Vector3(side * 0.85f, 0.42f, -2.05f), new Vector3(0.28f, 0.32f, 0.02f), dark);
            // Maçaneta.
            Box(root, new Vector3(side * 0.96f, 1.45f, 1.25f), new Vector3(0.03f, 0.04f, 0.16f), chrome);

            // Nome da empresa na porta.
            van.AddChild(new Label3D
            {
                Text = "DEAD FREQUENCY\nENTREGAS · 96.6",
                FontSize = 48,
                PixelSize = 0.0045f,
                Modulate = new Color(0.95f, 0.5f, 0.08f),
                OutlineSize = 6,
                OutlineModulate = new Color(0.05f, 0.05f, 0.05f),
                Position = new Vector3(side * 0.96f, 1.3f, 1.6f),
                Rotation = new Vector3(0f, side * Mathf.Pi * 0.5f, 0f),
            });
        }

        // Placas.
        Box(root, new Vector3(0f, 0.56f, 2.45f), new Vector3(0.5f, 0.13f, 0.02f), plate);
        Box(root, new Vector3(0f, 0.82f, -2.32f), new Vector3(0.5f, 0.13f, 0.02f), plate);
        foreach ((float z, float rot) in new[] { (2.465f, 0f), (-2.335f, Mathf.Pi) })
        {
            van.AddChild(new Label3D
            {
                Text = "DFQ-1996",
                FontSize = 40,
                PixelSize = 0.0022f,
                Modulate = new Color(0.08f, 0.08f, 0.1f),
                OutlineSize = 0,
                Position = new Vector3(0f, z > 0 ? 0.56f : 0.82f, z),
                Rotation = new Vector3(0f, rot, 0f),
            });
        }

        // Aviso na tampa traseira.
        van.AddChild(new Label3D
        {
            Text = "CARGA FRÁGIL · MANTENHA DISTÂNCIA",
            FontSize = 40,
            PixelSize = 0.0035f,
            Modulate = new Color(0.95f, 0.85f, 0.2f),
            OutlineSize = 6,
            OutlineModulate = new Color(0.05f, 0.05f, 0.05f),
            Position = new Vector3(0f, 1.24f, -2.31f),
            Rotation = new Vector3(0f, Mathf.Pi, 0f),
        });

        // Giroflex âmbar no teto da cabine.
        Box(root, new Vector3(0f, 2.47f, 1.6f), new Vector3(0.5f, 0.06f, 0.18f), dark);
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.08f, BottomRadius = 0.1f, Height = 0.14f, RadialSegments = 10 },
            Position = new Vector3(0f, 2.57f, 1.6f),
            MaterialOverride = beacon,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        // Antena do rádio.
        Box(root, new Vector3(-0.85f, 2.8f, 2.1f), new Vector3(0.015f, 0.8f, 0.015f), dark);
    }

    private static StandardMaterial3D Mat(Color c, float roughness) => new() { AlbedoColor = c, Roughness = roughness };

    private static void Box(Node3D parent, Vector3 pos, Vector3 size, Material mat) =>
        parent.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos, MaterialOverride = mat });
}
