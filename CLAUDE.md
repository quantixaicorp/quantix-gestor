# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture

**Quantix Gestor** is a multi-tenant SaaS business management system (ERP) for Brazilian SMBs. Two separate apps in one repo:

- `backend/` — ASP.NET Core 10 Minimal API (C#), PostgreSQL via EF Core
- `frontend/` — React 19 + Vite + TypeScript SPA

Authentication is handled by an external **Quantix Admin** identity server (OpenID Connect / PKCE). The backend validates JWTs; the frontend exchanges auth codes via `AuthContext`. The admin service runs separately at `http://localhost:5001` in dev.

## Commands

### Frontend (`cd frontend`)
```bash
npm run dev        # dev server on port 5174 (strictPort)
npm run build      # tsc + vite build
npm run lint       # eslint
npm test           # vitest run (single pass)
npm run test:watch # vitest watch mode
```

### Backend (`cd backend/src/GestorAI.API`)
```bash
dotnet run                                          # dev on http://localhost:5297
dotnet ef migrations add <Name>                     # add EF migration
dotnet ef database update                           # apply migrations
```

### Infrastructure
```bash
docker compose up -d   # start PostgreSQL on port 5433 + API on port 5002
```

### Dev env vars (frontend `.env.local`)
```
VITE_API_URL=http://localhost:5002
VITE_ADMIN_URL=http://localhost:5001
VITE_CLIENT_ID=gestorai
```

## Multi-tenancy

All tenant data is isolated by `EmpresaId` (Guid). Every entity implementing `ITenantEntity` gets an EF Core **global query filter** applied in `AppDbContext.OnModelCreating` — queries are automatically scoped. `TenantMiddleware` extracts `EmpresaId` from the JWT and populates `TenantContext` (scoped DI).

## Backend patterns

- **Minimal API endpoints** in `Endpoints/` — each module has its own static `Map*` extension method wired in `Program.cs`.
- **Services** in `Services/<Module>/` — business logic, injected as `Scoped`.
- **FluentValidation** validators live alongside their service; registered individually in `Program.cs`.
- **DTOs** in `DTOs/<Module>/` — separate request/response types.
- **Domain entities** in `Domain/Entities/` — plain EF Core entities.
- **Errors** use `AppException` (mapped to HTTP status by `ExceptionMiddleware`); validation errors return `ValidationProblem`.
- JSON uses `camelCase` + `JsonStringEnumConverter` globally.
- DB schema is `gestor`, snake_case column names via `EFCore.NamingConventions`.
- PDF generation uses `PuppeteerSharp`.
- External integrations: **Asaas** (payments/cobrancas), **ClickSign** (contract signatures), **Evolution API** (WhatsApp automation via `IEvolutionApiService`).
- `AutomacaoHostedService` is a background service for scheduled collection reminders.

## Frontend patterns

- **Router** at `src/router/index.tsx` — React Router v7, `AppLayout` wraps all authenticated routes; public routes (`/agendar/:slug`, `/orcamento/:token`, `/assinar/:slug`) render standalone.
- **`src/services/api.ts`** — thin fetch wrapper around `VITE_API_URL`, automatic JWT refresh on 401, surfaces `body.errors` (FluentValidation), `body.error`, or `body.title`.
- **Hooks** in `src/hooks/` — one hook per domain (`useClientes`, `useVendas`, etc.), each wrapping `api.*` calls with local `useState` for data/loading/error.
- **UI primitives** in `src/components/ui/` — shadcn/ui-style components (Radix UI + Tailwind).
- **Dashboard** uses a widget registry (`widgetRegistry.tsx`) and a user-configurable layout persisted via `DashboardLayout` entity.
- **Módulos** (feature flags) are encoded as `modules` claim in the JWT and fetched fresh from `/api/me/modules` on login; `AuthContext.enabledModules` drives sidebar visibility.
- Roles: `admin`, `financeiro`, `estoque`, `vendas` (backend enforces via policies; frontend gates via `isAdmin`).

## Testing

Frontend uses **Vitest** + **@testing-library/react**. Tests live in `src/test/`. Hook dependencies are mocked with `vi.mock`. Run a single test file:
```bash
npx vitest run src/test/agendamentos.test.tsx
```
There are no backend unit tests; backend validation is covered by FluentValidation.
