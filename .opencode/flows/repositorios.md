# Fluxo: Repositórios

Entidade central do PMW. Representa um repositório Git com projetos, menus, perfis de marcação e IDE.

## Estrutura do domínio

```
RepositorioRequestDTO
├── Identificador: Guid
├── Url: string                    → URL do Git (única — chave de deduplicação)
├── Nome: string                   → nome do diretório criado pelo git clone
├── Titulo: string                 → nome de exibição na UI
├── Cor: string?                   → cor do card (ex: "#1976d2")
├── BranchBase: string?            → branch principal (develop, dev, main)
├── IDEIdentificador: Guid?        → IDE padrão para abrir a pasta raiz
├── PerfilVSCode: string?          → perfil VSCode padrão
├── Indice: int                    → posição na ordenação
├── Subdiretorio: string?          → subpasta de trabalho para monorepos
├── CliComando: string?            → CLI para abrir (Kiro, etc.)
├── PerfilTerminal: string?        → perfil do terminal
├── AbrirWorkspace: bool           → abrir workspace ou pasta
├── Projetos: ProjetoDTO[]         → subprojetos com comandos
├── Agregados: Guid[]?             → outros repositórios clonados junto
├── Menus: MenuDTO[]?              → ações rápidas nas pastas
├── Perfis: PerfilMarcacaoDTO[]?   → perfis de marcação
├── CodigosTarefa: CodigoTarefaDTO[]? → códigos de tarefa (autopreenchimento do clone)
├── UrlBaseGestorTarefas: string?  → link do Jira, YouTrack, etc.
└── PastaCentralizadora: string?   → nome da pasta centralizadora (agrupa clones)
```

### CodigoTarefaDTO

```
CodigoTarefaDTO
├── Identificador: Guid
├── Iniciais: string               → "FATWEB", "TC" (usado no clone)
├── BranchPrincipal: string        → branch base do código
├── CriarBranchRemoto: bool        → cria branch remoto ao clonar
├── ClonarAgregados: bool          → clona agregados junto
├── BaixarHistoricoCompleto: bool  → sem --depth
├── HabilitarTipos: bool           → se true, habilita tipo feature/bugfix/hotfix
└── TiposHabilitados: string[]?    → ["feature", "bugfix", "hotfix"]
```

```
ProjetoDTO
├── Identificador: Guid
├── Nome: string
├── Subdiretorio: string?          → caminho relativo dentro do repositório
├── PerfilVSCode: string?
├── ArquivoCoverage: string?
├── Expandido: bool                → estado na UI (persistido)
└── Comandos: ComandoDTO { Instalar, Iniciar, Buildar, IDEIdentificador }
```

## Agregados

Outros repositórios clonados junto com o principal, no mesmo diretório.
Referenciados por `Guid` (não por URL).
Impacto: clone (baixa junto), pastas (projetos dos agregados aparecem na pasta), comandos (diretório calculado substituindo `Nome` do principal pelo do agregado).

## Menus

Ações rápidas executáveis em pastas associadas ao repositório:
- `APLICAR_ARQUIVO`: copia arquivos. Suporta `IgnorarGit` → `git update-index --assume-unchanged`
- `APLICAR_PASTA`: copia pastas via `Copy-Item -Recurse -Force`
- `COMANDO_AVULSO`: executa comandos PowerShell livres

## Perfis de marcação

Combinações de projetos + comandos para execução rápida.

## Frontend

Rota: `/repositorios` → `RepositoriosView.vue`
Service: `RepositoriosService.ts` (chama `toDTO()` antes de POST/PUT)
Model: `RepositorioModel.ts`

Componentes:
```
RepositorioCadastro.vue      → formulário de criação/edição
ProjetoCadastro.vue          → formulário de projeto
MenuCadastro.vue             → formulário de menu
PerfilMarcacaoCadastro.vue   → formulário de perfil
ListaRepositorios.vue        → listagem com ações
OrdenarRepositorios.vue      → drag-and-drop
SelectRepositorio.vue        → select reutilizável (usado no clone)
```

## Backend

Controller: `RepositorioController.cs`
Service: `RepositorioJsonService.cs`

Endpoints:
```
GET    /api/repositorios                              → lista todos por Indice
POST   /api/repositorios                              → cadastra (rejeita URL duplicada)
PUT    /api/repositorios/{id}                         → atualiza (preserva Identificador)
DELETE /api/repositorios/{id}                         → remove
PUT    /api/repositorios/indices                      → reordena
GET    /api/repositorios/codigos-tarefa/{iniciais}    → busca código de tarefa + repositório
POST   /api/repositorios/{id}/codigos-tarefa          → adiciona código de tarefa
PUT    /api/repositorios/{id}/codigos-tarefa/{codId}  → atualiza código de tarefa
DELETE /api/repositorios/{id}/codigos-tarefa/{codId}  → remove código de tarefa
```

`UpdateAsync` usa `with { Identificador = identificador }` — o Guid da URL prevalece.

## Aba Tarefas no cadastro

O `RepositoriosView.vue` possui abas no cadastro. A aba **Tarefas** (entre Geral e IDE/Terminal) contém:
- `UrlBaseGestorTarefas`: campo texto livre (max 200 chars) para link do gestor de tarefas
- CRUD de `CodigosTarefa`: adicionar/editar/remover códigos de tarefa localmente (salvos junto com o repositório)

Os códigos de tarefa são armazenados no `repositorio.codigosTarefa` e enviados ao backend no POST/PUT do repositório como parte do `RepositorioRequestDTO.CodigosTarefa`.

## Regra de negócio

URL é chave de unicidade. `AddAsync` lança exceção se URL já existe.
`Nome` deve coincidir com o que `git clone` cria no disco.

## Arquivos envolvidos

```
frontend/src/views/RepositoriosView.vue
frontend/src/components/repositorios/*.vue
frontend/src/components/repositorios/TarefasCadastro.vue
frontend/src/services/RepositoriosService.ts
frontend/src/models/RepositorioModel.ts
frontend/src/models/ProjetoModel.ts
frontend/src/models/MenuModel.ts
frontend/src/models/PerfilMarcacaoModel.ts
frontend/src/types/index.ts
backend/src/Controllers/RepositorioController.cs
backend/src/Services/RepositorioJsonService.cs
backend/src/DTOs/RepositorioRequestDTO.cs
backend/src/DTOs/RepositorioIndiceRequestDTO.cs
backend/src/DTOs/CodigoTarefaDTO.cs
```
