// Scripts/Core/GameVersion.cs
// Versão do jogo, novidades (changelog) e créditos.
// Ao lançar uma atualização: suba Current e adicione a entrada no topo de Changelog.
// O menu principal mostra as novidades automaticamente uma vez a cada versão nova
// (compara com GameSettings.LastSeenVersion, salvo em user://settings.cfg).
namespace DeadFrequency;

public static class GameVersion
{
    public const string Current = "0.9.1";

    public readonly record struct Release(string Version, string Date, string Title, string[] Changes);

    public static readonly Release[] Changelog =
    {
        new("0.9.1", "07/11/2026", "Orelhões de verdade", new[]
        {
            "Orelhões refeitos: concha oca de verdade (casca aberta na frente e embaixo, cinza por dentro, com borda), telefone com teclado lá dentro e o poste atrás.",
            "O logo aparece uma vez só, pintado na lateral da concha. O da morto agora é vertical; o da tchau tem a bolha amarela em volta do nome.",
            "Cada profissão começa no seu lugar de trabalho: locutor no estúdio da rádio, repositor na porta do Atacadinho, frentista no posto, gari na caçamba do depósito.",
            "A seta do topo da tela segue a profissão: rádio, mercado/estoque/prateleira, posto/carro, lixo/caçamba, passageiro/destino/van (entregador continua no depósito).",
            "Menu de pausa online mostra quem está na sala, com a cor, a profissão e quem é o anfitrião.",
            "Multiplayer: botão \"Salas online\" lista as salas abertas no servidor (com fase e jogadores) e entra com um clique.",
            "Tela de criar sala mais limpa: apelido e profissão na mesma linha e a engrenagem \"Sala\" com fase, profissões sorteadas e sala pública/privada. O botão Voltar não fica mais cortado.",
            "Mercado: os banners de oferta ficam entre as colunas da vitrine (não são mais cortados), letreiro acima da marquise e carrinhos de supermercado de verdade (cesto aramado, alça, cadeirinha e rodízios).",
            "Online: atualize o servidor com npm run deploy para a lista de salas (o protocolo continua 4).",
        }),
        new("0.9.0", "07/11/2026", "Cidade simulada", new[]
        {
            "Profissões! Escolha ao começar (ou deixe a sala online sortear): Entregador, Taxista, Gari, Repositor, Frentista e Locutor da 96.6, cada uma com as suas tarefas e pagamento.",
            "Taxista: pegue passageiros com a van e deixe no destino (corrida pela distância). Gari: recolha os sacos de lixo das calçadas e jogue nas caçambas.",
            "Repositor: reponha as prateleiras do Supermercado Atacadinho. Frentista: abasteça os carros que chegam no posto. Locutor: leia os comerciais no ar.",
            "Rádio Frequência 96.6 FM: estação com estúdio por dentro e torre de transmissão de 62 m. Cinco músicas originais, sem direitos autorais, tocando ao mesmo tempo para todo mundo.",
            "Rádio na caminhonete (tecla R), no escritório da empresa, no mercado, no saguão e no seu apartamento.",
            "Caminhonete nova: estilo Bongo dos anos 90, cabine avançada, carroceria de madeira e o rádio aceso no painel.",
            "Supermercado Atacadinho com interior: corredores, placas de seção, hortifrúti, padaria, frios, caixas com atendentes (E = comprar) e banners de oferta na porta.",
            "Edifício Sintonia: cada jogador ganha um apartamento. Dá para dormir e salvar o jogo, ver TV (jornal, clipe da 96.6, tempo) e passar o tempo de boas.",
            "Save do jogo na cama (user://save.tzn): profissão, turno, entregas e ganhos. O menu mostra o último save.",
            "Nomes das ruas no minimapa (cidade e rancho).",
            "Launcher: um programinha que baixa o jogo e instala as atualizações do GitHub sozinho. O menu também avisa quando sai versão nova.",
            "Outdoor \"siga twitch.tv/teuzin0dev · github.com/dieguesmosken\" e cartazes do recadin.com.br pela cidade.",
            "Correções: erro em série (ObjectDisposedException) ao jogar online depois de voltar ao menu; textos do quadro de avisos e do PC do escritório deslocados para a direita.",
            "Online: protocolo 4 (atualize o servidor com npm run deploy).",
        }),
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
            "   mobília do escritório e dos apartamentos, bancos, caçambas, caixa d'água, cemitério, abóboras, caixotes do hortifrúti e panela — licença CC0",
            "Operadoras \"tchau\" e \"morto\": paródias, sem relação com empresas reais.",
        }),
        new("Áudio original", new[]
        {
            "Músicas do menu, da noite e do dia, ambiente da cidade e efeitos de interface:",
            "   sintetizados por código (Python + NumPy), ver Audio/synth*.py.txt",
            "Rádio Frequência 96.6: \"Frequência Morta\" (Os Sintonizados), \"Bossa da Madrugada\" (Trio Estática),",
            "   \"Asfalto Molhado\" (Lo-Fi do Viaduto), \"Forró do Entregador\" (Zé da Van e Banda) e \"Neon na Rodovia\" (Cartucho 16)",
            "   — bandas fictícias; músicas originais geradas por código (Audio/synth_radio.py.txt), livres de direitos autorais",
        }),
        new("Paródias", new[]
        {
            "Os nomes de lojas são paródias e não têm relação com marcas reais.",
        }),
        new("Apoie o projeto", new[]
        {
            "twitch.tv/teuzin0dev · github.com/dieguesmosken · recadin.com.br",
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
