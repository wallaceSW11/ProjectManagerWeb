# Mapa do Projeto — PMW

Leia antes de qualquer tarefa. Contém a visão geral do sistema.

## O que é o PMW

Ferramenta desktop-web para gerenciar repositórios Git, pastas de projetos, IDEs e deploy no IIS.
Roda local na máquina do desenvolvedor — sem autenticação, sem multi-tenant, sem banco relacional.
Dados persistidos em arquivos JSON em `%APPDATA%\PMW\Banco\`.

## Stack

frontend: Vue 3 + TypeScript + Vuetify 3 + Pinia + Vue Router + Axios + Vite + pnpm
backend: .NET 9 + ASP.NET Core (sem EF, sem banco relacional)
persistência: arquivos JSON em disco (`%APPDATA%\PMW\Banco\`)
sem auth: aplicação local, sem JWT, sem cookies de sessão
infra: publicado no IIS local via `dotnet publish`

## Estrutura do monorepo

```
/
├── frontend/   Vue 3 SPA
└── backend/    .NET 9 Web API
```

## Domínio

- **Repositorio**: repositório Git com URL, projetos filhos, menus, perfis de marcação, IDE padrão, branch base, pasta centralizadora, URL do gestor de tarefas e códigos de tarefa.
- **Projeto**: subprojeto dentro de um repositório. Tem comandos (abrir IDE, executar scripts), branch, diretório.
- **Pasta**: diretório físico no disco. Pode ter projetos e menus associados. Suporta ocultação. Agrupada por `NomeAba` (Raiz ou pasta centralizadora).
- **PastaCentralizadora**: subpasta do diretório raiz que agrupa clones de um mesmo contexto (ex: Forizi, DeepRocket, Pessoal). Cadastrada na Configuração.
- **CodigoTarefa**: vincula iniciais (ex: FATWEB) a um repositório com flags de clone (branch remoto, agregados, histórico, tipos). Um repositório pode ter vários (monorepo).
- **IDE**: ferramenta de desenvolvimento (VS Code, Kiro, Delphi). Configurável com comando de abertura.
- **SiteIIS**: site configurado no IIS local. Tem pastas de deploy e pools de aplicação.
- **Configuracao**: configuração global — diretório raiz de trabalho, perfis VSCode, diretórios ocultos, pastas centralizadoras, CLIs.

## Persistência — arquivos JSON

Todos os dados em `%APPDATA%\PMW\Banco\`:

```
repositorios.json   → lista de Repositorio[]
Configuracao.json   → objeto ConfiguracaoRequestDTO
IDEs.json           → lista de IDEDTO[]
SiteIIS.json        → lista de SiteIISDTO[]
pastas.json         → lista de PastaResponseDTO[]
migrations.json     → registro de migrations executadas
```

Acesso concorrente protegido por `SemaphoreSlim(1,1)` em cada JsonService.

## Migrations

Sistema próprio em JSON (sem EF Migrations).
Registradas em `migrations.json`. Executadas no startup via `MigrationService.ExecuteMigrationsAsync()`.
Nova migration: adicionar método `Migration_00X_NomeDaMigration()` no `MigrationService` e registrar em `ExecuteMigrationsAsync()`.

## Deploy / infraestrutura

**Linux:**
- `bootstrap.sh` → instalação do zero (curl, unzip, git, baixa release, configura systemd)
- `infra/pmw.sh` → gerenciamento (start, stop, update, logs)
- `infra/pmw.service` → systemd user service (self-contained, não precisa de dotnet runtime)
- Diretórios: app em `/opt/pmw/`, scripts em `/opt/pmw-tools/`

**Windows:**
- `infra/pmw.ps1` → gerenciamento (start, stop, update, install) com detecção automática do diretório
- `infra/pmw-start.vbs` → inicialização sem console visível (WScript)
- `infra/pmw-start.bat` → atalho para o .vbs
- `bootstrap.ps1` → script de instalação inicial (baixa release + configura infra + inicia)
- `Atualizar_PMW.ps1` → script legado de atualização
- Diretórios: app em `C:\inetpub\wwwroot\PMW`, scripts em `C:\inetpub\wwwroot\PMW-Tools/`

**CI/CD:**
- `.github/workflows/ci.yml` — build frontend + backend em todo **Pull Request** (para `develop` ou `main`)
- `.github/workflows/release.yml` — push na `main` gera release com `PMW_Windows_*.zip` e `PMW_Linux_*.zip`

## Onde buscar informação

| Preciso de | Arquivo |
|------------|---------|
| Clone de repositório | `.opencode/flows/clone.md` |
| Pastas / comandos / menus | `.opencode/flows/pastas.md` |
| Repositórios / projetos / agregados | `.opencode/flows/repositorios.md` |
| IDEs | `.opencode/flows/ides.md` |
| Sites IIS / Deploy | `.opencode/flows/sitesiis.md` |
| Deploy Linux | `infra/pmw.sh`, `bootstrap.sh` |
| Deploy Windows | `bootstrap.ps1`, `infra/pmw.ps1`, `Atualizar_PMW.ps1` |
| Code style backend | `.opencode/code-style/backend.md` |
| Code style frontend | `.opencode/code-style/frontend.md` |
| Git / commits | `AGENTS.md` (seção Git) |

Leia o fluxo relevante antes de implementar qualquer alteração na feature.
