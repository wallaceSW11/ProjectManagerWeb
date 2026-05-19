# ProjectManagerWeb (PMW)

**Ferramenta desktop-web para gerenciar repositórios Git, pastas de projetos, IDEs e deploy no IIS.**  
Roda localmente na máquina do desenvolvedor — sem autenticação, sem banco relacional.

---

## 📦 Stack

| Camada | Tecnologia |
|--------|-----------|
| Frontend | Vue 3 + TypeScript + Vuetify 3 + Pinia + Vue Router + Axios + Vite |
| Backend | .NET 9 + ASP.NET Core |
| Persistência | Arquivos JSON em `%APPDATA%\PMW\Banco\` |
| Package Manager | pnpm |
| Infra | IIS local via `dotnet publish` |

---

## 🚀 Funcionalidades

- **Pastas** — listagem automática do diretório raiz, com fixação no topo, reordenação drag-and-drop, ocultação e exclusão
- **Repositórios** — CRUD completo com projetos, menus de contexto, perfis de marcação e agregados
- **Execução de comandos** — instalar, iniciar, buildar múltiplos projetos com um clique
- **IDEs** — configuração de IDEs (VS Code, Kiro, Delphi) com abertura direta da pasta
- **CLI de IA** — abertura de terminais com CLI configurável (Kiro CLI, Claude Code) + comando complementar por repositório
- **Sites IIS** — CRUD de sites, controle start/stop/restart, deploy automatizado
- **Configuração global** — diretório raiz, perfis VSCode, CLIs disponíveis, diretórios ocultos

---

## 🛠️ Como rodar localmente (dev)

```bash
# Clone
git clone https://github.com/wallaceSW11/ProjectManagerWeb.git
cd ProjectManagerWeb

# Frontend (porta 5173)
cd frontend
pnpm install
pnpm run dev

# Backend (porta 2024)
cd ../backend
dotnet restore
dotnet run
```

Acesse: [http://localhost:5173](http://localhost:5173)

---

## 📦 Build e publicação

```bash
# Build completo + publicação no IIS
cd frontend
pnpm run publish:all
```

Ou manualmente:

```bash
# Frontend
cd frontend
pnpm run build
pnpm run copy:to:backend

# Backend
cd ../backend
dotnet publish ProjectManagerWeb.csproj -c Release -o C:\inetpub\wwwroot\PMW
```

---

## 🔍 Estrutura do projeto

```
ProjectManagerWeb/
├── frontend/                # Vue 3 SPA
│   ├── src/
│   │   ├── views/           # PastasView, RepositoriosView, ConfiguracaoView, SitesIISView, IDEsView
│   │   ├── components/      # Componentes organizados por domínio
│   │   ├── stores/          # Pinia (Options API)
│   │   ├── services/        # Camada de API (Axios)
│   │   ├── models/          # Classes com constructor + toDTO()
│   │   ├── types/           # Interfaces TypeScript
│   │   └── composables/     # Composables reutilizáveis
│   └── package.json
│
├── backend/                 # .NET 9 Web API
│   ├── src/
│   │   ├── Controllers/     # Endpoints REST
│   │   ├── Services/        # Lógica de negócio + JsonServices
│   │   ├── DTOs/            # Records de request/response
│   │   └── Utils/           # PathHelper, ShellExecute
│   └── ProjectManagerWeb.csproj
│
├── .github/workflows/       # CI/CD (release.yml)
├── Atualizar_PMW.ps1        # Script de atualização local
└── README.md
```

---

## 🔄 CI/CD

Pipeline em `.github/workflows/release.yml` — dispara em push para `main`:

1. Build frontend (`pnpm install --frozen-lockfile` + `pnpm run build`)
2. Copia `dist/` para `backend/frontend/`
3. `dotnet publish` → pasta `publish/`
4. Compacta em `PMW_vYYYY.MM.DD.HHmm.zip`
5. Cria GitHub Release com artefato

Atualização local: `Atualizar_PMW.ps1` baixa o último release, para o processo, extrai e reinicia.

---

## ⚡ Workflow de uso

1. Configure o **diretório raiz** (onde ficam as pastas de trabalho)
2. Cadastre **repositórios** com seus projetos e comandos
3. Faça **clone** de repositórios direto pela interface
4. Na tela principal, selecione uma pasta → marque os projetos → **Executar**
5. Múltiplos projetos sobem com um clique (instalar + iniciar + buildar)

---

## 📋 Versão atual — Features recentes

- ✅ Pastas fixadas (pin no topo com reordenação independente)
- ✅ Comando complementar da CLI (ex: `chat --agent "delphi-dev"`)
- ✅ Aba IDE/Terminal no cadastro de repositório
- ✅ Perfil do Windows Terminal configurável
- ✅ Abrir workspace (.code-workspace)
- ✅ Perfis de marcação (seleção rápida de comandos)
- ✅ Menus de contexto com execução múltipla

---

## 🎯 Versão 2 — Planejamento

> Layout mais bonito e moderno. Ideias e decisões a definir:

- [ ] Redesign da tela principal (cards, cores, tipografia)
- [ ] Tema claro/escuro refinado
- [ ] Dashboard com visão geral (projetos ativos, últimos comandos)
- [ ] Animações e transições
- [ ] Responsividade (mesmo sendo desktop, melhorar em diferentes resoluções)
- [ ] Ícones e identidade visual própria
- [ ] _Outras ideias..._

---

## 📖 Convenções

- Código em inglês, UI em pt-br
- Sem AutoMapper, sem EF, sem banco relacional
- Camadas: Controller → Service → JsonService → JSON
- Frontend: Component → Store → Service → API
- Models com `constructor(Partial<I>)` + `toDTO()`
- Commits: `tipo(escopo): descrição em pt-br`
