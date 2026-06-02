# Fluxo: Sites IIS e Deploy

## Conceito — dois controllers separados

| Controller | Rota | Responsabilidade |
|------------|------|-----------------|
| `SiteIISController` | `/api/sites-iis` | CRUD de sites configurados + deploy |
| `IISController` | `/api/iis` | Controle de sites reais do IIS (start/stop/restart) |

**SiteIIS** = configuração de deploy salva em JSON (pastas, pools).
**IIS** = interação direta com `appcmd.exe` para gerenciar sites reais.

## Estrutura — SiteIIS

```
SiteIISRequestDTO
├── Identificador: Guid
├── Titulo: string              → nome de exibição
├── Nome: string                → nome do site no IIS (usado no appcmd)
├── PastaRaiz: string           → diretório raiz do IIS
├── Pastas: PastaDeployDTO[]    → pastas a publicar
└── PoolsAplicacao: string[]    → pools a parar/iniciar
```

```
PastaDeployDTO
├── Identificador: Guid
├── DiretorioTrabalho: string   → onde executar o publish
├── ComandoPublish: string      → comando de build
├── CaminhoOrigem: string       → pasta gerada pelo build
├── CaminhoDestino: string      → (legado)
└── NomePastaDestino: string    → nome dentro de PastaRaiz
```

Deploy real: `{PastaRaiz}\{NomePastaDestino}`

## Deploy — fluxo completo

`POST /api/sites-iis/{id}/atualizar` → `DeployIISService.AtualizarSiteAsync()`

### Fase 1: Build
Para cada pasta: `Set-Location {dir}` + `Invoke-Expression '{ComandoPublish}'`. Se `$LASTEXITCODE -ne 0`, aborta.

### Fase 2: Parar serviços
`appcmd stop site "{Nome}"` + `appcmd stop apppool "{pool}"` para cada pool.

### Fase 3: Backup e cópia
Para cada pasta: renomeia atual com timestamp, copia nova versão via `Copy-Item`.

### Fase 4: Iniciar serviços
`appcmd start site "{Nome}"` + `appcmd start apppool "{pool}"` + `appcmd recycle apppool "{pool}"`.

Script salvo em `%APPDATA%\PMW\Banco\deploy-logs\deploy_{Nome}_{timestamp}.ps1`.
Executado como administrador (UAC). Retorna imediatamente com log parcial.

## IIS — controle de sites reais

Endpoint base: `/api/iis/sites`
Service: `IISService`

### Listagem (`GET /api/iis/sites`)
3 estratégias com fallback:
1. `appcmd.exe list site` com `RedirectStandardOutput` (3s timeout)
2. Fallback: `cmd.exe /C appcmd > arquivo.txt` com `Verb = "runas"` (5s timeout)
3. Último recurso: `netstat` para detectar portas ativas

Cache de 5 segundos (`_cacheSites`).

### Ações
`POST /api/iis/sites/{nome}/iniciar|parar|reiniciar` — via `appcmd` com `Verb = "runas"`, timeout 10s.

### Endpoint genérico
`POST /api/iis/sites/acao` — body: `AcaoSiteIISRequestDTO { NomeSite, Acao }`.

## Regras de negócio

- Nome do site único — `AddAsync` rejeita duplicata (case-insensitive)
- Deploy requer UAC — não funciona sem elevação
- Backup automático antes de copiar — pasta renomeada com timestamp, nunca deletada
- Pools reciclados após iniciar — garante novo código carregado
- `GET /api/sites-iis` retorna resumo — detalhe só via `GET /{id}`

## Frontend

Rota: `/sites-iis` → `SitesIISView.vue`
Store: `useSiteIISStore` (Options API)
Services: `SiteIISService.ts` (CRUD + deploy) + `IISService.ts` (start/stop)
Model: `SiteIISModel.ts` (exporta `PastaDeployModel`)

Componentes:
```
ListaSitesIIS.vue       → listagem com ações
SiteIISCadastro.vue     → formulário (Principal | Pastas | Pools em tabs)
PastasCadastro.vue      → gerenciar pastas de deploy
PoolsCadastro.vue       → gerenciar pools
```

## Backend

```
GET    /api/sites-iis                         → lista (resumo)
GET    /api/sites-iis/{id}                    → detalhe
POST   /api/sites-iis                         → cadastra
PUT    /api/sites-iis/{id}                    → atualiza
DELETE /api/sites-iis/{id}                    → remove
POST   /api/sites-iis/{id}/atualizar          → deploy

GET    /api/iis/sites                         → lista sites reais
POST   /api/iis/sites/{nome}/iniciar          → inicia
POST   /api/iis/sites/{nome}/parar            → para
POST   /api/iis/sites/{nome}/reiniciar        → reinicia
POST   /api/iis/sites/acao                    → ação genérica
```

## Arquivos envolvidos

```
frontend/src/views/SitesIISView.vue
frontend/src/components/sitesiis/*.vue
frontend/src/stores/siteIIS.ts
frontend/src/services/SiteIISService.ts
frontend/src/services/IISService.ts
frontend/src/models/SiteIISModel.ts
frontend/src/types/index.ts                    → ISite
backend/src/Controllers/SiteIISController.cs
backend/src/Controllers/IISController.cs
backend/src/Services/SiteIISJsonService.cs
backend/src/Services/DeployIISService.cs
backend/src/Services/IISService.cs
backend/src/DTOs/SiteIISDTO.cs
backend/src/Utils/ShellExecute.cs
```
