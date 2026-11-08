// Scripts/World/CityGenerator.Apartments.cs
// Edifício Sintonia (v0.9): cada jogador ganha um apartamento. No saguão, o elevador leva para o seu
// apartamento (no online: um por jogador, pela ordem de entrada; no solo: o 301). Lá dá para dormir e
// salvar o jogo, ver TV, ouvir a 96.6 e passar o tempo.
// Os apartamentos ficam "dentro do prédio" para o jogador: na prática são salas montadas bem abaixo
// do quarteirão, e o elevador/porta teletransporta. A janela mostra a cidade à noite.
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    [ExportGroup("Apartamentos")]
    [Export] public Vector2I ApartmentBlock { get; set; } = new(2, -2);
    public const int ApartmentCount = 8;
    private const float ApartmentDepth = -60f;

    /// <summary>Onde o jogador aparece ao sair do elevador no saguão (e para onde volta).</summary>
    public Vector3 LobbyPoint { get; private set; }
    private Vector3 _aptBase;
    private StandardMaterial3D _windowMat;

    /// <summary>Número do apartamento do jogador: 1..8 (online pelo id da sala; solo = 1).</summary>
    public static int MyApartment()
    {
        NetClient net = NetClient.Instance;
        if (net == null || !net.IsOnline || net.MyId <= 0)
            return 1;
        return (net.MyId - 1) % ApartmentCount + 1;
    }

    public static string ApartmentLabel(int k) => $"Apto {300 + k}";

    private Vector3 ApartmentOrigin(int k) => _aptBase + new Vector3((k - 4.5f) * 13f, 0f, 0f);

    private void BuildApartmentBuilding(Node3D block, Vector3 center)
    {
        var st = new Node3D { Name = "EdificioSintonia", Position = center + new Vector3(0f, CurbHeight, 0f) };
        block.AddChild(st);
        _aptBase = new Vector3(center.X, ApartmentDepth, center.Z);

        StandardMaterial3D marble = Flat(new Color(0.52f, 0.47f, 0.42f), 0.5f);
        StandardMaterial3D wallMat = Flat(new Color(0.55f, 0.48f, 0.4f), 0.8f);
        StandardMaterial3D steel = Flat(new Color(0.6f, 0.6f, 0.62f), 0.3f);
        steel.Metallic = 0.7f;
        StandardMaterial3D wood = Flat(new Color(0.4f, 0.26f, 0.15f), 0.7f);
        StandardMaterial3D plant = Flat(new Color(0.15f, 0.4f, 0.15f));

        AddBox(st, new Vector3(0f, 0.01f, 0f), new Vector3(30f, 0.02f, 30f), _concrete, false);
        // Jardim na frente.
        AddBox(st, new Vector3(-8f, 0.2f, 11f), new Vector3(8f, 0.4f, 3f), _grass);
        AddBox(st, new Vector3(8f, 0.2f, 11f), new Vector3(8f, 0.4f, 3f), _grass);

        // ---------------- saguão (térreo, enterável). Frente em z = 8.
        const float x0 = -10f, x1 = 10f, z0 = -8f, z1 = 8f, h = 4f, t = 0.3f;
        AddBox(st, new Vector3(0f, 0.03f, 0f), new Vector3(x1 - x0, 0.04f, z1 - z0), marble, false);
        AddBox(st, new Vector3(0f, h * 0.5f, z0), new Vector3(x1 - x0, h, t), wallMat);
        AddBox(st, new Vector3(x0, h * 0.5f, 0f), new Vector3(t, h, z1 - z0), wallMat);
        AddBox(st, new Vector3(x1, h * 0.5f, 0f), new Vector3(t, h, z1 - z0), wallMat);
        const float doorA = -1.3f, doorB = 1.3f;
        foreach ((float a, float b) in new[] { (x0, doorA), (doorB, x1) })
        {
            AddBox(st, new Vector3((a + b) * 0.5f, 0.3f, z1), new Vector3(b - a, 0.6f, t), wallMat);
            AddBox(st, new Vector3((a + b) * 0.5f, 1.8f, z1), new Vector3(b - a, 2.4f, 0.08f), _glass);
            AddBox(st, new Vector3((a + b) * 0.5f, 3.5f, z1), new Vector3(b - a, 1f, t), wallMat);
        }
        AddBox(st, new Vector3(0f, 3.25f, z1), new Vector3(doorB - doorA, 1.5f, t), wallMat);
        // Marquise e letreiro.
        AddBox(st, new Vector3(0f, 2.65f, z1 + 1.4f), new Vector3(6f, 0.15f, 2.8f), _darkMetal, false);
        TexQuad(st, "res://Assets/Textures/Stores/sign_ed_sintonia.png", new Vector3(0f, 3.4f, z1 + 0.17f), new Vector2(5.2f, 0.74f), 0f, 0.8f);

        // Torre de apartamentos em cima do saguão (com janelas acesas à noite).
        const float towerH = 32f;
        MeshInstance3D tower = AddBox(st, new Vector3(0f, h + towerH * 0.5f, 0f), new Vector3(x1 - x0 + 0.4f, towerH, z1 - z0 + 0.4f), _buildingMaterial);
        SetBuildingParams(tower, new Color(0.5f, 0.42f, 0.34f), 33f, 0.5f, 2); // pastilha, prédio de BH anos 80
        AddBox(st, new Vector3(0f, h + towerH + 0.4f, 0f), new Vector3(x1 - x0 + 0.6f, 0.8f, z1 - z0 + 0.6f), _concrete, false);
        AddCylinder(st, new Vector3(4f, h + towerH + 2f, -3f), 1.6f, 3.2f, _darkMetal);
        AddBox(st, new Vector3(0f, h + towerH - 1.2f, z1 + 0.25f), new Vector3(10f, 1.5f, 0.1f), _darkMetal, false);
        TexQuad(st, "res://Assets/Textures/Stores/sign_ed_sintonia.png", new Vector3(0f, h + towerH - 1.2f, z1 + 0.31f), new Vector2(9.8f, 1.4f), 0f, 1.0f);

        // Por dentro: forro, tapete, portaria, caixas de correio, plantas e dois elevadores.
        AddBox(st, new Vector3(0f, h - 0.02f, 0f), new Vector3(x1 - x0 - 0.3f, 0.04f, z1 - z0 - 0.3f), Flat(new Color(0.85f, 0.83f, 0.78f), 0.9f), false);
        AddBox(st, new Vector3(0f, 0.055f, 1f), new Vector3(2.6f, 0.01f, 12f), Flat(new Color(0.45f, 0.08f, 0.08f), 1f), false);
        st.AddChild(new OmniLight3D { Position = new Vector3(0f, h - 0.5f, 0f), LightColor = new Color(1f, 0.9f, 0.75f), LightEnergy = 1.4f, OmniRange = 14f, ShadowEnabled = false });
        AddBox(st, new Vector3(0f, h - 0.07f, 0f), new Vector3(3f, 0.06f, 0.4f), Emissive(new Color(1f, 0.95f, 0.85f), 1.5f), false);
        AddBox(st, new Vector3(-6f, 0.55f, 1f), new Vector3(2.4f, 1.1f, 0.8f), wood);
        BuildCashier(st, new Vector3(-6f, 0f, 0.1f), 0f, new Color(0.15f, 0.2f, 0.4f));
        st.AddChild(new Label3D { Text = "PORTARIA", FontSize = 32, PixelSize = 0.005f, Modulate = new Color(1f, 0.85f, 0.5f), OutlineSize = 4, Position = new Vector3(-6f, 1.25f, 1.41f) });
        for (int i = 0; i < ApartmentCount; i++)
        {
            Vector3 mb = new(x0 + 0.2f, 1.2f + i / 4 * 0.35f, -3f + i % 4 * 0.5f);
            AddBox(st, mb, new Vector3(0.12f, 0.3f, 0.45f), steel, false);
            st.AddChild(new Label3D { Text = $"{301 + i}", FontSize = 20, PixelSize = 0.004f, Modulate = Colors.Black, OutlineSize = 0, Position = mb + new Vector3(0.07f, 0.08f, 0f), Rotation = new Vector3(0f, Mathf.Pi * 0.5f, 0f) });
        }
        foreach (float px in new[] { -8.8f, 8.8f })
        {
            AddCylinder(st, new Vector3(px, 0.35f, 6.8f), 0.35f, 0.7f, Flat(new Color(0.55f, 0.3f, 0.2f)));
            AddCylinder(st, new Vector3(px, 1.2f, 6.8f), 0.5f, 1.2f, plant, _treeTop);
        }
        AssetProps.Spawn(st, AssetProps.Furniture + "couch_pillows.gltf", new Vector3(6f, 0.03f, 3f), -Mathf.Pi * 0.5f, 0.6f, new Vector3(1.0f, 0.75f, 1.8f));
        RadioPlayer lobbyRadio = BuildRadioProp(st, new Vector3(-5.4f, 1.1f, 1.1f), 0f, true, -14f);
        lobbyRadio.MaxDistance = 14f;

        Poster(st, new Vector3(x1 - 0.17f, 1.6f, -2f), -Mathf.Pi * 0.5f);
        int mine = MyApartment();
        foreach (float ex in new[] { -2f, 2f })
        {
            AddBox(st, new Vector3(ex, 1.15f, z0 + 0.18f), new Vector3(1.4f, 2.3f, 0.06f), steel, false);
            AddBox(st, new Vector3(ex, 1.15f, z0 + 0.22f), new Vector3(0.02f, 2.3f, 0.02f), _darkMetal, false);
            AddBox(st, new Vector3(ex, 2.55f, z0 + 0.2f), new Vector3(0.5f, 0.18f, 0.04f), Emissive(new Color(1f, 0.5f, 0.1f), 1.5f), false);
            st.AddChild(new Label3D { Text = "▲ 3", FontSize = 28, PixelSize = 0.004f, Modulate = Colors.Black, OutlineSize = 0, Position = new Vector3(ex, 2.55f, z0 + 0.23f) });
            ActionArea.Create(st, new Vector3(ex, 1.1f, z0 + 0.5f), new Vector3(1.5f, 2.2f, 0.6f),
                p => p.IsHolding ? "Solte a caixa antes de pegar o elevador" : $"[E] Elevador: subir para o seu apartamento ({ApartmentLabel(mine)})",
                p => GoToApartment(p, mine),
                p => !p.IsHolding);
        }
        LobbyPoint = st.Position + new Vector3(0f, 0.1f, z0 + 1.6f);
        // Endereço que vai para o inventário (a frente do prédio dá para a avenida em +Z).
        int num = 300 + Mathf.Abs(ApartmentBlock.X * 37 + ApartmentBlock.Y * 11) % 600;
        HomeGuide.Set(st.Position + new Vector3(0f, 0f, z1 + 2f), $"{StreetZ(ApartmentBlock.Y)}, nº {num} · Edifício Sintonia, {ApartmentLabel(mine)}");

        // ---------------- apartamentos
        for (int k = 1; k <= ApartmentCount; k++)
            BuildApartment(k, k == mine);
    }

    private void GoToApartment(PlayerController player, int k)
    {
        player.Teleport(ApartmentOrigin(k) + new Vector3(0f, 0.1f, 2.6f), 0f);
        string who = NetClient.Instance?.IsOnline == true ? NetClient.Instance.PlayerName : "você";
        GameHud.Toast(this, $"{ApartmentLabel(k)} · Edifício Sintonia — lar de {who}. Cama = salvar o jogo.", new Color(1f, 0.85f, 0.5f), 4.0);
    }

    private void BuildApartment(int k, bool mine)
    {
        var apt = new Node3D { Name = $"Apto{300 + k}", Position = ApartmentOrigin(k) };
        _staticRoot.AddChild(apt);
        const float x0 = -5f, x1 = 5f, z0 = -4f, z1 = 4f, h = 2.8f, t = 0.2f;

        StandardMaterial3D floor = Flat(new Color(0.45f, 0.3f, 0.18f), 0.6f);
        StandardMaterial3D wall = Flat(k % 2 == 0 ? new Color(0.78f, 0.74f, 0.62f) : new Color(0.66f, 0.72f, 0.7f), 0.9f);
        StandardMaterial3D white = Flat(new Color(0.9f, 0.9f, 0.88f), 0.5f);
        StandardMaterial3D curtain = Flat(new Color(0.55f, 0.15f, 0.12f), 1f);

        AddBox(apt, new Vector3(0f, -0.1f, 0f), new Vector3(x1 - x0, 0.2f, z1 - z0), floor);
        AddBox(apt, new Vector3(0f, h + 0.1f, 0f), new Vector3(x1 - x0, 0.2f, z1 - z0), white);
        AddBox(apt, new Vector3(x0, h * 0.5f, 0f), new Vector3(t, h, z1 - z0), wall);
        AddBox(apt, new Vector3(x1, h * 0.5f, 0f), new Vector3(t, h, z1 - z0), wall);
        AddBox(apt, new Vector3(0f, h * 0.5f, z1), new Vector3(x1 - x0, h, t), wall);
        // Parede do fundo com janela para a cidade.
        AddBox(apt, new Vector3(-3.5f, h * 0.5f, z0), new Vector3(3f, h, t), wall);
        AddBox(apt, new Vector3(3.5f, h * 0.5f, z0), new Vector3(3f, h, t), wall);
        AddBox(apt, new Vector3(0f, 0.45f, z0), new Vector3(4f, 0.9f, t), wall);
        AddBox(apt, new Vector3(0f, 2.55f, z0), new Vector3(4f, 0.5f, t), wall);
        // Vista da cidade à noite: sem luz nem neblina (senão fica lavada, a sala está longe da superfície).
        _windowMat ??= ResourceLoader.Exists("res://Assets/Textures/Home/window_city.png") ? new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>("res://Assets/Textures/Home/window_city.png"),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            DisableFog = true,
        } : null;
        if (_windowMat != null)
            apt.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(4f, 1.5f) }, MaterialOverride = _windowMat, Position = new Vector3(0f, 1.6f, z0 - 0.12f), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        AddBox(apt, new Vector3(0f, 0.86f, z0 + 0.1f), new Vector3(4.1f, 0.06f, 0.25f), white, false); // peitoril
        AddBox(apt, new Vector3(-1.85f, 1.6f, z0 + 0.15f), new Vector3(0.5f, 1.6f, 0.06f), curtain, false);
        AddBox(apt, new Vector3(1.85f, 1.6f, z0 + 0.15f), new Vector3(0.5f, 1.6f, 0.06f), curtain, false);

        // Luz quentinha.
        apt.AddChild(new OmniLight3D { Position = new Vector3(0f, h - 0.4f, 0f), LightColor = new Color(1f, 0.85f, 0.65f), LightEnergy = 1.2f, OmniRange = 9f, ShadowEnabled = false });
        AddBox(apt, new Vector3(0f, h - 0.02f, 0f), new Vector3(0.5f, 0.04f, 0.5f), Emissive(new Color(1f, 0.9f, 0.7f), 1.5f), false);

        // Cama (salvar), criado-mudo com rádio.
        AssetProps.Spawn(apt, AssetProps.Furniture + "bed_double_A.gltf", new Vector3(-3.4f, 0f, -2.6f), 0f, 0.8f, new Vector3(1.7f, 0.6f, 2.2f));
        ActionArea.Create(apt, new Vector3(-3.4f, 0.6f, -2.4f), new Vector3(1.8f, 0.8f, 2.2f),
            p => mine ? "[E] Dormir e salvar o jogo" : "Cama de outro morador",
            p => Sleep(p),
            _ => mine);
        AssetProps.Spawn(apt, AssetProps.Furniture + "cabinet_small_decorated.gltf", new Vector3(-1.9f, 0f, -3.5f), 0f, 0.6f, new Vector3(0.6f, 0.6f, 0.5f));
        BuildRadioProp(apt, new Vector3(-1.9f, 0.62f, -3.45f), 0f, false, -8f);

        // Sala: tapete, sofá virado para a TV.
        AssetProps.Spawn(apt, AssetProps.Furniture + "rug_oval_A.gltf", new Vector3(1.6f, 0.01f, -1f), 0f, 0.8f);
        AddBox(apt, new Vector3(1.6f, 0.25f, -3.5f), new Vector3(1.3f, 0.5f, 0.55f), Flat(new Color(0.3f, 0.2f, 0.12f), 0.7f));
        apt.AddChild(new VirtualTV { Name = "TV", Position = new Vector3(1.6f, 0.5f, -3.45f) });
        AssetProps.Spawn(apt, AssetProps.Furniture + "couch_pillows.gltf", new Vector3(1.6f, 0f, 0.6f), Mathf.Pi, 0.6f, new Vector3(1.8f, 0.75f, 1.0f));
        AssetProps.Spawn(apt, AssetProps.Furniture + "lamp_standing.gltf", new Vector3(-4.4f, 0f, 3.4f), 0f, 0.7f, new Vector3(0.4f, 1.7f, 0.4f));
        AssetProps.Spawn(apt, AssetProps.Furniture + "pictureframe_large_A.gltf", new Vector3(x1 - 0.12f, 1.6f, 0f), -Mathf.Pi * 0.5f, 0.6f);
        AssetProps.Spawn(apt, AssetProps.Furniture + "shelf_A_small.gltf", new Vector3(x0 + 0.15f, 1.5f, 1.2f), Mathf.Pi * 0.5f, 0.6f);

        // Cozinha: bancada com pia, fogão e geladeira.
        AddBox(apt, new Vector3(4.4f, 0.45f, 1.6f), new Vector3(1.1f, 0.9f, 2.4f), white);
        AddBox(apt, new Vector3(4.4f, 0.91f, 1.2f), new Vector3(0.6f, 0.03f, 0.5f), _darkMetal, false);
        foreach (float bz in new[] { 2.2f, 2.6f })
            AddCylinder(apt, new Vector3(4.3f, 0.92f, bz), 0.1f, 0.02f, _darkMetal);
        AddBox(apt, new Vector3(4.45f, 0.9f, 3.45f), new Vector3(0.9f, 1.8f, 0.8f), white);
        AddBox(apt, new Vector3(3.99f, 1.3f, 3.45f), new Vector3(0.03f, 0.05f, 0.4f), _darkMetal, false);
        AssetProps.Spawn(apt, AssetProps.Furniture + "table_small.gltf", new Vector3(2.8f, 0f, 2.6f), 0f, 0.7f, new Vector3(0.8f, 0.75f, 0.8f));
        AssetProps.Spawn(apt, AssetProps.Furniture + "chair_A_wood.gltf", new Vector3(2.8f, 0f, 1.9f), 0f, 0.7f);
        AssetProps.Spawn(apt, AssetProps.Furniture + "cactus_medium_A.gltf", new Vector3(2.8f, 0.55f, 2.6f), 0f, 0.25f);

        // Porta de saída (volta para o saguão) com o número.
        AddBox(apt, new Vector3(-2.5f, 1.05f, z1 - 0.12f), new Vector3(1f, 2.1f, 0.05f), Flat(new Color(0.35f, 0.22f, 0.12f), 0.6f), false);
        AddBox(apt, new Vector3(-2.15f, 1.0f, z1 - 0.16f), new Vector3(0.1f, 0.04f, 0.04f), _darkMetal, false);
        apt.AddChild(new Label3D { Text = ApartmentLabel(k).ToUpperInvariant(), FontSize = 30, PixelSize = 0.004f, Modulate = new Color(1f, 0.85f, 0.5f), OutlineSize = 4, Position = new Vector3(-2.5f, 1.85f, z1 - 0.16f), Rotation = new Vector3(0f, Mathf.Pi, 0f) });
        ActionArea.Create(apt, new Vector3(-2.5f, 1.05f, z1 - 0.35f), new Vector3(1.1f, 2.1f, 0.5f),
            _ => "[E] Sair (descer para o saguão)",
            p => p.Teleport(LobbyPoint, Mathf.Pi),
            p => !p.IsHolding);
        if (mine)
        {
            string name = NetClient.Instance?.IsOnline == true ? NetClient.Instance.PlayerName : (GameSettings.Instance?.PlayerName ?? "você");
            apt.AddChild(new Label3D { Text = $"LAR DOCE LAR\n{name}", FontSize = 26, PixelSize = 0.004f, Modulate = new Color(0.2f, 0.12f, 0.08f), OutlineSize = 0, Position = new Vector3(x0 + 0.12f, 2.0f, -1.2f), Rotation = new Vector3(0f, Mathf.Pi * 0.5f, 0f) });
        }
    }

    private void Sleep(PlayerController player)
    {
        DeliveryManager delivery = GetTree().GetFirstNodeInGroup("delivery_manager") as DeliveryManager;
        SaveData data = SaveManager.Save(delivery);
        bool skipped = DayNightController.Instance?.SkipHours(0) == true;
        if (skipped)
            DayNightController.Instance.SetSoloHour(18f);
        string msg = data == null ? "Não foi possível salvar." : skipped
            ? $"Você dormiu até as 18h. Jogo salvo · turno {data.Shift} · $ {data.Wallet}"
            : $"Jogo salvo · {Professions.Get(data.Profession).Name} · $ {data.Wallet}";
        GameHud.Toast(this, msg, data == null ? UiStyle.Danger : UiStyle.Phosphor, 4.5);
        GameHud.FadeBlack(this, 1.2);
    }
}
