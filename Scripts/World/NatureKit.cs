// Scripts/World/NatureKit.cs
// Árvores, mato e pedras do Stylized Nature MegaKit (Quaternius, CC0) em Assets/Quaternius/Nature.
// Devolve a malha (com os materiais de casca/folha) para desenhar milhares de cópias com MultiMesh.
// As texturas foram reduzidas para 256 px e os mapas normais tirados (cara de PS1 e repositório leve).
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public static class NatureKit
{
    public const string Dir = "res://Assets/Quaternius/Nature/";

    public static readonly string[] CommonTrees = { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5" };
    public static readonly string[] Pines = { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" };
    public static readonly string[] DeadTrees = { "DeadTree_1", "DeadTree_2", "DeadTree_3", "DeadTree_4", "DeadTree_5" };
    public static readonly string[] Ground = { "Grass_Common_Tall", "Grass_Wispy_Tall", "Fern_1", "Bush_Common", "Bush_Common_Flowers", "Flower_3_Group", "Flower_4_Group", "Plant_1_Big" };
    public static readonly string[] Rocks = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" };

    private static readonly Dictionary<string, Mesh> Meshes = new();

    /// <summary>A malha do modelo (null se o arquivo não existir).</summary>
    public static Mesh Get(string name)
    {
        if (Meshes.TryGetValue(name, out Mesh mesh))
            return mesh;
        mesh = null;
        if (AssetProps.Load(Dir + name + ".gltf")?.Instantiate() is Node root)
        {
            foreach (Node n in root.FindChildren("*", "MeshInstance3D", true, false))
            {
                mesh = ((MeshInstance3D)n).Mesh;
                break;
            }
            root.Free();
            if (mesh != null)
                FixMaterials(mesh);
        }
        Meshes[name] = mesh;
        return mesh;
    }

    /// <summary>Folhas com recorte (sem ordenar transparência) e visíveis dos dois lados; filtro "pixelado".</summary>
    private static void FixMaterials(Mesh mesh)
    {
        for (int i = 0; i < mesh.GetSurfaceCount(); i++)
        {
            if (mesh.SurfaceGetMaterial(i) is not StandardMaterial3D m)
                continue;
            m.TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps;
            m.Roughness = 1f;
            if (m.Transparency != BaseMaterial3D.TransparencyEnum.Disabled)
            {
                m.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
                m.AlphaScissorThreshold = 0.5f;
                m.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            }
        }
    }

    /// <summary>Coloca uma cópia avulsa (com colisão de tronco opcional) — para poucos objetos.</summary>
    public static Node3D Spawn(Node3D parent, string name, Vector3 position, float rotY, float scale, float trunkRadius = 0f, float trunkHeight = 3f)
    {
        Mesh mesh = Get(name);
        if (mesh == null)
            return null;
        Node3D root;
        if (trunkRadius > 0f)
        {
            var body = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
            body.AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = trunkRadius, Height = trunkHeight }, Position = new Vector3(0f, trunkHeight * 0.5f, 0f) });
            root = body;
        }
        else
        {
            root = new Node3D();
        }
        root.Name = name;
        root.Position = position;
        root.Rotation = new Vector3(0f, rotY, 0f);
        root.AddChild(new MeshInstance3D { Mesh = mesh, Scale = Vector3.One * scale });
        parent.AddChild(root);
        return root;
    }
}
