// Scripts/Core/HomeGuide.cs
// Endereço de casa do jogador (o apartamento que ele ganha ao entrar) e o "Marcar casa" do inventário:
// com ele ligado, a seta do topo da tela aponta para casa até você chegar.
using Godot;

namespace DeadFrequency;

public static class HomeGuide
{
    /// <summary>Seta apontando para casa (botão "Marcar casa" no inventário).</summary>
    public static bool Active { get; set; }
    public static bool Available { get; private set; }
    public static Vector3 Position { get; private set; }
    public static string Address { get; private set; } = "";

    public static void Set(Vector3 position, string address)
    {
        Position = position;
        Address = address;
        Available = true;
    }

    public static void Clear()
    {
        Available = false;
        Active = false;
        Address = "";
    }
}
