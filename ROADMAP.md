# Dead Frequency — plano de melhorias

Atualizado em 07/11/2026. Ordem sugerida: de cima para baixo. Cada fase deixa o jogo jogável.

## Onde estamos
- **Base:** andar, pegar e arremessar caixas, van dirigível, cidade gerada por código (dia/noite), entregas com pagamento por integridade, turnos e game over.
- **Itens:** radar com minimapa, visão noturna, luvas, mochila, consumíveis, inventário com o boneco equipado, barracas.
- **Cidade:** ônibus com pontos, pedestres, placas, carros com 4 modelos, orelhões, ar-condicionados, lojas paródia, bombeiros, delegacia.
- **v0.5:** ciclo dia/noite com relógio, depósito com interior e escritório (PC com os ganhos), frota Kenney (CC0), van e carros com mais detalhes, telas de Novidades e Créditos.
- **v0.6:** Fase 2 (Rancho) com sítios, lago e capela; escolha de fase no menu e na sala online; orelhões tchau/morto; PC em tela cheia.
- **Online:** servidor na Cloudflare (salas com código, dinheiro do grupo, compras, game over) e painel /admin.

---

## Fase 1 — Estabilidade e desempenho (curto prazo)
| Tarefa | Por quê | Esforço |
|---|---|---|
| Rodar uma partida online completa com 2 PCs e anotar bugs | O online nunca foi testado de ponta a ponta com 2 pessoas | 1 sessão |
| Trocar objetos repetidos (postes, ar-condicionados, orelhões, placas) por `MultiMeshInstance3D` | Hoje são milhares de nós; MultiMesh corta draw calls | médio |
| `visibility_range` nos detalhes pequenos (somem a 80 m) | A neblina já esconde; ganha FPS | pequeno |
| Salvar progresso do solo (turno, dinheiro, posição da van) em `user://save.json` | Hoje só itens e carteira são salvos | pequeno |
| Tela de teclas configuráveis nas Opções | Pedido comum de jogador | médio |
| Chat online (tecla T) | O servidor já aceita `chat`; falta a caixa de texto | pequeno |

## Fase 2 — Painel da van (Módulo 3 do plano original)
- Combustível e bateria com ponteiros no painel; posto de gasolina (paródia: **Petrobraz**, **Ipiranguinha**) para abastecer.
- Fusíveis físicos: a carga com interferência eletromagnética (`EmInterference`) queima fusíveis e apaga faróis/rádio.
- **Rádio da van** com estações fictícias (locutor, jingles das lojas paródia) e chiado quando a carga interfere.
- Velocímetro e alertas no painel em vez do HUD (UI diegética, como pede o design).

## Fase 3 — Gameplay de entrega
- **Clientes com prazo e gorjeta:** entregou rápido e inteiro = gorjeta; atrasou = avaliação baixa.
- **Tipos de carga:** frágil (já tem), refrigerada (tempo limite), pesada (só com luvas/carrinho), perigosa (não pode bater).
- **Entregas especiais:** suprimentos para os bombeiros, documentos lacrados para a delegacia, pacote suspeito que "chama" no rádio.
- **Trânsito:** carros andando pelas ruas e semáforos nos cruzamentos com placa de PARE.
- **Polícia:** viatura multa quem passa muito rápido perto da delegacia (desconta do dinheiro).
- **Eventos da noite:** apagão em um bairro, neblina densa, orelhões tocando sozinhos (pista para uma entrega secreta).
- **Reputação:** nota de 1 a 5 estrelas que libera entregas mais caras.

## Fase 4 — Mundo
- Novos bairros: **morro** com ladeiras e vielas (a van não sobe tudo), **centro** com prédios altos, **porto** com contêineres.
- Mais lugares paródia: padaria, boteco, banca de jornal, feira livre aos domingos, posto, lotérica (**Loterias Caixa Baixa**), farmácia 24h.
- Interior simples em algumas lojas: entregar no balcão em vez da calçada.
- Lixeiras, bancas, árvores nas calçadas, pichações, gatos de rua.

## Fase 5 — Visual PS1 (Módulo 5 do plano original)
- Shader com vertex jitter (vértices "tremendo"), dithering e cores reduzidas.
- Renderizar em 640×360 e esticar com nearest-neighbor (opção nas configurações).
- Monitor CRT verde dentro da van mostrando o radar.

## Fase 6 — Online
- Lista de salas públicas no menu (o registro global do painel já existe).
- Ranking semanal de entregas (Cloudflare D1, também gratuito).
- Validação de entrega no servidor: gerar os pontos de entrega a partir da seed também no servidor e conferir a posição.
- Papéis no grupo: motorista, carregador, navegador (quem tem o radar).

## Fase 7 — Lançamento
- Exportar para Windows (e Linux), ícone, tela de carregamento.
- Página no itch.io com trailer.
- **Integração com a Twitch:** o chat da live pode votar em eventos (apagão, chuva, entrega bônus) via comandos.

---

## Notas técnicas
- A cidade usa dois sorteios: `_rng` (layout e endereços) e `_decoRng` (lojas, ar-condicionados, orelhões, hidrantes). Mexer no primeiro muda os endereços: suba `PROTOCOL_VERSION` (servidor) e `NetClient.ProtocolVersion` juntos.
- Listas que precisam bater entre jogo e servidor: `CargoCatalog` ↔ `CARGO_CATALOG` e `ItemCatalog` ↔ `SHOP_PRICES`.
- **Atualização nova:** suba `GameVersion.Current` e escreva a entrada no topo de `GameVersion.Changelog` (Scripts/Core/GameVersion.cs). O menu mostra as novidades uma vez por versão. Créditos ficam na mesma classe.
- Assets de terceiros ficam em `Assets/<autor>/` com um LICENSE.txt; só CC0 ou licenças que permitam uso comercial.
- Horário online = relógio do sistema (`Time.GetUnixTimeFromSystem`), todos veem a mesma hora sem tráfego.
- **Fases:** `GameLevels.cs` (id → cena) e `LEVELS` no servidor precisam bater. O rancho é o mesmo `CityGenerator` com `Biome = Farm` (CityGenerator.Farm.cs); as estradas são desenhadas pelo shader `farm_ground.gdshader`.
- Prédios especiais: `FireStationBlock` (1,-1) e `PoliceStationBlock` (-1,1) no nó City.
