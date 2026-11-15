#!/usr/bin/env python3
"""
Corta uma gravação do Google AI Studio com várias falas seguidas em um .ogg por fala.

A cota diária do AI Studio é pequena, então vale gerar as três falas de um morador numa geração só
(uma por linha, com uma pausa longa entre elas) e cortar aqui pelos silêncios. Roteiro em
docs/radio/falas_de_rua.csv; passo a passo em docs/radio/VOZES.md.

Uso (na pasta do projeto, com o ffmpeg instalado):
  python tools/tts/cortar_falas.py baixado.wav npc_senhor            # -> Audio/Street/npc_senhor_1..3.ogg
  python tools/tts/cortar_falas.py baixado.wav npc_moca --falas 2 --inicio 3   # -> npc_moca_3, npc_moca_4
  python tools/tts/cortar_falas.py baixado.wav npc_dona --simular    # só mostra onde cortaria

Se achar mais ou menos trechos que --falas, não grava nada e mostra os trechos: ajuste --silencio
(pausa mínima entre falas, em segundos) ou --limiar (volume em dB abaixo do qual conta como silêncio).
Cada fala sai como no resto do jogo: mono, 44,1 kHz, -16 LUFS, .ogg q5, com 80 ms de folga nas pontas.
"""
from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
FOLGA = 0.08  # segundos de respiro antes e depois de cada fala


def duracao(arquivo: Path) -> float:
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(arquivo)],
                         capture_output=True, text=True, check=True).stdout
    return float(out.strip())


def trechos_de_fala(arquivo: Path, silencio: float, limiar: float) -> list[tuple[float, float]]:
    """Trechos (início, fim) entre os silêncios de pelo menos `silencio` segundos."""
    log = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", str(arquivo),
                          "-af", f"silencedetect=noise={limiar}dB:d={silencio}", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    inicios = [float(x) for x in re.findall(r"silence_start: (-?[\d.]+)", log)]
    fins = [float(x) for x in re.findall(r"silence_end: ([\d.]+)", log)]
    total = duracao(arquivo)
    trechos, cursor = [], 0.0
    for i, ini in enumerate(inicios):
        if ini - cursor > 0.15:
            trechos.append((cursor, ini))
        cursor = fins[i] if i < len(fins) else total
    if total - cursor > 0.15:
        trechos.append((cursor, total))
    return trechos


def gravar(arquivo: Path, ini: float, fim: float, destino: Path, total: float) -> None:
    destino.parent.mkdir(parents=True, exist_ok=True)
    a, b = max(0.0, ini - FOLGA), min(total, fim + FOLGA)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(arquivo), "-ss", f"{a:.3f}", "-to", f"{b:.3f}",
                    "-af", "loudnorm=I=-16:TP=-1.5:LRA=11", "-ac", "1", "-ar", "44100",
                    "-c:a", "libvorbis", "-q:a", "5", str(destino)], check=True)


def main() -> int:
    ap = argparse.ArgumentParser(description="Corta várias falas de uma gravação pelos silêncios.")
    ap.add_argument("gravacao", type=Path, help=".wav (ou outro áudio) baixado do AI Studio")
    ap.add_argument("morador", help="tipo de morador: npc_senhor, npc_rapaz, npc_escritorio, npc_moca, npc_dona, npc_senhora")
    ap.add_argument("--falas", type=int, default=3, help="quantas falas a gravação tem (padrão 3)")
    ap.add_argument("--inicio", type=int, default=1, help="número da primeira fala (padrão 1)")
    ap.add_argument("--silencio", type=float, default=0.7, help="pausa mínima entre falas, em segundos (padrão 0,7)")
    ap.add_argument("--limiar", type=float, default=-38.0, help="volume que conta como silêncio, em dB (padrão -38)")
    ap.add_argument("--pasta", default="Audio/Street", help="pasta de saída, relativa ao projeto")
    ap.add_argument("--simular", action="store_true", help="só mostra os trechos, sem gravar")
    a = ap.parse_args()

    if not shutil.which("ffmpeg") or not shutil.which("ffprobe"):
        print("Precisa do ffmpeg (winget install ffmpeg).", file=sys.stderr)
        return 1
    if not a.gravacao.exists():
        print(f"Não achei {a.gravacao}.", file=sys.stderr)
        return 1

    trechos = trechos_de_fala(a.gravacao, a.silencio, a.limiar)
    total = duracao(a.gravacao)
    for i, (ini, fim) in enumerate(trechos, 1):
        print(f"  trecho {i}: {ini:6.2f}s -> {fim:6.2f}s ({fim - ini:.2f}s)")
    if len(trechos) != a.falas:
        print(f"Achei {len(trechos)} trecho(s), esperava {a.falas}. Ajuste --silencio ou --limiar.", file=sys.stderr)
        return 2
    for n, (ini, fim) in enumerate(trechos, a.inicio):
        destino = RAIZ / a.pasta / f"{a.morador}_{n}.ogg"
        print(f"  -> {destino.relative_to(RAIZ)}")
        if not a.simular:
            gravar(a.gravacao, ini, fim, destino, total)
    if not a.simular:
        print("Pronto. Abra o projeto na Godot uma vez para importar os áudios.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
