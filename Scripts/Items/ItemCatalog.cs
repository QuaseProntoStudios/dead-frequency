// Scripts/Items/ItemCatalog.cs
// Todos os itens do jogo. Os preços precisam bater com SHOP_PRICES em server/src/index.ts
// (o servidor confere o preço antes de descontar o dinheiro do grupo).
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public enum ItemSlot
{
    None,   // consumível: fica na mochila até usar
    Head,   // cabeça
    Hands,  // mãos
    Back,   // costas
    Wrist,  // pulso
}

public sealed class ItemDef
{
    public string Id = "";
    public string Name = "";
    public string Description = "";
    public int Price;
    public ItemSlot Slot;
    public Color Color = Colors.White;

    public bool IsGear => Slot != ItemSlot.None;
}

public static class ItemCatalog
{
    public const string Radar = "radar";
    public const string NightVision = "nightvision";
    public const string Gloves = "gloves";
    public const string Backpack = "backpack";
    public const string RepairKit = "repairkit";
    public const string DuctTape = "tape";
    public const string EnergyDrink = "energy";

    public static readonly IReadOnlyList<ItemDef> All = new[]
    {
        new ItemDef { Id = Radar, Name = "Radar de pulso", Price = 400, Slot = ItemSlot.Wrist, Color = new Color(0.3f, 0.9f, 0.45f),
            Description = "Mostra um minimapa com ruas, entregas, o depósito, a barraca e o ônibus." },
        new ItemDef { Id = NightVision, Name = "Óculos de visão noturna", Price = 350, Slot = ItemSlot.Head, Color = new Color(0.2f, 0.75f, 0.3f),
            Description = "Aperte N para ligar. Enxerga no escuro (tudo fica verde)." },
        new ItemDef { Id = Gloves, Name = "Luvas reforçadas", Price = 250, Slot = ItemSlot.Hands, Color = new Color(0.95f, 0.55f, 0.15f),
            Description = "Carrega até 60 kg, arremessa mais forte e a caixa na mão sofre 60% menos dano." },
        new ItemDef { Id = Backpack, Name = "Mochila de entregador", Price = 200, Slot = ItemSlot.Back, Color = new Color(0.85f, 0.2f, 0.15f),
            Description = "Aumenta o inventário de 4 para 10 espaços." },
        new ItemDef { Id = RepairKit, Name = "Kit de reparo", Price = 150, Slot = ItemSlot.None, Color = new Color(0.9f, 0.9f, 0.9f),
            Description = "Usar segurando uma caixa: recupera 40% da integridade." },
        new ItemDef { Id = DuctTape, Name = "Fita adesiva", Price = 50, Slot = ItemSlot.None, Color = new Color(0.6f, 0.6f, 0.65f),
            Description = "Usar segurando uma caixa: recupera 20% da integridade." },
        new ItemDef { Id = EnergyDrink, Name = "Energético Frequência", Price = 60, Slot = ItemSlot.None, Color = new Color(0.2f, 0.6f, 1f),
            Description = "Corre 35% mais rápido por 60 segundos." },
    };

    private static Dictionary<string, ItemDef> _byId;

    public static ItemDef Get(string id)
    {
        _byId ??= BuildIndex();
        return id != null && _byId.TryGetValue(id, out ItemDef def) ? def : null;
    }

    private static Dictionary<string, ItemDef> BuildIndex()
    {
        var index = new Dictionary<string, ItemDef>();
        foreach (ItemDef def in All)
            index[def.Id] = def;
        return index;
    }

    public static string SlotName(ItemSlot slot) => slot switch
    {
        ItemSlot.Head => "Cabeça",
        ItemSlot.Hands => "Mãos",
        ItemSlot.Back => "Costas",
        ItemSlot.Wrist => "Pulso",
        _ => "Consumível",
    };
}
