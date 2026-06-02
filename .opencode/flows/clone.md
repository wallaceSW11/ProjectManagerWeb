# Fluxo: Clone de Repositório Git

Clona um repositório (e opcionalmente agregados) em um diretório local,
cria/faz checkout do branch desejado e registra a pasta resultante em `pastas.json`.

## Frontend

Componente: `frontend/src/components/clone/CloneGit.vue`
Model: `frontend/src/models/CloneModel.ts` → `IClone`
Service: `frontend/src/services/CloneService.ts` → `clonar(clone: IClone)`

Campos do formulário (`IClone`):
- `repositorio`: `IRepositorio` selecionado — obrigatório
- `diretorioRaiz`: caminho base (vem da ConfiguracaoStore)
- `codigo`: código do projeto (ex: `PMW-123`) — vira parte do nome da pasta
- `descricao`: descrição curta — vira parte do nome (espaços → `_`)
- `tipo`: `'nenhum' | 'feature' | 'bug' | 'hotfix'` — prefixo do branch criado
- `branch`: branch de origem para checkout
- `criarBranchRemoto`: se `true`, cria novo branch local a partir do `branch` de origem
- `baixarAgregados`: se `true`, clona também agregados
- `historicoCompleto`: se `false` (padrão), usa `--filter=blob:none --single-branch`
- `salvarNoStorage`: flag de UI — não enviado ao backend

Fluxo:
1. Usuário seleciona repositório, preenche código, descrição, branch e opções
2. Submit → `CloneService.clonar(clone)` → `POST /api/clones`
3. Backend retorna a `PastaCadastroRequestDTO` criada
4. Frontend atualiza a lista de pastas (recarrega `GET /api/pastas`)

## Backend

Controller: `backend/src/Controllers/CloneController.cs`
Services: `CloneService`, `PastaService`, `RepositorioJsonService`

Endpoint: `POST /api/clones`
Body: `CloneRequestDTO { DiretorioRaiz, Codigo, Descricao, Tipo, Branch, RepositorioId, CriarBranchRemoto, BaixarAgregados, BaixarSomenteAgregados, HistoricoCompleto }`

Lógica do `CloneService.Clonar()`:
1. Monta `diretorioCompleto = DiretorioRaiz + Codigo + "_" + Descricao.Replace(" ", "_")`
2. Cria o diretório físico se não existir
3. Busca o repositório principal por `RepositorioId`
4. Monta e executa o comando git via `ShellExecute.ExecutarComando()`
5. Faz checkout do branch de origem
6. Se `CriarBranchRemoto = true`: cria branch de trabalho
7. Se `BaixarAgregados = true`: repete para cada agregado

### Comandos git por cenário

| Histórico completo | Criar branch remoto | Branch base | Comando |
|-|-|-|-|
| true | qualquer | qualquer | `git clone {url}` |
| false | false | qualquer | `git clone --filter=blob:none --single-branch --branch {branch}` |
| false | true | `develop/dev/main/master` | `git clone --filter=blob:none --single-branch --branch {branch}` + `git checkout -b {codigo}` |
| false | true | outra | `git clone --filter=blob:none --single-branch --branch {branch}` + `git fetch origin {branch}` + checkout + `git checkout -b {codigo}` |

### Branches base vs trabalho

Branches base (`develop`, `dev`, `main`, `master`): o `git clone --single-branch --branch` já traz a branch localmente.
Branches de trabalho: clona a principal, depois `git fetch origin {branch}` e checkout a partir do FETCH_HEAD.

```csharp
private static bool EhBranchBase(string branch) =>
    branch is "develop" or "dev" or "main" or "master";
```

## Nome da pasta no disco

```
{DiretorioRaiz}{Codigo}_{Descricao com espaços substituídos por _}
```

Exemplo: `DiretorioRaiz = C:\Dev\`, `Codigo = PMW-123`, `Descricao = "minha feature"`
→ `C:\Dev\PMW-123_minha_feature`

## Problema conhecido: branch principal ausente

Ao clonar branch não-base sem criar branch remoto, o repositório local fica sem a branch principal (`main`, `develop`), impedindo `git merge`/`git rebase`.
Solução planejada: detectar branch principal no remote via `git ls-remote` e adicionar `git fetch origin {branchPrincipal}; git branch --track {branchPrincipal} origin/{branchPrincipal};`.

## Comportamento dos agregados

Agregados são clonados dentro do mesmo diretório do principal.
Cada agregado em `{diretorioCompleto}\{agregado.Nome}`.
Checkout do branch usa `2>$null` para suprimir erro se não existir no agregado.

## Arquivos envolvidos

```
frontend/src/components/clone/CloneGit.vue
frontend/src/models/CloneModel.ts
frontend/src/services/CloneService.ts
frontend/src/types/index.ts              → IClone
backend/src/Controllers/CloneController.cs
backend/src/Services/CloneService.cs
backend/src/DTOs/CloneRequestDTO.cs
backend/src/Services/PastaService.cs
backend/src/Services/RepositorioJsonService.cs
backend/src/Utils/ShellExecute.cs
```
