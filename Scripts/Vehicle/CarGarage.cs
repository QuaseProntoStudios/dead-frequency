// Scripts/Vehicle/CarGarage.cs
// Carros comprados na concessionária (v0.9.7). Solo: guardados no inventory.json do jogador.
// Online: são da sala (comprados com o dinheiro do grupo), o servidor guarda e manda no "welcome".
// Quando um carro é comprado, a concessionária troca o carro de vitrine por um dirigível.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DeadFrequency;

public static class CarGarage
{
    public readonly record struct Model(string Id, CarType Type, Color Color);

    /// <summary>Mesma ordem das vagas do pátio da concessionária.</summary>
    public static readonly Model[] ForSale =
    {
        new(ItemCatalog.CarGou, CarType.Hatch, new Color(0.2f, 0.38f, 0.8f)),
        new(ItemCatalog.CarFusca, CarType.Fusca, new Color(0.88f, 0.84f, 0.66f)),
        new(ItemCatalog.CarOpalinha, CarType.Sedan, new Color(0.5f, 0.08f, 0.1f)),
    };

    private static readonly HashSet<string> Online = new();
    private static bool _online;

    /// <summary>Um carro passou a ser da sala/jogador (comprado agora ou veio no welcome).</summary>
    public static event Action<string> Bought;

    public static bool Owns(string id) => _online ? Online.Contains(id) : PlayerInventory.Instance?.Cars.Contains(id) == true;

    public static Model? Find(string id)
    {
        foreach (Model m in ForSale)
            if (m.Id == id)
                return m;
        return null;
    }

    /// <summary>Entrou numa sala: a lista passa a ser a do servidor.</summary>
    public static void SetOnline(IEnumerable<string> ids)
    {
        _online = true;
        Online.Clear();
        foreach (string id in ids)
            MarkBought(id);
    }

    public static void SetOffline() => _online = false;

    public static void MarkBought(string id)
    {
        if (Find(id) == null)
            return;
        if (_online)
        {
            if (!Online.Add(id))
                return;
        }
        else
        {
            if (PlayerInventory.Instance == null || PlayerInventory.Instance.Cars.Contains(id))
                return;
            PlayerInventory.Instance.AddCar(id);
        }
        Bought?.Invoke(id);
    }
}
