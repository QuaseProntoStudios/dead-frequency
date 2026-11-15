// Scripts/Characters/SidewalkTalk.cs
// Diálogos de calçada (v0.9.5): balões curtos estilo PS1 em cima da cabeça dos NPCs quando o
// jogador passa perto. Frases do dia a dia, comentários sobre a caixa que ele carrega
// (a eterna briga do biscoito × bolacha), o chinelo, o caramelo e a madrugada.
// v0.10.1: falas com voz. Cada tipo de morador (os 6 modelos do Meshy em CharacterModel) tem três falas
// próprias, gravadas no Google AI Studio (docs/radio/VOZES.md, roteiro em docs/radio/falas_de_rua.csv) e salvas
// em Audio/Street/<tipo>_<n>.ogg. Só se ouvem de perto; sem o arquivo, a fala aparece só no balão.
// As frases genéricas abaixo continuam só no balão.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public static class SidewalkTalk
{
    private static readonly RandomNumberGenerator Rng = new();
    private static ulong _lastTalkMs;

    /// <summary>Uma pessoa fala por vez (senão vira uma gritaria de balões).</summary>
    public static bool CanTalkNow() => Time.GetTicksMsec() - _lastTalkMs > 4500;

    public const string VoiceFolder = "res://Audio/Street/";

    /// <summary>Falas com voz de cada tipo de morador (CharacterModel.NpcKind). O áudio é Audio/Street/&lt;tipo&gt;_&lt;n&gt;.ogg, n a partir de 1.</summary>
    public static readonly Dictionary<string, string[]> KindLines = new()
    {
        ["npc_senhor"] = new[]
        {
            "No meu tempo a gente entregava tudo de bicicleta, sabia?",
            "Vou ali na praça jogar dominó. Se a patroa perguntar, eu fui no médico.",
            "Liga o rádio na 96.6 que daqui a pouco tem o programa do Seu Waldir.",
        },
        ["npc_rapaz"] = new[]
        {
            "E aí, firmeza? Tá entregando o quê hoje?",
            "Tô juntando pra comprar um Fuska. Falta só... quase tudo.",
            "Viu o jogo ontem? Nem eu, caiu a antena de novo.",
        },
        ["npc_escritorio"] = new[]
        {
            "Atrasado de novo... o relógio de ponto não perdoa.",
            "Três reuniões hoje e nenhuma resolveu nada.",
            "Será que o fax chegou? Mandei faz dois dias.",
        },
        ["npc_moca"] = new[]
        {
            "Oi! Você sabe se as lojas do shopping já abriram?",
            "Tô esperando o 966 faz meia hora. Já decorei o ponto inteiro.",
            "Pede pra tocar a minha música na 96.6? Eu liguei três vezes e nada.",
        },
        ["npc_dona"] = new[]
        {
            "Moço, vê se não amassa a minha encomenda, hein!",
            "O carro do ovo passou e eu perdi. De novo!",
            "Feijão no fogo e eu aqui conversando. Deixa eu ir!",
        },
        ["npc_senhora"] = new[]
        {
            "Ai, meu filho, me ajuda a atravessar? Brincadeira, eu atravesso sozinha.",
            "Você é neto da Dona Cida? Tem a cara dela.",
            "Leva um casaco, que essa neblina pega a gente.",
        },
    };

    // Áudios já procurados (null = não existe), para não perguntar ao ResourceLoader toda vez.
    private static readonly Dictionary<string, AudioStream> VoiceCache = new();

    /// <summary>Sorteia uma das falas do tipo de morador; false se o tipo não tem falas próprias.</summary>
    public static bool TryKindLine(string kind, out int index, out string text)
    {
        index = -1;
        text = null;
        if (string.IsNullOrEmpty(kind) || !KindLines.TryGetValue(kind, out string[] lines) || lines.Length == 0)
            return false;
        Rng.Randomize();
        index = Rng.RandiRange(0, lines.Length - 1);
        text = lines[index];
        return true;
    }

    /// <summary>Áudio da fala n (a partir de 0) do tipo de morador, ou null se ainda não foi gravado.</summary>
    public static AudioStream VoiceFor(string kind, int index)
    {
        string path = $"{VoiceFolder}{kind}_{index + 1}.ogg";
        if (!VoiceCache.TryGetValue(path, out AudioStream stream))
        {
            stream = ResourceLoader.Exists(path) ? ResourceLoader.Load<AudioStream>(path) : null;
            VoiceCache[path] = stream;
        }
        return stream;
    }

    /// <summary>Fala de um morador com voz: toca o áudio (ouvido só de perto) e mostra o balão enquanto dura.</summary>
    public static void SayVoiced(Node3D speaker, string kind, int index, string text, float height = 2.15f)
    {
        if (speaker == null || !GodotObject.IsInstanceValid(speaker) || !speaker.IsInsideTree())
            return;
        float seconds = 4f;
        AudioStream voice = VoiceFor(kind, index);
        if (voice != null)
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
            };
            speaker.AddChild(player);
            player.Finished += player.QueueFree;
            player.Play();
            seconds = Mathf.Max(seconds, (float)voice.GetLength() + 0.6f);
        }
        Say(speaker, text, height, seconds);
    }

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
