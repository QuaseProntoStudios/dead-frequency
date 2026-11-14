// Scripts/World/CityGenerator.RadioInterior.cs
// Rádio 96.6 por dentro (v0.10.1): o estúdio deixou de ser uma sala só.
//   recepção (sofá, balcão) · cabine do locutor com a mesa de som (vidro para a gravação e para a recepção)
//   sala de gravação (espuma, microfone, gravador de rolo) · discoteca (discos e fitas que tocam com E)
//   escritório (computadores, programação) · sala dos servidores (racks piscando).
// A mesa de som continua no mesmo ponto (RadioConsolePoint): o LocutorJob e o servidor de dados dependem dela.
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private const string RadioTex = "res://Assets/Textures/Radio/";
    private const float RoomWallT = 0.15f;

    private StandardMaterial3D _radioWallInside, _radioFloor, _radioFacade, _radioGlass, _radioMuro;

    /// <summary>Material com textura em coordenadas do mundo (cada textura vale tileMeters m). Sem a imagem, cor lisa.</summary>
    private StandardMaterial3D TriplanarTex(string file, Color fallback, float tileU, float tileV, float roughness = 0.9f)
    {
        StandardMaterial3D mat = Flat(fallback, roughness);
        if (ResourceLoader.Exists(RadioTex + file))
        {
            mat.AlbedoColor = Colors.White;
            mat.AlbedoTexture = GD.Load<Texture2D>(RadioTex + file);
            mat.Uv1Triplanar = true;
            mat.Uv1WorldTriplanar = true;
            mat.Uv1Scale = new Vector3(1f / tileU, 1f / tileV, 1f / tileU);
            mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps;
        }
        return mat;
    }

    private void EnsureRadioMaterials()
    {
        _radioFacade ??= TriplanarTex("fachada.png", new Color(0.24f, 0.28f, 0.36f), 4f, 2f, 0.8f);
        _radioMuro ??= TriplanarTex("muro.png", new Color(0.82f, 0.8f, 0.74f), 4f, 2f, 0.95f);
        _radioWallInside ??= TriplanarTex("parede_interna.png", new Color(0.84f, 0.8f, 0.71f), 2f, 5f, 0.9f);
        _radioFloor ??= TriplanarTex("piso.png", new Color(0.5f, 0.48f, 0.45f), 2f, 2f, 0.7f);
        _radioGlass ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.55f, 0.8f, 0.85f, 0.2f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.05f,
            Metallic = 0.2f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }

    /// <summary>
    /// Parede interna ao longo de X (z fixo) ou de Z (x fixo). Vão de porta opcional (de a a b, 2,3 m de altura)
    /// e janela de vidro opcional (de 0,9 m a 2,5 m).
    /// </summary>
    private void RoomWall(Node3D st, bool alongX, float fixedCoord, float from, float to, float doorA = float.NaN, float doorB = float.NaN, bool glass = false, float height = 4.78f)
    {
        void Seg(float a, float b, float y0, float y1, Material m, bool collide = true)
        {
            if (b - a < 0.01f)
                return;
            float mid = (a + b) * 0.5f, len = b - a, hh = y1 - y0, cy = (y0 + y1) * 0.5f;
            Vector3 pos = alongX ? new Vector3(mid, cy, fixedCoord) : new Vector3(fixedCoord, cy, mid);
            Vector3 size = alongX ? new Vector3(len, hh, RoomWallT) : new Vector3(RoomWallT, hh, len);
            MeshInstance3D mesh = AddBox(st, pos, size, m, collide);
            if (m == _radioGlass)
                mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }
        void Piece(float a, float b)
        {
            if (glass)
            {
                Seg(a, b, 0f, 0.9f, _radioWallInside);
                Seg(a, b, 0.9f, 2.5f, _radioGlass);
                Seg(a, b, 2.5f, height, _radioWallInside);
                // Caixilho escuro em cima e embaixo da janela.
                Seg(a, b, 0.88f, 0.94f, _darkMetal, false);
                Seg(a, b, 2.46f, 2.52f, _darkMetal, false);
            }
            else
            {
                Seg(a, b, 0f, height, _radioWallInside);
            }
        }
        if (float.IsNaN(doorA))
        {
            Piece(from, to);
            return;
        }
        Piece(from, doorA);
        Piece(doorB, to);
        Seg(doorA, doorB, 2.3f, height, _radioWallInside); // verga em cima da porta
    }

    private void RadioLabel(Node3D st, string text, Vector3 pos, float rotY, float size = 0.004f, Color? color = null, int font = 48)
    {
        st.AddChild(new Label3D
        {
            Text = text,
            FontSize = font,
            PixelSize = size,
            Modulate = color ?? Colors.White,
            OutlineSize = 4,
            OutlineModulate = Colors.Black,
            Position = pos,
            Rotation = new Vector3(0f, rotY, 0f),
        });
    }

    private OmniLight3D RadioLamp(Node3D st, Vector3 pos, Color color, float energy, float range)
    {
        var l = new OmniLight3D { Position = pos, LightColor = color, LightEnergy = energy, OmniRange = range };
        st.AddChild(l);
        return l;
    }

    /// <summary>Monta tudo por dentro. Coordenadas locais do nó da rádio; a casca (paredes de fora, porta, vitrines) já existe.</summary>
    private void BuildRadioInterior(Node3D st, float x0, float x1, float z0, float z1, float h, float doorA, float doorB)
    {
        EnsureRadioMaterials();
        StandardMaterial3D red = Flat(new Color(0.8f, 0.1f, 0.08f), 0.6f);
        StandardMaterial3D white = Flat(new Color(0.92f, 0.92f, 0.9f), 0.6f);
        StandardMaterial3D green = Emissive(new Color(0.35f, 1f, 0.45f), 1.4f);
        StandardMaterial3D foam = Flat(new Color(0.42f, 0.26f, 0.55f), 1f);
        StandardMaterial3D foamDark = Flat(new Color(0.2f, 0.2f, 0.26f), 1f);
        StandardMaterial3D wood = _trunk;
        float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;

        // Revestimento das paredes de fora por dentro, teto e piso.
        AddBox(st, new Vector3(cx, h * 0.5f, z0 + 0.14f), new Vector3(x1 - x0 - 0.4f, h - 0.1f, 0.02f), _radioWallInside, false);
        AddBox(st, new Vector3(x0 + 0.14f, h * 0.5f, cz), new Vector3(0.02f, h - 0.1f, z1 - z0 - 0.4f), _radioWallInside, false);
        AddBox(st, new Vector3(x1 - 0.14f, h * 0.5f, cz), new Vector3(0.02f, h - 0.1f, z1 - z0 - 0.4f), _radioWallInside, false);
        AddBox(st, new Vector3(cx, h - 0.16f, cz), new Vector3(x1 - x0 - 0.4f, 0.02f, z1 - z0 - 0.4f), Flat(new Color(0.8f, 0.78f, 0.72f)), false);

        // ---------------------------------------------------------------- divisórias
        // z = 2.6: frente das salas de trás. Cabine em x -9,2..-2,8 (vidro, porta em -6,6..-5,4), gravação à esquerda, discoteca à direita.
        const float front = 2.6f;
        RoomWall(st, true, front, x0, -9.2f, -12.2f, -11.0f);
        RoomWall(st, true, front, -9.2f, -2.8f, -6.6f, -5.4f, glass: true);
        RoomWall(st, true, front, -2.8f, x1, -1.9f, -0.7f);
        RoomWall(st, false, -9.2f, z0, front, glass: true);          // cabine <-> gravação (vidro)
        RoomWall(st, false, -2.8f, z0, front);                       // cabine <-> discoteca
        // Escritório e servidores na frente, à direita.
        RoomWall(st, true, 4.6f, -3.6f, x1);
        RoomWall(st, false, -3.6f, 4.6f, z1, 6.0f, 7.2f);
        RoomWall(st, false, -0.6f, 4.6f, z1, 6.2f, 7.2f);

        // ---------------------------------------------------------------- recepção (x -13..-3,6, z 2,6..9)
        BuildRadioLobby(st, red, white);

        // ---------------------------------------------------------------- cabine do locutor
        BuildRadioBooth(st, z0, foam, red, white, green);

        // ---------------------------------------------------------------- sala de gravação
        BuildRadioRecordingRoom(st, z0, foam, foamDark, red);

        // ---------------------------------------------------------------- discoteca
        BuildRadioRecordsRoom(st, z0, wood);

        // ---------------------------------------------------------------- escritório e servidores
        BuildRadioOffice(st, white);
        BuildRadioServerRoom(st, red);

        // Luz geral e da recepção.
        RadioLamp(st, new Vector3(-8f, h - 0.6f, 6.2f), new Color(1f, 0.93f, 0.8f), 2.0f, 9f);
        AddBox(st, new Vector3(-8f, h - 0.2f, 6.2f), new Vector3(2.4f, 0.05f, 0.3f), Emissive(new Color(1f, 0.95f, 0.85f), 1.8f), false);
    }

    private void BuildRadioLobby(Node3D st, Material red, Material white)
    {
        // Balcão da recepção.
        AddBox(st, new Vector3(-10.4f, 0.55f, 6.9f), new Vector3(3.2f, 1.1f, 0.8f), _trunk);
        AddBox(st, new Vector3(-10.4f, 1.12f, 6.9f), new Vector3(3.4f, 0.05f, 1.0f), _darkMetal, false);
        AddBox(st, new Vector3(-10.4f, 0.55f, 6.48f), new Vector3(3.0f, 0.9f, 0.02f), Flat(new Color(0.3f, 0.5f, 0.5f)), false);
        RadioLabel(st, "96.6", new Vector3(-10.4f, 0.55f, 6.47f), Mathf.Pi, 0.006f, new Color(0.95f, 0.9f, 0.7f), 64);
        AssetProps.Spawn(st, AssetProps.Furniture + "chair_C.gltf", new Vector3(-10.4f, 0.03f, 7.8f), 0f, 0.75f, new Vector3(0.55f, 0.9f, 0.55f));
        AddBox(st, new Vector3(-9.3f, 1.2f, 6.8f), new Vector3(0.14f, 0.06f, 0.14f), _signWhite, false); // sineta
        // Sofá, mesinha e tapete.
        AssetProps.Spawn(st, AssetProps.Furniture + "rug_rectangle_stripes_A.gltf", new Vector3(-11.4f, 0.04f, 4.2f), Mathf.Pi * 0.5f, 1f);
        AssetProps.Spawn(st, AssetProps.Furniture + "couch_pillows.gltf", new Vector3(-12.3f, 0.03f, 4.2f), Mathf.Pi * 0.5f, 1f, new Vector3(0.9f, 0.9f, 2.0f));
        AssetProps.Spawn(st, AssetProps.Furniture + "table_low.gltf", new Vector3(-11.0f, 0.03f, 4.2f), 0f, 1f, new Vector3(0.9f, 0.45f, 0.5f));
        AssetProps.Spawn(st, AssetProps.Furniture + "cactus_medium_A.gltf", new Vector3(-12.4f, 0.03f, 8.4f), 0f, 1f);
        AssetProps.Spawn(st, AssetProps.Furniture + "lamp_standing.gltf", new Vector3(-12.3f, 0.03f, 5.6f), 0f, 1f);
        // Quadros: discos de ouro e o letreiro.
        AssetProps.Spawn(st, AssetProps.Furniture + "pictureframe_large_A.gltf", new Vector3(-12.78f, 1.7f, 7.2f), Mathf.Pi * 0.5f, 1.2f);
        AssetProps.Spawn(st, AssetProps.Furniture + "pictureframe_medium.gltf", new Vector3(-12.78f, 1.7f, 5.6f), Mathf.Pi * 0.5f, 1.2f);
        Poster(st, new Vector3(-10.1f, 1.6f, 2.72f), 0f, 1.0f);
        RadioLabel(st, "RÁDIO FREQUÊNCIA 96.6 FM", new Vector3(-8.0f, 3.5f, z1Inside - 0.2f), Mathf.Pi, 0.009f, new Color(1f, 0.9f, 0.6f), 64);
        RadioLabel(st, "A VOZ DA SERRA", new Vector3(-8.0f, 3.05f, z1Inside - 0.2f), Mathf.Pi, 0.006f, new Color(0.9f, 0.9f, 0.9f), 48);
        // Aviso: placas das portas.
        RadioLabel(st, "ESCRITÓRIO", new Vector3(-3.69f, 2.55f, 6.6f), -Mathf.Pi * 0.5f, 0.0035f, Colors.White, 40);
        RadioLabel(st, "DISCOTECA", new Vector3(-1.3f, 2.55f, 2.72f), 0f, 0.0035f, Colors.White, 40);
        RadioLabel(st, "GRAVAÇÃO", new Vector3(-11.6f, 3.25f, 2.72f), 0f, 0.0035f, Colors.White, 40);
    }

    private const float z1Inside = 9f;

    private void BuildRadioBooth(Node3D st, float z0, Material foam, Material red, Material white, Material green)
    {
        // Espuma acústica no fundo da cabine.
        for (float x = -8.6f; x < -3.2f; x += 1.2f)
            AddBox(st, new Vector3(x, 2.6f, z0 + 0.21f), new Vector3(1.0f, 2.4f, 0.08f), foam, false);

        // Mesa de controle (mesmo ponto de sempre: RadioConsolePoint). Modelo do Meshy: mixer, VU, monitores,
        // decks e fone; sem o modelo, a mesa antiga de caixas.
        RadioConsolePoint = st.Position + new Vector3(-6f, 0f, z0 + 1.6f);
        if (MeshyVehicles.PlaceProp(st, "radio_mesa", new Vector3(-6f, 0.03f, z0 + 1.45f), 0f, new Vector3(3.4f, 1.65f, 1.35f)) != null)
        {
            RadioCollider(st, new Vector3(-6f, 0.45f, z0 + 1.45f), new Vector3(3.4f, 0.9f, 1.3f));
            foreach (float mx in new[] { -7.1f, -4.9f })
                MeshyVehicles.PlaceProp(st, "radio_microfone", new Vector3(mx, RadioDeskTop, z0 + 1.75f), Mathf.Pi * 0.5f, new Vector3(0.75f, 0.6f, 0.22f));
            MeshyVehicles.PlaceProp(st, "radio_rack", new Vector3(-8.55f, 0.03f, z0 + 0.9f), Mathf.Pi * 0.5f, new Vector3(0.6f, 1.45f, 0.5f));
            RadioCollider(st, new Vector3(-8.55f, 0.73f, z0 + 0.9f), new Vector3(0.5f, 1.45f, 0.6f));
        }
        else
        {
            AddBox(st, new Vector3(-6f, 0.45f, z0 + 1.6f), new Vector3(5f, 0.9f, 1.2f), _radioWallInside);
            AddBox(st, new Vector3(-6f, 0.93f, z0 + 1.55f), new Vector3(4.6f, 0.06f, 0.9f), _darkMetal, false);
            for (int i = 0; i < 18; i++)
            {
                float x = -8.1f + i * 0.25f;
                AddBox(st, new Vector3(x, 1.0f, z0 + 1.5f + (i % 3) * 0.05f), new Vector3(0.05f, 0.08f, 0.12f), i % 4 == 0 ? red : white, false);
            }
            AddBox(st, new Vector3(-6f, 1.35f, z0 + 1.15f), new Vector3(1.8f, 0.8f, 0.1f), green, false); // VU / monitor
        }
        foreach (float mx in new[] { -7.1f, -4.9f })
            AssetProps.Spawn(st, AssetProps.Furniture + "chair_C.gltf", new Vector3(mx, 0.03f, z0 + 2.9f), Mathf.Pi, 0.75f, new Vector3(0.55f, 0.9f, 0.55f));
        // O retorno do estúdio: a 96.6 tocando baixinho dentro da cabine (antes era um radinho em cima da mesa).
        st.AddChild(new RadioPlayer { Name = "StudioMonitor", On = true, VolumeDb = -4f, UnitSize = 3.5f, MaxDistance = 20f, Position = new Vector3(-6f, 1.4f, z0 + 0.6f) });
        // Luz de "NO AR" dentro da cabine (vira para quem está na mesa) e por fora, em cima da porta.
        AddBox(st, new Vector3(-6f, 3.0f, 2.5f), new Vector3(1.1f, 0.35f, 0.06f), Emissive(new Color(1f, 0.1f, 0.05f), 2.5f), false);
        RadioLabel(st, "NO AR", new Vector3(-6f, 3.0f, 2.46f), Mathf.Pi, 0.004f);
        AddBox(st, new Vector3(-6f, 2.75f, 2.69f), new Vector3(1.1f, 0.35f, 0.06f), Emissive(new Color(1f, 0.1f, 0.05f), 2.5f), false);
        RadioLabel(st, "NO AR", new Vector3(-6f, 2.75f, 2.73f), 0f, 0.004f);
        // Prateleira com fones e um relógio digital.
        AddBox(st, new Vector3(-3.1f, 1.7f, 0.2f), new Vector3(0.3f, 0.05f, 1.6f), _trunk, false);
        for (int i = 0; i < 2; i++)
        {
            AddBox(st, new Vector3(-3.1f, 1.85f, -0.3f + i * 0.9f), new Vector3(0.16f, 0.2f, 0.2f), _darkMetal, false);
            AddBox(st, new Vector3(-3.1f, 1.98f, -0.3f + i * 0.9f), new Vector3(0.05f, 0.04f, 0.3f), _darkMetal, false);
        }
        AddBox(st, new Vector3(-3.0f, 2.8f, 0.6f), new Vector3(0.05f, 0.3f, 0.9f), Flat(new Color(0.05f, 0.05f, 0.06f)), false);
        RadioLabel(st, "96.6 FM", new Vector3(-3.03f, 2.8f, 0.6f), -Mathf.Pi * 0.5f, 0.0035f, new Color(0.4f, 1f, 0.5f), 40);
        RadioLamp(st, new Vector3(-6f, 2.6f, z0 + 2.5f), new Color(1f, 0.85f, 0.7f), 1.5f, 6f);
        AddBox(st, new Vector3(-6f, 4.62f, 0.3f), new Vector3(2.4f, 0.05f, 0.3f), Emissive(new Color(1f, 0.95f, 1f), 1.8f), false);
    }

    /// <summary>Altura do tampo da mesa de controle do Meshy (onde ficam os microfones).</summary>
    private const float RadioDeskTop = 0.74f;

    private static void RadioCollider(Node3D parent, Vector3 center, Vector3 size)
    {
        var body = new StaticBody3D { Position = center, CollisionLayer = 1, CollisionMask = 0 };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(body);
    }

    private void BuildRadioRecordingRoom(Node3D st, float z0, Material foam, Material foamDark, Material red)
    {
        // Espuma nas paredes (x -13..-9,2, z -2..2,6): xadrez roxo e grafite.
        int n = 0;
        for (float z = z0 + 0.5f; z < 2.4f; z += 0.9f, n++)
            AddBox(st, new Vector3(-12.78f, 2.2f, z), new Vector3(0.08f, 2.6f, 0.8f), n % 2 == 0 ? foam : foamDark, false);
        n = 0;
        for (float x = -12.5f; x < -9.4f; x += 0.9f, n++)
            AddBox(st, new Vector3(x, 2.2f, z0 + 0.21f), new Vector3(0.8f, 2.6f, 0.08f), n % 2 == 0 ? foamDark : foam, false);
        // Microfone de estúdio com pop filter, no centro da sala.
        AddCylinder(st, new Vector3(-11.1f, 0.65f, 0.4f), 0.02f, 1.3f, _darkMetal);
        AddCylinder(st, new Vector3(-11.1f, 1.38f, 0.4f), 0.05f, 0.2f, _darkMetal);
        MeshInstance3D pop = AddCylinder(st, new Vector3(-11.1f, 1.38f, 0.5f), 0.1f, 0.01f, Flat(new Color(0.05f, 0.05f, 0.05f)));
        pop.Rotation = new Vector3(Mathf.Pi * 0.5f, 0f, 0f);
        AddCylinder(st, new Vector3(-11.1f, 0.03f, 0.4f), 0.2f, 0.06f, _darkMetal);
        // Banquinho e o gravador de rolo.
        AddCylinder(st, new Vector3(-11.1f, 0.35f, 1.5f), 0.18f, 0.7f, _trunk);
        if (MeshyVehicles.PlaceProp(st, "radio_rolo", new Vector3(-12.45f, 0.03f, 1.4f), Mathf.Pi * 0.5f, new Vector3(0.75f, 1.35f, 0.45f)) != null)
        {
            RadioCollider(st, new Vector3(-12.45f, 0.68f, 1.4f), new Vector3(0.45f, 1.35f, 0.75f));
        }
        else
        {
            AddBox(st, new Vector3(-12.3f, 0.4f, 1.6f), new Vector3(0.9f, 0.8f, 0.6f), _trunk);
            AddBox(st, new Vector3(-12.3f, 0.87f, 1.6f), new Vector3(0.6f, 0.1f, 0.4f), _darkMetal, false);
            foreach (float rx in new[] { -12.45f, -12.15f })
                AddCylinder(st, new Vector3(rx, 0.95f, 1.6f), 0.09f, 0.03f, _signWhite);
        }
        // Luz "GRAVANDO" em cima da porta, por fora.
        AddBox(st, new Vector3(-11.6f, 2.8f, 2.69f), new Vector3(1.1f, 0.3f, 0.06f), Emissive(new Color(0.7f, 0.08f, 0.04f), 1.6f), false);
        RadioLabel(st, "GRAVANDO", new Vector3(-11.6f, 2.8f, 2.73f), 0f, 0.0035f, new Color(1f, 0.8f, 0.8f), 40);
        RadioLamp(st, new Vector3(-11f, 3.2f, 0.4f), new Color(1f, 0.82f, 0.65f), 1.3f, 5f);
    }

    private static readonly (string Id, string Label)[] RadioTapes =
    {
        ("vh_966_classica", "VINHETA 96.6"), ("ad_agro_tudao", "AGRO-TUDÃO"), ("ad_atacadinho", "ATACADINHO"),
        ("ad_shello", "SHELLO"), ("ad_petrobraz", "PETROBRAZ"), ("co_sem_sinal", "SEM SINAL"),
    };

    private void BuildRadioRecordsRoom(Node3D st, float z0, Material wood)
    {
        // Estantes: fundo (fitas) e lado leste (discos).
        AddBox(st, new Vector3(-0.9f, 1.15f, z0 + 0.3f), new Vector3(3.4f, 2.3f, 0.4f), wood);
        for (int r = 0; r < 4; r++)
            AddBox(st, new Vector3(-0.9f, 0.45f + r * 0.55f, z0 + 0.52f), new Vector3(3.3f, 0.04f, 0.06f), _darkMetal, false);
        AddBox(st, new Vector3(0.78f, 1.15f, 0.3f), new Vector3(0.4f, 2.3f, 3.6f), wood);

        // Fitas de enfeite (várias) e as interativas (6 comerciais/vinhetas) na prateleira do meio.
        for (int i = 0; i < 20; i++)
        {
            float x = -2.45f + (i % 10) * 0.33f;
            float y = 0.57f + (i / 10) * 0.55f;
            AddBox(st, new Vector3(x, y, z0 + 0.58f), new Vector3(0.14f, 0.2f, 0.02f), Flat(WallPalette[i % WallPalette.Length] * 1.4f, 0.6f), false);
        }
        for (int i = 0; i < RadioTapes.Length; i++)
        {
            (string id, string label) = RadioTapes[i];
            var tape = new RadioMediaInteractable
            {
                Name = "Fita_" + id,
                Kind = "fita",
                Title = label,
                AudioPath = RadioStation.AdsFolder + id + ".ogg",
                CollisionLayer = 4,
                CollisionMask = 0,
                Position = new Vector3(-2.2f + i * 0.45f, 1.67f, z0 + 0.62f),
            };
            tape.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.34f, 0.24f, 0.1f) } });
            AddBox(tape, Vector3.Zero, new Vector3(0.3f, 0.2f, 0.03f), Flat(new Color(0.12f, 0.12f, 0.14f), 0.5f), false);
            AddBox(tape, new Vector3(0f, 0.02f, 0.017f), new Vector3(0.24f, 0.1f, 0.005f), Flat(new Color(0.92f, 0.88f, 0.7f), 0.7f), false);
            foreach (float rx in new[] { -0.06f, 0.06f })
                AddCylinder(tape, new Vector3(rx, -0.04f, 0.017f), 0.025f, 0.005f, _signWhite).Rotation = new Vector3(Mathf.Pi * 0.5f, 0f, 0f);
            tape.AddChild(new Label3D { Text = label, FontSize = 18, PixelSize = 0.0016f, Modulate = new Color(0.1f, 0.1f, 0.1f), OutlineSize = 0, Position = new Vector3(0f, 0.045f, 0.021f) });
            st.AddChild(tape);
        }

        // Discos (LPs) interativos: as 5 músicas da programação, em pé na estante leste.
        Color[] covers =
        {
            new(0.7f, 0.12f, 0.1f), new(0.12f, 0.3f, 0.6f), new(0.15f, 0.45f, 0.35f), new(0.85f, 0.65f, 0.12f), new(0.55f, 0.2f, 0.6f),
        };
        for (int i = 0; i < RadioStation.Playlist.Length; i++)
        {
            RadioStation.Track tr = RadioStation.Playlist[i];
            var disc = new RadioMediaInteractable
            {
                Name = "Disco_" + i,
                Kind = "disco",
                Title = tr.Title,
                Artist = tr.Artist,
                AudioPath = tr.Path,
                PreviewSeconds = 20f,
                CollisionLayer = 4,
                CollisionMask = 0,
                Position = new Vector3(0.52f, 1.35f, -1.15f + i * 0.62f),
            };
            disc.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.1f, 0.4f, 0.4f) } });
            AddBox(disc, Vector3.Zero, new Vector3(0.025f, 0.34f, 0.34f), Flat(covers[i], 0.6f), false);
            AddCylinder(disc, new Vector3(0.002f, 0f, 0f), 0.11f, 0.027f, Flat(new Color(0.04f, 0.04f, 0.05f), 0.3f)).Rotation = new Vector3(0f, 0f, Mathf.Pi * 0.5f);
            disc.AddChild(new Label3D { Text = tr.Title, FontSize = 14, PixelSize = 0.0015f, Modulate = Colors.White, OutlineSize = 4, OutlineModulate = Colors.Black, Position = new Vector3(-0.015f, 0.11f, 0f), Rotation = new Vector3(0f, -Mathf.Pi * 0.5f, 0f), Width = 190f, AutowrapMode = TextServer.AutowrapMode.WordSmart });
            st.AddChild(disc);
        }
        // Discos de enfeite nas outras prateleiras.
        for (int i = 0; i < 24; i++)
            AddBox(st, new Vector3(0.52f, 0.5f + (i / 12) * 1.5f, -1.4f + (i % 12) * 0.29f + 0.02f), new Vector3(0.025f, 0.3f, 0.26f), Flat(WallPalette[(i + 3) % WallPalette.Length] * 1.5f, 0.5f), false);

        // Toca-discos de enfeite no meio da sala (o som vem do disco escolhido).
        if (MeshyVehicles.PlaceProp(st, "radio_tocadiscos", new Vector3(-1.4f, 0.03f, 1.2f), RadioTurntableYaw, new Vector3(0.8f, 1.0f, 1.05f)) != null)
        {
            RadioCollider(st, new Vector3(-1.4f, 0.5f, 1.2f), new Vector3(1.05f, 1.0f, 1.05f));
        }
        else
        {
            AddBox(st, new Vector3(-1.4f, 0.45f, 1.2f), new Vector3(1.0f, 0.9f, 0.7f), wood);
            AddBox(st, new Vector3(-1.4f, 0.93f, 1.2f), new Vector3(0.9f, 0.06f, 0.6f), _darkMetal, false);
            AddCylinder(st, new Vector3(-1.5f, 0.98f, 1.2f), 0.24f, 0.03f, Flat(new Color(0.04f, 0.04f, 0.05f), 0.3f));
            AddBeam(st, new Vector3(-1.15f, 1.0f, 1.0f), new Vector3(-1.45f, 1.0f, 1.2f), 0.012f, _signWhite);
        }
        RadioLabel(st, "OUÇA OS DISCOS E AS FITAS (E)", new Vector3(-0.9f, 2.65f, z0 + 0.5f), 0f, 0.0035f, new Color(1f, 0.9f, 0.6f), 40);
        RadioLamp(st, new Vector3(-0.9f, 3.0f, 0.4f), new Color(1f, 0.8f, 0.55f), 1.4f, 5.5f);
    }

    private void BuildRadioOffice(Node3D st, Material white)
    {
        // Duas mesas com computadores de tubo.
        foreach (float z in new[] { 5.5f, 8.3f })
        {
            AddBox(st, new Vector3(-2.1f, 0.38f, z), new Vector3(1.3f, 0.76f, 0.7f), _trunk);
            AddBox(st, new Vector3(-2.1f, 0.78f, z), new Vector3(1.4f, 0.04f, 0.8f), _darkMetal, false);
            AddBox(st, new Vector3(-2.2f, 1.0f, z + 0.08f), new Vector3(0.45f, 0.38f, 0.4f), Flat(new Color(0.75f, 0.72f, 0.65f)), false);
            AddBox(st, new Vector3(-2.2f, 1.0f, z - 0.13f), new Vector3(0.36f, 0.28f, 0.02f), Emissive(new Color(0.35f, 1f, 0.45f), 1.1f), false);
            AddBox(st, new Vector3(-2.2f, 0.82f, z - 0.27f), new Vector3(0.4f, 0.03f, 0.13f), _darkMetal, false);
            AssetProps.Spawn(st, AssetProps.Furniture + "chair_A_wood.gltf", new Vector3(-2.1f, 0.03f, z - 0.9f), 0f, 0.75f, new Vector3(0.55f, 0.9f, 0.55f));
        }
        AssetProps.Spawn(st, AssetProps.Furniture + "cabinet_small_decorated.gltf", new Vector3(-3.25f, 0.03f, 5.2f), Mathf.Pi * 0.5f, 1f, new Vector3(0.5f, 0.9f, 0.9f));
        // Quadro da programação na parede do fundo.
        AddBox(st, new Vector3(-1.5f, 1.9f, 4.74f), new Vector3(2.0f, 1.2f, 0.04f), white, false);
        RadioLabel(st, "PROGRAMAÇÃO\n06h  Bom dia, Serra\n12h  Hora do Almoço\n18h  Voz do Entregador\n22h  Madrugada 96.6", new Vector3(-1.5f, 1.9f, 4.78f), 0f, 0.0028f, new Color(0.05f, 0.1f, 0.3f), 32);
        RadioLamp(st, new Vector3(-2f, 3.2f, 6.8f), new Color(0.95f, 0.95f, 1f), 1.5f, 6f);
        AddBox(st, new Vector3(-2f, 4.62f, 6.8f), new Vector3(2.0f, 0.05f, 0.3f), Emissive(new Color(0.95f, 0.97f, 1f), 1.6f), false);
    }

    private const float RadioTurntableYaw = Mathf.Pi * 0.5f; // prateleira de discos virada para a porta

    private void BuildRadioServerRoom(Node3D st, Material red)
    {
        // Modelos do Meshy: dois racks de servidor e o rack com o transmissor, de frente para a porta (-X).
        if (MeshyVehicles.HasProp("radio_servidor"))
        {
            foreach (float z in new[] { 5.35f, 8.25f })
            {
                MeshyVehicles.PlaceProp(st, "radio_servidor", new Vector3(0.45f, 0.03f, z), -Mathf.Pi * 0.5f, new Vector3(0.75f, 2.05f, 0.8f));
                RadioCollider(st, new Vector3(0.45f, 1.03f, z), new Vector3(0.8f, 2.05f, 0.75f));
            }
            MeshyVehicles.PlaceProp(st, "radio_rack", new Vector3(0.55f, 0.03f, 6.8f), -Mathf.Pi * 0.5f, new Vector3(0.65f, 1.55f, 0.55f));
            RadioCollider(st, new Vector3(0.55f, 0.78f, 6.8f), new Vector3(0.55f, 1.55f, 0.65f));
            AddBox(st, new Vector3(0.7f, 3.4f, 8.3f), new Vector3(0.3f, 0.5f, 1.0f), Flat(new Color(0.85f, 0.85f, 0.82f)), false);
            RadioLabel(st, "SALA DOS SERVIDORES\nNÃO DESLIGAR", new Vector3(-0.69f, 2.6f, 6.7f), -Mathf.Pi * 0.5f, 0.0032f, new Color(1f, 0.85f, 0.5f), 36);
            RadioLamp(st, new Vector3(0f, 2.8f, 7f), new Color(0.6f, 0.75f, 1f), 1.0f, 4.5f);
            return;
        }
        // Três racks encostados na parede leste, com LEDs que piscam em ritmos diferentes.
        StandardMaterial3D ledGreen = Emissive(new Color(0.3f, 1f, 0.4f), 2.2f);
        StandardMaterial3D ledAmber = Emissive(new Color(1f, 0.7f, 0.15f), 2.2f);
        StandardMaterial3D ledRed = Emissive(new Color(1f, 0.15f, 0.1f), 2.2f);
        foreach ((StandardMaterial3D m, double a, double b) in new[] { (ledGreen, 0.35, 0.5), (ledAmber, 0.8, 0.3), (ledRed, 1.7, 0.9) })
        {
            if (!IsInsideTree())
                break;
            Tween t = CreateTween().BindNode(this).SetLoops();
            t.TweenProperty(m, "emission_energy_multiplier", 0.15, a);
            t.TweenProperty(m, "emission_energy_multiplier", 2.2, b);
        }
        StandardMaterial3D rack = Flat(new Color(0.07f, 0.07f, 0.08f), 0.5f);
        for (int i = 0; i < 3; i++)
        {
            float z = 5.5f + i * 1.25f;
            AddBox(st, new Vector3(0.45f, 1.05f, z), new Vector3(0.75f, 2.1f, 0.95f), rack);
            for (int u = 0; u < 9; u++)
            {
                float y = 0.3f + u * 0.2f;
                AddBox(st, new Vector3(0.06f, y, z), new Vector3(0.02f, 0.14f, 0.85f), _darkMetal, false);
                for (int k = 0; k < 4; k++)
                {
                    StandardMaterial3D m = ((u + k + i) % 5) switch { 0 => ledAmber, 1 => ledRed, _ => ledGreen };
                    AddBox(st, new Vector3(0.045f, y, z - 0.3f + k * 0.1f), new Vector3(0.02f, 0.03f, 0.04f), m, false);
                }
            }
        }
        // Ar-condicionado e aviso.
        AddBox(st, new Vector3(0.7f, 3.4f, 8.3f), new Vector3(0.3f, 0.5f, 1.0f), Flat(new Color(0.85f, 0.85f, 0.82f)), false);
        RadioLabel(st, "SALA DOS SERVIDORES\nNÃO DESLIGAR", new Vector3(-0.69f, 2.6f, 6.7f), -Mathf.Pi * 0.5f, 0.0032f, new Color(1f, 0.85f, 0.5f), 36);
        RadioLamp(st, new Vector3(0f, 2.8f, 7f), new Color(0.6f, 0.75f, 1f), 1.0f, 4.5f);
    }
}
