---
inclusion: fileMatch
fileMatchPattern: "**/SiteIIS*,**/siteIIS*,**/Deploy*,**/IIS*,**/iis*"
description: Fluxo de gestão de sites IIS configurados, deploy e controle start/stop.
---

# flow-sitesiis

Fluxo de gestão de sites IIS e deploy no PMW.

## conceito — dois controllers separados

O PMW tem **dois controllers** para IIS com responsabilidades distintas:

| Controller | Rota | Responsabilidade |
|------------|------|-----------------|
| `SiteIISController` | `/api/sites-iis` | CRUD de sites **configurados** + disparar deploy |
| `IISController` | `/api/iis` | Controle de sites **reais** do IIS (listar/iniciar/parar/reiniciar) |

**SiteIIS** = configuração de deploy salva no JSON (quais pastas publicar, quais pools parar).
**IIS** = interação direta com `appcmd.exe` para gerenciar sites reais do IIS local.

## estrutura do domínio — SiteIIS (configuração de deploy)

```
SiteIISRequestDTO
├── Identificador: Guid
├── Titulo: string              → nome de exibição na UI
├── Nome: string                → nome do site no IIS (usado no appcmd)
├── PastaRaiz: string           → diretório raiz do IIS (ex: C:\inetpub\wwwroot)
├── Pastas: PastaDeployDTO[]    → lista de pastas a serem publicadas
└── PoolsAplicacao: string[]    → pools a parar/iniciar durante deploy
```

```
PastaDeployDTO
├── Identificador: Guid
├── DiretorioTrabalho: string   → onde executar o comando de publish (ex: C:\git\MeuProjeto)
├── ComandoPublish: string      → comando de build (ex: "dotnet publish -c Release -o ./publish")
├── CaminhoOrigem: string       → pasta gerada pelo build (ex: C:\git\MeuProjeto\publish)
├── CaminhoDestino: string      → (legado, não usado no deploy atual)
└── NomePastaDestino: string    → nome da pasta dentro de PastaRaiz (ex: "MeuApp")
```

O caminho real de deploy é: `{PastaRaiz}\{NomePastaDestino}` (ex: `C:\inetpub\wwwroot\MeuApp`).

## deploy — fluxo completo

Endpoint: `POST /api/sites-iis/{identificador}/atualizar`
Service: `DeployIISService.AtualizarSiteAsync()`

O deploy gera um **script PowerShell dinâmico** e o executa como administrador (UAC):

### Fase 1: Build e Validação
Para cada pasta configurada:
- `Set-Location {DiretorioTrabalho}`
- `Invoke-Expression '{ComandoPublish}'`
- Se `$LASTEXITCODE -ne 0` → aborta

### Fase 2: Parar Serviços
- `appcmd stop site "{Nome}"`
- Para cada pool: `appcmd stop apppool "{pool}"`

### Fase 3: Backup e Cópia
Para cada pasta:
- Renomeia pasta atual: `{PastaRaiz}\{NomePastaDestino}` → `{NomePastaDestino}_{timestamp}`
- Copia nova versão: `Copy-Item '{CaminhoOrigem}' → '{PastaRaiz}\{NomePastaDestino}'`

### Fase 4: Iniciar Serviços
- `appcmd start site "{Nome}"`
- Para cada pool: `appcmd start apppool "{pool}"` + `appcmd recycle apppool "{pool}"`

### Execução
- Script salvo em `%APPDATA%\PMW\Banco\deploy-logs\deploy_{Nome}_{timestamp}.ps1`
- Executado via `ShellExecute.ExecutarComandoComoAdministrador()` — abre janela PS com UAC
- Retorna imediatamente ao frontend com `Sucesso = true` + log parcial
- Progresso real acompanhado na janela PowerShell aberta

## IIS — controle de sites reais

Endpoint base: `/api/iis/sites`
Service: `IISService`

### Listagem (`GET /api/iis/sites`)
Estratégia com fallback:
1. Método rápido: `appcmd.exe list site` com `RedirectStandardOutput` (3s timeout)
2. Fallback: executa via `cmd.exe /C appcmd > arquivo.txt` com `Verb = "runas"` (5s timeout)
3. Último recurso: `netstat` para detectar portas ativas

Cache de 5 segundos (`_cacheSites`) para evitar chamadas repetidas ao appcmd.

### Ações (`POST /api/iis/sites/{nomeSite}/iniciar|parar|reiniciar`)
- Executa `appcmd start/stop site` + `appcmd start/stop apppool` via `cmd.exe` com `Verb = "runas"`
- Timeout de 10s
- Invalida cache após ação
- Verifica status após execução

### Endpoint genérico (`POST /api/iis/sites/acao`)
Body: `AcaoSiteIISRequestDTO { NomeSite, Acao }` — aceita "iniciar", "parar", "reiniciar" (ou em inglês).

## regras de negócio

- Nome do site é único — `AddAsync` rejeita duplicata (case-insensitive)
- Deploy requer privilégio de administrador (UAC) — não funciona sem elevação
- Backup automático antes de copiar — pasta renomeada com timestamp, nunca deletada
- Pools são reciclados após iniciar — garante que o novo código seja carregado
- `GET /api/sites-iis` retorna `SiteIISDeployResponseDTO` (resumo com contagens) — detalhe completo só via `GET /{id}`

## frontend

rota: `/sites-iis` → `SitesIISView.vue`
store: `useSiteIISStore` (Options API)
service: `SiteIISService.ts` (CRUD + deploy) e `IISService.ts` (controle start/stop)
model: `SiteIISModel.ts` (exporta também `PastaDeployModel`)

componentes:
```
ListaSitesIIS.vue     → listagem com ações (editar, excluir, duplicar)
SiteIISCadastro.vue   → formulário principal (titulo, nome, pastaRaiz)
PastasCadastro.vue    → gerenciar pastas de deploy do site
PoolsCadastro.vue     → gerenciar pools de aplicação
```

Cadastro usa tabs: Principal | Pastas | Pools.
Suporta duplicação de site (novo ID, sufixo " - Cópia").

## backend

```
GET    /api/sites-iis                         → lista todos (resumo)
GET    /api/sites-iis/{id}                    → detalhe completo
POST   /api/sites-iis                         → cadastra (rejeita nome duplicado)
PUT    /api/sites-iis/{id}                    → atualiza
DELETE /api/sites-iis/{id}                    → remove
POST   /api/sites-iis/{id}/atualizar          → dispara deploy

GET    /api/iis/sites                         → lista sites reais do IIS
POST   /api/iis/sites/{nome}/iniciar          → inicia site
POST   /api/iis/sites/{nome}/parar            → para site
POST   /api/iis/sites/{nome}/reiniciar        → reinicia site
POST   /api/iis/sites/acao                    → ação genérica (body com NomeSite + Acao)
```

## arquivos envolvidos

```
frontend/src/views/SitesIISView.vue
frontend/src/components/sitesiis/ListaSitesIIS.vue
frontend/src/components/sitesiis/SiteIISCadastro.vue
frontend/src/components/sitesiis/PastasCadastro.vue
frontend/src/components/sitesiis/PoolsCadastro.vue
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
backend/src/DTOs/SiteIISDTO.cs                 → todos os DTOs de SiteIIS/IIS
backend/src/Utils/ShellExecute.cs              → ExecutarComandoComoAdministrador
```
