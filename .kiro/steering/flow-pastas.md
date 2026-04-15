---
inclusion: fileMatch
fileMatchPattern: "**/Pasta*,**/pasta*,**/PastasView*"
description: Fluxo das ações da tela de pastas — listagem, comandos, menus, ocultação e reordenação.
---

# flow-pastas

Fluxo completo da tela principal de pastas (`PastasView`).

## o que é a tela de pastas

Tela principal do PMW (`/`). Lista todos os diretórios físicos dentro do `diretorioRaiz` configurado.
Cada pasta pode estar associada a um repositório cadastrado — se estiver, exibe projetos e menus disponíveis.
Pastas sem associação aparecem como "não cadastradas" (sem código, sem projetos).

## listagem — como funciona

`GET /api/pastas` → `PastaService.ObterTodas()`

lógica do backend:
1. lê `diretorioRaiz` e `diretoriosOcultos` da `ConfiguracaoService`
2. lista diretórios físicos em `diretorioRaiz`, excluindo os ocultos
3. limpa do `pastas.json` entradas cujo diretório não existe mais no disco (`LimparPastasInexistentes`)
4. para cada diretório no disco:
   - sem entrada no `pastas.json` → retorna `PastaResponseDTO` vazia (só `Diretorio` preenchido)
   - com entrada → busca o repositório associado, monta lista de `ProjetoDisponivelDTO` com comandos disponíveis
5. retorna ordenado por `Indice`

`ProjetoDisponivelDTO` inclui:
- `Comandos`: lista de `ETipoComando` (`INSTALAR`, `INICIAR`, `BUILDAR`, `ABRIR_NA_IDE`)
- `NomeIDE`: nome da IDE configurada no projeto (resolvido via `IDEJsonService`)
- `IdentificadorRepositorioAgregado`: preenchido quando o projeto vem de um repositório agregado

## cadastro de pasta

`POST /api/pastas` → `PastaService.Cadastrar()`

Usado automaticamente após clone. Pode ser chamado manualmente via `PastaCadastro.vue`.

```csharp
public sealed record PastaCadastroRequestDTO(
    Guid Identificador,
    string Diretorio,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    string Git,
    Guid RepositorioId,
    int? Indice = null
);
```

comportamento do `PastaJsonService.AddAsync`: se já existe entrada com o mesmo `Diretorio`, **substitui** (delete + add) — não lança erro de duplicata.

## executar comandos de projeto

`POST /api/comandos` → `ComandoService.ExecutarComando()`

body: `PastaRequestDTO { Diretorio, RepositorioId, Projetos: ProjetoDisponivelDTO[] }`

O frontend envia apenas os projetos com comandos selecionados pelo usuário.
O backend monta e executa os comandos via `ShellExecute.ExecutarComando()` em janelas PowerShell separadas.

lógica por tipo de comando (`ETipoComando`):
- `INSTALAR` → `cd {diretorio}\{subdiretorio}; {Comandos.Instalar}`
- `INICIAR` → verifica se `node_modules` existe antes de rodar `npm i` novamente; executa `Comandos.Iniciar`
- `BUILDAR` → `cd {diretorio}\{subdiretorio}; {Comandos.Buildar}`
- `ABRIR_NA_IDE` → `cd {diretorio}\{subdiretorio}; {ide.ComandoParaExecutar} [--profile "perfil"] .; Exit;`

projetos agregados: diretório calculado substituindo `repositorio.Nome` por `repositorioAgregado.Nome` no caminho.

## abrir pasta na IDE (sem projeto específico)

`POST /api/comandos/abrir-pasta-ide`

```csharp
public sealed record AbrirPastaIDERequestDTO(
    string Diretorio,
    Guid IDEIdentificador,
    string? PerfilVSCode
);
```

Abre o diretório raiz da pasta na IDE configurada. Se a IDE `AceitaPerfilPersonalizado` e `PerfilVSCode` está preenchido, adiciona `--profile "perfil"` ao comando.

## executar menu

`POST /api/comandos/menu` → `ComandoService.ExecutarComandoMenu()`

```csharp
public sealed record MenuRequestDTO(
    string Diretorio,
    Guid RepositorioId,
    Guid ComandoId
);
```

Menus são ações configuradas no repositório (`IMenu`). Tipos:
- `APLICAR_ARQUIVO`: copia arquivos para o diretório da pasta. Se `IgnorarGit = true`, executa `git update-index --assume-unchanged` após copiar.
- `APLICAR_PASTA`: copia pastas inteiras via `Copy-Item -Recurse -Force`
- `COMANDO_AVULSO`: executa comandos PowerShell livres

## reordenação de pastas

`PUT /api/pastas/indices`

body: `List<PastaIndiceRequestDTO> [{ Identificador, Indice }]`

Atualiza o campo `Indice` de cada pasta no `pastas.json`. A listagem sempre retorna ordenada por `Indice`.

## expandir/recolher projeto

`PATCH /api/pastas/projetos/expandido`

```csharp
public sealed record AtualizarExpandidoRequestDTO(
    Guid PastaId,
    Guid ProjetoId,
    bool Expandido
);
```

Persiste o estado expandido/recolhido do projeto no `repositorios.json` (campo `Projeto.Expandido`).
Busca primeiro no repositório principal; se não encontrar, percorre os agregados.

## ocultar / restaurar pasta

Ocultar: `POST /api/pastas/ocultar` → body: `{ diretorio: string }`
Restaurar: `POST /api/pastas/restaurar` → body: `{ diretorio: string }`
Listar ocultas: `GET /api/pastas/ocultas`

Diretórios ocultos são armazenados em `Configuracao.json` (`DiretoriosOcultos: string[]`).
Pastas ocultas não aparecem na listagem — filtradas antes de qualquer processamento.

## componentes frontend

```
PastasView.vue          → tela principal, carrega e exibe todas as pastas
CardPasta.vue           → card de uma pasta individual com projetos e ações
PastaCadastro.vue       → formulário de cadastro manual de pasta
PastasOcultas.vue       → gerenciamento de pastas ocultas
```

## arquivos envolvidos

```
frontend/src/views/PastasView.vue
frontend/src/components/pastas/CardPasta.vue
frontend/src/components/pastas/PastaCadastro.vue
frontend/src/components/pastas/PastasOcultas.vue
frontend/src/services/PastasService.ts
frontend/src/services/ComandosService.ts
frontend/src/models/PastaModel.ts
frontend/src/types/index.ts              → IPasta, IProjeto, IMenu, IClone
backend/src/Controllers/PastaController.cs
backend/src/Controllers/ComandoController.cs
backend/src/Services/PastaService.cs
backend/src/Services/PastaJsonService.cs
backend/src/Services/ComandoService.cs
backend/src/Services/ConfiguracaoService.cs
backend/src/Services/RepositorioJsonService.cs
backend/src/Services/IDEJsonService.cs
backend/src/DTOs/PastaResponseDTO.cs
backend/src/DTOs/PastaCadastroRequestDTO.cs
backend/src/DTOs/PastaRequestDTO.cs
backend/src/DTOs/AtualizarExpandidoRequestDTO.cs
backend/src/DTOs/PastaIndiceRequestDTO.cs
backend/src/DTOs/DiretorioRequestDTO.cs
backend/src/Enuns/ETipoComando.cs
backend/src/Utils/ShellExecute.cs
```
