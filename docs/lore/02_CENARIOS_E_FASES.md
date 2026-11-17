# Cenários e fases

Este documento descreve o estado dos cenários e o planejamento narrativo. A Fase 1 está concluída; as fases futuras devem permanecer isoladas até estarem prontas para integração.

## Fase 1 · Cidade (concluída na v0.10.1)

Cena atual: `Scenes/Levels/test_level.tscn` com `Scripts/World/CityGenerator*.cs` (cidade procedural).

A ambientação Brazilcore implementada inclui lombadas com colisão em rampa, asfalto remendado, bueiros e bocas de lobo, postes com fios e cartazes, orelhões, lixeiras de grade e árvores com a base caiada. Calçadas irregulares ficaram fora do escopo por decisão de design.

## Fase 2 · Rancho (reconstrução)

Cena atual: `Scenes/Levels/farm_level.tscn` (`WorldBiome.Farm`).

- Estradas de terra vermelha em circuito, ligando os sítios.
- Sítios com casa, varanda e bichos (`FarmAnimal`).
- *Packing house* de banana e palmito (galpão de embalar), ponto de carga para o entregador.
- Lago com pesqueiro.
- Sinalização rústica (placas de madeira pintadas à mão).

## Fase 3 · O Bananal — `Scenes/Levels/Bananal.tscn`

- Plantação fechada de bananeiras onde é fácil se perder.
- Navegação pelo **sinal da 96.6**: quanto mais perto da antena, mais limpa a música; longe, mais chiado.
  Modelo pronto: [`templates/RadioBeacon.cs.txt`](../templates/RadioBeacon.cs.txt).
- Bananeiras em `MultiMeshInstance3D` (são milhares).

## Fase 4 · Gruta da Estática — `Scenes/Levels/Gruta.tscn`

- Caverna de calcário montada com salas modulares encaixadas (geração procedural com semente fixa).
  Modelo pronto: [`templates/CaveGenerator.cs.txt`](../templates/CaveGenerator.cs.txt).
- Escura de verdade: lanterna e visão noturna (item que já existe, tecla N).
- A sala final tem a repetidora de rádio.

## Fase 5 · Estrada da Serra — `Scenes/Levels/Serra.tscn`

- Subida sinuosa com neblina densa.
- Teste de integridade da carga: curvas e buracos danificam as caixas na caçamba (`CargoItem`).

## Ideias de polimento (backlog opcional)

Entram como módulos pequenos, sem atrasar as fases:

- Praça da Matriz à noite com barracas de pastel e lanche; carro de som anunciando eventos.
- Festivais sazonais na praça (ex.: Festa da Colheita, Bon Odori).
- Travessia de balsa no rio.
- Caramelo na praça (o caramelo companheiro já existe desde a v0.9.5).
