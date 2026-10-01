# Setup

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0.x | `backend/global.json` pins 10.0.204 with `latestFeature` roll-forward |
| dotnet-ef | 10.x | `dotnet tool install -g dotnet-ef` |
| SQL Server | 2019+ or LocalDB | default connection string targets `(localdb)\MSSQLLocalDB` |
| Node.js | 20+ (22 recommended) | npm 10+ |

## Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project ExamPlatform.Api --launch-profile http   # http://localhost:5259
```

In the `Development` environment the API, on startup:

1. applies pending EF Core migrations (`Database:ApplyMigrationsOnStartup = true`), and
2. seeds demo data if the `Users` table is empty (`Seed:Enabled = true`).

Swagger UI: `http://localhost:5259/swagger` (Development only). Use **Authorize** with the token returned by `POST /api/auth/login`.

### Database & migrations

The initial migration is `InitialCreate` in `ExamPlatform.Infrastructure/Persistence/Migrations`.

```bash
cd backend
# create/upgrade the database without starting the API
dotnet ef database update --project ExamPlatform.Infrastructure --startup-project ExamPlatform.Api

# add a migration after changing the model
dotnet ef migrations add <Name> --project ExamPlatform.Infrastructure --startup-project ExamPlatform.Api --output-dir Persistence/Migrations
```

The `Admin` and `Teacher` roles are part of the migration (`HasData`), so they exist in every environment.
Everything else in the seed is development-only.

### Seed data (development only)

`DevelopmentDataSeeder` creates, when the database has no users:

- **Users**: 1 admin, 2 teachers (see README for emails). Password: `Seed:DefaultPassword`.
- **Departments**: Computer Science (CS), Mathematics (MATH)
- **Courses**: CS101, CS201, CS301, MATH201 with 3–4 topics each and teacher assignments
- **Questions**: 19 realistic questions across all 8 question types (one is an AI-sourced draft)
- **Exams**: a draft CS201 midterm (two sections) and a published CS101 quiz with its version snapshot

The seed password lives in `appsettings.Development.json` so a fresh clone runs immediately. It is a
**development-only** credential: the seeder never runs outside Development and does nothing if the password is empty.
Override it with `dotnet user-secrets set "Seed:DefaultPassword" "..."` if you prefer.

To reseed: drop the database (`dotnet ef database drop -f --project ExamPlatform.Infrastructure --startup-project ExamPlatform.Api`) and restart the API.

## Configuration reference

All settings can be provided through `appsettings*.json`, user-secrets (Development) or environment variables
(`Section__Key`, e.g. `Jwt__SigningKey`).

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:DefaultConnection` | LocalDB `ExamPlatformDb` | SQL Server connection string |
| `Jwt:Issuer` / `Jwt:Audience` | `ExamPlatform` / `ExamPlatform.Client` | token validation parameters |
| `Jwt:SigningKey` | *(empty; dev value in Development)* | HMAC-SHA256 key, **≥ 32 bytes**. Required — the API refuses to start without it. Use a secret store in production. |
| `Jwt:ExpiryMinutes` | `480` | access-token lifetime |
| `AI:Provider` | `Gemini` | `Gemini` or `Mock` (development-only sample data) |
| `AI:Model` | *(empty → `gemini-2.5-flash`)* | Gemini model id |
| `AI:ApiKey` | *(empty)* | Gemini API key. **Never commit.** Without it AI endpoints return `503`. |
| `AI:TimeoutSeconds` / `AI:Temperature` | `90` / `0.7` | provider call settings |
| `Institution:UniversityName` | `University of Excellence` | printed on previews and exports |
| `FileUpload:MaxFileSizeBytes` | `10485760` (10 MB) | max course-material upload |
| `FileUpload:AllowedExtensions` | `.pdf .docx .pptx` | allowed upload types |
| `Cors:AllowedOrigins` | `http://localhost:5173` | SPA origins allowed to call the API directly |
| `Database:ApplyMigrationsOnStartup` | `false` (`true` in Development) | auto-migrate in Development |
| `Seed:Enabled` / `Seed:DefaultPassword` / `Seed:EmailDomain` | `false` / empty / `exam-platform.local` | development seeding |
| `Bootstrap:AdminEmail` / `AdminPassword` / `AdminFullName` | empty | first administrator in production (created only when there are no users) |

### JWT

Tokens are HS256-signed, carry `sub`, `email`, `name` and `role` claims and expire after `Jwt:ExpiryMinutes`.
On every request the API also checks that the account still exists and is active, so deactivating a user revokes
their token immediately. Production: `Jwt__SigningKey=<long random secret>` from your secret store.

### AI

```bash
# Gemini (recommended)
dotnet user-secrets set "AI:ApiKey" "<key>" --project ExamPlatform.Api
# or environment variables
AI__Provider=Gemini AI__ApiKey=<key> AI__Model=gemini-2.5-flash
```

The Gemini provider uses the REST `generateContent` endpoint with a JSON response schema. Every call (success or
failure) is recorded in the `AIGenerations` table. Rate limit: 20 AI calls per user per minute.

## Frontend

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173
npm run build      # type-check + production build to dist/
```

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_BASE_URL` | empty | API base URL. Empty = same origin (`/api`), which uses the Vite proxy in development |
| `VITE_BASE_PATH` | `/` | public base path (`/ExamPlatform/` on GitHub Pages) |
| `VITE_API_PROXY_TARGET` | `http://localhost:5259` | dev-server proxy target |

See `frontend/.env.example`. For production, either serve `dist/` behind the same origin as the API (reverse proxy
`/api`) or set `VITE_API_BASE_URL` and add the SPA origin to `Cors:AllowedOrigins`.

## Export dependencies

- **PDF**: [QuestPDF](https://www.questpdf.com) — configured with the *Community* license
  (`QuestPDF.Settings.License = LicenseType.Community`), free for organizations under the QuestPDF revenue
  threshold. Review the license for your institution. Uses system fonts; no native dependencies on Windows/Linux x64.
- **Word**: DocumentFormat.OpenXml — generates real `.docx` files, no Office installation required.
- **Material upload** (AI grounding): PdfPig (PDF text) and OpenXml (DOCX/PPTX text). Files are processed in memory and never stored.

## Tests

```bash
cd backend
dotnet test
```

| Area | Covered |
|------|---------|
| Questions | create, invalid question, duplicate options, type-specific rules, search/filter, pagination, archive-vs-delete, duplicate |
| Exams | create, invalid duration, add question, prevent duplicate, prevent other course, total points, reorder, publish + snapshot + lock, preview hides answers |
| AI | request validation, provider failure (logged + mapped), generated-structure validation, accept-as-draft, response parsing, Gemini error/config/parse |
| Auth (HTTP) | valid login, invalid login, validation errors, 401 without/with bad token, `/me`, 403 for wrong role, 403 for unassigned course |

Integration tests run the real API pipeline with `WebApplicationFactory` and the EF Core InMemory provider.
