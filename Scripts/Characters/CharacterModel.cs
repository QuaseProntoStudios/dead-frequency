// Scripts/Characters/CharacterModel.cs
// Boneco low-poly (estilo PS1) montado em código: corpo, cabeça, braços e pernas articulados,
// com pontos de encaixe para os equipamentos (óculos, luvas, mochila, radar).
// Usado pelos outros jogadores, pedestres, pelo comerciante e pela prévia do inventário.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public partial class CharacterModel : Node3D
{
    public Color Shirt { get; private set; } = new(0.3f, 0.35f, 0.4f);
    public Color Pants { get; private set; } = new(0.15f, 0.15f, 0.2f);
    public Color Skin { get; private set; } = new(0.78f, 0.6f, 0.45f);
    /// <summary>Colete refletivo de entregador (jogadores).</summary>
    public Color? Vest { get; private set; }

    public Node3D Head { get; private set; }
    private Node3D _leftArm, _rightArm, _leftLeg, _rightLeg, _torso;
    private Node3D _equipmentRoot;
    private readonly Dictionary<string, Node3D> _equipment = new();
    private float _walkPhase;
    private string _equipmentKey = "";

    private static BoxMesh _box;
    private static BoxMesh Box => _box ??= new BoxMesh { Size = Vector3.One };

    public static CharacterModel Create(Color shirt, Color pants, Color skin, Color? vest = null)
    {
        var model = new CharacterModel { Shirt = shirt, Pants = pants, Skin = skin, Vest = vest, Name = "CharacterModel" };
        model.Build();
        return model;
    }

    private void Build()
    {
        StandardMaterial3D shirt = Mat(Shirt), pants = Mat(Pants), skin = Mat(Skin), shoes = Mat(new Color(0.08f, 0.08f, 0.08f));

        _torso = new Node3D { Position = new Vector3(0f, 0.95f, 0f) };
        AddChild(_torso);
        Part(_torso, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.28f), shirt);
        if (Vest.HasValue)
        {
            var vest = Mat(Vest.Value);
            vest.EmissionEnabled = true;
            vest.Emission = Vest.Value;
            vest.EmissionEnergyMultiplier = 0.5f;
            Part(_torso, new Vector3(0f, 0.32f, 0f), new Vector3(0.53f, 0.42f, 0.31f), vest);
        }

        Head = new Node3D { Position = new Vector3(0f, 0.72f, 0f) };
        _torso.AddChild(Head);
        Part(Head, new Vector3(0f, 0.13f, 0f), new Vector3(0.28f, 0.28f, 0.28f), skin);
        Part(Head, new Vector3(0f, 0.29f, 0.02f), new Vector3(0.3f, 0.07f, 0.3f), Mat(Pants.Darkened(0.3f))); // cabelo/boné
        Part(Head, new Vector3(0f, 0.26f, -0.19f), new Vector3(0.26f, 0.03f, 0.1f), Mat(Pants.Darkened(0.3f))); // aba do boné (frente)
        Part(Head, new Vector3(0f, 0.15f, -0.145f), new Vector3(0.18f, 0.035f, 0.01f), Mat(new Color(0.08f, 0.06f, 0.05f))); // olhos

        _leftArm = Limb(_torso, new Vector3(0.33f, 0.58f, 0f), new Vector3(0.14f, 0.56f, 0.14f), shirt, skin);
        _rightArm = Limb(_torso, new Vector3(-0.33f, 0.58f, 0f), new Vector3(0.14f, 0.56f, 0.14f), shirt, skin);

        _leftLeg = Limb(this, new Vector3(0.13f, 0.95f, 0f), new Vector3(0.18f, 0.9f, 0.2f), pants, shoes);
        _rightLeg = Limb(this, new Vector3(-0.13f, 0.95f, 0f), new Vector3(0.18f, 0.9f, 0.2f), pants, shoes);

        _equipmentRoot = new Node3D { Name = "Equipment" };
        AddChild(_equipmentRoot);
    }

    /// <summary>Membro com pivô no ombro/quadril; a ponta (mão/pé) usa outra cor.</summary>
    private static Node3D Limb(Node3D parent, Vector3 pivot, Vector3 size, Material main, Material tip)
    {
        var joint = new Node3D { Position = pivot };
        parent.AddChild(joint);
        Part(joint, new Vector3(0f, -size.Y * 0.45f, 0f), new Vector3(size.X, size.Y * 0.9f, size.Z), main);
        // Frente do boneco = -Z: o bico do pé/mão vai para -Z (antes ia para +Z e parecia andar de costas).
        Part(joint, new Vector3(0f, -size.Y * 0.95f, -size.Z * 0.15f), new Vector3(size.X * 1.05f, size.Y * 0.12f, size.Z * 1.3f), tip);
        return joint;
    }

    private static MeshInstance3D Part(Node3D parent, Vector3 position, Vector3 size, Material material)
    {
        var mesh = new MeshInstance3D { Mesh = Box, Scale = size, Position = position, MaterialOverride = material };
        parent.AddChild(mesh);
        return mesh;
    }

    private static StandardMaterial3D Mat(Color color) => new() { AlbedoColor = color, Roughness = 0.85f };

    // ---------------- Animação ----------------

    /// <summary>Balanço de caminhada. speed em m/s (0 = parado).</summary>
    public void Animate(float delta, float speed)
    {
        float amount = Mathf.Clamp(speed / 4f, 0f, 1f);
        _walkPhase += delta * (4f + speed * 2.2f);
        float swing = Mathf.Sin(_walkPhase) * 0.6f * amount;
        _leftLeg.Rotation = new Vector3(swing, 0f, 0f);
        _rightLeg.Rotation = new Vector3(-swing, 0f, 0f);
        _leftArm.Rotation = new Vector3(-swing * 0.8f, 0f, 0f);
        _rightArm.Rotation = new Vector3(swing * 0.8f, 0f, 0f);
        _torso.Position = new Vector3(0f, 0.95f + Mathf.Abs(Mathf.Cos(_walkPhase)) * 0.04f * amount, 0f);
    }

    public void SetHeadPitch(float pitch) => Head.Rotation = new Vector3(Mathf.Clamp(pitch, -0.8f, 0.8f), 0f, 0f);

    // ---------------- Equipamentos ----------------

    /// <summary>Recebe "radar,gloves,..." e monta só o que mudou.</summary>
    public void SetEquipment(string csv)
    {
        csv ??= "";
        if (csv == _equipmentKey)
            return;
        _equipmentKey = csv;
        var wanted = new HashSet<string>(csv.Split(',', System.StringSplitOptions.RemoveEmptyEntries));

        foreach (string id in new List<string>(_equipment.Keys))
        {
            if (!wanted.Contains(id))
            {
                _equipment[id].QueueFree();
                _equipment.Remove(id);
            }
        }
        foreach (string id in wanted)
        {
            if (!_equipment.ContainsKey(id))
            {
                Node3D item = BuildItem(id);
                if (item != null)
                    _equipment[id] = item;
            }
        }
    }

    private Node3D BuildItem(string id)
    {
        ItemDef def = ItemCatalog.Get(id);
        if (def == null)
            return null;
        StandardMaterial3D color = Mat(def.Color);
        StandardMaterial3D dark = Mat(new Color(0.08f, 0.08f, 0.08f));
        var root = new Node3D { Name = id };

        switch (id)
        {
            case ItemCatalog.NightVision:
                Head.AddChild(root);
                Part(root, new Vector3(0f, 0.16f, -0.16f), new Vector3(0.3f, 0.1f, 0.06f), dark);
                var lens = Mat(def.Color);
                lens.EmissionEnabled = true;
                lens.Emission = def.Color;
                lens.EmissionEnergyMultiplier = 1.5f;
                Part(root, new Vector3(0.07f, 0.16f, -0.21f), new Vector3(0.08f, 0.08f, 0.08f), lens);
                Part(root, new Vector3(-0.07f, 0.16f, -0.21f), new Vector3(0.08f, 0.08f, 0.08f), lens);
                Part(root, new Vector3(0f, 0.19f, 0f), new Vector3(0.31f, 0.04f, 0.31f), dark); // tira
                break;
            case ItemCatalog.Gloves:
                _equipmentRoot.AddChild(root);
                Part(_leftArm, new Vector3(0f, -0.53f, 0.02f), new Vector3(0.17f, 0.12f, 0.19f), color).Name = "GloveL";
                Part(_rightArm, new Vector3(0f, -0.53f, 0.02f), new Vector3(0.17f, 0.12f, 0.19f), color).Name = "GloveR";
                // Guardamos as luvas como filhas dos braços; o nó raiz só marca que está equipado.
                root.TreeExiting += () =>
                {
                    _leftArm.GetNodeOrNull("GloveL")?.QueueFree();
                    _rightArm.GetNodeOrNull("GloveR")?.QueueFree();
                };
                break;
            case ItemCatalog.Backpack:
                _torso.AddChild(root);
                Part(root, new Vector3(0f, 0.32f, 0.24f), new Vector3(0.42f, 0.5f, 0.22f), color);
                Part(root, new Vector3(0f, 0.5f, 0.24f), new Vector3(0.44f, 0.08f, 0.24f), dark);
                break;
            case ItemCatalog.Radar:
                _leftArm.AddChild(root);
                Part(root, new Vector3(0.02f, -0.4f, -0.03f), new Vector3(0.17f, 0.1f, 0.17f), dark);
                var screen = Mat(def.Color);
                screen.EmissionEnabled = true;
                screen.Emission = def.Color;
                screen.EmissionEnergyMultiplier = 2f;
                Part(root, new Vector3(0.02f, -0.4f, -0.12f), new Vector3(0.12f, 0.07f, 0.01f), screen);
                break;
            default:
                _equipmentRoot.AddChild(root);
                break;
        }
        return root;
    }
}
