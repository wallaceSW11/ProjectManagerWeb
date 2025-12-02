# Technology Stack

## Frontend

- **Framework**: Vue 3 with Composition API (`<script setup>` syntax)
- **UI Library**: Vuetify 3
- **State Management**: Pinia
- **Build Tool**: Vite
- **Language**: TypeScript
- **HTTP Client**: Axios
- **Event Bus**: mitt
- **Additional**: vite-plugin-pwa for PWA support, vuedraggable for drag-and-drop

### Frontend Structure
- Path alias `@` points to `src/`
- Global components registered in `main.ts`: BotaoPrimario, BotaoSecundario, BotaoTerciario, ModalPadrao, IconeComTooltip
- Services use BaseApiService for API communication
- Stores use Pinia for state management

## Backend

- **Framework**: .NET 9 (ASP.NET Core Web API)
- **Language**: C#
- **Data Storage**: JSON files stored in `%ProgramData%\PMW\Banco\`
- **Dependency Injection**: Singleton services
- **CORS**: Enabled for all origins (AllowAll policy)

### Backend Structure
- Controllers in `src/Controllers/` - handle HTTP endpoints
- Services in `src/Services/` - business logic and JSON file operations
- DTOs in `src/DTOs/` - data transfer objects
- Enums in `src/Enuns/` - enumerations
- Utils in `src/Utils/` - utility classes (e.g., ShellExecute)

## Integration

- Backend serves frontend static files from `frontend/` folder
- Backend runs on port **2024** (dev) or **2025** (production)
- Frontend dev server runs on port **5173**
- API routes prefixed with `/api`
- SPA fallback: all non-API routes serve `index.html`

## Common Commands

### Frontend
```bash
cd frontend
npm install          # Install dependencies
npm start            # Start dev server (port 5173)
npm run build        # Build for production
npm run type-check   # TypeScript type checking
npm run format       # Format code with Prettier
npm run publish:all  # Build + copy to backend + publish backend
```

### Backend
```bash
cd backend
dotnet restore       # Restore NuGet packages
dotnet run           # Run in development mode (port 2024)
dotnet build         # Build the project
dotnet publish -c Release -o <output-path>  # Publish for production
```

### Production Deployment
The project uses a custom workflow:
- `npm run publish:all` in frontend builds and deploys to `C:\inetpub\wwwroot\PMW`
- `Iniciar_PMW.bat` starts the backend as a background process
- Update scripts handle git pull and republishing

## File Storage

Backend stores configuration data as JSON files in:
```
%ProgramData%\PMW\Banco\
├── repositorios.json
├── pastas.json
├── sitesiis.json
└── configuracao.json
```

Services use `SemaphoreSlim` for thread-safe file access.
