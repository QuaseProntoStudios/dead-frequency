// Scripts/World/AssetProps.cs
// Instancia modelos prontos (glTF/GLB) de Assets/ com cache e, se quiser, uma caixa de colisão.
// Pacotes usados (todos CC0, ver Créditos): Kenney Starter Kit Racing, KayKit City Builder Bits,
// KayKit Furniture Bits e KayKit Halloween Bits (Kay Lousberg).
// Se o arquivo não existir (ou ainda não foi importado pelo editor), simplesmente não aparece.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public static class AssetProps
{
    public const string City = "res://Assets/KayKit/CityBuilder/";
    public const string Furniture = "res://Assets/KayKit/Furniture/";
    public const string Halloween = "res://Assets/KayKit/Halloween/";
    public const string Kenney = "res://Assets/Kenney/Racing/";

    private static readonly Dictionary<string, PackedScene> Cache = new();

    public static PackedScene Load(string path)
    {
        if (Cache.TryGetValue(path, out PackedScene scene))
            return scene;
        scene = ResourceLoader.Exists(path) ? GD.Load<PackedScene>(path) : null;
        Cache[path] = scene;
        return scene;
    }

    /// <summary>
    /// Coloca o modelo como filho de parent. collider = tamanho da caixa de colisão em metros
    /// (já escalado), apoiada no chão; null = sem colisão. Retorna o nó (ou null se não existir).
    /// </summary>
    public static Node3D Spawn(Node3D parent, string path, Vector3 position, float rotY = 0f, float scale = 1f, Vector3? collider = null)
    {
        if (Load(path)?.Instantiate() is not Node3D model)
            return null;
        model.Scale = Vector3.One * scale;
        if (collider is Vector3 size)
        {
            var body = new StaticBody3D { Position = position, Rotation = new Vector3(0f, rotY, 0f), CollisionLayer = 1, CollisionMask = 0 };
            body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size }, Position = new Vector3(0f, size.Y * 0.5f, 0f) });
            body.AddChild(model);
            parent.AddChild(body);
            return body;
        }
        model.Position = position;
        model.Rotation = new Vector3(0f, rotY, 0f);
        parent.AddChild(model);
        return model;
    }

    /// <summary>Número estável a partir de uma posição (escolhas de decoração sem gastar sorteio).</summary>
    public static int Hash(Vector3 p, int salt = 0)
    {
        unchecked
        {
            int h = Mathf.RoundToInt(p.X * 10f) * 73856093 ^ Mathf.RoundToInt(p.Z * 10f) * 19349663 ^ salt * 83492791;
            return h & 0x7fffffff;
        }
    }
}
