# Fluxo: IDEs

Ferramentas de desenvolvimento configuráveis. Qualquer ferramenta que possa ser aberta via linha de comando.

## Estrutura

```
IDEDTO
├── Identificador: Guid
├── Nome: string                    → nome de exibição (ex: "VS Code")
├── ComandoParaExecutar: string     → comando shell (ex: "code .")
└── AceitaPerfilPersonalizado: bool → suporta --profile "nome"
```

## IDEs padrão (criadas pela migration 001)

| Nome | Comando | Aceita Perfil |
|------|---------|---------------|
| VS Code | `code .` | sim |
| Kiro | `kiro .` | sim |
| Delphi | `bds -pDelphi -rBDSERP110203` | não |

## Como a IDE é usada

**Abrir projeto específico** (`POST /api/comandos`):
- Projeto tem `IDEIdentificador` → usa IDE do projeto
- Se `AceitaPerfilPersonalizado` e `Projeto.PerfilVSCode` preenchido → adiciona `--profile "perfil"`
- Comando: `cd {diretorio}\{subdir}; {Comando} [--profile "perfil"] .; Exit;`

**Abrir pasta raiz** (`POST /api/comandos/abrir-pasta-ide`):
- Repositório tem `IDEIdentificador` → usa IDE do repositório
- Se `AceitaPerfilPersonalizado` e `PerfilVSCode` preenchido → adiciona `--profile "perfil"`

**Hierarquia:** projeto prevalece sobre repositório.

## Regras de negócio

- Nome é único — `AddAsync` lança exceção se duplicado (case-insensitive)
- Não pode excluir IDE em uso — `DELETE` retorna `409 Conflict` se algum projeto referencia

## Frontend

Rota: `/ides` → `IDEsView.vue`
Service: `IDEsService.ts` (chama `toDTO()` antes de POST/PUT)
Model: `IDEModel.ts`
Interface: `IIDE` em `types/index.ts`

## Backend

Controller: `IDEController.cs`
Service: `IDEJsonService.cs`

Endpoints:
```
GET    /api/ides                    → lista todas
GET    /api/ides/{id}               → busca por id
POST   /api/ides                    → cadastra
PUT    /api/ides/{id}               → atualiza
DELETE /api/ides/{id}               → remove (409 se em uso)
```

## Arquivos envolvidos

```
frontend/src/views/IDEsView.vue
frontend/src/services/IDEsService.ts
frontend/src/models/IDEModel.ts
frontend/src/types/index.ts              → IIDE
backend/src/Controllers/IDEController.cs
backend/src/Services/IDEJsonService.cs
backend/src/DTOs/IDEDTO.cs
backend/src/Services/MigrationService.cs → Migration_001_AddIDEs
```
