# Plano de Implementação Cross-Platform (Windows + Linux) — PMW

> Documento gerado em 29/05/2026. Referência para implementação futura.

## Objetivo

Tornar o PMW funcional em **Windows** e **Linux (Ubuntu 26.04)** com o mesmo codebase.
Funcionalidades exclusivas do Windows (IIS, Deploy) serão ocultadas automaticamente no Linux.
O foco no Linux é: **clone de repositórios** e **iniciar projetos**.

## Decisões de Arquitetura

| Decisão | Escolha |
|---------|---------|
| Manter um único backend | ✅ Sim — .NET 9 é cross-platform nativo |
| Detecção de OS | Automática via `OperatingSystem.IsWindows()` / `IsLinux()` |
| Storage de dados | Por máquina (não por usuário) |
| Path do banco | Windows: `%APPDATA%\PMW\Banco` → Linux: `/var/lib/pmw/banco` ou `~/.config/PMW/Banco` |
| Terminal no Linux | Ghostty (`ghostty -e "comando"`) |
| Processo em background (Linux) | systemd user service |
| Features Windows-only | Ocultadas via endpoint `/api/versao/features` |

---

## Diagnóstico Completo — Pontos de Atrito

### 🔴 100% Windows-only (ocultar no Linux)

| Arquivo | Motivo |
|---------|--------|
| `Services/IISService.cs` | Usa `appcmd.exe` (C:\Windows\System32\inetsrv\appcmd.exe) |
| `Services/DeployIISService.cs` | Gera scripts PowerShell para deploy no IIS |
| `Services/SiteIISJsonService.cs` | Persistência de sites IIS |
| `Services/TerminalService.cs` | Lê perfis do Windows Terminal (LocalAppData) |
| `Controllers/IISController.cs` | Endpoints start/stop/restart de sites IIS |
| `Controllers/SiteIISController.cs` | CRUD de sites IIS + disparo de deploy |

### 🟡 Precisam de adaptação

#### 1. `Utils/ShellExecute.cs` — CRÍTICO

**Problema:** Hardcoded para Windows (`pwsh.exe`, `wt.exe`, `cmd.exe`).

**Métodos afetados:**
- `ExecutarComandoComInterface()` → usa `pwsh.exe` ou `wt.exe`
- `ExecutarComandoComoAdministrador()` → usa `pwsh.exe` com `Verb = "runas"`
- `ExecutarComandoSemInterface()` → usa `pwsh.exe -WindowStyle Hidden`

**Solução:** Criar abstração `IShellProvider` com implementações por OS.

---

#### 2. `Services/ComandoService.cs` — CRÍTICO

**Problema:** Concatenação de paths com `"\\"` hardcoded e uso de `Copy-Item` (PowerShell).

**Linhas afetadas:**

```csharp
// Linha ~15 — separador hardcoded
var diretorio = pasta.Diretorio + "\\" + repositorio.Nome + "\\";

// Linha ~70 — separador hardcoded em agregados
var diretorioAgregado = diretorio.Replace(repositorio.Nome, repositorioAgregado.Nome) + "\\";

// Linha ~100+ — Directory.Exists com path Windows
Directory.Exists($"{diretorio}{projetoCadastrado.Subdiretorio}\\node_modules")

// Linha ~130+ — Copy-Item (PowerShell)
comandos.Add($"Copy-Item \"{a.Arquivo}\" \"{diretorioDestino}\\{nomeArquivo}\" -Recurse -Force; Exit;");
comandos.Add($"Copy-Item \"{p.Origem}\" \"{caminhoDestinoCompleto}\" -Recurse -Force; Exit;");
```

**Solução:** Usar `Path.Combine()` e `Path.DirectorySeparatorChar`. Substituir `Copy-Item` por `File.Copy`/`Directory.Copy` nativo do .NET (sem depender de shell).

---

#### 3. `Services/PastaService.cs` — MODERADO

**Problema:** Concatenação com `"\\"` na verificação de agregados.

```csharp
// Linha ~87
if (!Directory.Exists(pastaNoDisco + "\\" + repositorioAgregado.Nome) || ...)
```

**Solução:** Trocar por `Path.Combine(pastaNoDisco, repositorioAgregado.Nome)`.

---

#### 4. `DTOs/ConfiguracaoRequestDTO.cs` — MENOR

**Problema:** Default hardcoded Windows.

```csharp
string DiretorioRaiz = "C:\\tools\\git"
```

**Solução:** Default dinâmico por OS:
- Windows: `C:\tools\git`
- Linux: `~/git` ou `/home/user/git`

---

#### 5. `Utils/PathHelper.cs` — OK (quase)

**Código atual:**
```csharp
public static string BancoPath =>
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PMW",
        "Banco"
    );
```

**No Linux:** `Environment.SpecialFolder.ApplicationData` resolve para `~/.config`.
Resultado: `~/.config/PMW/Banco` — **funciona sem mudança**.

**Decisão do usuário:** Pode ser por máquina. Se quiser path fixo tipo `/var/lib/pmw/banco`, precisa alterar. Se `~/.config/PMW/Banco` serve, não precisa mexer.

---

### 🟢 Já funciona no Linux sem mudança

| Componente | Motivo |
|------------|--------|
| `Program.cs` | Kestrel puro, sem dependência de IIS |
| `PathHelper.cs` | `SpecialFolder.ApplicationData` → `~/.config` |
| Todos os `*JsonService.cs` | Leitura/escrita de JSON com `System.IO` |
| `ConfiguracaoService.cs` | Usa `PathHelper.BancoPath` |
| `MigrationService.cs` | Manipula JSON |
| `RepositorioJsonService.cs` | CRUD em JSON |
| `PastaJsonService.cs` | CRUD em JSON |
| `IDEJsonService.cs` | CRUD em JSON |
| `CloneService.ExecutarComandoComRetornoAsync()` | Usa `git` direto (cross-platform) |
| Todo o frontend (Vue SPA) | Roda no browser |



---

## Implementação — Fase 1: Abstrair Execução de Comandos (IShellProvider)

### Objetivo

Substituir o `ShellExecute` estático por uma abstração injetável que delega para o provider correto por OS.

### Arquivos a criar

```
backend/src/Utils/
├── ShellExecute.cs              → MANTER (refatorar para delegar ao provider)
├── IShellProvider.cs            → NOVO (interface)
├── WindowsShellProvider.cs      → NOVO (lógica atual extraída)
└── LinuxShellProvider.cs        → NOVO (implementação Linux)
```

### Interface `IShellProvider.cs`

```csharp
namespace ProjectManagerWeb.src.Utils;

public interface IShellProvider
{
    /// <summary>
    /// Executa comando com terminal visível (usuário vê o output).
    /// Equivale a abrir um terminal com o comando.
    /// </summary>
    void ExecutarComInterface(string command, string? perfilTerminal = null);

    /// <summary>
    /// Executa comando sem interface (background, sem janela).
    /// </summary>
    void ExecutarSemInterface(string command);

    /// <summary>
    /// Executa comando com privilégios elevados (admin/sudo).
    /// </summary>
    void ExecutarComoAdministrador(string command, string? perfilTerminal = null);

    /// <summary>
    /// Retorna lista de perfis de terminal disponíveis.
    /// Windows: perfis do Windows Terminal.
    /// Linux: lista vazia ou terminais instalados.
    /// </summary>
    List<string> ObterPerfisTerminal();
}
```

### `WindowsShellProvider.cs` — Extrair lógica atual

Mover toda a lógica do `ShellExecute.cs` atual para cá:
- `ExecutarComInterface` → usa `pwsh.exe` ou `wt.exe` (código atual de `ExecutarComandoComInterface`)
- `ExecutarSemInterface` → usa `pwsh.exe -WindowStyle Hidden` (código atual de `ExecutarComandoSemInterface`)
- `ExecutarComoAdministrador` → usa `pwsh.exe` com `Verb = "runas"` (código atual)
- `ObterPerfisTerminal` → mover lógica do `TerminalService.cs` para cá

### `LinuxShellProvider.cs` — Implementação Linux

```csharp
namespace ProjectManagerWeb.src.Utils;

public class LinuxShellProvider : IShellProvider
{
    // Terminal padrão — pode vir de Configuracao.json futuramente
    private const string DefaultTerminal = "ghostty";

    public void ExecutarComInterface(string command, string? perfilTerminal = null)
    {
        // Ghostty: abre nova janela com o comando
        // ghostty -e bash -c "cd /path && comando; exec bash"
        // O "exec bash" mantém o terminal aberto após o comando
        var bashCommand = $"bash -c '{EscaparParaBash(command)}; exec bash'";

        var psi = new ProcessStartInfo
        {
            FileName = DefaultTerminal,
            Arguments = $"-e {bashCommand}",
            UseShellExecute = false,
            CreateNoWindow = false
        };

        Process.Start(psi);
    }

    public void ExecutarSemInterface(string command)
    {
        // Executa em background via bash sem terminal visível
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{EscaparParaBash(command)}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process.Start(psi);
    }

    public void ExecutarComoAdministrador(string command, string? perfilTerminal = null)
    {
        // No Linux, não há equivalente direto ao UAC.
        // Se precisar sudo, o comando já deve conter "sudo".
        // Para o PMW (app local), raramente precisa de sudo.
        ExecutarComInterface(command, perfilTerminal);
    }

    public List<string> ObterPerfisTerminal()
    {
        // Linux não tem conceito de "perfis de terminal" como o Windows Terminal.
        // Retorna lista vazia — campo de perfil fica oculto no frontend.
        return [];
    }

    private static string EscaparParaBash(string command)
    {
        // Converte separadores de comando PowerShell (;) para bash (&&) quando necessário
        // e escapa aspas simples
        return command
            .Replace("'", "'\\''");
    }
}
```

### Registro no DI (`Program.cs`)

```csharp
// Após os outros AddSingleton:
if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<IShellProvider, WindowsShellProvider>();
else
    builder.Services.AddSingleton<IShellProvider, LinuxShellProvider>();
```

### Refatorar `ShellExecute.cs`

O `ShellExecute` estático atual vira um wrapper que resolve o provider via DI.
Como ele é chamado de forma estática em vários lugares, duas opções:

**Opção A (recomendada):** Injetar `IShellProvider` nos services que usam (`ComandoService`, `CloneService`) e remover chamadas estáticas.

**Opção B (menor impacto):** Manter `ShellExecute` estático mas com um provider configurado no startup:

```csharp
public static class ShellExecute
{
    private static IShellProvider _provider = null!;

    public static void Configure(IShellProvider provider) => _provider = provider;

    public static void ExecutarComando(string command, string? perfilTerminal = null)
    {
        if (command.Contains("Exit;", StringComparison.OrdinalIgnoreCase))
            _provider.ExecutarSemInterface(command);
        else
            _provider.ExecutarComInterface(command, perfilTerminal);
    }

    public static void ExecutarComandoComInterface(string command, string? perfilTerminal = null)
        => _provider.ExecutarComInterface(command, perfilTerminal);

    public static void ExecutarComandoComoAdministrador(string command, string? perfilTerminal = null)
        => _provider.ExecutarComoAdministrador(command, perfilTerminal);

    public static void ExecutarComandoSemInterface(string command)
        => _provider.ExecutarSemInterface(command);
}
```

E no `Program.cs` após o build:
```csharp
var app = builder.Build();
ShellExecute.Configure(app.Services.GetRequiredService<IShellProvider>());
```

**Recomendação:** Opção B para menor impacto — não precisa refatorar `ComandoService`, `CloneService`, etc. Eles continuam chamando `ShellExecute.ExecutarComando()` normalmente.

### Adaptação de comandos PowerShell → Bash

Os comandos hoje são escritos em sintaxe PowerShell:
```powershell
cd C:\git\projeto; pnpm run dev;
```

No Linux precisam ser:
```bash
cd /home/user/git/projeto && pnpm run dev
```

**Onde tratar isso:** No `LinuxShellProvider`, antes de executar:
1. O separador `;` do PowerShell funciona em bash também (executa independente do resultado)
2. Paths já virão corretos se o `ComandoService` usar `Path.Combine`
3. Comandos como `pnpm run dev`, `dotnet run`, `code .` são idênticos nos dois OS

**Comandos que NÃO existem no Linux:**
- `Copy-Item` → substituir por `File.Copy` / `cp`
- `Exit;` → no bash não precisa (o terminal fecha sozinho ou mantém aberto conforme config)



---

## Implementação — Fase 2: Paths e Comandos Cross-Platform

### Objetivo

Eliminar separadores `\\` hardcoded e comandos PowerShell-only (`Copy-Item`).

### 2.1 — `ComandoService.cs`

#### Mudança 1: Separadores de path

**Antes:**
```csharp
var diretorio = pasta.Diretorio + "\\" + repositorio.Nome + "\\";
```

**Depois:**
```csharp
var diretorio = Path.Combine(pasta.Diretorio, repositorio.Nome);
```

**Antes:**
```csharp
var diretorioAgregado = diretorio.Replace(repositorio.Nome, repositorioAgregado.Nome) + "\\";
```

**Depois:**
```csharp
var diretorioAgregado = Path.Combine(
    Path.GetDirectoryName(diretorio)!,
    repositorioAgregado.Nome
);
```

**Antes:**
```csharp
Directory.Exists($"{diretorio}{projetoCadastrado.Subdiretorio}\\node_modules")
```

**Depois:**
```csharp
Directory.Exists(Path.Combine(diretorio, projetoCadastrado.Subdiretorio, "node_modules"))
```

#### Mudança 2: Substituir `Copy-Item` por operação nativa .NET

**Antes (ExecutarComandoMenu):**
```csharp
comandos.Add($"Copy-Item \"{a.Arquivo}\" \"{diretorioDestino}\\{nomeArquivo}\" -Recurse -Force; Exit;");
```

**Depois:**
```csharp
// Copiar diretamente via .NET — sem depender de shell
var destino = Path.Combine(diretorioDestino, nomeArquivo);
File.Copy(a.Arquivo, destino, overwrite: true);
```

Para pastas:
```csharp
// Antes:
comandos.Add($"Copy-Item \"{p.Origem}\" \"{caminhoDestinoCompleto}\" -Recurse -Force; Exit;");

// Depois:
CopiarDiretorioRecursivo(p.Origem, caminhoDestinoCompleto);
```

Método auxiliar:
```csharp
private static void CopiarDiretorioRecursivo(string origem, string destino)
{
    Directory.CreateDirectory(destino);

    foreach (var arquivo in Directory.GetFiles(origem))
        File.Copy(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)), true);

    foreach (var subDir in Directory.GetDirectories(origem))
        CopiarDiretorioRecursivo(subDir, Path.Combine(destino, Path.GetFileName(subDir)));
}
```

#### Mudança 3: Montagem de comandos de terminal

**Antes:**
```csharp
comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Iniciar}; ", projetoCadastrado.PerfilTerminal));
```

**Depois:**
```csharp
var pathProjeto = Path.Combine(diretorio, projetoCadastrado.Subdiretorio);
comandos.Add(($"cd \"{pathProjeto}\"; {projetoCadastrado.Comandos.Iniciar}", projetoCadastrado.PerfilTerminal));
```

> Nota: O `;` funciona tanto em PowerShell quanto em Bash. Não precisa converter para `&&`.

#### Mudança 4: Remover `Exit;` condicional

O `Exit;` é usado hoje para sinalizar ao `ShellExecute` que deve rodar sem interface.
No novo modelo, quem decide isso é o chamador (via `ExecutarSemInterface` vs `ExecutarComInterface`).

**Antes:**
```csharp
comandos.Add(($"cd {diretorio}; {texto}; Exit;", null));
```

**Depois:**
```csharp
// Para ABRIR_NA_IDE: sempre sem interface (abre a IDE e fecha o shell)
var pathProjeto = Path.Combine(diretorio, projetoCadastrado.Subdiretorio);
ShellExecute.ExecutarComandoSemInterface($"cd \"{pathProjeto}\"; {texto}");
```

Ou manter a convenção `Exit;` no `ShellExecute` wrapper — ele já detecta e redireciona.

---

### 2.2 — `PastaService.cs`

**Antes:**
```csharp
if (!Directory.Exists(pastaNoDisco + "\\" + repositorioAgregado.Nome) || ...)
```

**Depois:**
```csharp
if (!Directory.Exists(Path.Combine(pastaNoDisco, repositorioAgregado.Nome)) || ...)
```

---

### 2.3 — `ConfiguracaoRequestDTO.cs`

**Antes:**
```csharp
string DiretorioRaiz = "C:\\tools\\git"
```

**Depois:**
```csharp
string DiretorioRaiz = GetDefaultDiretorioRaiz()
```

Não dá pra chamar método em default de record. Alternativa:

```csharp
public sealed record ConfiguracaoRequestDTO
(
    string? DiretorioRaiz = null,
    List<PerfilVSCodeRequestDTO>? PerfisVSCode = default,
    List<string>? DiretoriosOcultos = default,
    [property: JsonPropertyName("clis")] List<CliRequestDTO>? CLIs = default
)
{
    public string DiretorioRaizEfetivo => DiretorioRaiz
        ?? (OperatingSystem.IsWindows() ? @"C:\tools\git" : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "git"));
}
```

E no `PastaService` usar `configuracao.DiretorioRaizEfetivo` ao invés de `configuracao.DiretorioRaiz`.

---

### 2.4 — `PathHelper.cs` — Opção por máquina

Se quiser path fixo por máquina (não por usuário):

```csharp
public static class PathHelper
{
    public static string BancoPath =>
        OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco")
            : "/var/lib/pmw/banco";
}
```

- Windows `CommonApplicationData` = `C:\ProgramData\PMW\Banco` (por máquina)
- Linux: `/var/lib/pmw/banco` (por máquina, padrão FHS)

**Ou manter por usuário** (mais simples, sem precisar de permissões especiais):
- Windows: `%APPDATA%\PMW\Banco` (atual, funciona)
- Linux: `~/.config/PMW/Banco` (funciona sem mudança no código atual)

**Recomendação:** Manter `~/.config/PMW/Banco` no Linux. É mais simples, não precisa de `sudo` para criar, e como é app local de um usuário, faz sentido. Se no futuro quiser por máquina, é só mudar o `PathHelper`.



---

## Implementação — Fase 3: Ocultar Features Windows-only

### Objetivo

O frontend deve saber quais features estão disponíveis e esconder menus/botões de IIS e Deploy quando rodar no Linux.

### 3.1 — Backend: Endpoint de features

Adicionar ao `VersaoController`:

```csharp
[HttpGet("features")]
public IActionResult Features()
{
    return Ok(new
    {
        Iis = OperatingSystem.IsWindows(),
        Deploy = OperatingSystem.IsWindows(),
        TerminalProfiles = OperatingSystem.IsWindows(),
        Os = OperatingSystem.IsWindows() ? "windows" : "linux"
    });
}
```

**Rota:** `GET /api/versao/features`

**Response:**
```json
// Windows
{ "iis": true, "deploy": true, "terminalProfiles": true, "os": "windows" }

// Linux
{ "iis": false, "deploy": false, "terminalProfiles": false, "os": "linux" }
```

### 3.2 — Backend: Registro condicional de services

No `Program.cs`, registrar services de IIS apenas no Windows:

```csharp
// Services cross-platform (sempre registrar)
builder.Services.AddSingleton<RepositorioJsonService>();
builder.Services.AddSingleton<ConfiguracaoService>();
builder.Services.AddSingleton<CloneService>();
builder.Services.AddSingleton<PastaService>();
builder.Services.AddSingleton<ComandoService>();
builder.Services.AddSingleton<PastaJsonService>();
builder.Services.AddSingleton<IDEJsonService>();
builder.Services.AddSingleton<MigrationService>();

// Services Windows-only
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<IISService>();
    builder.Services.AddSingleton<SiteIISJsonService>();
    builder.Services.AddSingleton<DeployIISService>();
    builder.Services.AddSingleton<TerminalService>();
}
```

**Problema:** Os controllers de IIS injetam esses services. Se não registrados, o DI falha.

**Solução:** Tornar os controllers de IIS condicionais também. Usar `[ApiExplorerSettings(IgnoreApi = true)]` não resolve o DI. Melhor abordagem:

```csharp
// IISController.cs — adicionar guard
[HttpGet]
public async Task<IActionResult> ListarSites()
{
    if (!OperatingSystem.IsWindows())
        return StatusCode(501, "Funcionalidade disponível apenas no Windows");

    // ... lógica atual
}
```

Ou registrar services como nullable/noop no Linux:

```csharp
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<IISService>();
    builder.Services.AddSingleton<SiteIISJsonService>();
    builder.Services.AddSingleton<DeployIISService>();
}
else
{
    // Registrar como null para não quebrar DI dos controllers
    builder.Services.AddSingleton<IISService>(_ => null!);
    builder.Services.AddSingleton<SiteIISJsonService>(_ => null!);
    builder.Services.AddSingleton<DeployIISService>(_ => null!);
}
```

**Recomendação:** A abordagem mais limpa é manter o registro normal e adicionar um guard no início de cada action dos controllers de IIS:

```csharp
if (!OperatingSystem.IsWindows())
    return StatusCode(501, new { mensagem = "Funcionalidade disponível apenas no Windows" });
```

Isso é simples, não quebra nada, e o frontend já não vai chamar esses endpoints porque os menus estarão ocultos.

### 3.3 — Frontend: Store de features

Criar `frontend/src/stores/features.ts`:

```typescript
import { defineStore } from 'pinia';
import VersaoService from '@/services/VersaoService';

interface FeaturesState {
  iis: boolean;
  deploy: boolean;
  terminalProfiles: boolean;
  os: string;
  carregado: boolean;
}

export const useFeaturesStore = defineStore('features', {
  state: (): FeaturesState => ({
    iis: false,
    deploy: false,
    terminalProfiles: false,
    os: 'windows',
    carregado: false,
  }),

  getters: {
    isWindows: (state) => state.os === 'windows',
    isLinux: (state) => state.os === 'linux',
  },

  actions: {
    async carregar() {
      if (this.carregado) return;
      const response = await VersaoService.obterFeatures();
      this.iis = response.iis;
      this.deploy = response.deploy;
      this.terminalProfiles = response.terminalProfiles;
      this.os = response.os;
      this.carregado = true;
    },
  },
});
```

### 3.4 — Frontend: Service de features

Adicionar ao `VersaoService.ts`:

```typescript
async obterFeatures(): Promise<FeaturesResponse> {
  const { data } = await api.get('/api/versao/features');
  return data;
}
```

### 3.5 — Frontend: Ocultar menus no `App.vue`

```html
<!-- Sites IIS — só mostra no Windows -->
<v-btn
  v-if="featuresStore.iis"
  class="text-none"
  @click="exibirModalSites = true"
>
  <v-icon class="pr-2" color="primary">mdi-web</v-icon>
  Sites IIS
</v-btn>

<!-- Deploy — só mostra no Windows -->
<v-menu v-if="featuresStore.deploy">
  <!-- ... conteúdo atual do menu deploy ... -->
</v-menu>
```

No `<script setup>`:
```typescript
import { useFeaturesStore } from '@/stores/features';
const featuresStore = useFeaturesStore();

onMounted(async () => {
  await featuresStore.carregar();
  // ... resto do onMounted atual
});
```

### 3.6 — Frontend: Ocultar campo "Perfil Terminal" em formulários

Nos componentes que mostram seleção de perfil do Windows Terminal, adicionar `v-if`:

```html
<v-select
  v-if="featuresStore.terminalProfiles"
  v-model="perfilTerminal"
  :items="perfisTerminal"
  label="Perfil do Terminal"
/>
```

### 3.7 — Frontend: Rota `/sites-iis`

Manter a rota registrada (não quebra nada). Se o usuário acessar diretamente a URL no Linux, a view pode mostrar uma mensagem:

```html
<!-- SitesIISView.vue -->
<template>
  <div v-if="!featuresStore.iis" class="pa-8 text-center">
    <v-icon size="64" color="grey">mdi-information</v-icon>
    <p class="mt-4 text-grey">Funcionalidade disponível apenas no Windows.</p>
  </div>
  <div v-else>
    <!-- conteúdo atual -->
  </div>
</template>
```

### 3.8 — Frontend: Não carregar sites IIS no Linux

No `App.vue`, condicionar o carregamento:

```typescript
onMounted(async () => {
  await featuresStore.carregar();
  await consultarVersao();

  if (featuresStore.iis) {
    await carregarSitesParaDeploy();
  }
});
```



---

## Implementação — Fase 4: Infraestrutura Linux

### Objetivo

Definir como o PMW sobe, para e é gerenciado no Linux — equivalente ao "rodar sem janela no gerenciador de tarefas" do Windows.

### 4.1 — systemd user service

Criar arquivo `infra/pmw.service`:

```ini
[Unit]
Description=Project Manager Web (PMW)
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/pmw
ExecStart=/usr/bin/dotnet ProjectManagerWeb.dll
Restart=on-failure
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:2025
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

[Install]
WantedBy=default.target
```

**Instalação:**
```bash
mkdir -p ~/.config/systemd/user/
cp infra/pmw.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable pmw    # inicia automaticamente com login
systemctl --user start pmw
```

**Comandos do dia-a-dia:**
```bash
systemctl --user start pmw     # iniciar
systemctl --user stop pmw      # parar
systemctl --user restart pmw   # reiniciar
systemctl --user status pmw    # ver status
journalctl --user -u pmw -f   # ver logs em tempo real
```

**Equivalência com Windows:**
| Windows | Linux |
|---------|-------|
| Processo sem janela no Task Manager | systemd user service |
| Iniciar com login do Windows | `systemctl --user enable pmw` |
| Matar pelo Task Manager | `systemctl --user stop pmw` |
| Ver se está rodando | `systemctl --user status pmw` |

### 4.2 — Script de conveniência `pmw.sh`

Criar `infra/pmw.sh`:

```bash
#!/bin/bash
# PMW - Project Manager Web - Script de gerenciamento

PMW_DIR="/opt/pmw"
SERVICE_NAME="pmw"

case "$1" in
  start)
    systemctl --user start $SERVICE_NAME
    echo "PMW iniciado. Acesse http://localhost:2025"
    ;;
  stop)
    systemctl --user stop $SERVICE_NAME
    echo "PMW parado."
    ;;
  restart)
    systemctl --user restart $SERVICE_NAME
    echo "PMW reiniciado."
    ;;
  status)
    systemctl --user status $SERVICE_NAME
    ;;
  logs)
    journalctl --user -u $SERVICE_NAME -f
    ;;
  install)
    echo "Instalando PMW..."
    sudo mkdir -p $PMW_DIR
    sudo chown $USER:$USER $PMW_DIR
    mkdir -p ~/.config/systemd/user/
    cp "$(dirname "$0")/pmw.service" ~/.config/systemd/user/
    systemctl --user daemon-reload
    systemctl --user enable $SERVICE_NAME
    echo "PMW instalado. Use 'pmw.sh start' para iniciar."
    ;;
  update)
    echo "Atualizando PMW..."
    systemctl --user stop $SERVICE_NAME
    # Aqui entraria a lógica de baixar o release do GitHub
    # Similar ao Atualizar_PMW.ps1 do Windows
    echo "Copie os novos arquivos para $PMW_DIR e execute 'pmw.sh start'"
    ;;
  *)
    echo "Uso: pmw.sh {start|stop|restart|status|logs|install|update}"
    exit 1
    ;;
esac
```

**Instalação do script no PATH:**
```bash
sudo ln -s /opt/pmw/infra/pmw.sh /usr/local/bin/pmw
```

Depois: `pmw start`, `pmw stop`, `pmw logs`, etc.

### 4.3 — Build e publicação para Linux

O `dotnet publish` já suporta Linux. Duas opções:

**Opção A — Framework-dependent (requer .NET runtime instalado):**
```bash
dotnet publish -c Release -o /opt/pmw
```
Menor tamanho (~30MB). Precisa do `dotnet-runtime-9.0` instalado.

**Opção B — Self-contained (binário único, sem dependências):**
```bash
dotnet publish -c Release -r linux-x64 --self-contained -o /opt/pmw
```
Maior tamanho (~80MB). Não precisa de nada instalado.

**Recomendação:** Opção A (framework-dependent). Instalar o runtime é um `apt install` e facilita updates de segurança do .NET.

### 4.4 — GitHub Actions: Release multi-plataforma

Adaptar `.github/workflows/release.yml` para gerar dois artefatos:

```yaml
jobs:
  build:
    strategy:
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            artifact: PMW_Windows
          - os: ubuntu-latest
            rid: linux-x64
            artifact: PMW_Linux

    runs-on: ${{ matrix.os }}

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Setup pnpm
        uses: pnpm/action-setup@v4
        with:
          version: 9

      - name: Install frontend deps
        working-directory: frontend
        run: pnpm install --frozen-lockfile

      - name: Build frontend
        working-directory: frontend
        run: pnpm run build

      - name: Copy frontend to backend
        # Windows usa xcopy, Linux usa cp
        run: |
          cp -r frontend/dist/* backend/frontend/ 2>/dev/null || xcopy frontend\dist backend\frontend /E /I /Y

      - name: Publish backend
        working-directory: backend
        run: dotnet publish -c Release -o ../publish

      - name: Package
        run: |
          zip -r ${{ matrix.artifact }}.zip publish/
```

Release teria dois zips:
- `PMW_Windows_v2026.05.29.zip`
- `PMW_Linux_v2026.05.29.zip`

### 4.5 — Instalação no Linux (primeira vez)

```bash
# 1. Instalar .NET runtime
sudo apt update
sudo apt install dotnet-runtime-9.0

# 2. Instalar Ghostty
# (seguir instruções oficiais do Ghostty para Ubuntu)

# 3. Baixar PMW
wget https://github.com/SEU_USER/PMW/releases/latest/download/PMW_Linux.zip
sudo mkdir -p /opt/pmw
sudo chown $USER:$USER /opt/pmw
unzip PMW_Linux.zip -d /opt/pmw

# 4. Instalar service
/opt/pmw/infra/pmw.sh install

# 5. Iniciar
pmw start

# 6. Acessar
xdg-open http://localhost:2025
```



---

## Ordem de Execução Recomendada

```
┌─────────────────────────────────────────────────────────────────┐
│ FASE 1: IShellProvider                                          │
│ ├── Criar IShellProvider.cs                                     │
│ ├── Criar WindowsShellProvider.cs (extrair lógica atual)        │
│ ├── Criar LinuxShellProvider.cs (Ghostty + bash)                │
│ ├── Refatorar ShellExecute.cs para delegar ao provider          │
│ ├── Registrar no DI (Program.cs)                                │
│ └── TESTAR NO WINDOWS (não pode quebrar nada)                   │
├─────────────────────────────────────────────────────────────────┤
│ FASE 2: Paths cross-platform                                    │
│ ├── ComandoService: Path.Combine em todos os pontos             │
│ ├── ComandoService: File.Copy ao invés de Copy-Item             │
│ ├── PastaService: Path.Combine no check de agregados            │
│ ├── ConfiguracaoRequestDTO: default dinâmico por OS             │
│ └── TESTAR NO WINDOWS (não pode quebrar nada)                   │
├─────────────────────────────────────────────────────────────────┤
│ FASE 3: Frontend condicional                                    │
│ ├── Backend: endpoint GET /api/versao/features                  │
│ ├── Frontend: criar useFeaturesStore                            │
│ ├── Frontend: VersaoService.obterFeatures()                     │
│ ├── Frontend: v-if nos menus de IIS e Deploy                    │
│ ├── Frontend: condicionar carregamento de sites no App.vue      │
│ └── TESTAR NO WINDOWS (menus devem continuar visíveis)          │
├─────────────────────────────────────────────────────────────────┤
│ FASE 4: Infraestrutura Linux                                    │
│ ├── Criar infra/pmw.service                                     │
│ ├── Criar infra/pmw.sh                                          │
│ ├── Adaptar GitHub Actions para multi-plataforma                │
│ ├── Documentar instalação Linux                                 │
│ └── TESTAR NO LINUX (clone + iniciar projeto)                   │
└─────────────────────────────────────────────────────────────────┘
```

**Regra de ouro:** Cada fase deve ser testada no Windows antes de avançar. Nenhuma fase pode quebrar o funcionamento atual.

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Quebrar Windows ao refatorar ShellExecute | Alto | Testar todos os fluxos (clone, iniciar, IDE, menu) após Fase 1 |
| Ghostty não disponível no Ubuntu 26.04 | Médio | LinuxShellProvider com terminal configurável (fallback para `x-terminal-emulator`) |
| Paths com espaços no Linux | Baixo | Sempre usar aspas em paths nos comandos: `cd "/path/com espaço"` |
| Permissões de arquivo no Linux | Baixo | Garantir que `/opt/pmw` e `~/.config/PMW` pertencem ao usuário |
| Comandos cadastrados com sintaxe PowerShell | Médio | Documentar que comandos devem ser cross-platform (git, pnpm, dotnet, code) |

---

## Checklist de Validação Final

### Windows (não pode regredir)
- [ ] Clone de repositório funciona
- [ ] Iniciar projeto (pnpm run dev, dotnet run) funciona
- [ ] Abrir IDE funciona
- [ ] Menus de repositório funcionam (Copy-Item → File.Copy)
- [ ] Sites IIS continuam visíveis e funcionais
- [ ] Deploy continua funcionando
- [ ] Perfis do Windows Terminal aparecem

### Linux (novo)
- [ ] PMW sobe via systemd (`pmw start`)
- [ ] Acessa http://localhost:2025 e carrega a SPA
- [ ] Menu de IIS e Deploy estão ocultos
- [ ] Configuração com DiretorioRaiz Linux funciona
- [ ] Pastas são listadas corretamente
- [ ] Clone de repositório abre Ghostty com git clone
- [ ] Iniciar projeto abre Ghostty com o comando
- [ ] Abrir IDE (code .) funciona
- [ ] Dados persistem em ~/.config/PMW/Banco/

---

## Arquivos Modificados (resumo)

### Backend — Modificar
| Arquivo | Mudança |
|---------|---------|
| `Program.cs` | Registro condicional de services + ShellExecute.Configure() |
| `Utils/ShellExecute.cs` | Refatorar para delegar ao IShellProvider |
| `Services/ComandoService.cs` | Path.Combine + File.Copy |
| `Services/PastaService.cs` | Path.Combine no check de agregados |
| `DTOs/ConfiguracaoRequestDTO.cs` | Default dinâmico por OS |
| `Controllers/VersaoController.cs` | Adicionar endpoint /features |
| `Controllers/IISController.cs` | Guard `OperatingSystem.IsWindows()` |
| `Controllers/SiteIISController.cs` | Guard `OperatingSystem.IsWindows()` |

### Backend — Criar
| Arquivo | Descrição |
|---------|-----------|
| `Utils/IShellProvider.cs` | Interface de abstração |
| `Utils/WindowsShellProvider.cs` | Implementação Windows (lógica atual) |
| `Utils/LinuxShellProvider.cs` | Implementação Linux (Ghostty + bash) |

### Frontend — Modificar
| Arquivo | Mudança |
|---------|---------|
| `App.vue` | v-if nos menus IIS/Deploy + carregar features |
| `views/SitesIISView.vue` | Guard para OS não-Windows |
| `services/VersaoService.ts` | Método obterFeatures() |

### Frontend — Criar
| Arquivo | Descrição |
|---------|-----------|
| `stores/features.ts` | Store de features disponíveis por OS |

### Infra — Criar
| Arquivo | Descrição |
|---------|-----------|
| `infra/pmw.service` | systemd unit file |
| `infra/pmw.sh` | Script de gerenciamento |

---

## Notas Adicionais

### Sobre o Ghostty

- Site: https://ghostty.org
- Instalação Ubuntu: via `.deb` do release ou build from source
- Comando para abrir com comando: `ghostty -e bash -c "comando"`
- Config: `~/.config/ghostty/config`

### Sobre comandos cadastrados nos repositórios

Os comandos que o usuário cadastra (Iniciar, Instalar, Buildar) precisam ser cross-platform:
- ✅ `pnpm run dev` — funciona nos dois
- ✅ `dotnet run` — funciona nos dois
- ✅ `code .` — funciona nos dois (VS Code)
- ✅ `git pull` — funciona nos dois
- ❌ `Copy-Item` — só PowerShell (mas isso é interno do PMW, não do usuário)

Se o usuário cadastrar um comando Windows-only (tipo `start chrome`), ele simplesmente não vai funcionar no Linux. Isso é responsabilidade do usuário — o PMW não precisa traduzir comandos arbitrários.

### Sobre o banco de dados compartilhado

Cada máquina tem seu próprio banco (`Configuracao.json`, `repositorios.json`, etc.).
Se quiser sincronizar repositórios entre Windows e Linux, pode:
1. Copiar manualmente os JSONs
2. Futuramente: export/import de configuração (feature nova, não neste escopo)

Os paths nos JSONs são absolutos (`C:\git\...` vs `/home/user/git/...`), então **não são portáveis entre OS**. Cada máquina precisa cadastrar suas pastas.

