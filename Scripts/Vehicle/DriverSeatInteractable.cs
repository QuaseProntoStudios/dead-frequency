// Scripts/Vehicle/DriverSeatInteractable.cs
// Área na porta do motorista. Mirar nela e apertar E senta o jogador no banco.
using Godot;

namespace DeadFrequency;

public partial class DriverSeatInteractable : Area3D, IInteractable
{
    [Export] public VanController Van { get; set; }
    [Export] public Node3D SeatMarker { get; set; }
    [Export] public Node3D ExitMarker { get; set; }

    public override void _Ready()
    {
        if (Van == null || SeatMarker == null || ExitMarker == null)
            GD.PushWarning($"{Name}: preencha Van, SeatMarker e ExitMarker no Inspector.");
    }

    public string GetInteractionPrompt(PlayerController player)
    {
        if (player.State != PlayerState.Walking)
            return "";
        return Van != null && Van.HasRemoteDriver ? "Outro entregador está dirigindo" : "[E] Dirigir a van";
    }

    public bool CanInteract(PlayerController player) =>
        player.State == PlayerState.Walking && !player.IsHolding && Van != null && SeatMarker != null && !Van.HasRemoteDriver;

    public void Interact(PlayerController player) => player.EnterVehicle(Van, SeatMarker, ExitMarker);
}
