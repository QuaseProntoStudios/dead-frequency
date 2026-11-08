# Dead Frequency — roadmap da cidade simulada

Atualizado em 07/11/2026 (v0.9.0). A partir da v0.9 o jogo deixa de ser só "entregas" e vira uma
**cidade simulada na zona morta**: cada jogador escolhe uma profissão, ganha um apartamento, ouve a
rádio 96.6 e ajuda a cidade a funcionar. Ordem sugerida: de cima para baixo; cada fase deixa o jogo jogável.

## Onde estamos (v0.9)

| Área | O que já tem |
|---|---|
| **Profissões** | Entregador (turnos de caixas), Taxista, Gari, Repositor, Frentista, Locutor da 96.6. Escolha no menu, troca na pausa; no online o criador da sala pode deixar o servidor sortear. Pagamento conferido pelo servidor (valor e intervalo mínimo). |
| **Rádio 96.6** | Estação com estúdio e torre, 5 músicas originais sincronizadas pelo relógio (todo mundo ouve o mesmo trecho), rádio na caminhonete (R), no escritório, mercado, saguão e apartamentos. |
| **Moradia** | Edifício Sintonia: um apartamento por jogador (elevador no saguão), cama que salva o jogo, TV com canais, rádio. |
| **Lugares com interior** | Depósito e escritório, Supermercado Atacadinho (corredores, seções, caixas), rádio, saguão e apartamentos. |
| **Cidade** | Bancos, lotéricas, padarias, mercados, postos, lava-jato, caixa d'água, bombeiros, delegacia, ônibus 966, pedestres que desviam, outdoors iluminados, orelhões. Fase 2: rancho. |
| **Online** | Servidor oficial (Cloudflare), servidor próprio ou hospedar no PC com link público. Protocolo 4. |
| **Distribuição** | Release automático no GitHub (Windows e Linux) e launcher que atualiza sozinho. |

---

## Fase 1 — Polimento da v0.9 (curto prazo)
| Tarefa | Por quê | Esforço |
|---|---|---|
| Partida online com 3+ PCs, uma pessoa em cada profissão | Validar pagamentos, sorteio de profissões e apartamentos | 1 sessão |
| Mostrar a profissão de cada jogador no nome em cima da cabeça e no minimapa (a pausa já lista) | Saber quem faz o quê | pequeno |
| Sacos de lixo e passageiros visíveis para todos no online | Hoje cada um vê as próprias tarefas | médio |
| Carro do frentista chegando pela rua (com o trânsito) em vez de "aparecer" na avenida | Mais natural | médio |
| Visitar o apartamento de um amigo (campainha no saguão) | Social | pequeno |
| `MultiMeshInstance3D` nos objetos repetidos e `visibility_range` nos detalhes | FPS | médio |
| Teclas configuráveis e chat (T) | Pedidos comuns | médio |

## Fase 2 — Economia da cidade
- **Carteira individual + caixa da cidade:** cada profissão paga o jogador; uma parte vai para a prefeitura (imposto) e libera melhorias para todos (postes novos, ônibus extra, reforma de prédios).
- **Contas do apartamento:** aluguel e luz por dia de jogo; atrasou, a TV e o rádio cortam.
- **Mercado de verdade:** comprar comida no Atacadinho (energia/fome), com estoque que o repositor abastece; se ninguém repor, as prateleiras esvaziam para todos.
- **Preço do combustível** varia com os postos atendidos pelo frentista; a caminhonete passa a gastar combustível (fase 3).
- **Ranking semanal** por profissão (Cloudflare D1).

## Fase 3 — Painel da caminhonete (Módulo 3 do plano original)
- Combustível e bateria com ponteiros; abastecer nos postos (a vaga do frentista já existe).
- Fusíveis físicos: carga com interferência (`EmInterference`) queima fusíveis e apaga faróis e rádio.
- **Rádio com dial:** sintonizar a 96.6 e estações piratas com chiado; a 96.6 interrompe para notícias da cidade (entregas feitas, apagões, eventos).
- Velocímetro e alertas no painel em vez do HUD (UI diegética).

## Fase 4 — Mais profissões e tarefas
| Profissão | Tarefa |
|---|---|
| **Motorista de ônibus** | Dirigir o 966 pela linha e parar nos pontos no horário |
| **Policial** | Multar quem corre perto da delegacia, achar a van "roubada" |
| **Bombeiro** | Apagar incêndios em prédios aleatórios com o caminhão |
| **Mecânico** | Consertar carros quebrados na rua e a caminhonete dos outros |
| **Padeiro** | Assar pães de madrugada e abastecer as padarias |
| **Carteiro (Correrios)** | Rotas de cartas a pé, caixa por caixa |
| **Lavador** | Lava-Jato Molhadinho: lavar carros que chegam |
| **DJ da 96.6** | Montar a programação (ordem das músicas) para todos os jogadores |

Cada profissão nova = um `JobBase` em `Scripts/Jobs/` + a regra em `PROFESSIONS` (servidor e LocalServer).

## Fase 5 — Mundo
- Mais interiores: banco (sacar/depositar), lotérica (raspadinha), padaria, farmácia 24h.
- Novos bairros: **morro** com ladeiras e vielas, **centro** com prédios altos, **porto** com contêineres.
- Trânsito de verdade: carros andando, semáforos, motoristas que respeitam o PARE.
- Eventos da noite: apagão num bairro, neblina densa, orelhões tocando sozinhos, chiado na 96.6 que leva a uma entrega secreta.
- Decoração do apartamento comprada nas lojas (sofá, pôster, planta).

## Fase 6 — Visual PS1 (Módulo 5 do plano original)
- Shader com vertex jitter, dithering e cores reduzidas; renderizar em 640×360 com nearest-neighbor.
- Monitor CRT verde dentro da caminhonete mostrando o radar e a música da 96.6.

## Fase 7 — Lançamento
- Ícone, tela de carregamento e página no itch.io com trailer.
- Integração com a Twitch: o chat da live vota em eventos (apagão, chuva, entrega bônus) e pede música na 96.6.
- Mais músicas na rádio (aceitar faixas da comunidade com licença livre).

---

## Notas técnicas
- **Sorteios da cidade:** `_rng` (layout e endereços) e `_decoRng` (detalhes). Quarteirões especiais pulam o `_rng`; mexer neles muda a cidade: suba `PROTOCOL_VERSION` (servidor) e `NetClient.ProtocolVersion` juntos (o LocalServer usa o do NetClient).
- **Quarteirões especiais:** depósito (0,0), bombeiros (1,-1), delegacia (-1,1), caixa d'água (-1,-1), postos (2,1) com lava-jato, (-2,-2) e (-3,1), **rádio (1,2)**, **mercado (-2,2)**, **Edifício Sintonia (2,-2)**.
- **Veículos:** `VanController.NetId` ↔ `VEHICLES` no servidor ↔ `LocalServer.Vehicles` (van, moto, truck_yellow, truck_red). Moto e caminhões são montados pelo `VehicleFactory`.
- **Listas que precisam bater:** `CargoCatalog` ↔ `CARGO_CATALOG` ↔ `LocalServer.CargoPayments`; `ItemCatalog` ↔ `SHOP_PRICES`; `Professions` ↔ `PROFESSIONS` ↔ `LocalServer.ProfessionRules`; `GameLevels` ↔ `LEVELS`.
- **Rádio:** `RadioStation` calcula música e posição pelo `Time.GetUnixTimeFromSystem()` — sem tráfego de rede. Músicas em `Audio/Radio/*.ogg`, geradas por `Audio/synth_radio.py.txt`.
- **Apartamentos:** salas montadas 60 m abaixo do quarteirão; o elevador e a porta teletransportam (`PlayerController.Teleport`). Apartamento = `(MyId - 1) % 8 + 1`.
- **Save:** `SaveManager` grava `user://save.tzn` (JSON). Carteira e inventário continuam em `user://inventory.json`.
- **Versão nova:** suba `GameVersion.Current`, escreva a entrada em `GameVersion.Changelog`, regenere `CHANGELOG.md` e faça push na main: o GitHub Actions compila Windows/Linux + launcher e cria o release.
- Assets de terceiros em `Assets/<autor>/` com LICENSE.txt; só CC0 ou licenças que permitam uso comercial.
- **Quaternius (v0.9.4):** personagens (`CharacterModel`, paleta em faixas pele/olhos/cabelo/camisa/calça/sapato), `FarmAnimal`, `NatureKit`. Texturas de natureza reduzidas para 256 px e sem normal map. Ainda não usados: Downtown City MegaKit e Ultimate Textured Building Pack (prédios modulares), Universal Animation Library (esqueleto diferente; precisaria de retarget).
