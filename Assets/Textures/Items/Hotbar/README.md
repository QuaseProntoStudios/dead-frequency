# Ícones da hotbar

Coloque os arquivos PNG nesta pasta com estes nomes exatos. A hotbar carrega os ícones por caminho `res://Assets/Textures/Items/Hotbar/`.

| Item no jogo | Arquivo |
|---|---|
| Radar de pulso | `radar.png` |
| Mochila de entregador | `backpack.png` |
| Luvas reforçadas | `gloves.png` |
| Óculos de visão noturna | `night_vision.png` |
| Energético Frequência | `energy_drink.png` |
| Guaraná Pinguim | `guarana.png` |
| Kit de reparo | `repair_kit.png` |
| Fita adesiva | `duct_tape.png` |
| Café Marreta | `coffee.png` |
| Marmitex Tia da Quentinha | `marmitex.png` |
| Espetinho do Duvidoso | `skewer.png` |
| Chinelos Legítimas | `slippers.png` |
| Prego | `nail.png` |
| Palha de aço Brilhaço | `steel_wool.png` |
| Repelente Espanta-Muriçoca | `repellent.png` |
| Lanterna de longo alcance | `flashlight.png` |
| Fusível de vidro | `fuse.png` |
| Galão de gasolina | `jerrycan.png` |

## Comportamento

- O número do espaço continua visível no botão.
- O nome completo do item aparece como tooltip.
- Se um PNG ainda não estiver nesta pasta, o slot continua funcional e mostra somente o número; a ausência do arquivo não impede o carregamento do jogo.
- Use PNG com transparência e proporção quadrada. Tamanho recomendado: 64×64 ou 128×128 px.
- O Godot gera os metadados `.import` automaticamente ao importar os PNGs.
