# Assinatura de código e verificação dos downloads

## O que já funciona (sem certificado)

- **SHA256SUMS.txt** em cada release (o CI calcula; releases antigos recebem pelo workflow *Notas dos releases*).
- **Atestado de build** (`actions/attest-build-provenance`) a partir da próxima versão: prova que o arquivo
  saiu do GitHub Actions deste repositório. Verificar: `gh attestation verify ARQUIVO --repo dieguesmosken/dead-frequency`.
- **Commits assinados** pelo mantenedor (chave SSH de assinatura no GitHub → selo *Verified*).

## SignPath Foundation (assinatura grátis para código aberto)

Regras: https://signpath.org/terms · Pedido: https://signpath.org/apply

### Checklist antes de pedir
- [x] **Licença open source aprovada pela OSI**: GPL-3.0 (arquivo `LICENSE`).
- [x] Todos os componentes open source (Godot = MIT; modelos Kenney/KayKit/Quaternius = CC0; músicas e sons gerados por código).
- [x] Projeto ativo e com a versão já publicada no GitHub Releases.
- [x] Página de download descreve o que o programa faz (site → seção "O jogo").
- [x] **Política de privacidade** e **Política de assinatura de código** no site (seções `#privacidade` e `#assinatura`) — já existem.
- [x] Instruções para desinstalar (seção Privacidade) — já existem.
- [ ] **2FA** ligado na conta do GitHub (e depois na do SignPath).
- [x] Nome e versão do produto iguais em todos os builds (o CI grava `X.Y.Z.0` nos `.exe`).

### Depois de aprovado
1. No SignPath, crie o projeto (slug `dead-frequency`), a *signing policy* (ex.: `release-signing`, com aprovação manual)
   e ligue o "trusted build system" GitHub.com ao repositório.
2. *Artifact configuration*: o CI envia um zip com `DeadFrequency.exe` e `DeadFrequencyLauncher-windows.exe` na raiz;
   configure os dois como `<pe-file>` com `authenticode-sign`, product name "Dead Frequency" / "Dead Frequency Launcher".
3. No GitHub (*Settings → Secrets and variables → Actions*):
   - Secret `SIGNPATH_API_TOKEN` (token de um usuário/CI do SignPath com permissão de *submitter*).
   - Variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG` (`dead-frequency`) e `SIGNPATH_POLICY_SLUG`.
4. Pronto: na próxima versão o `release.yml` envia os `.exe`, espera a sua aprovação no SignPath (até 1 h) e publica
   os arquivos assinados. Sem o secret, esses passos são pulados.
5. Troque o texto em `site/index.html` (`#signpath-credit`) pela frase exigida:
   *"Assinatura de código gratuita fornecida pela SignPath.io, certificado pela SignPath Foundation."*

> Observação: o executável do launcher tem o `.pck` embutido. Se o Windows reclamar da assinatura dele,
> mude `binary_format/embed_pck=false` em `launcher/app/export_presets.cfg` e publique o `.pck` junto (ou só zipado).
