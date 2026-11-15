// Scripts/World/CityGenerator.FarmElectrical.cs
// Rede elétrica rural: postes de madeira, transformadores pontuais e fios acompanhando as estradas de terra.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private void BuildFarmElectricalNetwork()
    {
        if (Biome != WorldBiome.Farm || _farmRoads.Count == 0)
            return;

        var root = new Node3D { Name = "FarmElectricalNetwork" };
        _staticRoot.AddChild(root);

        var shafts = new List<Transform3D>();
        var crossarms = new List<Transform3D>();
        var insulators = new List<Transform3D>();
        var usedPositions = new List<Vector3>();
        var collision = new StaticBody3D
        {
            Name = "FarmElectricalPoleCollisions",
            CollisionLayer = 1,
            CollisionMask = 0,
        };
        root.AddChild(collision);

        var wireTool = new SurfaceTool();
        wireTool.Begin(Mesh.PrimitiveType.Triangles);
        wireTool.SetMaterial(GetPoleWireMaterial());
        bool hasWireSpans = false;
        int poleNumber = 0;

        for (int roadIndex = 0; roadIndex < _farmRoads.Count; roadIndex++)
        {
            FarmRoad road = _farmRoads[roadIndex];
            if (road.Width < 6f)
                continue;

            Vector2 direction = (road.B - road.A).Normalized();
            Vector2 normal = new(-direction.Y, direction.X);
            Vector3 along = new(direction.X, 0f, direction.Y);
            Vector3 across = new(normal.X, 0f, normal.Y);
            float length = road.A.DistanceTo(road.B);
            var roadPoles = new List<Vector3>();

            // Uma única fileira no acostamento, com espaçamento rural mais aberto.
            for (float distance = 32f; distance < length - 18f; distance += 42f)
            {
                Vector2 point = road.A + direction * distance + normal * (road.Width * 0.5f + 1.6f);
                if (!IsFree(point, 1.2f) || DistanceToRoads(point, roadIndex) < 2.2f)
                    continue;

                Vector3 basePosition = new(point.X, 0f, point.Y);
                bool tooClose = false;
                foreach (Vector3 existing in usedPositions)
                {
                    if (existing.DistanceTo(basePosition) < 4.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                usedPositions.Add(basePosition);
                roadPoles.Add(basePosition);

                float yaw = Mathf.Atan2(-across.Z, across.X);
                Basis rotation = new(Vector3.Up, yaw);
                shafts.Add(new Transform3D(
                    rotation * Basis.FromScale(new Vector3(1f, ElectricalPoleHeight, 1f)),
                    basePosition + new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f)));

                AddCrossarmTransform(crossarms, rotation, basePosition, ElectricalPrimaryArmHeight, 2.2f, 0.14f);
                AddCrossarmTransform(crossarms, rotation, basePosition, ElectricalSecondaryArmHeight, 1.25f, 0.12f);

                foreach (float offset in new[] { -0.62f, 0f, 0.62f })
                {
                    Vector3 insulatorPosition = basePosition + rotation * new Vector3(offset, ElectricalWireHeight - 0.18f, 0f);
                    insulators.Add(new Transform3D(
                        rotation * Basis.FromScale(new Vector3(0.14f, 0.32f, 0.14f)),
                        insulatorPosition));
                }
                foreach (float offset in new[] { -0.24f, 0.24f })
                {
                    Vector3 insulatorPosition = basePosition + rotation * new Vector3(offset, ElectricalSecondaryWireHeight - 0.12f, 0f);
                    insulators.Add(new Transform3D(
                        rotation * Basis.FromScale(new Vector3(0.11f, 0.22f, 0.11f)),
                        insulatorPosition));
                }

                collision.AddChild(new CollisionShape3D
                {
                    Shape = new CylinderShape3D { Radius = 0.22f, Height = ElectricalPoleHeight },
                    Position = basePosition + new Vector3(0f, ElectricalPoleHeight * 0.5f, 0f),
                });

                // Transformadores mais espaçados, sempre em postes de madeira no rancho.
                if (poleNumber % 8 == 3)
                {
                    var transformer = new Node3D
                    {
                        Name = "RuralTransformer",
                        Position = basePosition,
                    };
                    AddElectricalTransformer(transformer, across);
                    root.AddChild(transformer);
                }
                poleNumber++;
            }

            // Os cinco condutores acompanham o eixo da estrada e fazem barriga entre os postes.
            for (int i = 0; i < roadPoles.Count - 1; i++)
            {
                Vector3 startPole = roadPoles[i];
                Vector3 endPole = roadPoles[i + 1];
                if (startPole.DistanceTo(endPole) > 58f)
                    continue;

                float[] offsets = { -0.62f, 0f, 0.62f, -0.24f, 0.24f };
                float[] heights =
                {
                    ElectricalWireHeight, ElectricalWireHeight + 0.04f, ElectricalWireHeight,
                    ElectricalSecondaryWireHeight, ElectricalSecondaryWireHeight,
                };
                float[] sags = { 0.55f, 0.48f, 0.55f, 0.72f, 0.72f };

                for (int wire = 0; wire < offsets.Length; wire++)
                {
                    AppendFarmWire(wireTool,
                        startPole + across * offsets[wire] + Vector3.Up * heights[wire],
                        endPole + across * offsets[wire] + Vector3.Up * heights[wire],
                        sags[wire]);
                    hasWireSpans = true;
                }
            }
        }

        AddElectricalMultiMesh(root, "WeatheredWoodPoles", GetWoodUtilityPoleMesh(), GetWeatheredWoodPoleMaterial(), shafts);
        AddElectricalMultiMesh(root, "RuralWoodCrossarms", _unitBox, GetWeatheredWoodPoleMaterial(), crossarms);
        AddElectricalMultiMesh(root, "RuralInsulators", _unitCylinder, GetElectricalInsulatorMaterial(), insulators);

        if (hasWireSpans)
        {
            wireTool.GenerateNormals();
            root.AddChild(new MeshInstance3D
            {
                Name = "RuralOverheadWires",
                Mesh = wireTool.Commit(),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
    }

    private void AppendFarmWire(SurfaceTool wireTool, Vector3 start, Vector3 end, float sag)
    {
        const int segments = 12;
        Vector3 previous = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = start.Lerp(end, t);
            point.Y -= 4f * sag * t * (1f - t);
            Vector3 delta = point - previous;
            var wireSegment = new BoxMesh
            {
                Size = new Vector3(0.028f, 0.028f, delta.Length() + 0.02f),
            };
            Basis basis = Basis.LookingAt(delta.Normalized(), Vector3.Up);
            wireTool.AppendFrom(wireSegment, 0, new Transform3D(basis, (point + previous) * 0.5f));
            previous = point;
        }
    }
}
