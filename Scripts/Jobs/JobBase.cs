// Scripts/Jobs/JobBase.cs
// Base das profissões. O ProfessionManager cria uma por vez como filha dele.
using System;
using Godot;

namespace DeadFrequency;

public abstract partial class JobBase : Node3D
{
    protected ProfessionManager M { get; private set; }
    protected CityGenerator City => M.City;
    protected PlayerController Player => M.Player;
    protected readonly RandomNumberGenerator Rng = new();

    public void Setup(ProfessionManager manager)
    {
        M = manager;
        Rng.Randomize();
    }

    /// <summary>Linhas do painel do canto superior direito.</summary>
    public abstract string PanelText();

    /// <summary>Para onde a seta do topo da tela aponta agora (label curto em maiúsculas).</summary>
    public virtual bool TryGetTarget(out Vector3 position, out string label)
    {
        position = Vector3.Zero;
        label = "";
        return false;
    }

    /// <summary>Onde o jogador começa nesta profissão (posição, direção em rad). false = no depósito.</summary>
    public virtual bool TryGetSpawn(out Vector3 position, out float yaw)
    {
        position = Vector3.Zero;
        yaw = 0f;
        return false;
    }

    protected Vector3 PlayerPos => Player != null && IsInstanceValid(Player) ? Player.GlobalPosition : Vector3.Zero;

    /// <summary>Ponto de entrega aleatório (calçada) entre minDist e maxDist do ponto dado; se não houver, qualquer um.</summary>
    protected int RandomPoint(Vector3 from, float minDist, float maxDist, int avoid = -1)
    {
        if (City == null || City.DeliveryPoints.Count == 0)
            return -1;
        var list = new System.Collections.Generic.List<int>();
        for (int i = 0; i < City.DeliveryPoints.Count; i++)
        {
            float d = City.DeliveryPoints[i].Position.DistanceTo(from);
            if (i != avoid && d >= minDist && d <= maxDist)
                list.Add(i);
        }
        if (list.Count == 0)
            return Rng.RandiRange(0, City.DeliveryPoints.Count - 1);
        return list[Rng.RandiRange(0, list.Count - 1)];
    }
}
