// Scripts/Cargo/CargoData.cs
// Dados de uma encomenda, separados da lógica. Crie variações no editor:
// FileSystem > botão direito > New Resource > CargoData > salvar como .tres
// (os arquivos .tzn ficam para save/manifesto no Módulo 4).
using Godot;

namespace DeadFrequency;

[GlobalClass]
public partial class CargoData : Resource
{
    [Export] public string CargoId { get; set; } = "cargo_generic";

    [Export] public string CargoName { get; set; } = "Caixa de Suprimentos";

    /// <summary>Peso real. Vira a massa do RigidBody3D e afeta a suspensão da van.</summary>
    [Export(PropertyHint.Range, "0.5,200,0.5,suffix:kg")]
    public float WeightKg { get; set; } = 10f;

    /// <summary>0 = indestrutível, 1 = vidro. Multiplica o dano de impacto.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Fragility { get; set; } = 0.3f;

    /// <summary>0 = nada, 1 = monitor CRT e rádio enlouquecem. Usado no Módulo 4/5.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float EmInterference { get; set; } = 0f;

    /// <summary>Pagamento com integridade 100%.</summary>
    [Export(PropertyHint.Range, "0,10000,1")]
    public int BasePayment { get; set; } = 100;
}
