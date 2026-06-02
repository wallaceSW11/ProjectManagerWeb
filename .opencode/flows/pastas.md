# Fluxo: Pastas (Tela Principal)

Tela principal do PMW (`/`). Lista diretórios físicos dentro do `diretorioRaiz` configurado.

## Listagem

`GET /api/pastas` → `PastaService.ObterTodas()`

Lógica do backend:
1. Lê `diretorioRaiz` e `diretoriosOcultos` da `ConfiguracaoService`
2. Lista diretórios físicos, excluindo ocultos
3. Limpa do `pastas.json` entradas cujo diretório não existe mais no disco
4. Para cada diretório:
   - Sem entrada no JSON → retorna `PastaResponseDTO` vazia (só `Diretorio`)
   - Com entrada → busca repositório, monta `ProjetoDisponivelDTO` com comandos
5. Retorna ordenado por `Indice`

`ProjetoDisponivelDTO` inclui:
- `Comandos`: lista de `ETipoComando` (`INSTALAR`, `INICIAR`, `BUILDAR`, `ABRIR_NA_IDE`)
- `NomeIDE`: nome da IDE (resolvido via `IDEJsonService`)
- `IdentificadorRepositorioAgregado`: preenchido se projeto vem de agregado

## Cadastro de pasta

`POST /api/pastas` → `PastaService.Cadastrar()`

```csharp
public sealed record PastaCadastroRequestDTO(
    Guid Identificador, string Diretorio, string Codigo,
    string Descricao, string Tipo, string Branch, string Git,
    Guid RepositorioId, int? Indice = null
);
```

`PastaJsonService.AddAsync`: upsert — se já existe entrada com mesmo `Diretorio`, substitui.

## Executar comandos de projeto

`POST /api/comandos` → `ComandoService.ExecutarComando()`
Body: `PastaRequestDTO { Diretorio, RepositorioId, Projetos: ProjetoDisponivelDTO[] }`

Lógica por tipo:
- `INSTALAR` → `cd {dir}\{subdir}; {Comandos.Instalar}`
- `INICIAR` → verifica se `node_modules` existe antes de `npm i`; executa `Comandos.Iniciar`
- `BUILDAR` → `cd {dir}\{subdir}; {Comandos.Buildar}`
- `ABRIR_NA_IDE` → `cd {dir}\{subdir}; {ide.Comando} [--profile "perfil"] .; Exit;`

## Abrir pasta na IDE

`POST /api/comandos/abrir-pasta-ide`
Body: `AbrirPastaIDERequestDTO { Diretorio, IDEIdentificador, PerfilVSCode }`

## Executar menu

`POST /api/comandos/menu` → `ComandoService.ExecutarComandoMenu()`
Body: `MenuRequestDTO { Diretorio, RepositorioId, ComandoId }`

Tipos de menu:
- `APLICAR_ARQUIVO`: copia arquivos. Se `IgnorarGit = true`, executa `git update-index --assume-unchanged`
- `APLICAR_PASTA`: copia pastas via `Copy-Item -Recurse -Force`
- `COMANDO_AVULSO`: executa comandos PowerShell livres

## Reordenação

`PUT /api/pastas/indices` body: `List<PastaIndiceRequestDTO>`

## Expandir/recolher projeto

`PATCH /api/pastas/projetos/expandido`
Body: `AtualizarExpandidoRequestDTO { PastaId, ProjetoId, Expandido }`
Persiste em `repositorios.json` (campo `Projeto.Expandido`).

## Ocultar / restaurar pasta

Ocultar: `POST /api/pastas/ocultar` body: `{ diretorio }`
Restaurar: `POST /api/pastas/restaurar` body: `{ diretorio }`
Listar ocultas: `GET /api/pastas/ocultas`
Diretórios ocultos armazenados em `Configuracao.json` (`DiretoriosOcultos: string[]`).

## Componentes frontend

```
PastasView.vue          → tela principal
CardPasta.vue           → card de uma pasta
PastaCadastro.vue       → formulário de cadastro manual
PastasOcultas.vue       → gerenciamento de pastas ocultas
```

## Arquivos envolvidos

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
backend/src/DTOs/*.cs                    → PastaResponse, PastaCadastro, PastaRequest, etc.
backend/src/Enuns/ETipoComando.cs
backend/src/Utils/ShellExecute.cs
```
