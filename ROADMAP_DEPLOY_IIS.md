# Roadmap - Sistema de Deploy Automático para Sites IIS

## 📋 Visão Geral
Implementar funcionalidade completa para automatizar o processo de atualização de sites no IIS do Windows, incluindo:
- Build do projeto (npm run publish)
- Cópia de arquivos para wwwroot
- Gerenciamento de sites e pools do IIS
- Backup automático com timestamp

---

## 🎯 ETAPA 1: Backend - CRUD de Sites IIS

### 1.1 Modelos e DTOs ✅
- [x] **Criar/Atualizar DTOs**
  - [x] `SiteIISRequestDTO.cs` - Dados principais do site
  - [x] `SiteIISResponseDTO.cs` - Resposta com dados do site
  - [x] `PastaDeployDTO.cs` - Representa uma pasta a ser copiada (origem/destino)
  - [x] `AtualizarSiteResponseDTO.cs` - Response do processo de atualização

**Estrutura do Modelo:**
```csharp
SiteIISRequestDTO:
  - Identificador: Guid
  - Nome: string (ex: "BimerUp_m")
  - PastaRaiz: string (ex: "C:\inetpub\wwwroot\BimerUP_m")
  - Pastas: List<PastaDeployDTO>
  - PoolsAplicacao: List<string> (ex: ["BimerUp_m", "BimerUp_m_WS"])

PastaDeployDTO:
  - Identificador: Guid
  - DiretorioTrabalho: string (ex: "C:\git\pmw\frontend") - Onde executar o build
  - ComandoPublish: string (ex: "npm run publish") - Comando para build/publish
  - CaminhoOrigem: string (ex: "C:\git\pmw\frontend\dist") - Pasta gerada após build
  - CaminhoDestino: string (ex: "C:\inetpub\wwwroot\BimerUP_m\bimerup") - Destino no IIS
  - NomePastaDestino: string (ex: "bimerup") - Nome da pasta no destino para backup
```

### 1.2 Service Layer ✅
- [x] **Criar `SiteIISJsonService.cs`**
  - [x] Seguir padrão de `RepositorioJsonService`
  - [x] Métodos CRUD: GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync
  - [x] Persistência em arquivo JSON (ex: `sites-iis.json`)
  - [x] Validações de dados

### 1.3 Controller ✅
- [x] **Criar `SiteIISController.cs`**
  - [x] `GET /api/sites-iis` - Listar todos os sites
  - [x] `GET /api/sites-iis/{id}` - Buscar site por ID
  - [x] `POST /api/sites-iis` - Cadastrar novo site
  - [x] `PUT /api/sites-iis/{id}` - Atualizar site
  - [x] `DELETE /api/sites-iis/{id}` - Excluir site
  - [x] `POST /api/sites-iis/{id}/atualizar` - Disparar processo de atualização

### 1.4 Service de Deploy ✅
- [x] **Criar `DeployIISService.cs`**
  - [x] Método `AtualizarSiteAsync(Guid siteId)`
  - [x] Gerar script PowerShell dinâmico baseado nos dados do site
  - [x] Integrar com execução PowerShell
  - [x] Logging de cada etapa do processo
  - [x] Tratamento de erros com validação de build

**Lógica do Deploy:**
1. Buscar dados do site no JSON
2. **Para cada pasta:**
   - Executar comando de publish no diretório de trabalho
   - **VALIDAR**: Se build falhou, PARAR processo e retornar erro
   - Se sucesso, continuar
3. **Apenas se todos os builds foram bem-sucedidos:**
   - Parar site no IIS
   - Parar todos os pools de aplicação
   - Para cada pasta: Renomear destino atual (adicionar timestamp como backup)
   - Para cada pasta: Copiar origem para destino
   - Iniciar site
   - Iniciar pools
   - Reciclar pools
4. Retornar log completo do processo

---

## 🎨 ETAPA 2: Frontend - View de Gerenciamento de Sites ✅

### 2.1 Models e Types ✅
- [x] **Criar interfaces TypeScript**
  - [x] `frontend/src/models/SiteIISModel.ts` (com interfaces exportadas)
  - [x] `PastaDeployModel` inclusa no mesmo arquivo

### 2.2 Service API ✅
- [x] **Criar `SiteIISService.ts`**
  - [x] Métodos para comunicação com API backend
  - [x] CRUD completo
  - [x] Método `dispararDeploy(id)`

### 2.3 Store (Pinia) ✅
- [x] **Criar `useSiteIISStore.ts`**
  - [x] State para lista de sites
  - [x] Actions para CRUD
  - [x] Getters necessários
  - [x] Action para disparar deploy

### 2.4 View Principal ✅
- [x] **Criar `SitesIISView.vue`**
  - [x] Seguir padrão de `RepositoriosView.vue`
  - [x] Tabela com lista de sites cadastrados
  - [x] Colunas: Nome, Pasta Raiz, Qtd Pastas, Qtd Pools, Ações
  - [x] Botões: Adicionar, Editar, Excluir
  - [x] Loading states e feedback

### 2.5 Modal de Cadastro/Edição ✅
- [x] **Criar componente `SiteIISModal.vue`**
  - [x] **Stepper com 3 etapas**
    - [x] Etapa 1: Nome do Site e Pasta Raiz
    - [x] Etapa 2: Gerenciamento de Pastas (inline edit + tabela)
    - [x] Etapa 3: Pools de Aplicação (lista editável)
  - [x] Validações em todos os campos obrigatórios
  - [x] Navegação entre etapas com validação

### 2.6 Navegação ✅
- [x] **Atualizar Router**
  - [x] Adicionar rota `/sites-iis`

---

## 🚀 ETAPA 3: Menu Dropdown de Deploy ✅

### 3.1 Componente de Menu ✅
- [x] **Atualizar barra superior/AppBar**
  - [x] Adicionar menu dropdown "Deploy" com ícone de foguete
  - [x] Botão "Gerenciar Sites" para acessar tela de CRUD
  - [x] Buscar lista de sites automaticamente no mount
  - [x] Menu item para cada site: "Atualizar [Nome do Site]"

### 3.2 Ação de Deploy ✅
- [x] **Implementar trigger de deploy**
  - [x] Ao clicar: Chamar API `POST /api/sites-iis/{id}/atualizar` imediatamente
  - [x] Abrir modal mostrando log do processo
  - [x] Feedback de sucesso/erro ao final

### 3.3 Modal de Log do Deploy ✅
- [x] **Criar `DeployLogModal.vue`**
  - [x] Exibir log completo do processo linha por linha
  - [x] Mostrar cada etapa com cores diferentes (info, success, error)
  - [x] Destaque para erros com ícones
  - [x] Mensagem final: "Concluído com sucesso" ou "Deploy falhou"
  - [x] Botão para fechar após conclusão
  - [x] Estilo monospace para log (formato terminal)

---

## 🔧 ETAPA 4: Script PowerShell e Integração

### 4.1 Template de Script
- [ ] **Criar gerador de script PowerShell**
  - [ ] Método em `DeployIISService` para gerar script dinâmico
  - [ ] Baseado no exemplo .bat fornecido
  - [ ] Parametrizado com dados do site
  - [ ] Incluir tratamento de erros
  - [ ] Logging detalhado

**Exemplo de estrutura do script:**
```powershell
# Obter timestamp para backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# FASE 1: BUILD E VALIDAÇÃO (para cada pasta)
Write-Host "=== Executando builds ===" -ForegroundColor Cyan
cd "C:\git\pmw\frontend"
npm run publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Build falhou! Processo abortado." -ForegroundColor Red
    exit 1
}
Write-Host "Build concluído com sucesso!" -ForegroundColor Green

# FASE 2: PARAR SERVIÇOS (só se builds OK)
Write-Host "=== Parando site e pools ===" -ForegroundColor Cyan
& "$env:SystemRoot\system32\inetsrv\appcmd" stop site "NomeSite"
foreach ($pool in $pools) {
    & "$env:SystemRoot\system32\inetsrv\appcmd" stop apppool $pool
}

# FASE 3: BACKUP E CÓPIA
Write-Host "=== Fazendo backup e copiando arquivos ===" -ForegroundColor Cyan
Rename-Item "C:\inetpub\wwwroot\BimerUP_m\bimerup" "bimerup_$timestamp"
Copy-Item "C:\git\pmw\frontend\dist" "C:\inetpub\wwwroot\BimerUP_m\bimerup" -Recurse

# FASE 4: INICIAR SERVIÇOS
Write-Host "=== Iniciando site e pools ===" -ForegroundColor Cyan
& "$env:SystemRoot\system32\inetsrv\appcmd" start site "NomeSite"
foreach ($pool in $pools) {
    & "$env:SystemRoot\system32\inetsrv\appcmd" start apppool $pool
    & "$env:SystemRoot\system32\inetsrv\appcmd" recycle apppool $pool
}

Write-Host "=== Deploy concluído com sucesso! ===" -ForegroundColor Green
```

### 4.2 Integração com ComandoService
- [ ] **Verificar integração existente**
  - [ ] Usar `ComandoService.ExecutarComando` ou similar
  - [ ] Garantir que suporta PowerShell
  - [ ] Capturar output em tempo real
  - [ ] Tratar timeout

### 4.3 Validações
- [ ] **Implementar validações básicas**
  - [ ] Validar se TODOS os builds foram bem-sucedidos antes de mexer no IIS
  - [ ] Capturar código de saída de cada comando
  - [ ] Parar processo imediatamente se qualquer build falhar
  - [ ] Retornar log detalhado de todas as etapas

---

## 📝 ETAPA 5: Testes e Refinamentos

### 5.1 Testes Backend
- [ ] Testar CRUD de sites
- [ ] Testar geração de script
- [ ] Testar execução em ambiente de desenvolvimento
- [ ] Validar tratamento de erros

### 5.2 Testes Frontend
- [ ] Testar navegação e UI
- [ ] Testar fluxo de cadastro completo
- [ ] Testar triggers de deploy
- [ ] Validar responsividade

### 5.3 Documentação
- [ ] Adicionar comentários no código
- [ ] Criar guia de uso
- [ ] Documentar estrutura do JSON
- [ ] Exemplos de configuração

---

## 📊 Ordem Sugerida de Implementação

1. ✅ **Backend Base** (Etapa 1.1 a 1.3) - CRUD funcional
2. ✅ **Service de Deploy** (Etapa 1.4 e 4.1) - Lógica de deploy
3. ✅ **Frontend Base** (Etapa 2.1 a 2.4) - UI para cadastro
4. ✅ **Modal Completo** (Etapa 2.5) - Cadastro de pastas
5. ✅ **Menu Dropdown** (Etapa 3) - Trigger de deploy
6. ⏳ **Testes Integrados** (Etapa 5) - Validação end-to-end

---

## 🎯 Status da Implementação

### ✅ BACKEND (100%)
1. ✅ DTOs completos (SiteIISRequestDTO, ResponseDTO, PastaDeployDTO, AtualizarSiteResponseDTO)
2. ✅ SiteIISJsonService (CRUD completo)
3. ✅ SiteIISController (todos endpoints)
4. ✅ DeployIISService (geração script PowerShell + execução)
5. ✅ Registrado no Program.cs
6. ✅ Arquivo SitesIIS.http criado

### ✅ FRONTEND (100%)
1. ✅ Models TypeScript (SiteIISModel, PastaDeployModel)
2. ✅ SiteIISService (comunicação API)
3. ✅ Store Pinia (useSiteIISStore)
4. ✅ SitesIISView (tabela com CRUD)
5. ✅ SiteIISModal (stepper 3 etapas)
6. ✅ DeployLogModal (log colorido)
7. ✅ Menu dropdown "Deploy" no AppBar
8. ✅ Rota `/sites-iis` configurada

### ⏳ PRÓXIMOS PASSOS
1. Testar fluxo completo (cadastro → deploy)
2. Validar script PowerShell gerado
3. Ajustes finais de UX/UI se necessário

---

## 📌 Observações Importantes

- **Permissões**: Backend roda via CMD com privilégios do usuário atual (suficiente)
- **Backup**: Sistema sempre faz backup antes de substituir (timestamp sem limite)
- **Validação de Build**: Se qualquer build falhar, todo processo é abortado
- **Multi-site**: Sistema suporta múltiplos sites e micro frontends
- **Log em Tempo Real**: Exibir todo o processo no frontend (build, cópia, parar/iniciar)
- **Público**: Sistema usado por DEVs com conhecimento técnico

---

## ✅ Questões Resolvidas

1. ✅ Comando de build: **Campo texto livre** - pode variar (npm, dotnet, etc)
2. ✅ Confirmação: **NÃO** - ao clicar já executa
3. ✅ Histórico de deploys: **NÃO** - sem logs por enquanto
4. ✅ Limite de backups: **SEM LIMITE** - usuário gerencia manualmente
5. ✅ Rollback: **NÃO** - apenas validar build antes de prosseguir
6. ✅ Notificações: **Modal com log em tempo real** no frontend

---

**Legenda:**
- ✅ Concluído
- ⏳ Em andamento
- [ ] Pendente
- ❌ Bloqueado
- ⚠️ Precisa revisão
