// Scripts/World/CityGenerator.PoleWires.cs
// Fase 1 · Cidade (ambientação Brazilcore): fios caindo entre os postes de luz (três fios com a barriga da catenária)
// e cartazes de "lambe-lambe" colados nos postes (VENDE-SE, CONSERTA TV, CACHORRO PERDIDO...). Os fios são uma malha
// só, compartilhada; os cartazes são um quadrinho de papel com Label3D (os Label3D somem a partir de 45 m).
// Escolha por AssetProps.Hash da posição do poste: não mexe nos sorteios da cidade (layout igual no online).
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private ArrayMesh _wireMesh;
    private StandardMaterial3D _wireMaterial;
    private QuadMesh _flyerMesh;
    private readonly System.Collections.Generic.Dictionary<int, StandardMaterial3D> _flyerPapers = new();

    private static readonly string[] FlyerTexts =
    {
        "VENDE-SE\nFUSCA 79\nTRATAR NA\nPADARIA",
        "CONSERTA\nTV E RÁDIO\nORÇAMENTO\nGRÁTIS",
        "PERDIDO\nCACHORRO\nCARAMELO\nGRATIFICA",
        "AULA DE\nVIOLÃO\nR$ 15 A HORA",
        "BAILE DA\nSERRA\nSÁBADO\n96.6 FM",
        "ALUGA-SE\nQUARTO\nPERTO DA\nRODOVIÁRIA",
        "PRECISA-SE\nAJUDANTE DE\nENTREGA",
        "SEM SINAL?\nGIRE A\nANTENA!",
    };

    private static readonly Color[] FlyerColors =
    {
        new(0.95f, 0.88f, 0.45f), new(0.92f, 0.92f, 0.88f), new(0.95f, 0.7f, 0.78f), new(0.68f, 0.82f, 0.92f),
    };

    private void BuildPoleWires()
    {
        if (_useLogicalBlockPlans)
            return; // Os fios acompanham o chunk.

        var root = new Node3D { Name = "PoleWires" };
        _staticRoot.AddChild(root);
        for (int bx = -Radius; bx <= Radius; bx++)
        {
            for (int bz = -Radius; bz <= Radius; bz++)
                AddPoleWiresForBlock(root, bx, bz);
        }
    }

    private void BuildPoleWiresForBlock(int bx, int bz)
    {
        var root = new Node3D { Name = $"PoleWires_{bx}_{bz}" };
        (_activeChunkRoot ?? _staticRoot).AddChild(root);
        AddPoleWiresForBlock(root, bx, bz);
    }

    /// <summary>Fios do poste A (lado +X) do bloco até o poste A do bloco ao lado em +Z, e do poste B (lado +Z) até o B em +X.</summary>
    private void AddPoleWiresForBlock(Node3D root, int bx, int bz)
    {
        EnsureWireMesh();
        float edge = BlockSize * 0.5f - 0.7f;
        Vector3 c = BlockCenter(bx, bz);
        root.AddChild(new MeshInstance3D
        {
            Mesh = _wireMesh,
            Position = c + new Vector3(edge, CurbHeight, -BlockSize * 0.25f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        root.AddChild(new MeshInstance3D
        {
            Mesh = _wireMesh,
            Position = c + new Vector3(-BlockSize * 0.25f, CurbHeight, edge),
            Rotation = new Vector3(0f, Mathf.Pi * 0.5f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    /// <summary>Três fios com a barriga da catenária, de z = 0 até z = Period, saindo do topo do poste (altura 6,5 m).</summary>
    private void EnsureWireMesh()
    {
        if (_wireMesh != null)
            return;
        _wireMaterial = Flat(new Color(0.03f, 0.03f, 0.03f), 1f);
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetMaterial(_wireMaterial);
        const int segments = 10;
        float length = Period;
        float[] sideOffsets = { -0.14f, 0f, 0.14f };
        float[] topHeights = { 6.2f, 6.35f, 6.2f };
        float[] sag = { 0.8f, 0.65f, 0.9f };
        for (int w = 0; w < 3; w++)
        {
            Vector3 previous = Vector3.Zero;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                var p = new Vector3(sideOffsets[w], topHeights[w] - 4f * sag[w] * t * (1f - t), t * length);
                if (i > 0)
                {
                    Vector3 delta = p - previous;
                    var box = new BoxMesh { Size = new Vector3(0.03f, 0.03f, delta.Length() + 0.02f) };
                    Basis basis = Basis.LookingAt(delta.Normalized(), Vector3.Up);
                    st.AppendFrom(box, 0, new Transform3D(basis, (p + previous) * 0.5f));
                }
                previous = p;
            }
        }
        st.GenerateNormals();
        _wireMesh = st.Commit();
    }

    /// <summary>Cartaz de lambe-lambe colado no poste, virado para quem anda pela calçada (a rua corre perpendicular a towardRoad).</summary>
    private void AddPoleFlyer(Node3D pole, Vector3 basePos, Vector3 towardRoad)
    {
        int hash = AssetProps.Hash(basePos, 71);
        if (hash % 100 >= 65)
            return;
        _flyerMesh ??= new QuadMesh { Size = new Vector2(0.42f, 0.55f) };
        int colorIndex = hash / 100 % FlyerColors.Length;
        if (!_flyerPapers.TryGetValue(colorIndex, out StandardMaterial3D paper))
        {
            paper = Flat(FlyerColors[colorIndex], 1f);
            paper.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            _flyerPapers[colorIndex] = paper;
        }
        // Plano do cartaz paralelo à rua: normal ao longo do eixo da rua (perpendicular a towardRoad).
        bool streetAlongX = Mathf.Abs(towardRoad.Z) > 0.5f;
        float yaw = streetAlongX ? Mathf.Pi * 0.5f : 0f;
        Vector3 normal = streetAlongX ? Vector3.Right : Vector3.Back;
        float side = hash / 1000 % 2 == 0 ? 1f : -1f;
        Vector3 pos = normal * side * 0.095f + new Vector3(0f, 1.9f + hash / 10000 % 4 * 0.1f, 0f);
        float faceYaw = yaw + (side > 0f ? 0f : Mathf.Pi);
        var flyer = new Node3D { Name = "Cartaz", Position = pos, Rotation = new Vector3(0f, faceYaw, 0f) };
        flyer.AddChild(new MeshInstance3D { Mesh = _flyerMesh, MaterialOverride = paper, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        flyer.AddChild(new Label3D
        {
            Text = FlyerTexts[hash / 7 % FlyerTexts.Length],
            FontSize = 28,
            PixelSize = 0.0021f,
            Modulate = new Color(0.08f, 0.07f, 0.06f),
            OutlineSize = 0,
            Shaded = false,
            DoubleSided = false,
            Position = new Vector3(0f, 0f, 0.004f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        pole.AddChild(flyer);
    }
}
