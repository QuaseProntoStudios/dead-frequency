// Scripts/World/GrassTufts.cs
// Capim em pixel art (v0.9.7) para gramados da cidade e o sítio. Milhares de tufos desenhados com
// MultiMesh em pedaços de 16 m; cada pedaço some sozinho além de ~45 m (VisibilityRange), então a
// placa de vídeo só desenha o capim perto da câmera. Sorteio próprio por posição: não mexe no mapa.
using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public static class GrassTufts
{
    public const float Chunk = 16f;
    public const float Range = 45f;

    private static ArrayMesh _mesh;
    private static ShaderMaterial _material;

    /// <summary>Duas placas cruzadas de 0,5 x 0,34 m com a base no chão.</summary>
    private static ArrayMesh TuftMesh()
    {
        if (_mesh != null)
            return _mesh;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        const float w = 0.25f, h = 0.34f;
        foreach (Vector3 axis in new[] { Vector3.Right, Vector3.Back })
        {
            Vector3 a = -axis * w, b = axis * w, up = Vector3.Up * h;
            void V(Vector3 p, Vector2 uv) { st.SetUV(uv); st.SetNormal(Vector3.Up); st.AddVertex(p); }
            V(a, new Vector2(0, 1)); V(b, new Vector2(1, 1)); V(b + up, new Vector2(1, 0));
            V(a, new Vector2(0, 1)); V(b + up, new Vector2(1, 0)); V(a + up, new Vector2(0, 0));
        }
        _mesh = st.Commit();
        return _mesh;
    }

    private static ShaderMaterial Material()
    {
        if (_material != null)
            return _material;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/grass_tuft.gdshader") };
        _material.SetShaderParameter("tuft_tex", GD.Load<Texture2D>("res://Assets/Textures/City/grass_tuft.png"));
        return _material;
    }

    /// <summary>Espalha capim num retângulo (centro e tamanho em XZ, chão em y). spacing = distância média
    /// entre tufos; allow (opcional) recusa pontos (estrada, prédio, lago).</summary>
    public static void Fill(Node3D parent, Vector3 center, Vector2 size, float spacing, Color baseColor, Func<Vector2, bool> allow = null)
    {
        var groups = new Dictionary<Vector2I, List<Transform3D>>();
        var colors = new Dictionary<Vector2I, List<Color>>();
        int nx = Mathf.Max(1, (int)(size.X / spacing)), nz = Mathf.Max(1, (int)(size.Y / spacing));
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < nz; j++)
            {
                float x = center.X - size.X * 0.5f + (i + 0.5f) * spacing;
                float z = center.Z - size.Y * 0.5f + (j + 0.5f) * spacing;
                uint h = Hash(x, z);
                x += ((h & 0xff) / 255f - 0.5f) * spacing;
                z += (((h >> 8) & 0xff) / 255f - 0.5f) * spacing;
                if (allow != null && !allow(new Vector2(x, z)))
                    continue;
                float scale = 0.7f + ((h >> 16) & 0xff) / 255f * 0.7f;
                float rot = ((h >> 24) & 0xff) / 255f * Mathf.Tau;
                var t = new Transform3D(new Basis(Vector3.Up, rot) * Basis.FromScale(new Vector3(scale, scale * (0.8f + (h % 7) * 0.08f), scale)), new Vector3(x, center.Y, z));
                float k = 0.85f + (h % 13) / 13f * 0.35f;
                var c = new Vector2I(Mathf.FloorToInt(x / Chunk), Mathf.FloorToInt(z / Chunk));
                if (!groups.TryGetValue(c, out var list))
                {
                    groups[c] = list = new List<Transform3D>();
                    colors[c] = new List<Color>();
                }
                list.Add(t);
                colors[c].Add(new Color(baseColor.R * k, baseColor.G * k, baseColor.B * k));
            }
        }
        foreach ((Vector2I c, List<Transform3D> list) in groups)
        {
            var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = TuftMesh(), InstanceCount = list.Count };
            List<Color> cols = colors[c];
            for (int i = 0; i < list.Count; i++)
            {
                mm.SetInstanceTransform(i, list[i]);
                mm.SetInstanceColor(i, cols[i]);
            }
            parent.AddChild(new MultiMeshInstance3D
            {
                Name = $"Capim_{c.X}_{c.Y}",
                Multimesh = mm,
                MaterialOverride = Material(),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                VisibilityRangeEnd = Range,
                VisibilityRangeEndMargin = 6f,
                VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled,
            });
        }
    }

    private static uint Hash(float x, float z)
    {
        uint h = (uint)(Mathf.RoundToInt(x * 10f) * 73856093) ^ (uint)(Mathf.RoundToInt(z * 10f) * 19349663);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return h;
    }
}
