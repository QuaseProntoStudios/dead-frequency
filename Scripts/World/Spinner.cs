// Scripts/World/Spinner.cs
// Gira o nó em volta de um eixo local (pás do cata-vento, ventiladores...).
using Godot;

namespace DeadFrequency;

public partial class Spinner : Node3D
{
    [Export] public Vector3 Axis { get; set; } = Vector3.Forward;
    /// <summary>Radianos por segundo.</summary>
    [Export] public float Speed { get; set; } = 1.5f;

    public override void _Process(double delta) => RotateObjectLocal(Axis.Normalized(), Speed * (float)delta);
}
