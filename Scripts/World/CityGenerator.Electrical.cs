// Scripts/World/CityGenerator.Electrical.cs
// Rede de distribuição elétrica da área urbana comercial.
// Os postes e componentes repetidos usam MultiMesh; transformadores e cartazes são variações pontuais.
// Os três condutores existentes continuam sendo gerados por CityGenerator.PoleWires.cs.
using Godot;
using System.Collections.Generic;

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
        Vector3 poleA = center + new Vector3(edge, y, -BlockSize * 0.25f);
        Vector3 poleB = center + new Vector3(-BlockSize * 0.25f, y, edge);

        var root = new Node3D { Name = $"ElectricalPoles_{bx}_{bz}" };
        (_activeChunkRoot ?? _staticRoot).AddChild(root);

        var poleTransforms = new List<Transform3D>(2);
        var baseTransforms = new List<Transform3D>(2);
        var armTransforms = new List<Transform3D>(2);
        var insulatorTransforms = new List<Transform3D>(6);

        AddElectricalPoleVisuals(poleA, 0f, poleTransforms, baseTransforms, armTransforms, insulatorTransforms);
        AddElectricalPoleVisuals(poleB, Mathf.Pi * 0.5f, poleTransforms, baseTransforms, armTransforms, insulatorTransforms);

        // Geometria repetida em MultiMesh: 1 chamada por tipo de peça em cada chunk/quarteirão.
        AddElectricalMultiMesh(root, "ConcreteShafts", _unitCylinder, _concrete, poleTransforms);
        AddElectricalMultiMesh(root, "ConcreteBases", _unitCylinder, _concrete, baseTransforms);
        AddElectricalMultiMesh(root, "ElectricalCrossarms", _unitBox, _darkMetal, armTransforms);
        AddElectricalMultiMesh(root, "ElectricalInsulators", _unitCylinder, GetElectricalInsulatorMaterial(), insulatorTransforms);

        AddElectricalPoleDetails(root, poleA, Vector3.Right);
        AddElectricalPoleDetails(root, poleB, Vector3.Back);
    }

    private void AddElectricalPoleVisuals(
        Vector3 basePosition,
        float yaw,
        List<Transform3D> shafts,
        List<Transform3D> bases,
        List<Transform3D> arms,
        List<Transform3D> insulators)
    {
        if (IsKeepClearAt(basePosition))
            return;

        RegisterObstacle(basePosition);
        Basis rotation = new(Vector3.Up, yaw);
        shafts.Add(new Transform3D(
            rotation * Basis.FromScale(new Vector3(0.29f, ElectricalPoleHeight, 0.29f)),
            basePosition + new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f)));
        bases.Add(new Transform3D(
            rotation * Basis.FromScale(new Vector3(0.38f, 1.35f, 0.38f)),
            basePosition + new Vector3(0f, 0.72f, 0f)));
        arms.Add(new Transform3D(
            rotation * Basis.FromScale(new Vector3(1.15f, 0.12f, 0.16f)),
            basePosition + new Vector3(0f, ElectricalWireHeight, 0f)));

        foreach (float offset in new[] { -0.14f, 0f, 0.14f })
        {
            Vector3 position = basePosition + rotation * new Vector3(offset, ElectricalWireHeight + 0.13f, 0f);
            insulators.Add(new Transform3D(
                rotation * Basis.FromScale(new Vector3(0.11f, 0.22f, 0.11f)),
                position));
        }
    }

    private void AddElectricalMultiMesh(
        Node3D parent,
        string nodeName,
        Mesh mesh,
        Material material,
        List<Transform3D> transforms)
    {
        if (transforms.Count == 0)
            return;

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = transforms.Count,
        };
        for (int i = 0; i < transforms.Count; i++)
            multiMesh.SetInstanceTransform(i, transforms[i]);

        parent.AddChild(new MultiMeshInstance3D
        {
            Name = nodeName,
            Multimesh = multiMesh,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private void AddElectricalPoleDetails(Node3D root, Vector3 basePosition, Vector3 roadDirection)
    {
        if (IsKeepClearAt(basePosition))
            return;

        // A colisão continua individual, mas a geometria principal é compartilhada por MultiMesh.
        var body = new StaticBody3D
        {
            Name = "ConcreteUtilityPoleCollision",
            Position = basePosition,
            CollisionLayer = 1,
            CollisionMask = 0,
        };
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(0.34f, ElectricalPoleHeight, 0.34f) },
            Position = new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f),
        });
        root.AddChild(body);

        Node3D details = null;
        int hash = AssetProps.Hash(basePosition, 883);
        if (hash % 100 < 14)
        {
            details = new Node3D { Name = "TransformerDetails", Position = basePosition };
            AddElectricalTransformer(details, roadDirection == Vector3.Right ? Vector3.Right : Vector3.Back);
            root.AddChild(details);
        }

        if (AssetProps.Hash(basePosition, 71) % 100 < 65)
        {
            details ??= new Node3D { Name = "PoleDetails", Position = basePosition };
            if (details.GetParent() == null)
                root.AddChild(details);
            AddPoleFlyer(details, basePosition, roadDirection);
        }
    }

    private StandardMaterial3D GetElectricalInsulatorMaterial()
    {
        _electricalInsulatorMaterial ??= Flat(new Color(0.16f, 0.18f, 0.17f), 0.72f);
        return _electricalInsulatorMaterial;
    }

    private void AddElectricalTransformer(Node3D pole, Vector3 crossarmAxis)
    {
        _electricalTransformerMaterial ??= Flat(new Color(0.48f, 0.5f, 0.49f), 0.82f);
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

        foreach (float offset in new[] { -0.2f, 0.2f })
        {
            Vector3 p = axis * offset;
            AddCylinder(pole, new Vector3(p.X, 5.83f, p.Z), 0.055f, 0.2f,
                GetElectricalInsulatorMaterial());
        }
    }
}
