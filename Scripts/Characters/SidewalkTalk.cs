// Scripts/Characters/SidewalkTalk.cs
// Diálogos de calçada (v0.9.5): balões curtos estilo PS1 em cima da cabeça dos NPCs quando o
// jogador passa perto. Frases do dia a dia, comentários sobre a caixa que ele carrega
// (a eterna briga do biscoito × bolacha), o chinelo, o caramelo e a madrugada.
using Godot;

namespace DeadFrequency;

public static class SidewalkTalk
{
    private static readonly RandomNumberGenerator Rng = new();
    private static ulong _lastTalkMs;

    /// <summary>Uma pessoa fala por vez (senão vira uma gritaria de balões).</summary>
    public static bool CanTalkNow() => Time.GetTicksMsec() - _lastTalkMs > 4500;

    private static readonly string[] Daily =
    {
        "Esse ônibus 966 nunca passa no horário...",
        "Tá sabendo? Disseram que a 96.6 toca sozinha de madrugada.",
        "O pão de sal subiu de novo, acredita?",
        "Meu primo conserta qualquer coisa. Qualquer coisa MESMO.",
        "Ê calor... ou é frio? Nessa cidade nunca sei.",
        "Você viu um orelhão da Telefóbica? Dizem que ainda tem um.",
        "Eu só vim comprar um guaraná e já tô aqui faz duas horas.",
        "Essa neblina não sai nunca, né?",
        "Ligaram no orelhão da tchau e caiu antes do alô.",
        "Vou na Tia da Quentinha que hoje é dia de feijoada.",
        "Cuidado com o Churrasquinho do Duvidoso... mas é bom, viu?",
        "Já ouviu a vinheta nova da 96.6? Gruda na cabeça.",
        "Moço, onde fica a Rua Estática? Ninguém sabe me dizer.",
    };

    private static readonly string[] Cargo =
    {
        "Isso aí na caixa é biscoito ou bolacha? Porque é BOLACHA, tá?",
        "Biscoito! Lá em casa sempre foi biscoito. Pode entregar que eu confirmo.",
        "Cuidado com essa {0}, hein. Não vai derrubar!",
        "Essa {0} é pra mim? Não? Que pena...",
        "Entregador de {0} a essa hora? Coragem.",
        "Bolacha é o que a gente come, biscoito é o que a gente vende. Anota aí.",
    };

    private static readonly string[] Night =
    {
        "Essa hora na rua? Toma um Café Marreta que passa.",
        "Madrugada aqui é esquisita... o rádio chia sozinho.",
        "Tô morrendo de sono. Você também tá com essa cara.",
    };

    private static readonly string[] Slippers =
    {
        "Ploc, ploc, ploc... de Legítimas, é? Respeito.",
        "Correr de chinelo na lama? Prepara o prego.",
    };

    private static readonly string[] Dog =
    {
        "Que caramelo bonito! É seu?",
        "Esse caramelo não larga de você, hein.",
    };

    /// <summary>Escolhe uma frase pensando no que o jogador está fazendo.</summary>
    public static string LineFor(PlayerController player)
    {
        Rng.Randomize();
        if (player != null && player.HeldItem is CargoItem held && Rng.Randf() < 0.6f)
            return string.Format(Cargo[Rng.RandiRange(0, Cargo.Length - 1)], held.Data?.CargoName ?? "caixa");
        if (FarmAnimal.Companion != null && Rng.Randf() < 0.4f)
            return Dog[Rng.RandiRange(0, Dog.Length - 1)];
        if (PlayerInventory.Instance?.IsEquipped(ItemCatalog.Slippers) == true && Rng.Randf() < 0.3f)
            return Slippers[Rng.RandiRange(0, Slippers.Length - 1)];
        float h = DayNightController.Hour;
        if ((h >= 23f || h < 5f) && Rng.Randf() < 0.4f)
            return Night[Rng.RandiRange(0, Night.Length - 1)];
        return Daily[Rng.RandiRange(0, Daily.Length - 1)];
    }

    /// <summary>Mostra o balão em cima de quem fala por alguns segundos.</summary>
    public static void Say(Node3D speaker, string text, float height = 2.15f, float seconds = 4f)
    {
        if (speaker == null || !GodotObject.IsInstanceValid(speaker) || !speaker.IsInsideTree())
            return;
        speaker.GetNodeOrNull<Label3D>("Balao")?.QueueFree();
        _lastTalkMs = Time.GetTicksMsec();
        var label = new Label3D
        {
            Name = "Balao",
            Text = Wrap(text, 30),
            FontSize = 32,
            PixelSize = 0.0026f,
            Position = new Vector3(0f, height, 0f),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(1f, 1f, 0.92f),
            OutlineModulate = new Color(0.02f, 0.03f, 0.05f),
            OutlineSize = 14,
            NoDepthTest = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            FixedSize = false,
        };
        speaker.AddChild(label);
        Tween tw = label.CreateTween();
        tw.TweenProperty(label, "position:y", height + 0.12f, 0.25);
        tw.TweenInterval(seconds);
        tw.TweenProperty(label, "modulate:a", 0f, 0.4);
        tw.TweenCallback(Callable.From(label.QueueFree));
    }

    private static string Wrap(string text, int width)
    {
        var sb = new System.Text.StringBuilder();
        int col = 0;
        foreach (string word in text.Split(' '))
        {
            if (col > 0 && col + word.Length > width)
            {
                sb.Append('\n');
                col = 0;
            }
            else if (col > 0)
            {
                sb.Append(' ');
                col++;
            }
            sb.Append(word);
            col += word.Length;
        }
        return sb.ToString();
    }
}
