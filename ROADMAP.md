# Dead Frequency — roadmap da cidade simulada

Atualizado em 08/11/2026 (v0.9.5). A partir da v0.9 o jogo deixa de ser só "entregas" e vira uma
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
| **Online** | Servidor oficial (Cloudflare), servidor próprio ou hospedar no PC com link público, lista de salas públicas. Protocolo 5. |
| **Gente e bichos** | Personagens animados (Quaternius) para jogadores e NPCs, bichos no sítio, caramelos. |
| **Distribuição** | Release automático no GitHub (Windows e Linux) com SHA256SUMS e atestado de build, launcher que atualiza sozinho, site em www.teuzin.com.br/dead-frequency. |

---

## Planejamento de conteúdo (v0.9.5+)

### v0.9.5 — Calçada viva ✅
Conecta os itens com a "gente de verdade" da v0.9.4.

| Ideia | Como funciona no jogo |
|---|---|
| **O "trato" no caramelo** | Caramelos andam pela cidade e moram na varanda dos sítios. Use um **Espetinho** perto de um: ele segue você por 3 minutos, late de vez em quando e, no sítio, espanta os **pernilongos** da noite. |
| **Diálogos de calçada** | Pedestres, comerciantes e caixas soltam balões curtos estilo PS1 quando você passa perto: frases do dia a dia e a eterna briga do biscoito × bolacha (comentam a caixa que você carrega). |
| **Passageiros exigentes** (taxista) | Cada passageiro tem um jeito, que aparece no painel: *executivo* (roupa social: quer a Rádio 96.6 ligada, sem passar de 60 km/h e sem batida — paga bônus ou desconta), *apressado* (bônus se chegar rápido) e *tranquilo*. |
| **Guaraná Pinguim** | Lata verde: recupera o fôlego e acelera a recuperação por um tempo. |
| **Café Marreta** | Garrafa térmica: corta a leseira (sono da madrugada) na hora e segura por alguns minutos. |
| **Marmitex Tia da Quentinha** | Enche todo o fôlego, mas dá o "efeito pós-almoço": 10 s mais lento. |
| **Chinelos Legítimas** | Calçado: anda mais rápido com o "ploc-ploc" nos passos. Correndo na lama do sítio (ou de vez em quando no asfalto) a tira pode arrebentar; o **Prego** conserta. |
| **Fôlego e sono** | Correr gasta fôlego (barra no HUD). Entre meia-noite e 5h bate a leseira: a tela escurece nas bordas e você anda mais devagar. |
| **Botijão do Gás do Zé** | O botijão que já existia agora vaza: arremessado ou caindo forte da caçamba, solta um chiado com gás saindo e perde integridade até parar. |
| **Churrasquinho do Duvidoso** | Barraca de calçada: depois das 18h acende a chapa, solta fumaça e junta gente em volta. Vende espetinho, guaraná, café e marmitex. |

### v0.9.6 — Lojas da zona morta
| Ideia | Como funciona no jogo |
|---|---|
| **Agro-Tudão (casa de rações)** | Na estrada de terra entre a cidade e o sítio. Profissão/entrega nova: sacos pesados de ração para os cercados dos bichos. |
| **Conserta-Tudo do Primo** | O gari junta **sucata** nas calçadas e vende aqui, ou troca por melhorias permanentes (lanterna de longo alcance, pilhas melhores para a visão noturna). |
| **Caixa de Lã de Aço 1001 Utilidades** | Carga leve do repositor; guardada no inventário, melhora a imagem chiada da TV do apartamento. |
| Fachadas novas | Texturas geradas em Python (`Assets/Textures/Stores/*.py.txt`) para as lojas acima. |

### v0.9.7 — Trânsito de verdade
| Ideia | Como funciona no jogo |
|---|---|
| **Trânsito procedural** | Carros andando pelas ruas, parando no semáforo e no PARE (base para os dois abaixo). |
| **Duno Mille (com escada)** | Hatch quadrado; quando nasce com a escada de fibra no teto, ignora o limite de velocidade e anda mais rápido que a viatura. |
| **Maré-Azar 20V** | Sedã esportivo para o jogador e o trânsito, com temperatura escondida: acelerador no talo por muito tempo na estrada de terra → fumaça preta no capô e o motor morre (som `motor_ferveu.wav` já pronto). Precisa de protocolo novo (lista `VEHICLES`). |


## Fase 1 — Polimento da v0.9 (curto prazo)
| Tarefa | Por quê | Esforço |
|---|---|---|
| Partida online com 3+ PCs, uma pessoa em cada profissão | Validar pagamentos, sorteio de profissões e apartamentos | 1 sessão |
| Mostrar a profissão de cada jogador no nome em cima da cabeça e no minimapa (a pausa já lista) | Saber quem faz o quê | pequeno |
| Sacos de lixo e passageiros visíveis para todos no online | Hoje cada um vê as próprias tarefas | médio |
| Visitar o apartamento de um amigo (campainha no saguão) | Social | pequeno |
| `MultiMeshInstance3D` nos objetos repetidos e `visibility_range` nos detalhes | FPS | médio |
| Chat (T) | Pedido comum | médio |

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
