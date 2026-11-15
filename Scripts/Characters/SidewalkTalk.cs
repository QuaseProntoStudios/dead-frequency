// Scripts/Characters/SidewalkTalk.cs
// Diálogos de calçada (v0.9.5): balões curtos estilo PS1 em cima da cabeça dos NPCs quando o
// jogador passa perto. Frases do dia a dia, comentários sobre a caixa que ele carrega
// (a eterna briga do biscoito × bolacha), o chinelo, o caramelo e a madrugada.
// v0.10.1: falas com voz. Cada frase fixa tem um id; o áudio fica em Audio/Street/Calcada/<id>_m.ogg (homem)
// ou <id>_f.ogg (mulher), roteiro em docs/radio/falas_de_rua.csv. Sem o arquivo, só o balão aparece.
// Cada pedestre tem três frases do dia a dia "suas" (sorteadas pelo id do nó) e repete as mesmas.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public static class SidewalkTalk
{
    private static readonly RandomNumberGenerator Rng = new();
    private static ulong _lastTalkMs;

    /// <summary>Uma pessoa fala por vez (senão vira uma gritaria de balões).</summary>
    public static bool CanTalkNow() => Time.GetTicksMsec() - _lastTalkMs > 4500;

    /// <summary>Uma frase da calçada: id do áudio (null = sem voz, ex.: frase com o nome da carga) e texto.</summary>
    public readonly record struct Line(string Id, string Text);

    public const string VoiceFolder = "res://Audio/Street/Calcada/";
    /// <summary>Quantas frases do dia a dia cada pedestre tem.</summary>
    public const int LinesPerNpc = 3;

    private static readonly Line[] Daily =
    {
        new("rua_dia_01", "Esse ônibus 966 nunca passa no horário..."),
        new("rua_dia_02", "Tá sabendo? Disseram que a 96.6 toca sozinha de madrugada."),
        new("rua_dia_03", "O pão de sal subiu de novo, acredita?"),
        new("rua_dia_04", "Meu primo conserta qualquer coisa. Qualquer coisa MESMO."),
        new("rua_dia_05", "Ê calor... ou é frio? Nessa cidade nunca sei."),
        new("rua_dia_06", "Você viu um orelhão da Telefóbica? Dizem que ainda tem um."),
        new("rua_dia_07", "Eu só vim comprar um guaraná e já tô aqui faz duas horas."),
        new("rua_dia_08", "Essa neblina não sai nunca, né?"),
        new("rua_dia_09", "Ligaram no orelhão da tchau e caiu antes do alô."),
        new("rua_dia_10", "Vou na Tia da Quentinha que hoje é dia de feijoada."),
        new("rua_dia_11", "Cuidado com o Churrasquinho do Duvidoso... mas é bom, viu?"),
        new("rua_dia_12", "Já ouviu a vinheta nova da 96.6? Gruda na cabeça."),
        new("rua_dia_13", "Moço, onde fica a Rua Estática? Ninguém sabe me dizer."),
    };

    private static readonly Line[] Cargo =
    {
        new("rua_carga_01", "Isso aí na caixa é biscoito ou bolacha? Porque é BOLACHA, tá?"),
        new("rua_carga_02", "Biscoito! Lá em casa sempre foi biscoito. Pode entregar que eu confirmo."),
        new(null, "Cuidado com essa {0}, hein. Não vai derrubar!"),
        new(null, "Essa {0} é pra mim? Não? Que pena..."),
        new(null, "Entregador de {0} a essa hora? Coragem."),
        new("rua_carga_03", "Bolacha é o que a gente come, biscoito é o que a gente vende. Anota aí."),
    };

    private static readonly Line[] Night =
    {
        new("rua_noite_01", "Essa hora na rua? Toma um Café Marreta que passa."),
        new("rua_noite_02", "Madrugada aqui é esquisita... o rádio chia sozinho."),
        new("rua_noite_03", "Tô morrendo de sono. Você também tá com essa cara."),
    };

    private static readonly Line[] Slippers =
    {
        new("rua_chinelo_01", "Ploc, ploc, ploc... de Legítimas, é? Respeito."),
        new("rua_chinelo_02", "Correr de chinelo na lama? Prepara o prego."),
    };

    private static readonly Line[] Dog =
    {
        new("rua_cao_01", "Que caramelo bonito! É seu?"),
        new("rua_cao_02", "Esse caramelo não larga de você, hein."),
    };

    /// <summary>Todas as frases com voz (para conferir a planilha docs/radio/falas_de_rua.csv).</summary>
    public static IEnumerable<Line> VoicedLines()
    {
        foreach (Line[] group in new[] { Daily, Cargo, Night, Slippers, Dog })
            foreach (Line l in group)
                if (l.Id != null)
                    yield return l;
    }

    /// <summary>As frases do dia a dia de um pedestre: sempre as mesmas para a mesma semente.</summary>
    public static Line[] PickOwnLines(ulong seed)
    {
        var rng = new RandomNumberGenerator { Seed = seed };
        var pool = new List<Line>(Daily);
        var own = new Line[Mathf.Min(LinesPerNpc, pool.Count)];
        for (int i = 0; i < own.Length; i++)
        {
            int k = rng.RandiRange(0, pool.Count - 1);
            own[i] = pool[k];
            pool.RemoveAt(k);
        }
        return own;
    }

    private static Line Pick(Line[] lines) => lines[Rng.RandiRange(0, lines.Length - 1)];

    /// <summary>
    /// Escolhe uma frase pensando no que o jogador está fazendo. Sem contexto, sai uma das frases
    /// próprias do pedestre (<paramref name="own"/>) ou, sem elas, qualquer uma do dia a dia.
    /// </summary>
    public static Line PickLine(PlayerController player, Line[] own)
    {
        Rng.Randomize();
        if (player != null && player.HeldItem is CargoItem held && Rng.Randf() < 0.6f)
        {
            Line c = Pick(Cargo);
            return c.Id != null ? c : new Line(null, string.Format(c.Text, held.Data?.CargoName ?? "caixa"));
        }
        if (FarmAnimal.Companion != null && Rng.Randf() < 0.4f)
            return Pick(Dog);
        if (PlayerInventory.Instance?.IsEquipped(ItemCatalog.Slippers) == true && Rng.Randf() < 0.3f)
            return Pick(Slippers);
        float h = DayNightController.Hour;
        if ((h >= 23f || h < 5f) && Rng.Randf() < 0.4f)
            return Pick(Night);
        return own is { Length: > 0 } ? Pick(own) : Pick(Daily);
    }

    // Áudios já procurados (null = não existe), para não perguntar ao ResourceLoader toda vez.
    private static readonly Dictionary<string, AudioStream> VoiceCache = new();

    /// <summary>Áudio da frase na voz de homem ou mulher; se só existir uma das vozes, usa a que tiver.</summary>
    public static AudioStream VoiceFor(string id, bool woman)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        return LoadVoice(id + (woman ? "_f" : "_m")) ?? LoadVoice(id + (woman ? "_m" : "_f"));
    }

    private static AudioStream LoadVoice(string name)
    {
        if (VoiceCache.TryGetValue(name, out AudioStream cached))
            return cached;
        string path = VoiceFolder + name + ".ogg";
        AudioStream stream = ResourceLoader.Exists(path) ? ResourceLoader.Load<AudioStream>(path) : null;
        VoiceCache[name] = stream;
        return stream;
    }

    /// <summary>Mostra o balão em cima de quem fala e, se a frase tiver áudio, toca a voz (só se ouve de perto).</summary>
    public static void Say(Node3D speaker, Line line, bool woman, float height = 2.15f)
    {
        AudioStream voice = VoiceFor(line.Id, woman);
        float seconds = 4f;
        if (voice != null && speaker != null && GodotObject.IsInstanceValid(speaker) && speaker.IsInsideTree())
        {
            speaker.GetNodeOrNull<AudioStreamPlayer3D>("Voz")?.QueueFree();
            var player = new AudioStreamPlayer3D
            {
                Name = "Voz",
                Stream = voice,
                Bus = AudioManager.SfxBus,
                UnitSize = 3f,
                MaxDistance = 14f,
                Position = new Vector3(0f, height - 0.45f, 0f),
                PitchScale = (float)GD.RandRange(0.96, 1.04), // cada pessoa soa um pouquinho diferente
            };
            speaker.AddChild(player);
            player.Finished += player.QueueFree;
            player.Play();
            seconds = Mathf.Max(seconds, (float)voice.GetLength() + 0.6f);
        }
        Say(speaker, line.Text, height, seconds);
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
