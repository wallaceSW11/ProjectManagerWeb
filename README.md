# ProjectManagerWeb (PMW)

[![CI](https://github.com/wallaceSW11/ProjectManagerWeb/actions/workflows/ci.yml/badge.svg)](https://github.com/wallaceSW11/ProjectManagerWeb/actions/workflows/ci.yml)

> Gerencie seu ambiente de desenvolvimento do clone ao deploy com um clique.  
> Uma interface web local que centraliza repositórios, projetos, IDEs e IIS.

---

## Sumário

- [O que é o PMW](#-o-que-é-o-pmw)
- [Para quem é](#-para-quem-é)
- [Principais recursos](#-principais-recursos)
- [Stack](#-stack)
- [Instalação rápida](#-instalação-rápida)
  - [Linux](#linux)
  - [Windows](#windows)
- [Como usar no dia a dia](#-como-usar-no-dia-a-dia)
- [Estrutura do projeto](#-estrutura-do-projeto)
- [Desenvolvimento local](#-desenvolvimento-local)
- [CI/CD e build](#-cicd-e-build)
- [Licença](#-licença)

---

## 🎯 O que é o PMW

O **ProjectManagerWeb** é uma ferramenta desktop-web que unifica as tarefas mais repetitivas do dia a dia de um desenvolvedor:

- **Clonar** repositórios GitHub/GitLab com um clique, autopreenchimento e pastas organizadas
- **Gerenciar** dezenas de projetos simultaneamente — instalar dependências, iniciar em dev, buildar
- **Abrir** projetos na IDE correta (VS Code, Kiro, Delphi) direto da interface
- **Deployar** no IIS local sem abrir o Gerenciador do IIS

Tudo roda **localmente** na sua máquina. Sem nuvem, sem autenticação, sem banco relacional.  
Os dados são salvos em arquivos JSON em `%APPDATA%\PMW\Banco\`.

---

## 👨‍💻 Para quem é

- **Desenvolvedores fullstack** que trabalham com múltiplos repositórios e projetos simultaneamente
- **Equipes pequenas** que querem um hub local de projetos sem depender de ferramentas cloud
- **Devs que usam IIS** e precisam de deploy rápido sem abrir console ou Gerenciador do IIS
- **Quem cansa** de abrir 10 terminais manualmente toda manhã para subir o ambiente

---

## ✨ Principais recursos

### 📁 Pastas e projetos
- Listagem automática do diretório raiz de trabalho
- Abas por **pasta centralizadora** (agrupa projetos por contexto: cliente, pessoal, etc.)
- Fixação no topo, reordenação drag-and-drop, ocultação
- Execução de comandos em **múltiplos projetos** ao mesmo tempo

### 📦 Repositórios Git
- CRUD completo com projetos, menus de contexto e perfis de marcação
- **Clone inteligente**: autopreenchimento via código de tarefa + leitura de clipboard
- Suporte a **branch base**, **URL do gestor de tarefas** (Jira/YouTrack) e comandos complementares por repositório
- Códigos de tarefa com flags de clone (branch remoto, agregados, histórico)

### 🚀 Execução de comandos
- Instalar, iniciar e buildar projetos em lote com um clique
- Menus de contexto com **execução múltipla** de comandos
- CLI de IA configurável (Kiro CLI, Claude Code) com comando complementar por repositório

### 🧰 IDEs
- Cadastro de IDEs (VS Code, Kiro, Delphi, qualquer outra)
- Abertura direta do projeto na IDE a partir da interface
- Perfil do Windows Terminal configurável

### 🌐 IIS — Deploy
- CRUD de sites IIS
- Start/Stop/Restart dos sites e pools de aplicação
- **Deploy automatizado** com um clique

### ⚙️ Configuração global
- Diretório raiz de trabalho
- Pastas centralizadoras, perfis de terminal, CLIs disponíveis
- Diretórios ocultos na listagem

### 🖥️ Multiplataforma
- **Linux**: serviço systemd, auto-start, atualização com backup
- **Windows**: script PowerShell, inicialização sem console (ideal para startup)
- **Self-contained**: não precisa instalar .NET runtime

---

## 📋 Stack

| Camada | Tecnologia |
|--------|-----------|
| Frontend | Vue 3 + TypeScript + Vuetify 3 + Pinia + Vue Router + Axios + Vite |
| Backend | .NET 9 + ASP.NET Core |
| Persistência | Arquivos JSON em `%APPDATA%\PMW\Banco\` |
| Package Manager | pnpm |
| Infra (Linux) | systemd user service + scripts em `/opt/pmw-tools/` |
| Infra (Windows) | Scripts PowerShell + VBS em `C:\inetpub\wwwroot\PMW-Tools` |

---

## 🚀 Instalação rápida

### Linux

```bash
curl -sL https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.sh | bash
```

O script instala dependências, baixa o último release e configura o serviço systemd.

**Comandos:**

```bash
pmw start     # Inicia o serviço
pmw stop      # Para o serviço
pmw restart   # Reinicia
pmw status    # Status
pmw logs      # Logs em tempo real
pmw update    # Atualiza com backup automático
```

Acesse: [http://localhost:2025](http://localhost:2025)

### Windows

Abra o **PowerShell** como administrador e execute:

```powershell
powershell -ExecutionPolicy Bypass -Command "iwr -Uri 'https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.ps1' -OutFile '$env:TEMP\bootstrap.ps1'; & '$env:TEMP\bootstrap.ps1'"
```

**Comandos:**

```powershell
pmw start     # Inicia o PMW em segundo plano
pmw stop      # Para o PMW
pmw restart   # Reinicia
pmw status    # Mostra se está rodando
pmw update    # Atualiza com backup automático
```

Acesse: [http://localhost:2025](http://localhost:2025)

> O PMW é **self-contained** — não precisa instalar .NET runtime. Tudo já vem no pacote.

---

## 💡 Como usar no dia a dia

```
1. Configura o diretório raiz de trabalho
2. Cadastra seus repositórios com os comandos dos projetos
3. Na tela principal, navega pelas pastas
4. Marca os projetos que quer iniciar
5. Clica em "Executar" — todos sobem em paralelo
6. Precisa deployar? Vai em Sites IIS → Deploy → Pronto
```

Exemplo real: você tem 5 microsserviços em repositórios diferentes.  
Com o PMW você clona todos, instala dependências, inicia em dev e abre na IDE em **menos de 30 segundos**.

---

## 📁 Estrutura do projeto

```
ProjectManagerWeb/
├── frontend/                # Vue 3 SPA
│   ├── src/
│   │   ├── views/           # Telas (Pastas, Repositórios, Configuração, Sites, IDEs)
│   │   ├── components/      # Componentes por domínio
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
│   ├── pmw.sh               # Linux: gerenciamento
│   ├── pmw.service           # Linux: systemd user service
│   ├── pmw.ps1              # Windows: gerenciamento
│   ├── pmw-start.vbs        # Windows: inicialização sem console
│   └── pmw-start.bat        # Windows: atalho para o .vbs
│
├── bootstrap.sh             # Instalação do zero no Linux
├── bootstrap.ps1            # Instalação do zero no Windows
├── .github/workflows/       # CI/CD — GitHub Actions
└── README.md
```

---

## 🛠️ Desenvolvimento local

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) + [pnpm](https://pnpm.io/installation)
- IIS — apenas para testar deploy

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

## 🔄 CI/CD e build

O build é feito via **GitHub Actions** em push para `main`:

1. **Frontend:** `pnpm install --frozen-lockfile` + `pnpm run build`
2. **Backend:**
   - **Windows:** `dotnet publish -c Release -r win-x64 --self-contained true`
   - **Linux:** `dotnet publish -c Release -r linux-x64 --self-contained true`
3. **Artefatos:** `PMW_Windows_*.zip` e `PMW_Linux_*.zip` publicados no GitHub Releases

Build manual:

```bash
# Frontend
cd frontend && pnpm run build

# Backend
cd ../backend && dotnet build
```

### Atualização

| | Primeira instalação | Atualização |
|---|---|---|
| **Linux** | `curl ... \| bash` | `pmw update` |
| **Windows** | `bootstrap.ps1` | `pmw update` |

> Ambos fazem **backup automático** antes de atualizar.

---

## 📄 Licença

Este projeto é de uso pessoal e distribuído sob licença MIT.
