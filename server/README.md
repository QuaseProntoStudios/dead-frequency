# Servidor multiplayer — Dead Frequency

Worker da Cloudflare com um **Durable Object por sala**. Cabe no plano gratuito.

## O que o servidor decide
- Código da sala (5 caracteres, sem 0/O/1/I) e até 8 jogadores por sala.
- Seed da cidade: todos geram exatamente a mesma cidade e os mesmos endereços.
- Turnos, pedidos, pagamento (base × integridade) e o dinheiro do grupo.
- **Posse dos objetos:** quem pega uma caixa ou senta na van vira o dono. Só o dono pode mandar
  o estado daquele objeto ou reportar a entrega. Ninguém toma a van de quem está dirigindo
  nem a caixa da mão de outro.
- Anfitrião (passa para o próximo se ele sair). Sala vazia é apagada depois de 30 minutos.

A física continua no Godot: o dono simula e os outros só acompanham o estado recebido.

## Rodar no seu PC (teste)
```bash
cd server
npm install
npm run dev          # sobe em http://localhost:8787
npm test             # em outro terminal: teste automático (26 checagens, inclui o painel)
```
No jogo: **Multiplayer → Servidor:** `http://localhost:8787` → **Criar sala**.
Para testar com 2 janelas, abra o jogo exportado duas vezes, ou rode pelo editor
(Depuração → Executar várias instâncias).

## Colocar no ar (grátis)
```bash
cd server
npx wrangler login   # abre o navegador para entrar na sua conta Cloudflare
npm run deploy
```
O Wrangler mostra o endereço, algo como `https://dead-frequency.teuzin0dev.workers.dev`.
Coloque esse endereço no campo **Servidor** do jogo. Seus amigos usam o mesmo endereço
e o código da sala.

## Painel de admin
Página com jogadores online, salas, entregas, dinheiro, detalhes de cada sala (quem está dirigindo,
quem segura cada caixa), aviso para todas as salas e botão para encerrar uma sala.

1. Crie o token (uma senha longa qualquer):
   ```bash
   npx wrangler secret put ADMIN_TOKEN
   ```
2. Faça o deploy de novo (`npm run deploy`) e abra **https://dead-frequency.teuzin0dev.workers.dev/admin**.
3. Digite o mesmo token. Ele fica salvo só no seu navegador.

No teste local, copie `.dev.vars.example` para `.dev.vars` e use o token `teste-local`
em http://localhost:8787/admin.

## Limites do plano gratuito
Cada mensagem recebida por WebSocket conta como 1/20 de requisição de Durable Object.
O jogo manda 10 atualizações por segundo por jogador: uma partida de 1 hora com 4 jogadores
dá cerca de 7 mil requisições, bem abaixo do limite diário gratuito.

## Protocolo (JSON por WebSocket)
| Cliente → servidor | Para quê |
|---|---|
| `hello {version}` | entrar de fato na sala |
| `tick {p, ry, hp, st, held, objs[]}` | posição do jogador + objetos que ele controla |
| `claim {id}` / `release {id}` | pedir/soltar posse (`van` ou id da caixa) |
| `deliver {cargoId, integ}` / `lost {cargoId}` | entrega concluída / caixa destruída |
| `chat {msg}` · `ping {ts}` | |

| Servidor → cliente | |
|---|---|
| `welcome` | estado completo: seed, jogadores, pedidos, donos, objetos |
| `join` · `leave` · `host` | entrada/saída/anfitrião |
| `tick` | estado de outro jogador (só objetos que ele realmente controla) |
| `owner` · `claimFail` | mudança de posse / pedido negado |
| `shift` · `delivered` · `lost` · `shiftDone` | turnos e entregas |

**Importante:** a lista `CARGO_CATALOG` em `src/index.ts` precisa ter a mesma ordem de
`DeliveryManager.CargoCatalog` no Godot.

## Limitação conhecida
O servidor confia no cliente sobre a posição e a integridade da caixa entregue (não roda física).
Para jogar com amigos isso basta; contra trapaça séria seria preciso validar posições no servidor.
