# ProjectManagerWeb (PMW)

Gerenciador de repositórios Git, pastas de projetos, IDEs e deploy IIS.

## Stack

- **Frontend:** Vue 3 + TypeScript + Vuetify 3 + Pinia + Vue Router + Axios + Vite + pnpm
- **Backend:** .NET 9 + ASP.NET Core (sem EF, sem banco relacional)
- **Persistência:** Arquivos JSON em `%APPDATA%\PMW\Banco\`
- **Infra:** IIS local via `dotnet publish`

## Estrutura

```
/
├── frontend/     Vue 3 SPA
│   └── src/
│       ├── views/
│       ├── stores/
│       ├── services/
│       ├── models/
│       ├── components/
│       ├── types/
│       └── composables/
├── backend/      .NET 9 Web API
│   └── src/
│       ├── Controllers/
│       ├── Services/
│       ├── DTOs/
│       └── Utils/
└── .opencode/    Configuração do OpenCode
```

## Comandos

```bash
# Frontend
cd frontend
pnpm install              # instalar dependências
pnpm run dev              # dev server (localhost:5173)
pnpm run build            # build produção
pnpm run lint             # ESLint

# Backend
cd backend
dotnet build              # compilar
dotnet run                # dev server (localhost:5000)
dotnet publish            # publicar para IIS

# Tudo junto (raiz)
.\Atualizar_PMW.ps1      # atualizar do último release
```

## Git

Branches: `main`, `develop`, `feature/*`, `fix/*`, `chore/*`
Commits: `tipo(escopo): descrição em pt-br`
Tipos: feat, fix, chore, refactor, style, docs, test
PRs sempre para `develop`. Commits atômicos. Sem secrets no commit.
