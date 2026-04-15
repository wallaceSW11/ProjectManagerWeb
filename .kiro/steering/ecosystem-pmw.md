---
inclusion: always
---

# ecosystem-pmw

Mapa do projeto ProjectManagerWeb (PMW). Leia antes de qualquer tarefa.

## o que é o PMW

Ferramenta desktop-web para gerenciar repositórios Git, pastas de projetos, IDEs e deploy no IIS.
Roda localmente na máquina do desenvolvedor — sem autenticação, sem multi-tenant, sem banco de dados relacional.
Dados persistidos em arquivos JSON em `%APPDATA%\PMW\Banco\`.

## stack

frontend: Vue 3 + TypeScript + Vuetify 3 + Pinia + Vue Router + Axios + Vite + pnpm
backend: .NET 9 + ASP.NET Core (sem EF, sem banco relacional)
persistência: arquivos JSON em disco (`%APPDATA%\PMW\Banco\`)
sem auth: aplicação local, sem JWT, sem cookies de sessão
infra: publicado no IIS local via `dotnet publish`

## monorepo

```
/
├── frontend/   Vue 3 SPA
└── backend/    .NET 9 Web API
```

## domínio

- **Repositorio**: repositório Git com URL, projetos filhos, menus, perfis de marcação e IDE padrão.
- **Projeto**: subprojeto dentro de um repositório. Tem comandos (abrir IDE, executar scripts), branch, diretório.
- **Pasta**: diretório físico no disco. Pode ter projetos e menus associados. Suporta ocultação.
- **IDE**: ferramenta de desenvolvimento (VS Code, Kiro, Delphi). Configurável com comando de abertura.
- **SiteIIS**: site configurado no IIS local. Tem pastas de deploy e pools de aplicação.
- **Configuracao**: configuração global — diretório raiz de trabalho, perfis VSCode, diretórios ocultos.

## persistência — arquivos JSON

Todos os dados ficam em `%APPDATA%\PMW\Banco\`:

```
repositorios.json   → lista de Repositorio[]
Configuracao.json   → objeto ConfiguracaoRequestDTO
IDEs.json           → lista de IDEDTO[]
SiteIIS.json        → lista de SiteIISDTO[]
pastas.json         → lista de PastaResponseDTO[]
migrations.json     → registro de migrations executadas
```

Acesso concorrente protegido por `SemaphoreSlim(1,1)` em cada JsonService.

## migrations

Sistema próprio de migrations em JSON (sem EF Migrations).
Registradas em `migrations.json`. Executadas no startup via `MigrationService.ExecuteMigrationsAsync()`.
Novas migrations: adicionar método `Migration_00X_NomeDaMigration()` no `MigrationService` e registrar em `ExecuteMigrationsAsync()`.

Scripts PowerShell de migration manual ficam em `backend/migrations/`.

## módulos do backend

```
src/
├── Controllers/
│   ├── RepositorioController.cs   → CRUD de repositórios + reordenação
│   ├── PastaController.cs         → CRUD de pastas + ocultar/restaurar
│   ├── CloneController.cs         → clone de repositório Git
│   ├── ComandoController.cs       → execução de comandos avulsos
│   ├── ConfiguracaoController.cs  → leitura/escrita da configuração global
│   ├── IDEController.cs           → CRUD de IDEs
│   ├── IISController.cs           → listar/iniciar/parar/reiniciar sites IIS
│   ├── SiteIISController.cs       → CRUD de sites IIS configurados + deploy
│   └── VersaoController.cs        → versão da aplicação
│
├── Services/
│   ├── RepositorioJsonService.cs  → CRUD + reordenação de repositorios.json
│   ├── PastaService.cs            → lógica de pastas (leitura de disco + JSON)
│   ├── PastaJsonService.cs        → persistência de pastas.json
│   ├── ConfiguracaoService.cs     → leitura/escrita de Configuracao.json
│   ├── CloneService.cs            → executa git clone via ShellExecute
│   ├── ComandoService.cs          → executa comandos PowerShell via ShellExecute
│   ├── IDEJsonService.cs          → CRUD de IDEs.json
│   ├── IISService.cs              → interação com appcmd.exe do IIS
│   ├── SiteIISJsonService.cs      → CRUD de SiteIIS.json
│   ├── DeployIISService.cs        → executa deploy (publish + xcopy) para IIS
│   ├── InformacaoService.cs       → informações do sistema
│   └── MigrationService.cs        → migrations de dados JSON
│
├── DTOs/                          → records de request/response
├── Enuns/                         → ETipoComando, ETipoMenu
└── Utils/
    ├── PathHelper.cs              → BancoPath (%APPDATA%\PMW\Banco)
    └── ShellExecute.cs            → execução de comandos PowerShell
```

## módulos do frontend

```
src/
├── views/
│   ├── PastasView.vue         → tela principal (rota /)
│   ├── RepositoriosView.vue   → gerenciar repositórios (rota /repositorios)
│   ├── ConfiguracaoView.vue   → configuração global (rota /configuracao)
│   ├── SitesIISView.vue       → sites IIS configurados (rota /sites-iis)
│   └── IDEsView.vue           → gerenciar IDEs (rota /ides)
│
├── stores/
│   ├── configuracao.ts        → useConfiguracaoStore (Options API style)
│   └── siteIIS.ts             → useSiteIISStore (Options API style)
│
├── services/
│   ├── BaseApiService.ts      → axios base
│   ├── RepositoriosService.ts
│   ├── PastasService.ts
│   ├── CloneService.ts
│   ├── ComandosService.ts
│   ├── ConfiguracaoService.ts
│   ├── IDEsService.ts
│   ├── IISService.ts
│   ├── SiteIISService.ts
│   └── VersaoService.ts
│
├── models/                    → classes de domínio com constructor + toDTO()
│   ├── RepositorioModel.ts
│   ├── ProjetoModel.ts
│   ├── PastaModel.ts
│   ├── PastaMenuModel.ts
│   ├── SiteIISModel.ts        → também exporta PastaDeployModel
│   ├── ConfiguracaoModel.ts
│   ├── IDEModel.ts
│   ├── MenuModel.ts
│   ├── ArquivoModel.ts
│   ├── PerfilMarcacaoModel.ts
│   └── PerfilMarcacaoProjetoModel.ts
│
├── components/
│   ├── clone/                 → componentes de clone de repositório
│   ├── comum/                 → BotaoPrimario, BotaoSecundario, BotaoTerciario, ModalPadrao, IconeComTooltip
│   ├── pastas/                → componentes da tela de pastas
│   ├── repositorios/          → componentes da tela de repositórios
│   └── sitesiis/              → componentes da tela de sites IIS
│
├── types/index.ts             → interfaces TypeScript (IRepositorio, IPasta, IConfiguracao...)
├── constants/                 → constantes do projeto
├── composables/               → composables reutilizáveis
├── plugins/vuetify.ts         → configuração do Vuetify
└── router/index.ts            → rotas da SPA
```

## rotas do frontend

```
/               → PastasView (tela principal)
/repositorios   → RepositoriosView
/configuracao   → ConfiguracaoView
/sites-iis      → SitesIISView
/ides           → IDEsView
```

Sem autenticação. Sem guard de rota.

## componentes globais registrados (main.ts)

```
BotaoPrimario, BotaoSecundario, BotaoTerciario   → botões padrão do projeto
ModalPadrao                                        → modal reutilizável
IconeComTooltip                                    → ícone com tooltip
```

Esses componentes são registrados globalmente — não precisam de import nas views.

## stores — padrão atual

As stores existentes usam **Options API style** (não Composition API).
Ao criar nova store: verificar o padrão das existentes antes de decidir o estilo.

## ambientes

dev: frontend http://localhost:5173 | backend http://localhost:5000 (ou porta do IIS)
prod: publicado no IIS local via `dotnet publish`, porta padrão `2025` (configurável em `appsettings.json`)

## build e publicação

```bash
# frontend
cd frontend
pnpm run build          # gera dist/
pnpm run copy:to:backend  # copia dist/ para backend/frontend/

# backend
cd backend
dotnet publish ProjectManagerWeb.csproj -c Release -o C:\inetpub\wwwroot\PMW

# tudo junto
cd frontend
pnpm run publish:all
```

Scripts de atualização: `Atualizar_PMW.ps1` (raiz) e `backend/atualizar_PWM_pwsh.ps1`.

## CI/CD — GitHub Actions

Pipeline: `.github/workflows/release.yml` — dispara em push para `main`.

Fluxo:
1. Build do frontend (`pnpm install --frozen-lockfile` + `pnpm run build`) em `windows-latest`
2. Copia `frontend/dist/` para `backend/frontend/` via `xcopy`
3. `dotnet publish` gera pasta `publish/`
4. Compacta em `PMW_vYYYY.MM.DD.HHmm.zip`
5. Cria GitHub Release com o zip como artefato e release notes automáticas

Cache ativo: pnpm (key: `pnpm-lock.yaml`) e NuGet (key: hash do `.csproj`) — economiza ~85s por run.

**Convenção importante:** o script `build` no `package.json` não inclui `pnpm install` — o install é responsabilidade do chamador (workflow em CI, desenvolvedor localmente). Nunca adicionar `pnpm i &&` de volta ao script `build`.

Atualização local: `Atualizar_PMW.ps1` (raiz) — baixa o último release do GitHub, para o processo, extrai e reinicia. **Preserva `appsettings.json` local** — nunca sobrescrever esse arquivo no deploy.

O `backend/atualizar_PWM_pwsh.ps1` é um script legado (faz `git pull` direto) — não usar, o fluxo correto é via release.

## convenções

- código em inglês, UI em pt-br
- sem comentários — nomes auto-documentados (exceto métodos complexos de IIS/shell)
- sem AutoMapper — sem EF — sem banco relacional
- sem lógica no controller — apenas orquestração
- camadas: Controller → Service → JsonService → arquivo JSON
- frontend: Component → Store → Service → API (sem pular camadas)
- models do frontend têm `constructor(obj: Partial<IModel>)` + `toDTO()` — seguir esse padrão

## onde buscar informação

| Preciso de | Onde ir | Carregamento |
|------------|---------|--------------|
| Clone de repositório | `flow-clone` | automático (Clone/**) |
| Pastas / comandos / menus | `flow-pastas` | automático (Pasta/**) |
| Repositórios / projetos / agregados | `flow-repositorios` | automático (Repositorio/**) |
| IDEs | `flow-ides` | automático (IDE/**) |
| Code style backend | `rule-code-style-backend` | automático (backend/**) |
| Code style frontend | `rule-code-style-frontend` | automático (frontend/**) |
| Criar/editar steering | `#skill-criar-steering` | manual |
| Git/commits | `rule-git` | sempre |

## arquivos-chave por feature

```
Repositórios:
  backend/src/Controllers/RepositorioController.cs
  backend/src/Services/RepositorioJsonService.cs
  frontend/src/views/RepositoriosView.vue
  frontend/src/services/RepositoriosService.ts
  frontend/src/models/RepositorioModel.ts

Pastas:
  backend/src/Controllers/PastaController.cs
  backend/src/Services/PastaService.cs
  backend/src/Services/PastaJsonService.cs
  frontend/src/views/PastasView.vue
  frontend/src/services/PastasService.ts
  frontend/src/models/PastaModel.ts

Clone:
  backend/src/Controllers/CloneController.cs
  backend/src/Services/CloneService.cs
  frontend/src/components/clone/
  frontend/src/services/CloneService.ts

IDEs:
  backend/src/Controllers/IDEController.cs
  backend/src/Services/IDEJsonService.cs
  frontend/src/views/IDEsView.vue
  frontend/src/services/IDEsService.ts
  frontend/src/models/IDEModel.ts

Sites IIS (deploy):
  backend/src/Controllers/SiteIISController.cs
  backend/src/Services/SiteIISJsonService.cs
  backend/src/Services/DeployIISService.cs
  frontend/src/views/SitesIISView.vue
  frontend/src/stores/siteIIS.ts
  frontend/src/services/SiteIISService.ts
  frontend/src/models/SiteIISModel.ts

IIS (controle start/stop):
  backend/src/Controllers/IISController.cs
  backend/src/Services/IISService.cs
  frontend/src/services/IISService.ts

Configuração:
  backend/src/Controllers/ConfiguracaoController.cs
  backend/src/Services/ConfiguracaoService.cs
  frontend/src/views/ConfiguracaoView.vue
  frontend/src/stores/configuracao.ts
  frontend/src/services/ConfiguracaoService.ts
  frontend/src/models/ConfiguracaoModel.ts

Migrations:
  backend/src/Services/MigrationService.cs
  backend/migrations/   → scripts PowerShell de migration manual
```
