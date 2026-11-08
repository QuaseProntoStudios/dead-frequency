// Scripts/Jobs/JobKit.cs
// Marcador das profissões: feixe colorido + anel + texto, visível no minimapa.
using System;
using Godot;

namespace DeadFrequency;

public static class JobMarkers
{
    private static Shader _beam;

    /// <summary>Feixe de luz colorido + anel no chão + texto. Aparece no minimapa.</summary>
    public static Node3D Create(Node parent, Vector3 worldPos, Color color, string text, float beamHeight = 45f, float radius = 1.1f)
    {
        _beam ??= GD.Load<Shader>("res://Shaders/beacon_beam.gdshader");
        var marker = new Node3D { Name = "JobMarker" };
        parent.AddChild(marker);
        marker.GlobalPosition = worldPos;
        if (beamHeight > 0f)
        {
            var mat = new ShaderMaterial { Shader = _beam };
            mat.SetShaderParameter("beam_color", new Vector3(color.R, color.G, color.B));
            marker.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f, RadialSegments = 10, Rings = 1 },
                MaterialOverride = mat,
                Scale = new Vector3(radius * 2f, beamHeight, radius * 2f),
                Position = new Vector3(0f, beamHeight * 0.5f, 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
        marker.AddChild(new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = radius * 1.6f, OuterRadius = radius * 1.8f, Rings = 20, RingSegments = 4 },
            MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = color },
            Position = new Vector3(0f, 0.06f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        marker.AddChild(new Label3D
        {
            Name = "Text",
            Text = text,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 56,
            PixelSize = 0.008f,
            Modulate = color,
            OutlineSize = 10,
            NoDepthTest = true,
            Position = new Vector3(0f, 3.2f, 0f),
        });
        marker.AddToGroup(MinimapView.MarkerGroup);
        marker.SetMeta("map_color", color);
        marker.SetMeta("map_kind", "job");
        return marker;
    }

    public static void SetText(Node3D marker, string text)
    {
        if (marker != null && GodotObject.IsInstanceValid(marker) && marker.GetNodeOrNull<Label3D>("Text") is Label3D label)
            label.Text = text;
    }

    public static void Free(ref Node3D marker)
    {
        if (marker != null && GodotObject.IsInstanceValid(marker))
            marker.QueueFree();
        marker = null;
    }
}
