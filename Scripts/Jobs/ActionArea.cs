// Scripts/Jobs/ActionArea.cs
// Área genérica que o jogador mira e aperta E (sacos de lixo, caçambas, prateleiras, elevador...).
using System;
using Godot;

namespace DeadFrequency;

/// <summary>Área que o jogador mira e aperta E. O texto e a ação vêm de funções (sem criar uma classe por objeto).</summary>
public partial class ActionArea : Area3D, IInteractable
{
    public Func<PlayerController, string> Prompt { get; set; }
    public Action<PlayerController> Action { get; set; }
    public Func<PlayerController, bool> Can { get; set; }

    public string GetInteractionPrompt(PlayerController player) => Prompt?.Invoke(player) ?? "";
    public void Interact(PlayerController player) => Action?.Invoke(player);
    public bool CanInteract(PlayerController player) => Can?.Invoke(player) ?? true;

    public static ActionArea Create(Node3D parent, Vector3 position, Vector3 size, Func<PlayerController, string> prompt, Action<PlayerController> action, Func<PlayerController, bool> can = null)
    {
        var area = new ActionArea
        {
            Prompt = prompt,
            Action = action,
            Can = can,
            CollisionLayer = 4, // Interactables
            CollisionMask = 0,
            Monitoring = false,
            Position = position,
        };
        area.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(area);
        return area;
    }
}
