// Scripts/Audio/RadioInteractable.cs
// Rádio que dá para ligar e desligar com E (no escritório, no apartamento...).
using Godot;

namespace DeadFrequency;

public partial class RadioInteractable : StaticBody3D, IInteractable
{
    public RadioPlayer Radio { get; set; }

    public string GetInteractionPrompt(PlayerController player)
        => Radio == null ? "" : Radio.On ? $"[E] Desligar o rádio ({Radio.NowPlaying})" : $"[E] Ligar o rádio {RadioStation.Frequency} FM";

    public void Interact(PlayerController player)
    {
        if (Radio == null)
            return;
        Radio.Toggle();
        if (Radio.On)
            GameHud.Toast(this, "♪ " + Radio.NowPlaying);
    }
}
