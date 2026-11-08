// Scripts/World/CityGenerator.Market.cs
// Supermercado Atacadinho (v0.9): dá para entrar. Estacionamento com carrinhos, banners de oferta na
// porta e na vitrine; por dentro corredores de gôndolas com placas de seção penduradas (HORTIFRUTI,
// PADARIA, BEBIDAS, LIMPEZA, HIGIENE, MERCEARIA, FRIOS, CAIXAS), geladeiras, padaria, caixas com
// atendentes (E = comprar) e o estoque no fundo (usado pelo repositor). Rádio 96.6 tocando na loja.
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    [ExportGroup("Supermercado")]
    [Export] public Vector2I MarketBlock { get; set; } = new(-2, 2);

    private const string MarketTex = "res://Assets/Textures/Market/";

    private StandardMaterial3D RepeatTex(string path, float repeatX)
    {
        string key = path + "|rep" + repeatX;
        if (_texMaterials.TryGetValue(key, out StandardMaterial3D mat))
            return mat;
        if (!ResourceLoader.Exists(path))
            return null;
        mat = new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>(path),
            Uv1Scale = new Vector3(repeatX, 1f, 1f),
            TextureRepeat = true,
            Roughness = 0.6f,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };
        _texMaterials[key] = mat;
        return mat;
    }

    private void Quad(Node3D parent, Material mat, Vector3 pos, Vector2 size, float rotY)
    {
        if (mat == null)
            return;
        parent.AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = size },
            Position = pos,
            Rotation = new Vector3(0f, rotY, 0f),
            MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    /// <summary>Placa de seção pendurada no teto, legível dos dois lados.</summary>
    private void SectionSign(Node3D parent, string name, Vector3 pos, float rotY = 0f)
    {
        string path = MarketTex + "section_" + name + ".png";
        AddBeam(parent, pos + new Vector3(-1.0f, 0.3f, 0f).Rotated(Vector3.Up, rotY), pos + new Vector3(-1.0f, 1.4f, 0f).Rotated(Vector3.Up, rotY), 0.01f, _darkMetal);
        AddBeam(parent, pos + new Vector3(1.0f, 0.3f, 0f).Rotated(Vector3.Up, rotY), pos + new Vector3(1.0f, 1.4f, 0f).Rotated(Vector3.Up, rotY), 0.01f, _darkMetal);
        AddBox(parent, pos, new Vector3(2.5f, 0.64f, 0.04f), _darkMetal, false, rotY);
        TexQuad(parent, path, pos + new Vector3(0f, 0f, 0.025f).Rotated(Vector3.Up, rotY), new Vector2(2.4f, 0.6f), rotY, 0.3f);
        TexQuad(parent, path, pos + new Vector3(0f, 0f, -0.025f).Rotated(Vector3.Up, rotY), new Vector2(2.4f, 0.6f), rotY + Mathf.Pi, 0.3f);
    }

    private void BuildSupermarket(Node3D block, Vector3 center)
    {
        var st = new Node3D { Name = "Atacadinho", Position = center + new Vector3(0f, CurbHeight, 0f) };
        block.AddChild(st);

        StandardMaterial3D red = Flat(new Color(0.78f, 0.1f, 0.12f), 0.6f);
        StandardMaterial3D cream = Flat(new Color(0.92f, 0.9f, 0.84f), 0.8f);
        StandardMaterial3D tile = Flat(new Color(0.8f, 0.8f, 0.77f), 0.35f);
        StandardMaterial3D asphalt = Flat(new Color(0.15f, 0.15f, 0.16f), 0.9f);
        StandardMaterial3D paintLine = Flat(new Color(0.9f, 0.9f, 0.85f), 0.8f);
        StandardMaterial3D gondola = Flat(new Color(0.88f, 0.88f, 0.9f), 0.5f);
        StandardMaterial3D steel = Flat(new Color(0.55f, 0.56f, 0.58f), 0.4f);
        steel.Metallic = 0.6f;
        StandardMaterial3D tube = Emissive(new Color(0.95f, 1f, 1f), 2f);
        StandardMaterial3D fridgeGlass = Emissive(new Color(0.75f, 0.9f, 1f), 0.6f);
        StandardMaterial3D belt = Flat(new Color(0.08f, 0.08f, 0.08f), 0.9f);
        StandardMaterial3D wood = Flat(new Color(0.5f, 0.33f, 0.18f), 0.8f);

        // ---------------- estacionamento
        AddBox(st, new Vector3(0f, 0.01f, 12f), new Vector3(30f, 0.02f, 10f), asphalt, false);
        for (float x = -12f; x <= 12f; x += 3f)
            AddBox(st, new Vector3(x, 0.025f, 13.5f), new Vector3(0.12f, 0.01f, 4.5f), paintLine, false);
        for (int i = 0; i < 4; i++)
            BuildCart(st, new Vector3(9f + i * 0.35f, 0f, 9f), 0f, steel);
        BuildCart(st, new Vector3(-7.5f, 0f, 12.5f), 0.7f, steel);

        // ---------------- prédio. Frente em z = 7, porta automática no meio.
        const float x0 = -14f, x1 = 14f, z0 = -13f, z1 = 7f, h = 6f, t = 0.3f;
        AddBox(st, new Vector3(0f, 0.03f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.04f, z1 - z0), tile, false);
        AddBox(st, new Vector3(0f, h, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + 0.6f, 0.3f, z1 - z0 + 0.6f), cream);
        AddBox(st, new Vector3(0f, h * 0.5f, z0), new Vector3(x1 - x0, h, t), cream);
        AddBox(st, new Vector3(x0, h * 0.5f, (z0 + z1) * 0.5f), new Vector3(t, h, z1 - z0), cream);
        AddBox(st, new Vector3(x1, h * 0.5f, (z0 + z1) * 0.5f), new Vector3(t, h, z1 - z0), cream);
        const float doorA = -2.2f, doorB = 2.2f;
        // Fachada: mureta + vitrines de vidro + faixa vermelha em cima.
        foreach ((float a, float b) in new[] { (x0, doorA), (doorB, x1) })
        {
            float cx = (a + b) * 0.5f, w = b - a;
            AddBox(st, new Vector3(cx, 0.4f, z1), new Vector3(w, 0.8f, t), red);
            AddBox(st, new Vector3(cx, 2.0f, z1), new Vector3(w, 2.4f, 0.08f), _glass);
            for (float x = a + 2f; x < b - 0.5f; x += 2.5f)
                AddBox(st, new Vector3(x, 2.0f, z1), new Vector3(0.1f, 2.4f, 0.14f), steel, false);
        }
        AddBox(st, new Vector3(0f, (3.2f + h) * 0.5f, z1), new Vector3(x1 - x0, h - 3.2f, t), red);
        // Portas de vidro abertas (correm para os lados).
        AddBox(st, new Vector3(doorA - 0.9f, 1.6f, z1 + 0.15f), new Vector3(1.8f, 3.2f, 0.05f), _glass, false);
        AddBox(st, new Vector3(doorB + 0.9f, 1.6f, z1 + 0.15f), new Vector3(1.8f, 3.2f, 0.05f), _glass, false);
        AddBox(st, new Vector3(0f, 3.3f, z1 + 0.2f), new Vector3(doorB - doorA + 0.4f, 0.2f, 0.25f), steel, false);
        // Letreiro e marquise.
        AddBox(st, new Vector3(0f, 4.6f, z1 + 0.3f), new Vector3(17.4f, 2.6f, 0.3f), _darkMetal, false);
        TexQuad(st, "res://Assets/Textures/Stores/sign_atacadinho.png", new Vector3(0f, 4.6f, z1 + 0.46f), new Vector2(17f, 2.43f), 0f, 1.0f);
        AddBox(st, new Vector3(0f, 3.35f, z1 + 1.4f), new Vector3(8f, 0.15f, 2.6f), red, false);

        // Banners de oferta: na vitrine e em bandeiras ao lado da porta.
        string[] promos = { "cafe", "arroz", "refri", "sabao", "leite", "pao" };
        float[] windowX = { -12f, -9.2f, -6.4f, 6.4f, 9.2f, 12f };
        for (int i = 0; i < promos.Length; i++)
            TexQuad(st, MarketTex + "promo_" + promos[i] + ".png", new Vector3(windowX[i], 2.0f, z1 + 0.06f), new Vector2(1.5f, 2.25f), 0f, 0.5f);
        foreach ((float x, string promo) in new[] { (-4f, "refri"), (4f, "cafe") })
        {
            AddCylinder(st, new Vector3(x, 1.5f, z1 + 2.2f), 0.04f, 3f, steel);
            AddBox(st, new Vector3(x, 0.05f, z1 + 2.2f), new Vector3(0.6f, 0.1f, 0.6f), _darkMetal);
            TexQuad(st, MarketTex + "promo_" + promo + ".png", new Vector3(x + 0.55f, 2.0f, z1 + 2.2f), new Vector2(1.0f, 1.5f), 0f, 0.4f);
            TexQuad(st, MarketTex + "promo_" + promo + ".png", new Vector3(x + 0.55f, 2.0f, z1 + 2.19f), new Vector2(1.0f, 1.5f), Mathf.Pi, 0.4f);
            RegisterObstacle(st.Position + new Vector3(x, 0f, z1 + 2.2f));
        }

        // ---------------- iluminação (sempre acesa: é dentro da loja)
        for (float z = z0 + 2.5f; z < z1; z += 4f)
            for (float x = -10f; x <= 10f; x += 5f)
                AddBox(st, new Vector3(x, h - 0.2f, z), new Vector3(2.4f, 0.06f, 0.25f), tube, false);
        foreach (Vector3 lp in new[] { new Vector3(-7f, h - 0.8f, -8f), new Vector3(7f, h - 0.8f, -8f), new Vector3(-7f, h - 0.8f, 1f), new Vector3(7f, h - 0.8f, 1f) })
            st.AddChild(new OmniLight3D { Position = lp, LightColor = new Color(0.95f, 1f, 1f), LightEnergy = 1.3f, OmniRange = 12f, ShadowEnabled = false });
        // A loja toca a rádio.
        st.AddChild(new RadioPlayer { Name = "SomAmbiente", On = true, VolumeDb = -12f, UnitSize = 6f, MaxDistance = 26f, Position = new Vector3(0f, h - 0.6f, -3f) });

        // ---------------- gôndolas (corredores ao longo de Z)
        string[] faces = { "mercearia", "bebidas", "limpeza", "higiene", "mercearia" };
        float[] gx = { -10f, -6f, -2f, 2f, 6f, 10f };
        const float ga = -8.5f, gb = 0.5f, gh = 1.9f, gd = 0.9f;
        for (int g = 0; g < gx.Length; g++)
        {
            float x = gx[g];
            AddBox(st, new Vector3(x, gh * 0.5f, (ga + gb) * 0.5f), new Vector3(gd * 0.55f, gh, gb - ga), gondola);
            AddBox(st, new Vector3(x, gh + 0.25f, (ga + gb) * 0.5f), new Vector3(0.05f, 0.5f, gb - ga), gondola, false); // testeira
            foreach (int side in new[] { -1, 1 })
            {
                // Corredor para o qual esta face está virada.
                int aisle = side < 0 ? g - 1 : g;
                string face = aisle >= 0 && aisle < faces.Length ? faces[aisle] : (side < 0 ? "mercearia" : "bebidas");
                float fx = x + side * gd * 0.5f;
                for (float y = 0.15f; y < gh; y += 0.45f)
                    AddBox(st, new Vector3(x + side * gd * 0.4f, y, (ga + gb) * 0.5f), new Vector3(gd * 0.25f, 0.03f, gb - ga), steel, false);
                for (int row = 0; row < 4; row++)
                {
                    float y = 0.4f + row * 0.45f;
                    Quad(st, RepeatTex(MarketTex + "shelf_" + face + ".png", 4f), new Vector3(fx, y - 0.02f, (ga + gb) * 0.5f), new Vector2(gb - ga, 0.4f), side * Mathf.Pi * 0.5f);
                }
                foreach (float z in new[] { -7f, -4.5f, -2f })
                    foreach (float y in new[] { 0.6f, 1.05f })
                        MarketShelfSlots.Add((st.Position + new Vector3(x + side * (gd * 0.5f + 0.05f), y, z), Mathf.Pi * 0.5f));
            }
        }
        for (int a = 0; a < faces.Length; a++)
        {
            float x = (gx[a] + gx[a + 1]) * 0.5f;
            SectionSign(st, faces[a], new Vector3(x, 3.6f, gb + 0.5f));
            SectionSign(st, faces[a], new Vector3(x, 3.6f, ga - 0.3f));
        }

        // ---------------- hortifrúti (parede esquerda)
        SectionSign(st, "hortifruti", new Vector3(-12.6f, 3.6f, -4.5f), Mathf.Pi * 0.5f);
        for (int i = 0; i < 4; i++)
        {
            float z = -8.5f + i * 2.2f;
            AddBox(st, new Vector3(-12.9f, 0.45f, z), new Vector3(1.3f, 0.9f, 1.9f), wood);
            string crate = i % 2 == 0 ? "crate_tomatoes.gltf" : "crate_carrots.gltf";
            AssetProps.Spawn(st, "res://Assets/KayKit/Restaurant/" + crate, new Vector3(-12.9f, 0.92f, z - 0.45f), 0.2f, 0.9f);
            AssetProps.Spawn(st, "res://Assets/KayKit/Restaurant/" + crate, new Vector3(-12.9f, 0.92f, z + 0.45f), -0.2f, 0.9f);
        }
        // Frutas soltas numa bancada (esferas coloridas).
        var fruitMesh = new SphereMesh { Radius = 0.09f, Height = 0.18f, RadialSegments = 6, Rings = 3 };
        Color[] fruitColors = { new(0.95f, 0.75f, 0.1f), new(0.85f, 0.15f, 0.1f), new(0.35f, 0.7f, 0.2f), new(1f, 0.5f, 0.1f) };
        AddBox(st, new Vector3(-12.9f, 0.45f, 0.4f), new Vector3(1.3f, 0.9f, 1.8f), wood);
        for (int i = 0; i < 40; i++)
        {
            var m = new MeshInstance3D { Mesh = fruitMesh, MaterialOverride = Flat(fruitColors[i % 4], 0.5f), Position = new Vector3(-13.4f + i % 6 * 0.2f, 0.98f + i / 24 * 0.15f, -0.3f + i / 6 % 4 * 0.42f) };
            st.AddChild(m);
        }

        // ---------------- frios (parede direita): geladeiras com porta de vidro
        SectionSign(st, "frios", new Vector3(12.6f, 3.6f, -4.5f), -Mathf.Pi * 0.5f);
        for (int i = 0; i < 5; i++)
        {
            float z = -9.5f + i * 2.1f;
            AddBox(st, new Vector3(13.3f, 1.1f, z), new Vector3(1.1f, 2.2f, 2f), steel);
            AddBox(st, new Vector3(12.74f, 1.15f, z), new Vector3(0.03f, 1.9f, 1.85f), fridgeGlass, false);
            Quad(st, RepeatTex(MarketTex + "shelf_bebidas.png", 1f), new Vector3(12.72f, 0.75f, z), new Vector2(1.8f, 0.45f), -Mathf.Pi * 0.5f);
            Quad(st, RepeatTex(MarketTex + "shelf_mercearia.png", 1f), new Vector3(12.72f, 1.4f, z), new Vector2(1.8f, 0.45f), -Mathf.Pi * 0.5f);
        }

        // ---------------- padaria (fundo, à esquerda)
        SectionSign(st, "padaria", new Vector3(-8f, 3.6f, z0 + 2.2f));
        AddBox(st, new Vector3(-8f, 0.55f, z0 + 2.4f), new Vector3(6f, 1.1f, 0.9f), wood);
        AddBox(st, new Vector3(-8f, 1.35f, z0 + 2.4f), new Vector3(6f, 0.5f, 0.8f), _glass, false);
        var bread = Flat(new Color(0.78f, 0.52f, 0.22f), 0.8f);
        for (int i = 0; i < 18; i++)
            AddBox(st, new Vector3(-10.6f + i * 0.3f, 1.17f, z0 + 2.3f + (i % 2) * 0.2f), new Vector3(0.24f, 0.1f, 0.14f), bread, false);
        AddBox(st, new Vector3(-8f, 1.5f, z0 + 0.5f), new Vector3(6f, 3f, 0.6f), wood); // prateleira de pães atrás
        BuildCashier(st, new Vector3(-8f, 0f, z0 + 1.3f), Mathf.Pi, new Color(0.95f, 0.95f, 0.95f));

        // ---------------- estoque (fundo, à direita): porta "SÓ FUNCIONÁRIOS" e palete
        AddBox(st, new Vector3(7f, 1.3f, z0 + 0.2f), new Vector3(2.4f, 2.6f, 0.06f), _doorDark, false);
        st.AddChild(new Label3D { Text = "ESTOQUE\nsó funcionários", FontSize = 40, PixelSize = 0.006f, Modulate = new Color(1f, 0.85f, 0.2f), OutlineSize = 6, Position = new Vector3(7f, 2.9f, z0 + 0.26f) });
        MarketStockPoint = st.Position + new Vector3(7f, 0f, z0 + 1.4f);
        AddBox(st, new Vector3(7f, 0.08f, z0 + 1.4f), new Vector3(2.1f, 0.16f, 1.1f), wood, false);

        // ---------------- caixas (frente, à esquerda da porta)
        SectionSign(st, "caixas", new Vector3(-8f, 3.6f, 3.6f));
        for (int i = 0; i < 3; i++)
        {
            float x = -11.5f + i * 3f;
            AddBox(st, new Vector3(x, 0.45f, 3.6f), new Vector3(0.8f, 0.9f, 2.6f), cream);
            AddBox(st, new Vector3(x, 0.91f, 3.4f), new Vector3(0.6f, 0.02f, 1.8f), belt, false);
            AddBox(st, new Vector3(x, 1.05f, 4.6f), new Vector3(0.35f, 0.25f, 0.3f), _darkMetal, false); // registradora
            AddBox(st, new Vector3(x, 1.3f, 4.6f), new Vector3(0.3f, 0.2f, 0.04f), Emissive(new Color(0.4f, 1f, 0.5f), 0.8f), false);
            AddCylinder(st, new Vector3(x + 0.4f, 1.6f, 4.9f), 0.03f, 1.4f, steel);
            st.AddChild(new Label3D { Text = $"CAIXA {i + 1}", FontSize = 36, PixelSize = 0.006f, Modulate = new Color(1f, 0.9f, 0.3f), OutlineSize = 6, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Position = new Vector3(x + 0.4f, 2.45f, 4.9f) });
            BuildCashier(st, new Vector3(x + 0.8f, 0f, 4.4f), Mathf.Pi * 0.5f, new Color(0.8f, 0.1f, 0.12f));
            Vector3 counter = new(x, 0.9f, 3.6f);
            ActionArea.Create(st, counter, new Vector3(1.0f, 0.5f, 2.6f),
                _ => "[E] Comprar no Atacadinho",
                _ => ShopScreen.Instance?.OpenFor("Supermercado Atacadinho"),
                p => ShopScreen.Instance != null && !p.IsHolding);
        }
        Poster(st, new Vector3(x1 - 0.17f, 1.7f, 5.2f), -Mathf.Pi * 0.5f);
        // Pilha de ofertas perto da entrada (à direita).
        for (int i = 0; i < 3; i++)
        {
            AddBox(st, new Vector3(7f + i * 1.6f, 0.08f, 3.5f), new Vector3(1.2f, 0.16f, 1.2f), wood);
            for (int k = 0; k < 6; k++)
                AddBox(st, new Vector3(6.75f + i * 1.6f + k % 2 * 0.5f, 0.35f + k / 2 * 0.38f, 3.5f), new Vector3(0.45f, 0.36f, 1.0f), Flat(fruitColors[(i + k) % 4] * 0.8f, 0.7f), false);
            TexQuad(st, MarketTex + "promo_" + promos[i + 2] + ".png", new Vector3(7f + i * 1.6f, 2.0f, 4.12f), new Vector2(0.8f, 1.2f), 0f, 0.3f);
        }
    }

    private void BuildCart(Node3D parent, Vector3 pos, float rotY, Material steel)
    {
        var cart = new Node3D { Position = pos, Rotation = new Vector3(0f, rotY, 0f) };
        parent.AddChild(cart);
        AddBox(cart, new Vector3(0f, 0.75f, 0f), new Vector3(0.55f, 0.04f, 0.85f), steel, false);
        AddBox(cart, new Vector3(0f, 0.95f, 0.42f), new Vector3(0.55f, 0.4f, 0.02f), steel, false);
        AddBox(cart, new Vector3(0f, 0.95f, -0.42f), new Vector3(0.55f, 0.4f, 0.02f), steel, false);
        AddBox(cart, new Vector3(0.27f, 0.95f, 0f), new Vector3(0.02f, 0.4f, 0.85f), steel, false);
        AddBox(cart, new Vector3(-0.27f, 0.95f, 0f), new Vector3(0.02f, 0.4f, 0.85f), steel, false);
        AddBox(cart, new Vector3(0f, 1.2f, 0.55f), new Vector3(0.55f, 0.04f, 0.04f), Flat(new Color(0.8f, 0.1f, 0.1f)), false);
        foreach (float x in new[] { -0.22f, 0.22f })
            foreach (float z in new[] { -0.35f, 0.35f })
            {
                AddBox(cart, new Vector3(x, 0.4f, z), new Vector3(0.02f, 0.7f, 0.02f), steel, false);
                AddBox(cart, new Vector3(x, 0.05f, z), new Vector3(0.04f, 0.1f, 0.1f), _darkMetal, false);
            }
    }

    private static void BuildCashier(Node3D parent, Vector3 pos, float rotY, Color shirt)
    {
        CharacterModel npc = CharacterModel.Create(shirt, new Color(0.15f, 0.15f, 0.2f), new Color(0.7f, 0.5f, 0.38f));
        npc.Position = pos;
        npc.Rotation = new Vector3(0f, rotY, 0f);
        parent.AddChild(npc);
    }
}
