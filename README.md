# ProjectManagerWeb (PMW)

**Ferramenta desktop-web para gerenciar repositórios Git, pastas de projetos, IDEs e deploy.**  
Roda localmente na máquina do desenvolvedor — sem autenticação, sem banco relacional.

---

## 📦 Stack

| Camada | Tecnologia |
|--------|-----------|
| Frontend | Vue 3 + TypeScript + Vuetify 3 + Pinia + Vue Router + Axios + Vite |
| Backend | .NET 9 + ASP.NET Core |
| Persistência | Arquivos JSON em `%APPDATA%\PMW\Banco\` |
| Package Manager | pnpm |
| Infra (Linux) | systemd user service + scripts em `/opt/pmw-tools/` |
| Infra (Windows) | Scripts PowerShell em `C:\inetpub\wwwroot\PMW-Tools` |

---

## 🚀 Funcionalidades

- **Pastas** — listagem automática do diretório raiz, com abas por pasta centralizadora, fixação no topo, reordenação drag-and-drop, ocultação e exclusão
- **Repositórios** — CRUD completo com projetos, menus de contexto, perfis de marcação, códigos de tarefa e agregados
- **Execução de comandos** — instalar, iniciar, buildar múltiplos projetos com um clique
- **IDEs** — configuração de IDEs (VS Code, Kiro, Delphi) com abertura direta da pasta
- **CLI de IA** — abertura de terminais com CLI configurável (Kiro CLI, Claude Code) + comando complementar por repositório
- **Sites IIS** — CRUD de sites, controle start/stop/restart, deploy automatizado
- **Configuração global** — diretório raiz, pastas centralizadoras, perfis VSCode, CLIs disponíveis, diretórios ocultos
- **Clone inteligente** — autopreenchimento via código de tarefa, suporte a pasta centralizadora, leitura de clipboard

---

## 🐧 Instalação no Linux

### Instalação do zero (automática)

```bash
curl -sL https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.sh | bash
```

Ou via wget:

```bash
wget -qO- https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.sh | bash
```

O script instala as dependências (curl, unzip, git), baixa o último release (self-contained — não precisa de .NET runtime), extrai em `/opt/pmw/` e configura o systemd service.

### Comandos disponíveis após a instalação

```bash
pmw start     # Inicia o serviço
pmw stop      # Para o serviço
pmw restart   # Reinicia o serviço
pmw status    # Status do serviço
pmw logs      # Logs em tempo real
pmw update    # Atualiza para o último release (com backup automático)
```

Acesse: [http://localhost:2025](http://localhost:2025)

> **Nota:** O PMW para Linux é publicado como **self-contained** — não precisa instalar o .NET runtime separadamente. Tudo já vem no pacote.

### Estrutura de diretórios no Linux

```
/opt/pmw/              → Aplicação (backend + frontend)
/opt/pmw-tools/        → Scripts de infra (pmw.sh, pmw.service) — nunca sobrescritos
/opt/pmw-backup-*/     → Backups automáticos com data/hora
/usr/local/bin/pmw     → Link simbólico → /opt/pmw-tools/pmw.sh
```

---

## 🪟 Instalação no Windows

**Pré-requisito:** [.NET 9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (o pacote Windows é framework-dependent — o runtime precisa estar instalado)

### Instalação do zero (automática)

Abra o **PowerShell** e execute:

```powershell
powershell -ExecutionPolicy Bypass -Command "iwr -Uri 'https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.ps1' -OutFile '$env:TEMP\bootstrap.ps1'; & '$env:TEMP\bootstrap.ps1'"
```

O script baixa o último release, extrai em `C:\inetpub\wwwroot\PMW`, copia os scripts de gerenciamento para `C:\inetpub\wwwroot\PMW-Tools` e adiciona ao PATH do usuário.

> Para instalar em outro diretório:
> ```powershell
> # Após baixar o bootstrap.ps1:
> .\$env:TEMP\bootstrap.ps1 -Pasta "C:\inetpub\wwwroot\PMW"
> ```

### Instalação manual

1. Acesse [GitHub Releases](https://github.com/wallaceSW11/ProjectManagerWeb/releases/latest)
2. Baixe o arquivo `PMW_Windows_*.zip`
3. Extraia para `C:\inetpub\wwwroot\PMW`
4. Execute a configuração:

```powershell
# Configura a infraestrutura (copia scripts para C:\inetpub\wwwroot\PMW-Tools, adiciona ao PATH):
C:\inetpub\wwwroot\PMW\infra\pmw.ps1 install

# Reabra o terminal e use o comando:
pmw start
```

### Comandos disponíveis após a instalação

```powershell
pmw start     # Inicia o PMW em segundo plano
pmw stop      # Para o PMW
pmw restart   # Reinicia
pmw status    # Mostra se está rodando
pmw update    # Atualiza para o último release (com backup automático)
```

Acesse: [http://localhost:2025](http://localhost:2025)

> **Nota:** O PMW para Windows é publicado como **framework-dependent** — precisa do .NET Runtime 9 instalado.  
> Para iniciar sem janela de console (ideal para inicialização do Windows):
> ```powershell
> wscript.exe "C:\inetpub\wwwroot\PMW-Tools\pmw-start.vbs"
> ```
> Coloque esse comando em `shell:startup` para iniciar automaticamente.

### Estrutura de diretórios no Windows

```
C:\inetpub\wwwroot\PMW          → Aplicação (backend + frontend)
C:\inetpub\wwwroot\PMW-Tools    → Scripts de infra — nunca sobrescritos
│   ├── pmw.ps1                 →   gerenciamento (start, stop, update...)
│   ├── pmw-start.vbs           →   inicia sem janela de terminal
│   └── pmw-start.bat           →   atalho para o .vbs
C:\PMW-backup-*                 → Backups automáticos com data/hora
```

---

## 🛠️ Desenvolvimento local

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) + [pnpm](https://pnpm.io/installation)
- IIS (Internet Information Services) — apenas para testar deploy

### Setup

```bash
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

## 📦 Build (CI/CD)

O build completo é feito via **GitHub Actions** (`.github/workflows/release.yml`):

1. **Frontend:** `pnpm install --frozen-lockfile` + `pnpm run build`
2. **Backend:**
   - **Windows:** `dotnet publish -c Release -r win-x64 --self-contained false`
   - **Linux:** `dotnet publish -c Release -r linux-x64 --self-contained true`
3. **Artefatos:** `PMW_Windows_*.zip` e `PMW_Linux_*.zip`

Para build manual durante o desenvolvimento:

```bash
# Frontend
cd frontend
pnpm run build

# Backend (compilar apenas, sem publicar)
cd ../backend
dotnet build
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
├── infra/                   # Scripts de deploy e gerenciamento
│   ├── pmw.sh              #   Linux: gerenciamento (start, stop, update...)
│   ├── pmw.service          #   Linux: systemd user service
│   ├── pmw.ps1             #   Windows: gerenciamento (start, stop, update...)
│   ├── pmw-start.vbs       #   Windows: inicialização sem console
│   └── pmw-start.bat       #   Windows: atalho para o .vbs
│
├── bootstrap.sh             # Instalação do zero no Linux
├── bootstrap.ps1            # Instalação do zero no Windows
├── .github/workflows/       # CI/CD (release.yml)
├── Atualizar_PMW.ps1        # Script de atualização Windows (legado)
└── README.md
```

---

## 🔄 CI/CD

Pipeline em `.github/workflows/release.yml` — dispara em push para `main`:

1. Build frontend (`pnpm install --frozen-lockfile` + `pnpm run build`)
2. Copia `dist/` para `backend/frontend/`
3. `dotnet publish`:
   - **Windows:** `-r win-x64 --self-contained false` (precisa do runtime)
   - **Linux:** `-r linux-x64 --self-contained true` (runtime incluso)
4. Copia `infra/` para ambos os pacotes
5. Gera dois artefatos: `PMW_Windows_*.zip` e `PMW_Linux_*.zip`
6. Cria GitHub Release com ambos os artefatos

### Instalação / Atualização local

| | Primeira instalação | Atualização |
|---|---|---|
| **Linux** | `curl ... \| bash` (via `bootstrap.sh`) | `pmw update` |
| **Windows** | `bootstrap.ps1` (one-liner) | `pmw update` |

> Ambos fazem **backup automático com data/hora** antes de atualizar.

---

## ⚡ Workflow de uso

1. Configure o **diretório raiz** (onde ficam as pastas de trabalho)
2. Cadastre **repositórios** com seus projetos e comandos
3. Faça **clone** de repositórios direto pela interface
4. Na tela principal, selecione uma pasta → marque os projetos → **Executar**
5. Múltiplos projetos sobem com um clique (instalar + iniciar + buildar)

---

## 📋 Versão atual — Features recentes

### 🏗️ Infraestrutura
- ✅ **Self-contained Linux** — não precisa de .NET runtime
- ✅ **Script Windows** (`pmw.ps1`) — gerenciamento completo com detecção automática
- ✅ **Inicialização sem console** — `.vbs` invisível, ideal para startup do Windows
- ✅ **Backup automático** — antes de atualizar, salva com data/hora
- ✅ **Separação infra/aplicação** — scripts de gerenciamento em `/opt/pmw-tools` ou `C:\inetpub\wwwroot\PMW-Tools`

### 🚀 Funcionalidades
- ✅ **Pastas centralizadoras** — agrupa clones por contexto (ex: Forizi, Pessoal)
- ✅ **Abas na listagem** — filtra pastas por aba (Raiz + pastas centralizadoras)
- ✅ **Código de tarefa** — vincula iniciais (ex: `FATWEB`) a repositório com flags
- ✅ **Clone inteligente** — autopreenchimento via código, clipboard, pasta centralizadora
- ✅ **Branch base** — campo no repositório, usado como fallback no clone
- ✅ **URL do gestor de tarefas** — link do Jira/YouTrack por repositório
- ✅ **Pastas fixadas** — pin no topo com reordenação independente
- ✅ **Comando complementar da CLI** (ex: `chat --agent "delphi-dev"`)
- ✅ **Aba IDE/Terminal** no cadastro de repositório
- ✅ **Perfil do Windows Terminal** configurável
- ✅ **Abrir workspace** (`.code-workspace`)
- ✅ **Perfis de marcação** — seleção rápida de comandos
- ✅ **Menus de contexto** com execução múltipla

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

### 🎨 Padrão de scroll em telas com abas

Telas que usam `v-tabs` + `v-tabs-window` devem ter o scroll **dentro do conteúdo da aba**, não na página inteira.

**Regra:** Cada `v-tabs-window-item` deve envolver seu conteúdo em uma `<div class="conteudo-aba">`:

```html
<v-tabs-window>
  <v-tabs-window-item>
    <div class="conteudo-aba">
      <!-- conteúdo da aba aqui -->
    </div>
  </v-tabs-window-item>
</v-tabs-window>
```

**CSS obrigatório:**

```css
.conteudo-aba {
  height: calc(100dvh - 320px);  /* 320px = header + tabs + toolbar */
  overflow: auto;
}

/* Para o primeiro nível de abas (sem tabs internas): */
.altura-limitada {
  height: calc(100dvh - 220px);  /* 220px = header + toolbar */
  overflow: auto;
}
```

Ajuste o `calc()` conforme a altura ocupada por headers, toolbars e tabs acima do conteúdo. Se houver abas aninhadas, use `conteudo-aba` nas abas internas e `altura-limitada` no container externo.
