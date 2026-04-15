---
inclusion: fileMatch
fileMatchPattern: "**/IDE*,**/ide*,**/IDEs*"
description: Fluxo de cadastro e uso de IDEs no PMW.
---

# flow-ides

Fluxo de gestão de IDEs no PMW.

## o que é uma IDE no PMW

Representa uma ferramenta de desenvolvimento configurável. Não é apenas VS Code —
qualquer ferramenta que possa ser aberta via linha de comando pode ser cadastrada.
IDEs são referenciadas por `Guid` em repositórios e projetos.

## estrutura

```
IDEDTO
├── Identificador: Guid
├── Nome: string                    → nome de exibição (ex: "VS Code", "Kiro", "Delphi")
├── ComandoParaExecutar: string     → comando shell (ex: "code .", "kiro .", "bds -pDelphi")
└── AceitaPerfilPersonalizado: bool → se true, suporta --profile "nome"
```

## IDEs padrão (criadas pela migration 001)

| Nome | Comando | Aceita Perfil |
|------|---------|---------------|
| VS Code | `code .` | sim |
| Kiro | `kiro .` | sim |
| Delphi | `bds -pDelphi -rBDSERP110203` | não |

## como a IDE é usada

**Abrir projeto específico** (`POST /api/comandos`):
- Projeto tem `Comandos.IDEIdentificador` → usa a IDE do projeto
- Se `AceitaPerfilPersonalizado = true` e `Projeto.PerfilVSCode` preenchido → adiciona `--profile "perfil"`
- Comando montado: `cd {diretorio}\{subdiretorio}; {ComandoParaExecutar} [--profile "perfil"] .; Exit;`

**Abrir pasta raiz** (`POST /api/comandos/abrir-pasta-ide`):
- Repositório tem `IDEIdentificador` → usa a IDE do repositório
- Se `AceitaPerfilPersonalizado = true` e `PerfilVSCode` preenchido → adiciona `--profile "perfil"`
- Comando montado: `cd {diretorio}; {ComandoParaExecutar} [--profile "perfil"] .; Exit;`

**Hierarquia de IDE** (projeto prevalece sobre repositório):
- Projeto com `IDEIdentificador` → usa a IDE do projeto
- Projeto sem `IDEIdentificador` → usa a IDE do repositório (via `IDEIdentificador` do repositório)

## regras de negócio

- Nome é único — `AddAsync` lança exceção se nome duplicado (case-insensitive)
- **Não pode excluir IDE em uso** — `DELETE` retorna `409 Conflict` se algum projeto referencia a IDE via `IDEIdentificador`
- A verificação de uso percorre todos os projetos de todos os repositórios

## frontend

rota: `/ides` → `IDEsView.vue`
service: `frontend/src/services/IDEsService.ts`
model: `frontend/src/models/IDEModel.ts`
interface: `IIDE` em `frontend/src/types/index.ts`

o service chama `toDTO()` antes de POST/PUT se disponível:
```ts
const dto = (ide as any).toDTO ? (ide as any).toDTO() : ide;
```

## backend

controller: `backend/src/Controllers/IDEController.cs`
service: `backend/src/Services/IDEJsonService.cs`

endpoints:
```
GET    /api/ides                    → lista todas
GET    /api/ides/{identificador}    → busca por id
POST   /api/ides                    → cadastra (rejeita nome duplicado)
PUT    /api/ides/{identificador}    → atualiza (preserva Identificador original)
DELETE /api/ides/{identificador}    → remove (409 se em uso por algum projeto)
```

validações no controller (antes de chamar o service):
- `Nome` obrigatório
- `ComandoParaExecutar` obrigatório

## arquivos envolvidos

```
frontend/src/views/IDEsView.vue
frontend/src/services/IDEsService.ts
frontend/src/models/IDEModel.ts
frontend/src/types/index.ts              → IIDE
backend/src/Controllers/IDEController.cs
backend/src/Services/IDEJsonService.cs
backend/src/DTOs/IDEDTO.cs
backend/src/Services/MigrationService.cs → Migration_001_AddIDEs (IDEs padrão)
```
