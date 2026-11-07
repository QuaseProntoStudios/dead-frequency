# Dead Frequency — contexto do projeto

**Engine:** Godot 4.7.2 .NET (Windows x86_64, C# / .NET 10)
**Linguagem:** C# para toda a lógica de gameplay + GDShader para efeitos PS1/CRT
**Estética:** PS1/PSX low-poly (320x240 / 640x480, nearest-neighbor, vertex jiggle, dithering, fog denso)
**Gênero:** simulador de entrega e sobrevivência em 1ª pessoa, com UI diegética e física tátil

## Visão
Entregador solitário dirigindo uma van utilitária anos 90 por uma zona perigosa.
- Sem HUD tradicional: a informação vem do painel físico da van (ponteiros, luzes, monitor CRT verde).
- Caixas são `RigidBody3D` carregadas na mão, empilhadas na caçamba, que sofrem dano por impacto.
- A van consome bateria, queima fusíveis físicos e sofre interferência eletromagnética da carga.

## Regras de código
- Sempre `public partial class X : NodeType`; `[Export]` para o Inspector; `[GlobalClass]` em Resources.
- `GetNode<T>()` no `_Ready()`, com referências guardadas em campos privados. Nunca `GetParent().GetParent()`.
- `Vector3`, `Transform3D`, `Basis` e `Color` são structs: copie, altere e reatribua.
- Física em `_PhysicsProcess(double delta)` com `float dt = (float)delta;`.
- Interação via interface `IInteractable` (`collider is IInteractable`), sem checar grupos por string.
- "Call down, signal up": `[Signal]` ou `event Action<T>` para falar com quem está acima.
- Dados de design ficam em `Resource` `.tres`. Save, manifesto e telemetria ficam em `.tzn` (JSON via `System.Text.Json` + `FileAccess`). `ResourceSaver` não aceita extensões customizadas.

## Collision layers
1 World · 2 Player · 3 Interactables · 4 Cargo · 5 Vehicle

## Estrutura
- `Scripts/Core` interfaces e utilitários · `Scripts/Player` · `Scripts/Cargo` · `Scripts/Vehicle` · `Scripts/UI`
- UI montada em código com o tema de `Scripts/Core/UiStyle.cs` (verde fósforo CRT)
- `Scenes/...` cenas · `Data/Cargo/*.tres` catálogo de cargas

## Roadmap
1. ✅ Movimento, interação e pegar/arremessar caixas (IInteractable, CargoData, CargoItem, PlayerController)
2. ✅ VanController (VehicleBody3D) + DriverSeatInteractable, entrar/sair com tween
   ✅ UI: menu principal, pausa (Esc/P), opções salvas em user://settings.cfg (autoload GameSettings), HUD de protótipo (F3 = debug)
   ✅ Cidade procedural (Scripts/World/CityGenerator.cs + Shaders/), noite com neblina, depósito no quarteirão (0,0)
   ✅ Loop de entregas (Scripts/Gameplay/DeliveryManager.cs): feixe verde no destino, pagamento pela integridade, turnos
   ✅ Áudio: autoload AudioManager, buses Music/SFX/UI, trilha e efeitos sintetizados (Audio/synth.py.txt)
   ✅ Multiplayer: servidor em server/ (Cloudflare Worker + Durable Object por sala, código de 5 letras);
      cliente em Scripts/Net (NetClient autoload, NetSync na fase, RemotePlayer). Servidor manda nas regras,
      física roda no dono de cada objeto. CARGO_CATALOG do servidor = DeliveryManager.CargoCatalog (mesma ordem).
   ✅ Itens: ItemCatalog + PlayerInventory (autoload, salvo em user://inventory.json), InventoryScreen (I/Tab),
      ShopScreen + StreetVendor (barracas), radar/minimapa, visão noturna (N), luvas, mochila, consumíveis.
      Preços = SHOP_PRICES no servidor. Game over quando todas as caixas do turno quebram.
   ✅ Cidade: CarModels (4 tipos), placas PARE/faixa, BusLine (ônibus + pontos, posição pelo relógio), Pedestrians.
3. Painel diegético: DashboardButton, ElectricalSystem, EngineSystem, fusíveis
4. CargoBayManager + SaveManager (.tzn)
5. Shaders PS1 + monitor CRT

## Versões e GitHub
- Repositório: https://github.com/dieguesmosken/dead-frequency (público).
- A cada atualização: subir `GameVersion.Current`, escrever a entrada em `GameVersion.Changelog`,
  regenerar `CHANGELOG.md`, fazer um commit "vX.Y.Z — título" e criar a tag `vX.Y.Z`.
- Nunca versionar `server/.dev.vars` (ADMIN_TOKEN), `server/node_modules`, `.godot/` nem `_to_delete/`.
