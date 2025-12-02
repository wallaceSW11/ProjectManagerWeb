# Project Structure

## Root Layout

```
ProjectManagerWeb/
├── frontend/          # Vue 3 application
├── backend/           # .NET 9 Web API
└── README.md
```

## Frontend Structure (`frontend/`)

```
frontend/
├── src/
│   ├── components/    # Vue components organized by feature
│   │   ├── clone/           # Git clone functionality
│   │   ├── comum/           # Shared/common components
│   │   │   └── botao/       # Button components
│   │   ├── pastas/          # Folder management
│   │   ├── repositorios/    # Repository management
│   │   │   └── menuItemCadastro/  # Menu item forms
│   │   ├── sites/           # Site management
│   │   └── sitesiis/        # IIS site management
│   ├── composables/   # Vue composables (reusable logic)
│   ├── constants/     # Application constants
│   ├── models/        # TypeScript models/interfaces
│   ├── plugins/       # Vue plugins (Vuetify config)
│   ├── router/        # Vue Router configuration
│   ├── services/      # API service layer
│   ├── stores/        # Pinia stores
│   ├── types/         # TypeScript type definitions
│   ├── utils/         # Utility functions (eventBus)
│   ├── views/         # Page-level components
│   ├── App.vue        # Root component
│   ├── main.ts        # Application entry point
│   └── style.css      # Global styles
├── public/            # Static assets
├── dist/              # Build output (generated)
├── package.json
├── vite.config.ts
└── tsconfig.json
```

### Frontend Conventions

- **Components**: Organized by feature domain (clone, pastas, repositorios, sites, sitesiis)
- **Common Components**: Reusable UI components in `comum/` folder
- **Services**: Each service extends `BaseApiService` and handles API communication for a specific domain
- **Models**: TypeScript interfaces/types for data structures
- **Stores**: Pinia stores for global state (configuracao, siteIIS)
- **Views**: Top-level page components (ConfiguracaoView, PastasView, RepositoriosView, SitesIISView)

## Backend Structure (`backend/`)

```
backend/
├── src/
│   ├── Controllers/   # API controllers (endpoints)
│   ├── DTOs/          # Data Transfer Objects
│   ├── Enuns/         # Enumerations
│   ├── Services/      # Business logic and data access
│   └── Utils/         # Utility classes
├── frontend/          # Built frontend files (served by backend)
├── Banco/             # Database folder (empty, actual data in %ProgramData%)
├── bin/               # Build output
├── obj/               # Build intermediates
├── Properties/        # Launch settings
├── Program.cs         # Application entry point
├── ProjectManagerWeb.csproj
├── Iniciar_PMW.bat    # Start backend script
├── atualizar_PMW.bat  # Update script
└── logo.ico           # Application icon
```

### Backend Conventions

- **Controllers**: RESTful API controllers with route prefix `/api/{resource}`
  - Use dependency injection via primary constructors
  - Return `IActionResult` (Ok, BadRequest, NotFound, NoContent)
  - Follow naming: `{Resource}Controller`

- **Services**: Business logic layer
  - Registered as Singletons in DI container
  - JSON services handle file I/O with thread-safe semaphores
  - Follow naming: `{Resource}Service` or `{Resource}JsonService`

- **DTOs**: Request/Response objects
  - Use record types where appropriate
  - Follow naming: `{Purpose}RequestDTO` or `{Purpose}ResponseDTO`

- **File Operations**: All JSON services use:
  - `SemaphoreSlim` for thread safety
  - `JsonSerializerOptions` with `WriteIndented = true`
  - Files stored in `%ProgramData%\PMW\Banco\`

## Naming Conventions

### Frontend
- **Components**: PascalCase (e.g., `CardPasta.vue`, `BotaoPrimario.vue`)
- **Files**: PascalCase for components, camelCase for utilities
- **Services**: PascalCase with "Service" suffix (e.g., `CloneService.ts`)
- **Stores**: camelCase filenames (e.g., `configuracao.ts`)
- **Models**: PascalCase with "Model" suffix (e.g., `RepositorioModel.ts`)

### Backend
- **Controllers**: PascalCase with "Controller" suffix
- **Services**: PascalCase with "Service" suffix
- **DTOs**: PascalCase with "DTO" suffix
- **Enums**: PascalCase with "E" prefix (e.g., `ETipoComando`)

## Key Integration Points

1. **Static File Serving**: Backend serves frontend build from `frontend/` folder
2. **API Communication**: Frontend services call backend at `/api/*` endpoints
3. **SPA Routing**: Backend fallback middleware serves `index.html` for non-API routes
4. **Build Pipeline**: Frontend build copies to `backend/frontend/` before backend publish
