# Arquitetura de Terminais Linux

## Visão geral

No Linux, o PMW suporta múltiplos emuladores de terminal. O usuário escolhe qual usar na tela de Configuração (campo "Terminal"). A seleção é global e persiste em `Configuracao.json` no campo `terminalLinux`.

No Windows, o terminal é sempre o Windows Terminal (`wt.exe`) — sem seleção.

## Estrutura

```
backend/src/Utils/
├── IShellProvider.cs              → interface comum (Windows/Linux)
├── WindowsShellProvider.cs        → implementação Windows (wt.exe + pwsh.exe)
├── LinuxShellProvider.cs          → implementação Linux (delega pro terminal selecionado)
└── Terminais/
    ├── ITerminalEmulator.cs       → interface de um emulador de terminal
    ├── TerminalEmulatorFactory.cs → factory que resolve o emulador pela config
    ├── PtyxisTerminal.cs          → implementação Ptyxis (GNOME 46+)
    └── GhosttyTerminal.cs         → implementação Ghostty
```

## Fluxo

1. `LinuxShellProvider` recebe `ConfiguracaoService` via DI
2. Na primeira chamada, lê `TerminalLinux` da config e usa `TerminalEmulatorFactory.Criar()` para instanciar o emulador correto
3. O emulador é cacheado até que `RecarregarTerminal()` seja chamado (acontece ao salvar configuração)
4. Cada emulador implementa `ITerminalEmulator` com dois métodos:
   - `Executar(command, perfilTerminal?)` — abre o terminal com o comando
   - `ObterPerfis()` — retorna os perfis disponíveis do terminal

## Como adicionar um novo terminal

### 1. Criar a classe do terminal

Criar arquivo em `backend/src/Utils/Terminais/NovoTerminal.cs`:

```csharp
using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

public class NovoTerminal : ITerminalEmulator
{
    public void Executar(string command, string? perfilTerminal = null)
    {
        var trimmed = command.TrimEnd(' ', ';');

        // Adaptar argumentos conforme a CLI do terminal
        Process.Start(new ProcessStartInfo
        {
            FileName = "novo-terminal",
            Arguments = $"<args para executar bash -c \"{trimmed}; exec bash\">",
            UseShellExecute = false
        });
    }

    public List<string> ObterPerfis()
    {
        // Retornar lista de perfis disponíveis, ou [] se não suportar
        return [];
    }
}
```

### 2. Registrar na factory

Em `backend/src/Utils/Terminais/TerminalEmulatorFactory.cs`, adicionar o case:

```csharp
public static ITerminalEmulator Criar(string? terminal) => terminal?.ToLower() switch
{
    "ghostty" => new GhosttyTerminal(),
    "novo-terminal" => new NovoTerminal(),  // ← adicionar aqui
    _ => new PtyxisTerminal()
};
```

### 3. Adicionar no select do frontend

Em `frontend/src/views/ConfiguracaoView.vue`, adicionar na lista:

```typescript
const terminaisLinux = ['ptyxis', 'ghostty', 'novo-terminal'];
```

### 4. Testar

- Selecionar o novo terminal na tela de Configuração
- Executar qualquer comando (clone, abrir terminal, etc.)
- Verificar se o terminal abre corretamente com o comando

## Referência de CLIs

| Terminal | Executar comando | Perfil |
|----------|-----------------|--------|
| Ptyxis | `ptyxis --tab -- bash -c "CMD; exec bash"` | `--tab-with-profile=UUID` (resolve label→UUID via dconf) |
| Ghostty | `ghostty -e bash -c "CMD; exec bash"` | Não suporta via CLI |
| gnome-terminal | `gnome-terminal --tab -- bash -c "CMD; exec bash"` | `--profile="NOME"` |
| kitty | `kitty bash -c "CMD; exec bash"` | Não suporta via CLI |
| wezterm | `wezterm start -- bash -c "CMD; exec bash"` | `--profile NOME` (se configurado) |

## Perfis do Ptyxis

O Ptyxis armazena perfis via `dconf`:

- Lista de UUIDs: `dconf read /org/gnome/Ptyxis/profile-uuids`
- Label de cada perfil: `dconf read /org/gnome/Ptyxis/Profiles/{uuid}/label`
- O PMW exibe o label para o usuário e resolve para UUID ao executar
