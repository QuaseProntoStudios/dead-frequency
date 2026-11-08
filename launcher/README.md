# Launcher do Dead Frequency

Programa pequeno que mantém o jogo atualizado sozinho:

1. Confere o último release em [github.com/dieguesmosken/dead-frequency/releases](https://github.com/dieguesmosken/dead-frequency/releases).
2. Se a versão instalada for mais antiga (ou o jogo não estiver instalado), baixa o zip do seu sistema
   e extrai na pasta `game/`, ao lado do launcher.
3. Abre o jogo. Sem internet, abre a versão que já estiver instalada.

Os saves e as opções ficam na pasta de usuário do Godot (`%APPDATA%\Godot\app_userdata\Dead Frequency`),
então atualizar não apaga nada.

## Usar

Baixe `DeadFrequencyLauncher-windows.exe` (ou `-linux`) no release e coloque numa pasta só dele.
Abra sempre por ele.

| Opção | O que faz |
|---|---|
| `--check` | só verifica se há atualização |
| `--no-launch` | atualiza e não abre o jogo |
| `--dir <pasta>` | instala em outra pasta |

## Compilar

```bash
dotnet publish launcher/DeadFrequencyLauncher.csproj -c Release -r win-x64 -o out/win
dotnet publish launcher/DeadFrequencyLauncher.csproj -c Release -r linux-x64 -o out/linux
```

O GitHub Actions (`.github/workflows/release.yml`) já compila e anexa os dois no release.
