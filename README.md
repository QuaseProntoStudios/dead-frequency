# Dead Frequency

Jogo de entregas em primeira pessoa, estilo PS1, feito em **Godot 4.7 (.NET / C#)**.
Você é o entregador do turno da noite: pega as caixas no depósito, carrega a van de 1996 e entrega
pela cidade (ou pelo rancho) antes que tudo quebre. Tem física nas caixas, ciclo de dia e noite,
itens, lojas paródia e multiplayer online.

**Versão atual:** v0.8.0 · veja o [CHANGELOG](CHANGELOG.md) · roteiro em [ROADMAP](ROADMAP.md)

## Como rodar

1. Instale o **Godot 4.7 .NET** e o **.NET SDK 10** (`winget install Microsoft.DotNet.SDK.10`).
2. Abra a pasta no Godot (Importar → `project.godot`).
3. Clique no martelo (**Build**) e depois em **Executar** (F5).

### Controles

| Tecla | Ação |
|---|---|
| WASD / Shift / Espaço | andar, correr, pular (na van: freio de mão) |
| E | pegar caixa, entrar na van, usar orelhão/PC/barraca |
| Clique esquerdo | arremessar |
| F | lanterna |
| I ou Tab | inventário |
| N | visão noturna (com o item) |
| Esc / P | pausa |

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
- Lojas, bancos e marcas do jogo são **paródias** e não têm relação com empresas reais.
