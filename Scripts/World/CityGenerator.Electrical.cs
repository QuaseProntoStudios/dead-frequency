// Scripts/World/CityGenerator.Electrical.cs
// Rede de distribuição elétrica da área urbana comercial.
// Os postes de concreto sustentam os três condutores já gerados por PoleWires;
// transformadores são distribuídos deterministicamente, sem alterar o RNG do mundo.
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private StandardMaterial3D _electricalTransformerMaterial;
    private StandardMaterial3D _electricalInsulatorMaterial;

    private const float ElectricalPoleHeight = 7.2f;
    private const float ElectricalWireHeight = 6.3f;

    private void BuildElectricalPoles()
    {
        if (_useLogicalBlockPlans)
            return; // A rede acompanha os chunks.

        for (int bx = -Radius; bx <= Radius; bx++)
        {
            for (int bz = -Radius; bz <= Radius; bz++)
                BuildElectricalPolesForBlock(bx, bz);
        }
    }

    private void BuildElectricalPolesForBlock(int bx, int bz)
    {
        float edge = BlockSize * 0.5f - 0.7f;
        Vector3 center = BlockCenter(bx, bz);
        float y = CurbHeight;

        // Poste A: condutores seguem pelo eixo Z; braço transversal no eixo X.
        BuildElectricalPoleIfClear(
            center + new Vector3(edge, y, -BlockSize * 0.25f),
            Vector3.Right,
            Vector3.Right);

        // Poste B: condutores seguem pelo eixo X; braço transversal no eixo Z.
        BuildElectricalPoleIfClear(
            center + new Vector3(-BlockSize * 0.25f, y, edge),
            Vector3.Back,
            Vector3.Back);
    }

    private void BuildElectricalPoleIfClear(Vector3 basePosition, Vector3 roadDirection, Vector3 crossarmAxis)
    {
        // A rede não deve ocupar rampas ou acessos de veículos.
        if (IsKeepClearAt(basePosition))
            return;

        RegisterObstacle(basePosition);

        var pole = new StaticBody3D
        {
            Name = "ConcreteUtilityPole",
            Position = basePosition,
            CollisionLayer = 1,
            CollisionMask = 0,
        };
        pole.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(0.34f, ElectricalPoleHeight, 0.34f) },
            Position = new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f),
        });

        // Fuste de concreto com base ligeiramente mais larga, em geometria low-poly.
        AddCylinder(pole, new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f), 0.145f,
            ElectricalPoleHeight, _concrete);
        AddCylinder(pole, new Vector3(0f, 0.72f, 0f), 0.19f, 1.35f, _concrete);

        StandardMaterial3D insulator = GetElectricalInsulatorMaterial();
        Vector3 axis = crossarmAxis.Normalized();
        Vector3 crossarmSize = Mathf.Abs(axis.X) > 0.5f
            ? new Vector3(1.15f, 0.12f, 0.16f)
            : new Vector3(0.16f, 0.12f, 1.15f);
        AddBox(pole, new Vector3(0f, ElectricalWireHeight, 0f), crossarmSize, _darkMetal, false);

        // Três isoladores alinhados aos três fios existentes.
        foreach (float offset in new[] { -0.14f, 0f, 0.14f })
        {
            Vector3 p = axis * offset;
            AddCylinder(pole, new Vector3(p.X, ElectricalWireHeight + 0.13f, p.Z),
                0.055f, 0.22f, insulator);
        }

        // Mãos-francesas simples reforçam a leitura do braço transversal.
        AddBox(pole, axis * 0.28f + new Vector3(0f, ElectricalWireHeight - 0.22f, 0f),
            new Vector3(Mathf.Abs(axis.X) > 0.5f ? 0.08f : 0.36f, 0.08f,
                Mathf.Abs(axis.Z) > 0.5f ? 0.08f : 0.36f), _darkMetal, false);

        int hash = AssetProps.Hash(basePosition, 883);
        if (hash % 100 < 14)
            AddElectricalTransformer(pole, axis);

        AddPoleFlyer(pole, basePosition, roadDirection);
        (_activeChunkRoot ?? _staticRoot).AddChild(pole);
    }

    private StandardMaterial3D GetElectricalInsulatorMaterial()
    {
        _electricalInsulatorMaterial ??= Flat(new Color(0.16f, 0.18f, 0.17f), 0.72f);
        return _electricalInsulatorMaterial;
    }

    private void AddElectricalTransformer(Node3D pole, Vector3 crossarmAxis)
    {
        _electricalTransformerMaterial ??= Flat(new Color(0.48f, 0.5f, 0.49f), 0.82f);

        // Transformador compacto preso abaixo do braço, com radiadores e buchas.
        AddBox(pole, new Vector3(0f, 5.35f, 0f),
            new Vector3(0.78f, 0.78f, 0.72f), _electricalTransformerMaterial, false);

        Vector3 axis = crossarmAxis.Normalized();
        for (int i = -1; i <= 1; i++)
        {
            Vector3 offset = axis * (i * 0.29f);
            AddBox(pole, offset + new Vector3(0f, 5.35f, 0f),
                new Vector3(Mathf.Abs(axis.X) > 0.5f ? 0.08f : 0.62f, 0.62f,
                    Mathf.Abs(axis.Z) > 0.5f ? 0.08f : 0.62f), _darkMetal, false);
        }

        foreach (float x in new[] { -0.2f, 0.2f })
        {
            AddCylinder(pole, new Vector3(x, 5.83f, 0f), 0.055f, 0.2f,
                GetElectricalInsulatorMaterial());
        }
    }
}
