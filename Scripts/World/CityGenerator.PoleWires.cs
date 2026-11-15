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
    private readonly Dictionary<int, ArrayMesh> _flyerWrapMeshes = new();
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
    private ArrayMesh GetFlyerWrapMesh(float poleRadius, float bandHeight)
    {
        int key = Mathf.RoundToInt(poleRadius * 1000f);
        if (_flyerWrapMeshes.TryGetValue(key, out ArrayMesh cached))
            return cached;

        float wrapRadius = poleRadius + 0.006f;
        float paperWidth = Mathf.Clamp(poleRadius * 1.8f, 0.16f, 0.29f);
        float arc = Mathf.Clamp(paperWidth / wrapRadius, 1.15f, 1.9f);
        float halfArc = arc * 0.5f;
        float halfHeight = bandHeight * 0.5f;

        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        const int segments = 20;

        for (int i = 0; i < segments; i++)
        {
            float u0 = i / (float)segments;
            float u1 = (i + 1) / (float)segments;
            float a0 = Mathf.Lerp(-halfArc, halfArc, u0);
            float a1 = Mathf.Lerp(-halfArc, halfArc, u1);
            Vector3 n0 = new(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
            Vector3 n1 = new(Mathf.Sin(a1), 0f, Mathf.Cos(a1));

            float bottom0 = -halfHeight + Mathf.Sin(a0 * 4f + 0.7f) * 0.003f;
            float bottom1 = -halfHeight + Mathf.Sin(a1 * 4f + 0.7f) * 0.003f;
            float top0 = halfHeight + Mathf.Sin(a0 * 3f + 0.3f) * 0.004f;
            float top1 = halfHeight + Mathf.Sin(a1 * 3f + 0.3f) * 0.004f;

            Vector3 pBottom0 = new(n0.X * wrapRadius, bottom0, n0.Z * wrapRadius);
            Vector3 pBottom1 = new(n1.X * wrapRadius, bottom1, n1.Z * wrapRadius);
            Vector3 pTop0 = new(n0.X * wrapRadius, top0, n0.Z * wrapRadius);
            Vector3 pTop1 = new(n1.X * wrapRadius, top1, n1.Z * wrapRadius);

            AddFlyerVertex(surface, pBottom0, n0, new Vector2(u0, 0f));
            AddFlyerVertex(surface, pBottom1, n1, new Vector2(u1, 0f));
            AddFlyerVertex(surface, pTop0, n0, new Vector2(u0, 1f));

            AddFlyerVertex(surface, pBottom1, n1, new Vector2(u1, 0f));
            AddFlyerVertex(surface, pTop1, n1, new Vector2(u1, 1f));
            AddFlyerVertex(surface, pTop0, n0, new Vector2(u0, 1f));
        }

        ArrayMesh mesh = surface.Commit();
        _flyerWrapMeshes[key] = mesh;
        return mesh;
    }

    private static void AddFlyerVertex(SurfaceTool surface, Vector3 position, Vector3 normal, Vector2 uv)
    {
        surface.SetNormal(normal);
        surface.SetUV(uv);
        surface.AddVertex(position);
    }

    private void AddCurvedFlyerText(Node3D flyer, string text, float wrapRadius, float poleRadius, float bandHeight)
    {
        string[] lines = text.Split('\n');
        int maxChars = 1;
        foreach (string line in lines)
            maxChars = Mathf.Max(maxChars, line.Length);

        float paperWidth = Mathf.Clamp(poleRadius * 1.8f, 0.16f, 0.29f);
        float availableWidth = paperWidth - 0.035f;
        float glyphStep = Mathf.Min(0.023f, availableWidth / maxChars);
        float textRadius = wrapRadius + 0.0025f;
        float rowStep = bandHeight / (lines.Length + 1f);

        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row];
            float lineSpan = Mathf.Max(0, line.Length - 1) * glyphStep;
            float y = bandHeight * 0.5f - rowStep * (row + 1);

            for (int i = 0; i < line.Length; i++)
            {
                float alongArc = i * glyphStep - lineSpan * 0.5f;
                float angle = alongArc / textRadius;
                var glyph = new Label3D
                {
                    Text = line[i].ToString(),
                    FontSize = 18,
                    PixelSize = 0.00115f,
                    Width = 18f,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Modulate = new Color(0.08f, 0.07f, 0.06f),
                    OutlineSize = 0,
                    Shaded = false,
                    DoubleSided = false,
                    Position = new Vector3(
                        Mathf.Sin(angle) * textRadius,
                        y,
                        Mathf.Cos(angle) * textRadius),
                    Rotation = new Vector3(0f, angle, 0f),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                flyer.AddChild(glyph);
            }
        }
    }

    /// <summary>Folha de papel estreita, parcialmente curvada e encostada ao poste; as letras acompanham a curva.</summary>
    private void AddPoleFlyer(Node3D pole, Vector3 basePos, Vector3 towardRoad, float poleRadius)
    {
        int hash = AssetProps.Hash(basePos, 71);
        if (hash % 100 >= 65)
            return;

        int colorIndex = hash / 100 % FlyerColors.Length;
        float bandHeight = 0.50f;
        float wrapRadius = poleRadius + 0.006f;
        bool streetAlongX = Mathf.Abs(towardRoad.Z) > 0.5f;
        float yaw = streetAlongX ? Mathf.Pi * 0.5f : 0f;
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
            Mesh = GetFlyerWrapMesh(poleRadius, bandHeight),
            MaterialOverride = GetFlyerPaperMaterial(colorIndex),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        AddCurvedFlyerText(
            flyer,
            FlyerTexts[hash / 7 % FlyerTexts.Length],
            wrapRadius,
            poleRadius,
            bandHeight);
        pole.AddChild(flyer);
    }
}
