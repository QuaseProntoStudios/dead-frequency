"""Gera site/data/site.json a partir do jogo: versão e changelog (Scripts/Core/GameVersion.cs),
fotos de cada versão (site/assets/shots/vX.Y.Z/captions.json) e o Pix copia e cola.
Rode da raiz do repositório:  python3 site/build.py   (o GitHub Actions roda antes de publicar o site)."""
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SITE = os.path.join(ROOT, "site")
SHOTS = os.path.join(SITE, "assets", "shots")

src = open(os.path.join(ROOT, "Scripts", "Core", "GameVersion.cs"), encoding="utf-8").read()
current = re.search(r'Current\s*=\s*"([^"]+)"', src).group(1)
block = src[src.index("Changelog ="):src.index("record struct Credit")]

changelog = []
for m in re.finditer(r'new\("([\d.]+)", "([^"]+)", "([^"]+)", new\[\]\s*\{(.*?)\}\),', block, re.S):
    version, date, title, body = m.groups()
    changes = [c.replace('\\"', '"') for c in re.findall(r'"((?:[^"\\]|\\.)*)"', body)]
    shots = []
    cap_file = os.path.join(SHOTS, "v" + version, "captions.json")
    if os.path.exists(cap_file):
        for s in json.load(open(cap_file, encoding="utf-8")):
            shots.append({"src": f"assets/shots/v{version}/{s['file']}", "caption": s["caption"]})
    changelog.append({"version": version, "date": date, "title": title, "changes": changes, "shots": shots})

# Galeria: as primeiras fotos das versões mais recentes, mais as panorâmicas.
gallery = []
for name, cap in [("hero", "O depósito da Dead Frequency Entregas à noite"), ("van_noite", "A caminhonete no pátio do depósito, de dia"), ("cidade_dia", "Rua do Supermercado Carretour"), ("menu", "Menu principal com as novidades da versão")]:
    if os.path.exists(os.path.join(SHOTS, name + ".jpg")):
        gallery.append({"src": f"assets/shots/{name}.jpg", "caption": cap})
for r in changelog:
    gallery.extend(r["shots"][:4])
gallery = gallery[:20]

pix = open(os.path.join(SITE, "assets", "pix.txt"), encoding="utf-8").read().strip()
os.makedirs(os.path.join(SITE, "data"), exist_ok=True)
json.dump({"version": current, "changelog": changelog, "gallery": gallery, "pix": pix},
          open(os.path.join(SITE, "data", "site.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(f"site.json: v{current}, {len(changelog)} versões, {sum(len(r['shots']) for r in changelog)} fotos, galeria {len(gallery)}")
