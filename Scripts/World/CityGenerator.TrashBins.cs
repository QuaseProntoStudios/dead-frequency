// Scripts/World/CityGenerator.TrashBins.cs
// Fase 1 · Cidade (ambientação Brazilcore): lixeiras de grade nas calçadas, aquelas cestas de ferro suspensas em dois
// postes com o saco preto aparecendo por cima. Uma malha só (feita uma vez de primitivas) compartilhada por todas.
// Escolha por AssetProps.Hash do bloco: não mexe nos sorteios da cidade (layout igual para todos no online).
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private ArrayMesh _trashBinMesh;
    private CylinderShape3D _trashBinShape;

    private void BuildTrashBins()
    {
        if (_useLogicalBlockPlans)
            return; // As lixeiras acompanham o chunk.

        var root = new Node3D { Name = "TrashBins" };
        _staticRoot.AddChild(root);
        for (int bx = -Radius; bx <= Radius; bx++)
        {
            for (int bz = -Radius; bz <= Radius; bz++)
                AddTrashBinsForBlock(root, bx, bz);
        }
    }

    private void BuildTrashBinsForBlock(int bx, int bz)
    {
        var root = new Node3D { Name = $"TrashBins_{bx}_{bz}" };
        (_activeChunkRoot ?? _staticRoot).AddChild(root);
        AddTrashBinsForBlock(root, bx, bz);
    }

    private void AddTrashBinsForBlock(Node3D root, int bx, int bz)
    {
        if (bx == 0 && bz == 0)
            return; // Quarteirão do depósito tem projeto próprio.
        Vector3 c = BlockCenter(bx, bz) + new Vector3(0f, CurbHeight, 0f);
        int hash = AssetProps.Hash(c, 67);
        float edge = BlockSize * 0.5f - 0.6f;
        // Calçada leste (+X), de frente para a rua; bancos ficam em +Z e caçambas em -X.
        if (hash % 100 < 60)
            SpawnTrashBin(root, c + new Vector3(edge, 0f, (hash / 100 % 9 - 4) * 2.4f), -Mathf.Pi * 0.5f);
        // Segunda lixeira na calçada norte (-Z), em alguns blocos.
        if (hash / 1000 % 100 < 30)
            SpawnTrashBin(root, c + new Vector3((hash / 100000 % 9 - 4) * 2.4f, 0f, -edge), Mathf.Pi);
    }

    private void SpawnTrashBin(Node3D root, Vector3 position, float rotY)
    {
        EnsureTrashBinMesh();
        RegisterObstacle(position);
        var body = new StaticBody3D { Name = "LixeiraDeGrade", Position = position, Rotation = new Vector3(0f, rotY, 0f), CollisionLayer = 1, CollisionMask = 0 };
        body.AddChild(new CollisionShape3D { Shape = _trashBinShape, Position = new Vector3(0f, 0.55f, 0f) });
        body.AddChild(new MeshInstance3D { Mesh = _trashBinMesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        root.AddChild(body);
    }

    private void EnsureTrashBinMesh()
    {
        if (_trashBinMesh != null)
            return;
        _trashBinShape = new CylinderShape3D { Radius = 0.3f, Height = 1.1f };

        // Ferro galvanizado: postes, aros e barras da cesta.
        var metal = new SurfaceTool();
        metal.Begin(Mesh.PrimitiveType.Triangles);
        metal.SetMaterial(Flat(new Color(0.34f, 0.38f, 0.34f), 0.55f));
        var post = new CylinderMesh { TopRadius = 0.03f, BottomRadius = 0.03f, Height = 1.1f, RadialSegments = 6, Rings = 1 };
        var bar = new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.012f, Height = 0.42f, RadialSegments = 4, Rings = 1 };
        var ring = new TorusMesh { InnerRadius = 0.235f, OuterRadius = 0.265f, RingSegments = 6, Rings = 14 };
        foreach (float z in new[] { -0.2f, 0.2f })
            metal.AppendFrom(post, 0, new Transform3D(Basis.Identity, new Vector3(0f, 0.55f, z)));
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.Tau / 10f;
            metal.AppendFrom(bar, 0, new Transform3D(Basis.Identity, new Vector3(Mathf.Cos(a) * 0.25f, 0.86f, Mathf.Sin(a) * 0.25f)));
        }
        metal.AppendFrom(ring, 0, new Transform3D(Basis.Identity, new Vector3(0f, 1.07f, 0f)));
        metal.AppendFrom(ring, 0, new Transform3D(Basis.Identity, new Vector3(0f, 0.65f, 0f)));
        // Fundo de chapa.
        var bottom = new CylinderMesh { TopRadius = 0.25f, BottomRadius = 0.25f, Height = 0.03f, RadialSegments = 10, Rings = 1 };
        metal.AppendFrom(bottom, 0, new Transform3D(Basis.Identity, new Vector3(0f, 0.66f, 0f)));
        metal.GenerateNormals();
        ArrayMesh mesh = metal.Commit();

        // Saco preto cheio, aparecendo por cima do aro.
        var bag = new SurfaceTool();
        bag.Begin(Mesh.PrimitiveType.Triangles);
        bag.SetMaterial(Flat(new Color(0.04f, 0.05f, 0.04f), 0.7f));
        var sphere = new SphereMesh { Radius = 0.24f, Height = 0.4f, RadialSegments = 8, Rings = 4 };
        bag.AppendFrom(sphere, 0, new Transform3D(Basis.Identity, new Vector3(0.02f, 0.92f, 0f)));
        bag.GenerateNormals();
        bag.Commit(mesh);

        _trashBinMesh = mesh;
    }
}
