// Scripts/World/CityGenerator.PoleWires.cs
// Fase 1 · Cidade: condutores com espaçamento de rede de distribuição e cartazes colados ao redor dos postes.
// A malha continua compartilhada e a escolha de cartazes permanece determinística por posição.
using Godot;
using System.Collections.Generic;

namespace DeadFrequency;

public partial class CityGenerator
{
    private ArrayMesh _wireMesh;
    private StandardMaterial3D _wireMaterial;
    private CylinderMesh _flyerWrapMesh;
    private readonly Dictionary<int, ShaderMaterial> _flyerPapers = new();

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
            return;

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

    /// <summary>Conecta o poste A ao bloco +Z e o poste B ao bloco +X.</summary>
    private void AddPoleWiresForBlock(Node3D root, int bx, int bz)
    {
        EnsureWireMesh();
        float edge = BlockSize * 0.5f - 0.7f;
        Vector3 c = BlockCenter(bx, bz);
        Vector3 poleA = c + new Vector3(edge, CurbHeight, -BlockSize * 0.25f);
        Vector3 poleB = c + new Vector3(-BlockSize * 0.25f, CurbHeight, edge);

        if (bz < Radius)
        {
            Vector3 nextPoleA = BlockCenter(bx, bz + 1)
                + new Vector3(edge, CurbHeight, -BlockSize * 0.25f);
            if (!IsKeepClearAt(poleA) && !IsKeepClearAt(nextPoleA))
            {
                root.AddChild(new MeshInstance3D
                {
                    Mesh = _wireMesh,
                    Position = poleA,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                });
            }
        }

        if (bx < Radius)
        {
            Vector3 nextPoleB = BlockCenter(bx + 1, bz)
                + new Vector3(-BlockSize * 0.25f, CurbHeight, edge);
            if (!IsKeepClearAt(poleB) && !IsKeepClearAt(nextPoleB))
            {
                root.AddChild(new MeshInstance3D
                {
                    Mesh = _wireMesh,
                    Position = poleB,
                    Rotation = new Vector3(0f, Mathf.Pi * 0.5f, 0f),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                });
            }
        }
    }

    private StandardMaterial3D GetPoleWireMaterial()
    {
        _wireMaterial ??= Flat(new Color(0.025f, 0.025f, 0.025f), 1f);
        return _wireMaterial;
    }

    /// <summary>
    /// Cinco condutores: três fases espaçadas no braço superior e dois cabos secundários no braço inferior.
    /// </summary>
    private void EnsureWireMesh()
    {
        if (_wireMesh != null)
            return;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetMaterial(GetPoleWireMaterial());

        const int segments = 12;
        float length = Period;
        float[] sideOffsets = { -0.62f, 0f, 0.62f, -0.24f, 0.24f };
        float[] topHeights =
        {
            ElectricalWireHeight, ElectricalWireHeight + 0.04f, ElectricalWireHeight,
            ElectricalSecondaryWireHeight, ElectricalSecondaryWireHeight,
        };
        float[] sag = { 0.55f, 0.48f, 0.55f, 0.72f, 0.72f };

        for (int wire = 0; wire < sideOffsets.Length; wire++)
        {
            Vector3 previous = Vector3.Zero;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                var point = new Vector3(
                    sideOffsets[wire],
                    topHeights[wire] - 4f * sag[wire] * t * (1f - t),
                    t * length);

                if (i > 0)
                {
                    Vector3 delta = point - previous;
                    var box = new BoxMesh { Size = new Vector3(0.028f, 0.028f, delta.Length() + 0.02f) };
                    Basis basis = Basis.LookingAt(delta.Normalized(), Vector3.Up);
                    st.AppendFrom(box, 0, new Transform3D(basis, (point + previous) * 0.5f));
                }
                previous = point;
            }
        }

        st.GenerateNormals();
        _wireMesh = st.Commit();
    }

    private ShaderMaterial GetFlyerPaperMaterial(int colorIndex)
    {
        if (_flyerPapers.TryGetValue(colorIndex, out ShaderMaterial paper))
            return paper;

        paper = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://Shaders/poster_wrap.gdshader"),
        };
        paper.SetShaderParameter("paper_color", FlyerColors[colorIndex]);
        _flyerPapers[colorIndex] = paper;
        return paper;
    }

    /// <summary>Faixa de papel curva ao redor do poste; o texto fica na face voltada à calçada.</summary>
    private void AddPoleFlyer(Node3D pole, Vector3 basePos, Vector3 towardRoad, float poleRadius)
    {
        int hash = AssetProps.Hash(basePos, 71);
        if (hash % 100 >= 65)
            return;

        _flyerWrapMesh ??= new CylinderMesh
        {
            TopRadius = 0.5f,
            BottomRadius = 0.5f,
            Height = 1f,
            RadialSegments = 12,
            CapTop = false,
            CapBottom = false,
        };

        int colorIndex = hash / 100 % FlyerColors.Length;
        float wrapRadius = poleRadius + 0.025f;
        float bandHeight = 0.58f;
        bool streetAlongX = Mathf.Abs(towardRoad.Z) > 0.5f;
        float yaw = streetAlongX ? Mathf.Pi * 0.5f : 0f;
        Vector3 normal = streetAlongX ? Vector3.Right : Vector3.Back;
        float side = hash / 1000 % 2 == 0 ? 1f : -1f;
        float faceYaw = yaw + (side > 0f ? 0f : Mathf.Pi);

        var flyer = new Node3D
        {
            Name = "Cartaz",
            Position = new Vector3(0f, 1.9f + hash / 10000 % 4 * 0.1f, 0f),
            Rotation = new Vector3(0f, faceYaw, 0f),
        };
        flyer.AddChild(new MeshInstance3D
        {
            Name = "PapelCurvo",
            Mesh = _flyerWrapMesh,
            Scale = new Vector3(wrapRadius * 2f, bandHeight, wrapRadius * 2f),
            MaterialOverride = GetFlyerPaperMaterial(colorIndex),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        // O plano do texto é estreito e fica ligeiramente à frente da superfície curva.
        float labelWidth = Mathf.Clamp(wrapRadius * 1.75f, 0.23f, 0.34f);
        flyer.AddChild(new Label3D
        {
            Text = FlyerTexts[hash / 7 % FlyerTexts.Length],
            FontSize = 24,
            PixelSize = 0.0017f,
            Width = 220f,
            Modulate = new Color(0.08f, 0.07f, 0.06f),
            OutlineSize = 0,
            Shaded = false,
            DoubleSided = false,
            Position = new Vector3(0f, 0f, wrapRadius + 0.008f),
            Scale = new Vector3(labelWidth / 0.34f, 1f, 1f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        pole.AddChild(flyer);
    }
}
