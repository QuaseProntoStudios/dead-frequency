# Vozes dos personagens

Geradas no **Google AI Studio** (Playground → modelo **Gemini 3.8 Flash TTS**, "Single speaker"), uma fala por vez,
pelo plano Google AI Pro do Matheus. O campo **Style** recebe a descrição abaixo; o texto vai na caixa da fala.
Depois de baixar o .wav: corta o silêncio das pontas, normaliza em -16 LUFS, mono 44,1 kHz, .ogg (q5).
As falas do carro do ovo ganham o filtro de megafone (ver `tools/tts/gerar_audios.py`, `FILTRO_MEGAFONE`).
O AI Studio tem limite diário: quando aparece "Quota limit reached", para e continua no dia seguinte
(não trocar de plano nem ligar chave paga sem o Matheus autorizar).

| Personagem | Voz | Style (resumo) |
|---|---|---|
| Seu Waldir (96.6) | **Arlo** | Locutor veterano de rádio AM do interior de SP, anos 90, sotaque caipira leve, grave, caloroso, pausado. |
| Kátia (96.6) | **Cleo** | Locutora de comerciais de FM, animada, sorridente e rápida, ênfase nos preços e nomes das lojas. |
| Toninho (96.6) | **Mako** | Locutor de humor, moleque e debochado, pausinha antes da piada, final com malandragem. |
| Prefeitura | **Kira** | Comunicado oficial de cidade pequena, clara, formal e neutra, ritmo calmo. |
| Carro do ovo | **Enzo** | Vendedor no alto-falante, sotaque caipira, empolgado e cantado, voz alta e arrastada. |
| Dona Lurdes | **Enya** | Gerente da central de entregas, ~55 anos, seca e direta mas justa, fundo de humor. |
| Seu Tonho | **Algenib** | Dono do Agro-Tudão, ~70 anos, sotaque caipira forte puxando o R, voz rouca, bonachão. |

## Arquivos

- Comerciais, eventos, comunicados e vinhetas: `Audio/Radio/Ads/<id>.ogg` (ids em `anuncios_966.csv`).
  A 96.6 toca um por intervalo entre as músicas (`RadioStation.Spots`); o carro de som usa os mesmos.
- Carro do ovo: `Audio/Street/ovo_0N.ogg` (`carros_de_rua.csv`).
- Falas de calçada (v0.10.1): `Audio/Street/<morador>_<n>.ogg` (n de 1 a 3), roteiro em `falas_de_rua.csv`, texto igual
  ao de `SidewalkTalk.KindLines`. Ver "Falas de calçada" abaixo.
- Modo História: `Audio/Story/story_c<capítulo>_<intro|outro|broken>_<n>.ogg`, na ordem das falas de
  `Scripts/Story/StoryChapters.cs` (n começa em 1). Sem o arquivo, a caixa de diálogo só mostra o texto.

## Capítulo II completo

As 6 falas que faltavam (`story_c2_intro_1..3`, `story_c2_outro_1..2`, `story_c2_broken_1`) foram geradas no AI Studio
com as vozes Algenib (Seu Tonho) e Mako (Toninho) e os mesmos estilos da tabela acima. Para cortar o silêncio das pontas
use `silenceremove` só no começo e no fim (com `areverse`), senão o ffmpeg corta a fala na primeira pausa.

## Falas de calçada (moradores, v0.10.1)

Três falas por tipo de morador (os 6 modelos do Meshy), tocadas perto do jogador junto do balão do `SidewalkTalk`.
Também no AI Studio, uma voz por morador, diferente das vozes dos locutores e dos personagens com nome. Para gastar
menos cota, as três falas de um morador vão **numa geração só**, uma por linha, com uma pausa longa entre elas.
Na caixa da fala use a coluna `falado` da planilha quando ela estiver preenchida (ex.: "96.6" vira "noventa e seis
ponto seis", "966" vira "nove meia meia"). Depois corte pelos silêncios:

```
python tools/tts/cortar_falas.py baixado.wav npc_senhor --simular
python tools/tts/cortar_falas.py baixado.wav npc_senhor
```

O script já corta, normaliza em -16 LUFS e salva em mono 44,1 kHz .ogg q5. Se achar trechos a mais ou a menos, ajuste
`--silencio`/`--limiar`, ou gere uma fala solta e use `--falas 1 --inicio N`.

| Morador | Style (resumo) |
|---|---|
| `npc_senhor` | Aposentado de ~70 anos do interior de SP, falando devagar e saudosista, bem-humorado. |
| `npc_rapaz` | Rapaz de ~20 anos, descontraído, gíria dos anos 90 ("firmeza"), ritmo rápido. |
| `npc_escritorio` | Homem de ~40 anos de escritório, cansado e apressado, meio resmungão. |
| `npc_moca` | Moça de ~20 anos, simpática e animada, fala rápido. |
| `npc_dona` | Dona de casa de ~45 anos, direta e expansiva, sempre com pressa. |
| `npc_senhora` | Senhora de ~75 anos, carinhosa e brincalhona, voz suave e um pouco trêmula. |

## Vozes locais (VoiceStudio, VoxCPM2)

Deixado de lado: as vozes do jogo saem só do Google AI Studio.

