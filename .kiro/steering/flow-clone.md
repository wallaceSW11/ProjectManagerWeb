---
inclusion: fileMatch
fileMatchPattern: "**/Clone*,**/clone*"
description: Fluxo de clonagem de repositório Git com criação automática de pasta.
---

# flow-clone

Fluxo de clonagem de repositório Git no PMW.

## o que faz

Clona um repositório (e opcionalmente seus agregados) em um diretório local,
cria/faz checkout do branch desejado e registra a pasta resultante no `pastas.json`.
Tudo em uma única operação — o usuário sai com o projeto pronto no disco e já listado na tela principal.

## frontend

componente: `frontend/src/components/clone/CloneGit.vue`
model: `frontend/src/models/CloneModel.ts` → `IClone`
service: `frontend/src/services/CloneService.ts` → `clonar(clone: IClone)`

campos do formulário (`IClone`):
- `repositorio`: `IRepositorio` selecionado — obrigatório
- `diretorioRaiz`: caminho base onde a pasta será criada (vem da `ConfiguracaoStore`)
- `codigo`: código do projeto (ex: `PMW-123`) — vira parte do nome da pasta
- `descricao`: descrição curta — vira parte do nome da pasta (espaços → `_`)
- `tipo`: `'nenhum' | 'feature' | 'bug' | 'hotfix'` — prefixo do branch criado
- `branch`: branch de origem para checkout
- `criarBranchRemoto`: se `true`, cria novo branch local a partir do `branch` de origem
- `baixarAgregados`: se `true`, clona também os repositórios agregados do repositório principal
- `historicoCompleto`: se `false` (padrão), usa `--depth 1` para clone raso
- `salvarNoStorage`: flag de UI — não enviado ao backend

fluxo:
1. usuário seleciona repositório, preenche código, descrição, branch e opções
2. submit → `CloneService.clonar(clone)` → `POST /api/clones`
3. backend retorna a `PastaCadastroRequestDTO` criada
4. frontend atualiza a lista de pastas (recarrega `GET /api/pastas`)

## backend

controller: `backend/src/Controllers/CloneController.cs`
services: `CloneService`, `PastaService`, `RepositorioJsonService`

endpoint: `POST /api/clones`
body: `CloneRequestDTO`

```csharp
public sealed record CloneRequestDTO(
    string DiretorioRaiz,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    Guid RepositorioId,
    bool CriarBranchRemoto,
    bool BaixarAgregados,
    bool BaixarSomenteAgregados,
    bool HistoricoCompleto = false
);
```

lógica do `CloneService.Clonar()`:
1. monta `diretorioCompleto = DiretorioRaiz + Codigo + "_" + Descricao.Replace(" ", "_")`
2. cria o diretório físico se não existir
3. busca o repositório principal por `RepositorioId`
4. monta e executa o comando git via `ShellExecute.ExecutarComando()`:
   - `HistoricoCompleto = true`                                                          → `git clone`
   - `HistoricoCompleto = false` + `CriarBranchRemoto = false`                          → `git clone --depth 1 --no-single-branch`
   - `HistoricoCompleto = false` + `CriarBranchRemoto = true` + branch base             → `git clone --depth 1`
   - `HistoricoCompleto = false` + `CriarBranchRemoto = true` + branch **não-base**     → `git clone --depth 1 --no-single-branch`
5. faz checkout do branch de origem
6. se `CriarBranchRemoto = true`:
   - `Tipo == "nenhum"` → `git checkout -b {Codigo}`
   - `Tipo != "nenhum"` → `git checkout -b {Tipo}/{Codigo}`
7. se `BaixarAgregados = true`: repete os passos 4–6 para cada `Guid` em `repositorio.Agregados`

## branches base vs. branches de trabalho (criarBranchRemoto)

Quando `CriarBranchRemoto = true`, o comportamento do clone depende do tipo de branch de origem:

**Branches base** (`develop`, `dev`, `main`, `master`):
- `git clone --depth 1` é suficiente — a branch padrão do remote já é uma dessas.
- O `git checkout <branch>` subsequente funciona porque a branch já está presente localmente.

**Branches de trabalho** (qualquer outro nome, ex: `adequacao-cnpj`):
- `git clone --depth 1` baixa **apenas a branch padrão do remote** — o `git checkout adequacao-cnpj` falha silenciosamente (suprimido por `2>$null`) e a nova branch é criada em cima da branch padrão, não da branch desejada.
- Solução: usar `git clone --depth 1 --no-single-branch` para baixar todas as refs remotas, permitindo o checkout correto.

O método `EhBranchBase(string branch)` encapsula essa verificação (case-insensitive):
```csharp
private static bool EhBranchBase(string branch) =>
    branch is "develop" or "dev" or "main" or "master";
```

Essa regra se aplica tanto ao repositório principal quanto aos agregados.

lógica do `CloneController` após o clone:
1. chama `CriarPasta()` — monta `PastaCadastroRequestDTO` e chama `PastaService.Cadastrar()`
2. retorna a pasta criada como `Ok(pastaCriada)`

## nome da pasta no disco

```
{DiretorioRaiz}{Codigo}_{Descricao com espaços substituídos por _}
```

Exemplo: `DiretorioRaiz = C:\Dev\`, `Codigo = PMW-123`, `Descricao = "minha feature"` → `C:\Dev\PMW-123_minha_feature`

## problema: branch principal ausente ao clonar branch não-base diretamente

Quando `CriarBranchRemoto = false` e `Branch` é uma branch não-base (ex: `feature/TC-123`), o comando montado é:

```bash
git clone --depth 1 --branch feature/TC-123 <url>
```

O `--branch` no `git clone` define o HEAD do clone como aquela branch. O repositório local fica **sem a branch principal** (`main`, `develop`, etc.), o que impede:
- `git pull` (sem upstream da branch principal configurado)
- `git merge develop` / `git rebase develop`

**Solução planejada:** após o clone de branch não-base sem `CriarBranchRemoto`, detectar automaticamente qual branch principal existe no remote (tentando `main` → `master` → `develop` → `dev` em ordem via `git ls-remote`) e adicionar ao script:

```bash
git fetch origin <branchPrincipal>; git branch --track <branchPrincipal> origin/<branchPrincipal>;
```

Isso garante que a branch principal fique disponível localmente para merge/rebase sem precisar de campo extra no repositório.

O mesmo comportamento se aplica aos **agregados** clonados com branch não-base.

## comportamento dos agregados

Agregados são clonados **dentro do mesmo diretório** que o repositório principal.
Cada agregado fica em `{diretorioCompleto}\{agregado.Nome}`.
O checkout do branch nos agregados usa `2>$null` para suprimir erro caso o branch não exista no agregado.

## arquivos envolvidos

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
