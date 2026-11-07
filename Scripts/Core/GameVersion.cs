// Scripts/Core/GameVersion.cs
// Versão do jogo, novidades (changelog) e créditos.
// Ao lançar uma atualização: suba Current e adicione a entrada no topo de Changelog.
// O menu principal mostra as novidades automaticamente uma vez a cada versão nova
// (compara com GameSettings.LastSeenVersion, salvo em user://settings.cfg).
namespace DeadFrequency;

public static class GameVersion
{
    public const string Current = "0.8.0";

    public readonly record struct Release(string Version, string Date, string Title, string[] Changes);

    public static readonly Release[] Changelog =
    {
        new("0.8.0", "07/11/2026", "Cidade comercial", new[]
        {
            "Bancos (Caixote Econômica, Etaú, Bravesco, Santo Ander), lotéricas, padarias e supermercados paródia, cada um com letreiro próprio.",
            "Postos Petrobraz, Ipiranguinha e Shello com cobertura, bombas, loja de conveniência e totem de preços; e o Lava-Jato Molhadinho com escovões girando.",
            "Caixa d'água municipal numa praça, com refletores à noite.",
            "Outdoors na beira da cidade, iluminados de baixo para cima à noite. Os anúncios mudam a cada partida.",
            "Ônibus da Viação Frequência, linha 966, com letreiro de LED e anúncios na lateral e na traseira.",
            "O ônibus buzina duas vezes quando você está na frente. Se não sair... é atropelado e voa longe (e larga a caixa).",
            "Pedestres desviam de postes, placas, orelhões, bancos, caçambas, pontos de ônibus e de você.",
            "Toldos das lojas inclinados para a calçada: agora dá para ler as faixas.",
            "Cada encomenda tem a sua cara: monitor de tubo, bateria, maleta, gerador, caixote de válvulas...",
            "Seis encomendas novas: botijão de gás, bolo de casamento, aquário com peixe, antena parabólica, caixote de tomates e panela de feijoada.",
            "Online: protocolo 3 (atualize o servidor com npm run deploy).",
        }),
        new("0.7.0", "07/11/2026", "Hospede no seu PC", new[]
        {
            "Multiplayer com três opções: Servidor oficial (padrão, sem precisar de endereço), Meu servidor e Hospedar no meu PC.",
            "Hospedar no meu PC: o jogo vira o servidor da sala e gera um link público grátis (Cloudflare Tunnel, tipo ngrok). Na primeira vez ele baixa o cloudflared oficial.",
            "Amigos na mesma rede também entram pelo IP do PC, sem túnel.",
            "\"Colar convite\" já preenche o código e o servidor do amigo.",
        }),
        new("0.6.2", "07/11/2026", "Letreiros e orelhões", new[]
        {
            "Lojas com letreiros de verdade: arte própria para cada uma das 12 paródias (ícone, nome e slogan), com moldura e luz à noite.",
            "Orelhões com o logo da tchau e da morto pintado na própria concha (atrás e dos lados), sem plaquinha em cima.",
        }),
        new("0.6.1", "07/11/2026", "Assets novos", new[]
        {
            "Modelos 3D gratuitos (CC0) da KayKit, de Kay Lousberg: mobília, cidade e Halloween.",
            "Escritório mobiliado: cadeira, sofá, mesinha com livros, tapete, abajur, prateleira, quadro e cacto na mesa.",
            "Cidade com bancos nas calçadas e caçambas de lixo (algumas com caixas de papelão do lado).",
            "Rancho: caixa d'água na cooperativa, Cemitério da Frequência ao lado da capela (com lanternas verdes), canteiros de abóbora, abóboras iluminadas nas varandas e árvores secas.",
        }),
        new("0.6.0", "07/11/2026", "Fase 2: Rancho", new[]
        {
            "Nova fase: o Rancho. Estradas de terra, sítios e fazendas para entregar, lago com pesqueiro, capela e mato fechado.",
            "Cada sítio tem casa com varanda, celeiro, silo, plantação de milho ou soja, curral com vacas ou porcos, trator e cata-vento girando.",
            "A Cooperativa Rancho Fundo é o depósito da roça (com o mesmo escritório e PC).",
            "Iniciar turno agora pergunta a fase. No multiplayer, quem cria a sala escolhe a fase e todos vão juntos.",
            "Orelhões das operadoras \"tchau\" (laranja) e \"morto\" (roxo), cada uma com suas ligações.",
            "O PC do escritório abre em tela cheia (E): relatório, histórico e pedidos abertos, fácil de ler.",
            "Minimapa desenha as estradas do rancho e marca as porteiras.",
        }),
        new("0.5.0", "07/11/2026", "Turno da madrugada", new[]
        {
            "Ciclo de dia e noite: o sol nasce no leste e se põe no oeste, a lua aparece à noite e a cidade acende aos poucos.",
            "Relógio no topo da tela. Em Opções: ciclo, sempre noite ou sempre dia, e a duração do dia.",
            "Música nova para o dia; à noite volta a trilha da frequência morta.",
            "A luz da lua vinha de baixo para cima; agora ilumina do céu para o chão.",
            "Depósito por dentro: prateleiras por setor, paletes com produtos, empilhadeira e carrinho.",
            "Escritório com PC de tubo mostrando caixa, ganhos, turno e histórico de entregas (E troca a página).",
            "Frota no pátio: caminhões e moto de entrega (modelos CC0 da Kenney).",
            "Van com para-choques, grade, piscas, retrovisores, placa DFQ-1996 e o nome da empresa nas portas.",
            "Carros com grade nova, piscas, retrovisores e maçanetas; a grade parou de piscar (z-fighting).",
            "Pneu cantando nas curvas e no freio de mão, e novos sons de batida (Kenney, CC0).",
            "Avisos do orelhão não ficam mais por trás do painel de pedido.",
            "Telas de Novidades e Créditos no menu.",
        }),
        new("0.4.0", "07/11/2026", "Cidade viva", new[]
        {
            "Orelhões com dicas da central (e ligações estranhas).",
            "Ar-condicionados nas fachadas, lojas paródia, corpo de bombeiros e delegacia.",
            "Placas de PARE corrigidas, pedestres andando para a frente e vidros dos carros acertados.",
        }),
        new("0.3.0", "07/11/2026", "Equipamento", new[]
        {
            "Inventário e mochila; radar com minimapa, visão noturna, luvas e consumíveis.",
            "Barracas de rua que vendem itens; seta apontando para o depósito.",
            "Tela de game over, sons de batida, ônibus com pontos, placas e faixas de pedestre.",
        }),
        new("0.2.0", "07/11/2026", "Online", new[]
        {
            "Multiplayer com salas por código no servidor da Cloudflare e painel /admin.",
            "Modo dia, passos novos e correção da van flutuando no online.",
        }),
        new("0.1.0", "07/11/2026", "Primeiro turno", new[]
        {
            "Andar, pegar e arremessar caixas, dirigir a van e entregar pela cidade gerada.",
            "Menu, pausa, opções, música e efeitos sintetizados.",
        }),
    };

    public readonly record struct Credit(string Section, string[] Lines);

    public static readonly Credit[] Credits =
    {
        new("Criação", new[]
        {
            "Matheus (teuzin0dev) — direção, design e testes",
            "Código, cidade procedural, servidor e áudio sintetizado feitos com o Claude (Anthropic)",
        }),
        new("Motor", new[]
        {
            "Godot Engine 4.7 (.NET) — licença MIT · godotengine.org",
            "Servidor multiplayer: Cloudflare Workers + Durable Objects",
            "Link público ao hospedar: cloudflared (Cloudflare Quick Tunnels)",
        }),
        new("Modelos 3D e sons de terceiros", new[]
        {
            "Kenney · www.kenney.nl — Starter Kit Racing (github.com/KenneyNL/Starter-Kit-Racing)",
            "   caminhões, moto, pneu cantando e batida — licença CC0 (domínio público)",
            "KayKit · Kay Lousberg (www.kaylousberg.com) — City Builder Bits, Furniture Bits, Halloween Bits e Restaurant Bits",
            "   mobília do escritório, bancos, caçambas, caixa d'água, cemitério, abóboras, caixote de tomates e panela — licença CC0",
            "Operadoras \"tchau\" e \"morto\": paródias, sem relação com empresas reais.",
        }),
        new("Áudio original", new[]
        {
            "Músicas do menu, da noite e do dia, ambiente da cidade e efeitos de interface:",
            "   sintetizados por código (Python + NumPy), ver Audio/synth*.py.txt",
        }),
        new("Paródias", new[]
        {
            "Os nomes de lojas são paródias e não têm relação com marcas reais.",
        }),
        new("Obrigado por jogar", new[]
        {
            "Sintonize 96.6 e boa entrega.",
        }),
    };

    /// <summary>Texto BBCode das novidades. onlyNewerThan = mostra só o que veio depois dessa versão.</summary>
    public static string ChangelogBbcode(string onlyNewerThan = null)
    {
        var sb = new System.Text.StringBuilder();
        foreach (Release r in Changelog)
        {
            if (!string.IsNullOrEmpty(onlyNewerThan) && Compare(r.Version, onlyNewerThan) <= 0)
                continue;
            sb.Append($"[color=#8cff9e][b]v{r.Version} — {r.Title}[/b][/color]  [color=#478c54]{r.Date}[/color]\n");
            foreach (string c in r.Changes)
                sb.Append($" • {c}\n");
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public static string CreditsBbcode()
    {
        var sb = new System.Text.StringBuilder();
        foreach (Credit c in Credits)
        {
            sb.Append($"[color=#ffb840][b]{c.Section.ToUpperInvariant()}[/b][/color]\n");
            foreach (string l in c.Lines)
                sb.Append(l).Append('\n');
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Compara "0.5.0" com "0.4.2". Versões vazias contam como mais antigas.</summary>
    public static int Compare(string a, string b)
    {
        string[] pa = (a ?? "").Split('.'), pb = (b ?? "").Split('.');
        for (int i = 0; i < 3; i++)
        {
            int x = i < pa.Length && int.TryParse(pa[i], out int vx) ? vx : 0;
            int y = i < pb.Length && int.TryParse(pb[i], out int vy) ? vy : 0;
            if (x != y)
                return x.CompareTo(y);
        }
        return 0;
    }
}
