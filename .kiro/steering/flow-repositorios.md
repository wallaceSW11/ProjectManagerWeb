---
inclusion: fileMatch
fileMatchPattern: "**/Repositorio*,**/repositorio*,**/RepositoriosView*"
description: Fluxo de cadastro e gestão de repositórios Git no PMW.
---

# flow-repositorios

Fluxo completo de gestão de repositórios no PMW.

## o que é um repositório

Entidade central do PMW. Representa um repositório Git com seus projetos, menus de ação,
perfis de marcação e configurações de IDE. É a "template" que define o que pode ser feito
em qualquer pasta clonada a partir dele.

## estrutura do domínio

```
RepositorioRequestDTO
├── Identificador: Guid
├── Url: string                    → URL do repositório Git (única — chave de deduplicação)
├── Nome: string                   → nome do diretório criado pelo git clone
├── Titulo: string                 → nome de exibição na UI
├── Cor: string?                   → cor do card na UI (ex: "#1976d2")
├── IDEIdentificador: Guid?        → IDE padrão para abrir a pasta raiz
├── PerfilVSCode: string?          → perfil VSCode padrão do repositório
├── Indice: int                    → posição na ordenação
├── Projetos: ProjetoDTO[]         → subprojetos com comandos
├── Agregados: Guid[]?             → identificadores de outros repositórios clonados junto
├── Menus: MenuDTO[]?              → ações rápidas disponíveis nas pastas
└── Perfis: PerfilMarcacaoDTO[]?   → perfis de marcação de projetos
```

```
ProjetoDTO
├── Identificador: Guid
├── Nome: string
├── Subdiretorio: string?          → caminho relativo dentro do repositório
├── PerfilVSCode: string?          → perfil VSCode específico deste projeto
├── ArquivoCoverage: string?       → caminho do arquivo de cobertura de testes
├── Expandido: bool                → estado expandido/recolhido na UI (persistido)
└── Comandos: ComandoDTO
    ├── Instalar: string?          → ex: "npm i"
    ├── Iniciar: string?           → ex: "npm run dev"
    ├── Buildar: string?           → ex: "npm run build"
    └── IDEIdentificador: Guid?    → IDE para abrir este projeto específico
```

## agregados — conceito importante

Agregados são outros repositórios que devem ser clonados **junto** com o repositório principal,
no mesmo diretório de trabalho. Referenciados por `Guid` (não por URL).

Impacto em outras features:
- **Clone**: se `BaixarAgregados = true`, clona cada agregado no mesmo `diretorioCompleto`
- **Pastas/ObterTodas**: projetos dos agregados aparecem na pasta se o diretório `{pasta}\{agregado.Nome}` existir no disco
- **Comandos**: projetos agregados têm `IdentificadorRepositorioAgregado` preenchido — o diretório de execução é calculado substituindo `repositorio.Nome` por `repositorioAgregado.Nome`

## menus — tipos disponíveis

Menus são ações rápidas executáveis em qualquer pasta associada ao repositório.

```
MenuDTO
├── Identificador: Guid
├── Titulo: string
├── Tipo: 'APLICAR_ARQUIVO' | 'APLICAR_PASTA' | 'COMANDO_AVULSO'
├── Ativo: bool
├── Arquivos: ArquivosDTO[]   → { Arquivo, Destino, IgnorarGit }
├── Pastas: PastaDTO[]        → { Origem, Destino }
└── Comandos: string[]        → comandos PowerShell livres
```

- `APLICAR_ARQUIVO`: copia arquivos para o diretório da pasta. `IgnorarGit = true` executa `git update-index --assume-unchanged` após copiar.
- `APLICAR_PASTA`: copia pastas inteiras via `Copy-Item -Recurse -Force`
- `COMANDO_AVULSO`: executa comandos PowerShell livres

## perfis de marcação

Permitem salvar combinações de projetos + comandos selecionados para execução rápida.

```
PerfilMarcacaoDTO
├── Identificador: Guid
├── Nome: string
└── Projetos: PerfilMarcacaoProjetoDTO[]
    ├── IdentificadorProjeto: Guid
    └── Comandos: string[]   → lista de ETipoComando selecionados
```

## frontend

rota: `/repositorios` → `RepositoriosView.vue`
service: `frontend/src/services/RepositoriosService.ts`
model: `frontend/src/models/RepositorioModel.ts`

componentes:
```
RepositorioCadastro.vue      → formulário de criação/edição do repositório
ProjetoCadastro.vue          → formulário de projeto dentro do repositório
MenuCadastro.vue             → formulário de menu de ação
PerfilMarcacaoCadastro.vue   → formulário de perfil de marcação
ListaRepositorios.vue        → listagem com ações
OrdenarRepositorios.vue      → drag-and-drop de reordenação
SelectRepositorio.vue        → select reutilizável (usado no clone)
```

detalhe do service — `toDTO()` antes de enviar:
```ts
// RepositoriosService chama toDTO() se disponível antes de POST/PUT
const dto = (repositorio as any).toDTO ? (repositorio as any).toDTO() : repositorio;
```
Isso garante que o `RepositorioModel` (com métodos de instância) seja serializado corretamente.

## backend

controller: `backend/src/Controllers/RepositorioController.cs`
service: `backend/src/Services/RepositorioJsonService.cs`

endpoints:
```
GET    /api/repositorios                    → lista todos, ordenados por Indice
POST   /api/repositorios                    → cadastra (rejeita URL duplicada)
PUT    /api/repositorios/{identificador}    → atualiza (preserva Identificador original)
DELETE /api/repositorios/{identificador}    → remove
PUT    /api/repositorios/indices            → reordena (body: [{Identificador, Indice}])
```

comportamento do `UpdateAsync`: usa `with { Identificador = identificador }` — garante que o `Guid` da URL prevalece sobre o do body.

comportamento do `RenomearPerfilVSCodeAsync`: ao renomear um perfil VSCode na configuração, atualiza automaticamente todos os repositórios e projetos que referenciam o nome antigo.

## regra de negócio — URL como chave

A URL é a chave de unicidade do repositório. `AddAsync` lança exceção se URL já existe.
O `Nome` (nome do diretório no disco) é derivado do repositório Git — deve coincidir com o que `git clone` cria.

## arquivos envolvidos

```
frontend/src/views/RepositoriosView.vue
frontend/src/components/repositorios/RepositorioCadastro.vue
frontend/src/components/repositorios/ProjetoCadastro.vue
frontend/src/components/repositorios/MenuCadastro.vue
frontend/src/components/repositorios/PerfilMarcacaoCadastro.vue
frontend/src/components/repositorios/ListaRepositorios.vue
frontend/src/components/repositorios/OrdenarRepositorios.vue
frontend/src/components/repositorios/SelectRepositorio.vue
frontend/src/services/RepositoriosService.ts
frontend/src/models/RepositorioModel.ts
frontend/src/models/ProjetoModel.ts
frontend/src/models/MenuModel.ts
frontend/src/models/PerfilMarcacaoModel.ts
frontend/src/types/index.ts
backend/src/Controllers/RepositorioController.cs
backend/src/Services/RepositorioJsonService.cs
backend/src/DTOs/RepositorioRequestDTO.cs   → também define ProjetoDTO, ComandoDTO, MenuDTO, PerfilMarcacaoDTO
backend/src/DTOs/RepositorioIndiceRequestDTO.cs
```
