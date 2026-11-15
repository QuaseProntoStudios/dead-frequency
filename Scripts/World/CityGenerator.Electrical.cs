// Scripts/World/CityGenerator.Electrical.cs
// Rede de distribuição elétrica urbana e componentes visuais dos postes.
using Godot;
using System.Collections.Generic;

namespace DeadFrequency;

public partial class CityGenerator
{
    private StandardMaterial3D _electricalTransformerMaterial;
    private StandardMaterial3D _electricalInsulatorMaterial;
    private ShaderMaterial _weatheredWoodPoleMaterial;
    private CylinderMesh _woodUtilityPoleMesh;

    private const float ElectricalPoleHeight = 8f;
    private const float ElectricalWireHeight = 7.22f;
    private const float ElectricalSecondaryWireHeight = 6.57f;
    private const float ElectricalPrimaryArmHeight = 6.84f;
    private const float ElectricalSecondaryArmHeight = 6.25f;

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

        var concreteShafts = new List<Transform3D>(2);
        var concreteBases = new List<Transform3D>(2);
        var concreteArms = new List<Transform3D>(4);
        var woodShafts = new List<Transform3D>(2);
        var woodArms = new List<Transform3D>(4);
        var insulatorTransforms = new List<Transform3D>(10);

        bool woodA = ShouldUseWoodenCityPole(bx, bz, poleA);
        bool woodB = ShouldUseWoodenCityPole(bx, bz, poleB);
        AddElectricalPoleVisuals(poleA, 0f, woodA,
            concreteShafts, concreteBases, concreteArms, woodShafts, woodArms, insulatorTransforms);
        AddElectricalPoleVisuals(poleB, Mathf.Pi * 0.5f, woodB,
            concreteShafts, concreteBases, concreteArms, woodShafts, woodArms, insulatorTransforms);

        AddElectricalMultiMesh(root, "ConcreteShafts", _unitCylinder, _concrete, concreteShafts);
        AddElectricalMultiMesh(root, "ConcreteBases", _unitCylinder, _concrete, concreteBases);
        AddElectricalMultiMesh(root, "ConcreteCrossarms", _unitBox, _darkMetal, concreteArms);
        AddElectricalMultiMesh(root, "WoodenShafts", GetWoodUtilityPoleMesh(), GetWeatheredWoodPoleMaterial(), woodShafts);
        AddElectricalMultiMesh(root, "WoodenCrossarms", _unitBox, GetWeatheredWoodPoleMaterial(), woodArms);
        AddElectricalMultiMesh(root, "ElectricalInsulators", _unitCylinder, GetElectricalInsulatorMaterial(), insulatorTransforms);

        // Um transformador por aproximadamente cinco quarteirões, em posição fixa na malha.
        // Se a posição preferencial estiver bloqueada por uma garagem, usa o outro poste do bloco.
        bool transformerBlock = Mathf.PosMod(bx * 2 + bz * 3, 5) == 0;
        bool transformerA = false;
        bool transformerB = false;
        if (transformerBlock)
        {
            bool preferA = AssetProps.Hash(poleA, 883) % 2 == 0;
            if (preferA && !IsKeepClearAt(poleA))
                transformerA = true;
            else if (!IsKeepClearAt(poleB))
                transformerB = true;
            else if (!IsKeepClearAt(poleA))
                transformerA = true;
        }

        AddElectricalPoleDetails(root, poleA, Vector3.Right, woodA, transformerA);
        AddElectricalPoleDetails(root, poleB, Vector3.Back, woodB, transformerB);
    }

    private bool ShouldUseWoodenCityPole(int bx, int bz, Vector3 position)
    {
        // Postes antigos de madeira aparecem mais nas bordas e em trechos menos centrais.
        int ring = Mathf.Max(Mathf.Abs(bx), Mathf.Abs(bz));
        int outerRing = Radius > 1 ? Radius - 1 : 1;
        int chance = ring >= outerRing ? 24 : 6;
        return AssetProps.Hash(position, 887) % 100 < chance;
    }

    private void AddElectricalPoleVisuals(
        Vector3 basePosition,
        float yaw,
        bool wooden,
        List<Transform3D> concreteShafts,
        List<Transform3D> concreteBases,
        List<Transform3D> concreteArms,
        List<Transform3D> woodShafts,
        List<Transform3D> woodArms,
        List<Transform3D> insulators)
    {
        if (IsKeepClearAt(basePosition))
            return;

        RegisterObstacle(basePosition);
        Basis rotation = new(Vector3.Up, yaw);
        Vector3 poleCenter = basePosition + new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f);

        if (wooden)
        {
            // Tronco cônico de madeira, sem sapata de concreto.
            woodShafts.Add(new Transform3D(
                rotation * Basis.FromScale(new Vector3(1f, ElectricalPoleHeight, 1f)),
                poleCenter));
            AddCrossarmTransform(woodArms, rotation, basePosition, ElectricalPrimaryArmHeight, 2.2f, 0.14f);
            AddCrossarmTransform(woodArms, rotation, basePosition, ElectricalSecondaryArmHeight, 1.25f, 0.12f);
        }
        else
        {
            concreteShafts.Add(new Transform3D(
                rotation * Basis.FromScale(new Vector3(0.29f, ElectricalPoleHeight, 0.29f)),
                poleCenter));
            concreteBases.Add(new Transform3D(
                rotation * Basis.FromScale(new Vector3(0.38f, 1.35f, 0.38f)),
                basePosition + new Vector3(0f, 0.72f, 0f)));
            AddCrossarmTransform(concreteArms, rotation, basePosition, ElectricalPrimaryArmHeight, 2.2f, 0.14f);
            AddCrossarmTransform(concreteArms, rotation, basePosition, ElectricalSecondaryArmHeight, 1.25f, 0.12f);
        }

        // Três isoladores no braço superior e dois no braço inferior.
        foreach (float offset in new[] { -0.62f, 0f, 0.62f })
        {
            Vector3 position = basePosition + rotation * new Vector3(offset, ElectricalWireHeight - 0.18f, 0f);
            insulators.Add(new Transform3D(
                rotation * Basis.FromScale(new Vector3(0.14f, 0.32f, 0.14f)),
                position));
        }

        foreach (float offset in new[] { -0.24f, 0.24f })
        {
            Vector3 position = basePosition + rotation * new Vector3(offset, ElectricalSecondaryWireHeight - 0.12f, 0f);
            insulators.Add(new Transform3D(
                rotation * Basis.FromScale(new Vector3(0.11f, 0.22f, 0.11f)),
                position));
        }
    }

    private void AddCrossarmTransform(
        List<Transform3D> destination,
        Basis rotation,
        Vector3 basePosition,
        float height,
        float width,
        float depth)
    {
        destination.Add(new Transform3D(
            rotation * Basis.FromScale(new Vector3(width, 0.12f, depth)),
            basePosition + new Vector3(0f, height, 0f)));
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

    private void AddElectricalPoleDetails(
        Node3D root,
        Vector3 basePosition,
        Vector3 roadDirection,
        bool wooden,
        bool hasTransformer)
    {
        if (IsKeepClearAt(basePosition))
            return;

        float poleWidth = wooden ? 0.42f : 0.34f;
        var body = new StaticBody3D
        {
            Name = wooden ? "WoodUtilityPoleCollision" : "ConcreteUtilityPoleCollision",
            Position = basePosition,
            CollisionLayer = 1,
            CollisionMask = 0,
        };
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(poleWidth, ElectricalPoleHeight, poleWidth) },
            Position = new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f),
        });
        root.AddChild(body);

        int hash = AssetProps.Hash(basePosition, 71);
        bool hasFlyer = hash % 100 < 65;
        if (!hasTransformer && !hasFlyer)
            return;

        var details = new Node3D
        {
            Name = "PoleDetails",
            Position = basePosition,
        };
        if (hasTransformer)
            AddElectricalTransformer(details, roadDirection);
        if (hasFlyer)
            AddPoleFlyer(details, basePosition, roadDirection, wooden ? 0.235f : 0.16f);
        root.AddChild(details);
    }

    private StandardMaterial3D GetElectricalInsulatorMaterial()
    {
        _electricalInsulatorMaterial ??= Flat(new Color(0.18f, 0.2f, 0.19f), 0.78f);
        return _electricalInsulatorMaterial;
    }

    private ShaderMaterial GetWeatheredWoodPoleMaterial()
    {
        _weatheredWoodPoleMaterial ??= new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://Shaders/weathered_wood_pole.gdshader"),
        };
        return _weatheredWoodPoleMaterial;
    }

    private CylinderMesh GetWoodUtilityPoleMesh()
    {
        _woodUtilityPoleMesh ??= new CylinderMesh
        {
            TopRadius = 0.18f,
            BottomRadius = 0.25f,
            Height = 1f,
            RadialSegments = 8,
            CapTop = true,
            CapBottom = true,
        };
        return _woodUtilityPoleMesh;
    }

    private void AddElectricalTransformer(Node3D pole, Vector3 crossarmAxis)
    {
        _electricalTransformerMaterial ??= Flat(new Color(0.52f, 0.54f, 0.53f), 0.9f);
        Vector3 axis = crossarmAxis.Normalized();
        Vector3 tankCenter = axis * 0.5f + new Vector3(0f, 5.48f, 0f);

        var tankMesh = new CylinderMesh
        {
            TopRadius = 0.34f,
            BottomRadius = 0.34f,
            Height = 0.92f,
            RadialSegments = 10,
            CapTop = true,
            CapBottom = true,
        };
        pole.AddChild(new MeshInstance3D
        {
            Name = "TransformerTank",
            Mesh = tankMesh,
            Position = tankCenter,
            MaterialOverride = _electricalTransformerMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        // Suporte metálico e duas cintas deixam o transformador claramente separado do poste.
        Basis bracketRotation = new Basis(Vector3.Up, Mathf.Atan2(-axis.Z, axis.X));
        pole.AddChild(new MeshInstance3D
        {
            Name = "TransformerBracket",
            Mesh = _unitBox,
            Transform = new Transform3D(
                bracketRotation * Basis.FromScale(new Vector3(0.96f, 0.12f, 0.16f)),
                axis * 0.42f + new Vector3(0f, 5.0f, 0f)),
            MaterialOverride = _darkMetal,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        var bandMesh = new CylinderMesh
        {
            TopRadius = 0.355f,
            BottomRadius = 0.355f,
            Height = 0.055f,
            RadialSegments = 10,
            CapTop = true,
            CapBottom = true,
        };
        foreach (float offset in new[] { -0.25f, 0.25f })
        {
            pole.AddChild(new MeshInstance3D
            {
                Mesh = bandMesh,
                Position = tankCenter + new Vector3(0f, offset, 0f),
                MaterialOverride = _darkMetal,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }

        var bushingMesh = new CylinderMesh
        {
            TopRadius = 0.075f,
            BottomRadius = 0.09f,
            Height = 0.2f,
            RadialSegments = 8,
            CapTop = true,
            CapBottom = true,
        };
        foreach (float offset in new[] { -0.17f, 0f, 0.17f })
        {
            pole.AddChild(new MeshInstance3D
            {
                Mesh = bushingMesh,
                Position = tankCenter + axis * offset + new Vector3(0f, 0.55f, 0f),
                MaterialOverride = GetElectricalInsulatorMaterial(),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
    }
}
