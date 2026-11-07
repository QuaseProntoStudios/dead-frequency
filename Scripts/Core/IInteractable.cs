// Scripts/Core/IInteractable.cs
// Contrato único para tudo que o jogador pode usar no mundo:
// botões do painel, banco do motorista, fusíveis, portas, caixas de carga...
using Godot;

namespace DeadFrequency;

public interface IInteractable
{
    /// <summary>
    /// Texto curto exibido quando o jogador mira no objeto (ex.: "Pegar Fitas Magnéticas (8 kg)").
    /// Recebe o player para poder variar o texto conforme o contexto (ex.: "Pesado demais").
    /// </summary>
    string GetInteractionPrompt(PlayerController player);

    /// <summary>Executa a ação. Chamado quando o jogador aperta "interact" mirando no objeto.</summary>
    void Interact(PlayerController player);

    /// <summary>
    /// Se false, o objeto ainda mostra o prompt, mas Interact não é chamado.
    /// Implementação padrão (C# 8+): sempre pode interagir.
    /// </summary>
    bool CanInteract(PlayerController player) => true;
}
