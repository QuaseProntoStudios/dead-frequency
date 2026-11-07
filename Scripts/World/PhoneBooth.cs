// Scripts/World/PhoneBooth.cs
// Orelhão. Mire e aperte E: às vezes a central dá uma dica de entrega, às vezes... é só a frequência morta.
// O visual é montado em código (concha aberta para a rua = +Z local).
// Duas operadoras paródia: "tchau" (concha laranja) e "morto" (concha roxa).
using Godot;

namespace DeadFrequency;

public enum PhoneBrand
{
    Tchau,
    Morto,
}

public partial class PhoneBooth : Area3D, IInteractable
{
    public PhoneBrand Brand { get; set; } = PhoneBrand.Tchau;
    public StandardMaterial3D ShellMaterial { get; set; }
    public StandardMaterial3D PhoneMaterial { get; set; }
    public StandardMaterial3D PoleMaterial { get; set; }

    private static readonly string[] StrangeCalls =
    {
        "Ninguém atende. Ao fundo, uma rádio toca uma música que não existe mais.",
        "Linha cruzada: duas pessoas discutem o preço do pão de sal.",
        "Uma voz chiada: \"não corra com o monitor CRT na caçamba...\"",
        "Tom de discagem. Depois, a sua própria voz dizendo \"alô?\".",
        "\"Bom dia, você ganhou um carnê das Casas Baixa!\" (tu-tu-tu)",
        "Só estática. Por um segundo, parece alguém lendo números ao contrário.",
        "Dona Cida: \"Minha barraca tem óculos de visão noturna, viu? Passa lá.\"",
        "Zé: \"Com o radar de pulso você nunca mais se perde, entregador.\"",
        "\"Correrios informa: sua encomenda está a caminho desde 1994.\"",
        "Uma gravação: \"Esta linha foi desativada pela frequência 96.6.\"",
    };

    private static readonly string[] TchauCalls =
    {
        "Gravação da tchau: \"Seus créditos acabaram antes de a ligação começar. tchau!\"",
        "tchau informa: o cartão telefônico de 20 unidades virou 2. Obrigado pela preferência.",
        "\"Você ligou para a tchau. Para continuar, insira mais fichas... ou desista.\"",
        "A ligação completa e alguém do outro lado só diz: \"tchau.\" (tu-tu-tu)",
    };

    private static readonly string[] MortoCalls =
    {
        "morto: \"A ligação caiu. Como sempre. Sinal: nenhum.\"",
        "\"Bem-vindo à morto, conectando você ao além desde 1998.\"",
        "morto informa: \"Área de cobertura: o cemitério da Rua Estática.\"",
        "Um silêncio total. Até o chiado da linha está morto.",
    };

    private AudioStreamPlayer3D _sound;
    private double _cooldown;
    private static readonly RandomNumberGenerator Rng = new();

    public override void _Ready()
    {
        CollisionLayer = 4; // Interactables
        CollisionMask = 0;
        Monitoring = false;
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1.0f, 1.2f, 1.0f) }, Position = new Vector3(0f, 1.6f, 0.15f) });

        AddToGroup(MinimapView.MarkerGroup);
        SetMeta("map_kind", "phone");
        SetMeta("map_color", new Color(1f, 0.55f, 0.1f));

        var body = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.15f, 2.4f, 0.15f) }, Position = new Vector3(0f, 1.2f, -0.35f) });
        AddChild(body);

        ShellMaterial ??= new StandardMaterial3D { AlbedoColor = BrandColor(Brand), CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        PhoneMaterial ??= new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.21f, 0.23f) };
        PoleMaterial ??= new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.1f, 0.12f) };

        // Poste.
        AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = 2.7f, RadialSegments = 8 },
            Position = new Vector3(0f, 1.35f, -0.38f),
            MaterialOverride = PoleMaterial,
        });
        // Concha: meia esfera deitada com a abertura para a rua.
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.55f, Height = 0.55f, IsHemisphere = true, RadialSegments = 16, Rings = 6 },
            Position = new Vector3(0f, 1.75f, -0.32f),
            Rotation = new Vector3(-Mathf.Pi * 0.5f, 0f, 0f),
            Scale = new Vector3(1f, 1.15f, 1.35f),
            MaterialOverride = ShellMaterial,
        });
        // Logo da operadora pintado na própria concha: decais projetam a textura na superfície curva
        // (atrás e nos dois lados), sem placa por cima.
        string logoPath = $"res://Assets/Textures/phone_logo_{BrandName(Brand)}.png";
        if (ResourceLoader.Exists(logoPath))
        {
            var logo = GD.Load<Texture2D>(logoPath);
            // As caixas dos três decais não se cruzam (antes o logo do lado vazava por cima do de trás).
            AddLogoDecal(logo, new Vector3(0f, 0f, -1f), new Vector3(0f, 1.84f, -0.9f), new Vector3(0.72f, 0.34f, 0.36f));
            AddLogoDecal(logo, new Vector3(-1f, 0f, 0f), new Vector3(-0.45f, 1.84f, -0.42f), new Vector3(0.42f, 0.4f, 0.21f));
            AddLogoDecal(logo, new Vector3(1f, 0f, 0f), new Vector3(0.45f, 1.84f, -0.42f), new Vector3(0.42f, 0.4f, 0.21f));
        }

        // Telefone, gancho e visor.
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.24f, 0.36f, 0.12f) }, Position = new Vector3(0f, 1.55f, -0.24f), MaterialOverride = PhoneMaterial });
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.24f, 0.07f) }, Position = new Vector3(-0.15f, 1.58f, -0.16f), MaterialOverride = PoleMaterial });
        var display = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.9f, 0.4f), EmissionEnabled = true, Emission = new Color(0.3f, 0.9f, 0.4f), EmissionEnergyMultiplier = 0.8f };
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.14f, 0.05f, 0.01f) }, Position = new Vector3(0f, 1.67f, -0.175f), MaterialOverride = display });

        _sound = new AudioStreamPlayer3D { Bus = AudioManager.SfxBus, UnitSize = 4f, Position = new Vector3(0f, 1.6f, 0f) };
        if (ResourceLoader.Exists("res://Audio/SFX/phone_call.wav"))
            _sound.Stream = GD.Load<AudioStream>("res://Audio/SFX/phone_call.wav");
        AddChild(_sound);
    }

    /// <summary>
    /// Decal projeta no seu -Y local. outward = lado da concha onde fica quem olha; a textura sai
    /// em pé e sem espelhar para essa pessoa. size = (largura, profundidade, altura).
    /// </summary>
    private void AddLogoDecal(Texture2D logo, Vector3 outward, Vector3 position, Vector3 size)
    {
        Vector3 right = (-outward).Cross(Vector3.Up);
        var decal = new Decal
        {
            TextureAlbedo = logo,
            Size = size,
            Transform = new Transform3D(new Basis(right, outward, Vector3.Down), position),
            NormalFade = 0.12f, // não pinta o lado de dentro da concha
            UpperFade = 0.05f,
            LowerFade = 0.05f,
            CullMask = 1,
        };
        AddChild(decal);
    }

    public override void _Process(double delta)
    {
        if (_cooldown > 0)
            _cooldown -= delta;
    }

    public static Color BrandColor(PhoneBrand brand) =>
        brand == PhoneBrand.Morto ? new Color(0.42f, 0.12f, 0.58f) : new Color(0.95f, 0.5f, 0.08f);

    public static string BrandName(PhoneBrand brand) => brand == PhoneBrand.Morto ? "morto" : "tchau";

    public string GetInteractionPrompt(PlayerController player) =>
        _cooldown > 0 ? "Orelhão (ocupado...)" : $"[E] Usar o orelhão da {BrandName(Brand)}";

    public bool CanInteract(PlayerController player) => _cooldown <= 0;

    public void Interact(PlayerController player)
    {
        _cooldown = 6.0;
        _sound.Play();
        string message = Rng.Randf() < 0.45f ? DeliveryTip(player) : null;
        if (message == null)
        {
            // Metade das ligações "normais" tem a cara da operadora.
            string[] pool = Rng.Randf() < 0.5f ? (Brand == PhoneBrand.Morto ? MortoCalls : TchauCalls) : StrangeCalls;
            message = pool[Rng.RandiRange(0, pool.Length - 1)];
        }
        // Mostra depois do "tu... tu..." da ligação.
        GetTree().CreateTimer(1.6).Timeout += () =>
        {
            if (IsInsideTree())
                GameHud.Toast(this, $"ORELHÃO {BrandName(Brand).ToUpperInvariant()}: " + message,
                    Brand == PhoneBrand.Morto ? new Color(0.85f, 0.6f, 1f) : new Color(1f, 0.75f, 0.35f), 6.0);
        };
    }

    /// <summary>A central informa a entrega pendente mais próxima de quem ligou.</summary>
    private string DeliveryTip(PlayerController player)
    {
        if (GetTree().GetFirstNodeInGroup("delivery_manager") is not DeliveryManager delivery || delivery.Jobs.Count == 0)
            return null;
        DeliveryJob best = null;
        float bestDistance = float.MaxValue;
        foreach (DeliveryJob job in delivery.Jobs)
        {
            float d = player.GlobalPosition.DistanceTo(job.Point.Position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = job;
            }
        }
        return best == null ? null : $"Central: entrega de {best.CargoName} na {best.Point.Address} ({bestDistance:0} m daqui).";
    }
}
