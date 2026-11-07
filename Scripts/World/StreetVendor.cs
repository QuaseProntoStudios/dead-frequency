// Scripts/World/StreetVendor.cs
// Barraquinha de rua com comerciante. Mire no balcão e aperte E para abrir a loja.
// A barraca é montada em código; a frente fica virada para -Z do nó.
using Godot;

namespace DeadFrequency;

public partial class StreetVendor : Area3D, IInteractable
{
    [Export] public string VendorName { get; set; } = "Barraca do Zé";
    [Export] public Color CanopyColor { get; set; } = new(0.8f, 0.15f, 0.12f);
    [Export] public Color ShirtColor { get; set; } = new(0.55f, 0.35f, 0.15f);

    private CharacterModel _vendor;
    private float _idle;

    public override void _Ready()
    {
        CollisionLayer = 4; // Interactables
        CollisionMask = 0;
        Monitoring = false;
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(2.6f, 1.4f, 1.2f) }, Position = new Vector3(0f, 0.7f, -0.2f) });

        AddToGroup(MinimapView.MarkerGroup);
        SetMeta("map_color", new Color(1f, 0.85f, 0.2f));
        SetMeta("map_kind", "vendor");

        BuildStall();
    }

    private static StandardMaterial3D Mat(Color c, float rough = 0.9f) => new() { AlbedoColor = c, Roughness = rough };

    private void Box(Node3D parent, Vector3 pos, Vector3 size, Material mat)
    {
        parent.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos, MaterialOverride = mat });
    }

    private void BuildStall()
    {
        var wood = Mat(new Color(0.42f, 0.28f, 0.16f));
        var metal = Mat(new Color(0.2f, 0.2f, 0.22f), 0.5f);
        var white = Mat(new Color(0.9f, 0.88f, 0.82f));
        var canopy = Mat(CanopyColor);

        // Corpo físico (balcão) para não atravessar.
        var body = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(2.4f, 1f, 0.8f) }, Position = new Vector3(0f, 0.5f, 0f) });
        AddChild(body);

        // Balcão com mercadorias.
        Box(this, new Vector3(0f, 0.45f, 0f), new Vector3(2.4f, 0.9f, 0.8f), wood);
        Box(this, new Vector3(0f, 0.92f, 0f), new Vector3(2.5f, 0.05f, 0.9f), white);
        string[] goods = { ItemCatalog.Radar, ItemCatalog.NightVision, ItemCatalog.Gloves, ItemCatalog.EnergyDrink, ItemCatalog.RepairKit };
        for (int i = 0; i < goods.Length; i++)
        {
            ItemDef def = ItemCatalog.Get(goods[i]);
            var m = Mat(def.Color, 0.5f);
            m.EmissionEnabled = goods[i] is ItemCatalog.Radar or ItemCatalog.NightVision;
            m.Emission = def.Color;
            m.EmissionEnergyMultiplier = 0.6f;
            Box(this, new Vector3(-0.9f + i * 0.45f, 1.02f, -0.1f), new Vector3(0.28f, 0.16f + (i % 2) * 0.08f, 0.24f), m);
        }

        // Postes e toldo listrado.
        foreach (float x in new[] { -1.2f, 1.2f })
            foreach (float z in new[] { -0.45f, 0.9f })
                Box(this, new Vector3(x, 1.2f, z), new Vector3(0.06f, 2.4f, 0.06f), metal);
        for (int i = 0; i < 6; i++)
            Box(this, new Vector3(-1.25f + i * 0.5f + 0.25f, 2.42f, 0.22f), new Vector3(0.5f, 0.06f, 1.6f), i % 2 == 0 ? canopy : white);
        Box(this, new Vector3(0f, 2.3f, -0.58f), new Vector3(2.6f, 0.2f, 0.04f), canopy);

        // Caixotes ao lado.
        Box(this, new Vector3(1.6f, 0.25f, 0.4f), new Vector3(0.5f, 0.5f, 0.5f), wood);
        Box(this, new Vector3(1.6f, 0.7f, 0.45f), new Vector3(0.4f, 0.4f, 0.4f), wood);

        // Placa e luz.
        AddChild(new Label3D
        {
            Text = VendorName.ToUpperInvariant() + "\n[E] COMPRAR",
            FontSize = 56,
            PixelSize = 0.006f,
            Modulate = new Color(1f, 0.9f, 0.5f),
            OutlineSize = 10,
            Position = new Vector3(0f, 2.3f, -0.62f),
            RotationDegrees = new Vector3(0f, 180f, 0f),
        });
        var bulb = Mat(new Color(1f, 0.85f, 0.5f));
        bulb.EmissionEnabled = true;
        bulb.Emission = new Color(1f, 0.8f, 0.45f);
        bulb.EmissionEnergyMultiplier = 3f;
        Box(this, new Vector3(0f, 2.25f, 0.3f), new Vector3(0.12f, 0.12f, 0.12f), bulb);
        AddChild(new OmniLight3D { Position = new Vector3(0f, 2.1f, 0f), LightColor = new Color(1f, 0.8f, 0.5f), LightEnergy = 1.6f, OmniRange = 6f });

        // Comerciante atrás do balcão, de frente para a rua.
        _vendor = CharacterModel.Create(ShirtColor, new Color(0.2f, 0.2f, 0.25f), new Color(0.65f, 0.48f, 0.36f));
        _vendor.Position = new Vector3(0f, 0f, 0.75f);
        AddChild(_vendor);
    }

    public override void _Process(double delta)
    {
        // Mexe a cabeça de vez em quando (parado, mas vivo).
        _idle += (float)delta;
        _vendor?.SetHeadPitch(Mathf.Sin(_idle * 0.7f) * 0.15f);
        _vendor?.Animate((float)delta, 0f);
    }

    public string GetInteractionPrompt(PlayerController player) => $"[E] Comprar na {VendorName}";

    public bool CanInteract(PlayerController player) => ShopScreen.Instance != null && !player.IsHolding;

    public void Interact(PlayerController player) => ShopScreen.Instance?.OpenFor(VendorName);
}
