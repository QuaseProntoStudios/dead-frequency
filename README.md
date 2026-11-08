# Dead Frequency

Cidade simulada em primeira pessoa, estilo PS1, feita em **Godot 4.7 (.NET / C#)**.
Escolha uma profissão — entregador, taxista, gari, repositor, frentista ou locutor da rádio 96.6 —,
ganhe um apartamento no Edifício Sintonia e ajude a cidade da zona morta a funcionar, sozinho ou com
os amigos online. Tem física nas caixas, ciclo de dia e noite, rádio com músicas originais, itens,
lojas paródia e multiplayer.

**Versão atual:** v0.9.0 · veja o [CHANGELOG](CHANGELOG.md) · roteiro em [ROADMAP](ROADMAP.md)

## Baixar e jogar

Baixe o **launcher** em [Releases](https://github.com/dieguesmosken/dead-frequency/releases/latest)
(`DeadFrequencyLauncher-windows.exe` ou `-linux`), coloque numa pasta só dele e abra: ele baixa o jogo
e instala as próximas atualizações sozinho. Também dá para baixar o zip do jogo direto.

## Rodar pelo código

1. Instale o **Godot 4.7 .NET** e o **.NET SDK 10** (`winget install Microsoft.DotNet.SDK.10`).
2. Abra a pasta no Godot (Importar → `project.godot`).
3. Clique no martelo (**Build**) e depois em **Executar** (F5).

### Controles

| Tecla | Ação |
|---|---|
| WASD / Shift / Espaço | andar, correr, pular (na van: freio de mão) |
| E | pegar caixa, entrar na van, usar orelhão/PC/barraca, elevador, cama (salvar), TV, rádio |
| R | rádio 96.6 da caminhonete |
| Clique esquerdo | arremessar |
| F | lanterna |
| I ou Tab | inventário |
| N | visão noturna (com o item) |
| Esc / P | pausa |

## Profissões

| Profissão | Tarefa |
|---|---|
| Entregador | caixas do depósito até o feixe verde (paga pela integridade) |
| Taxista | passageiros com a van, corrida pela distância |
| Gari | sacos de lixo das calçadas para as caçambas |
| Repositor | caixas do estoque para as prateleiras do Atacadinho |
| Frentista | abastecer os carros que chegam no posto |
| Locutor da 96.6 | ler os comerciais quando a luz NO AR acender |

No online, quem cria a sala pode deixar o servidor escolher a profissão de cada um.

## Fases

- **Fase 1 · Cidade:** prédios, ônibus, pedestres, lojas, bancos, postos, bombeiros e delegacia.
- **Fase 2 · Rancho:** estradas de terra, sítios, lago com pesqueiro, capela e cemitério.

## Multiplayer

No menu **Multiplayer**:

- **Servidor oficial:** o servidor do projeto na Cloudflare (padrão).
- **Meu servidor:** o endereço de outro servidor ou o link de um amigo que hospeda.
- **Hospedar no meu PC:** o jogo vira o servidor e gera um link público grátis com o Cloudflare Tunnel.

O servidor fica em [`server/`](server/) (Cloudflare Workers + Durable Objects). Para publicar:

```bash
cd server
npm install
npx wrangler login
npm run deploy
```

## Créditos

- Criação: Matheus (teuzin0dev). Código, cidade procedural, servidor e áudio sintetizado feitos com o Claude (Anthropic).
- [Godot Engine](https://godotengine.org) (MIT).
- [Kenney](https://kenney.nl): Starter Kit Racing (CC0).
- [KayKit](https://kaylousberg.com), de Kay Lousberg: City Builder, Furniture, Halloween e Restaurant Bits (CC0).
- Músicas, efeitos, letreiros e anúncios: gerados por código (`Audio/*.py.txt`, `Assets/Textures/Stores/*.py.txt`).
- Rádio 96.6: cinco músicas originais de bandas fictícias, geradas por `Audio/synth_radio.py.txt` (livres de direitos autorais).
- Siga [twitch.tv/teuzin0dev](https://twitch.tv/teuzin0dev) · [github.com/dieguesmosken](https://github.com/dieguesmosken) · [recadin.com.br](https://recadin.com.br)
- Lojas, bancos e marcas do jogo são **paródias** e não têm relação com empresas reais.
