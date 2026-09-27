# Exam Platform

A web-based exam management platform for university teachers: manage courses and a reusable question bank,
build exams, draft questions with AI (always teacher-reviewed), preview exams as printed papers and export
exams and answer keys to **PDF** and **Word (.docx)**.

> The student online-exam module is intentionally out of scope for this MVP.

| Layer    | Stack |
|----------|-------|
| Backend  | ASP.NET Core Web API (.NET 10), EF Core 10 + SQL Server, CQRS with MediatR, FluentValidation, JWT + role policies, Swagger |
| Frontend | SolidJS + TypeScript, Vite, Tailwind CSS v4, Kobalte, Axios, Solid Router |
| Exports  | QuestPDF (PDF), DocumentFormat.OpenXml (Word) |
| AI       | `IAIQuestionGenerator` abstraction — Google Gemini provider (plus a development-only `Mock` provider) |

## Quick start (development)

Prerequisites: .NET SDK 10, Node.js 20+ (22 recommended), SQL Server or SQL Server **LocalDB**, `dotnet-ef` (`dotnet tool install -g dotnet-ef`).

```bash
# 1. Backend — creates the database from migrations and seeds demo data on first run (Development only)
cd backend
dotnet run --project ExamPlatform.Api --launch-profile http
# API:     http://localhost:5259
# Swagger: http://localhost:5259/swagger
```

```bash
# 2. Frontend (new terminal)
cd frontend
npm install
npm run dev
# App: http://localhost:5173  (Vite proxies /api to the backend)
```

Sign in with one of the seeded development accounts (password = `Seed:DefaultPassword` in
`backend/ExamPlatform.Api/appsettings.Development.json`):

| Role    | Email                               | Courses |
|---------|-------------------------------------|---------|
| Admin   | `admin@exam-platform.local`         | all |
| Teacher | `sarah.ahmed@exam-platform.local`   | CS101, CS201 |
| Teacher | `omar.hassan@exam-platform.local`   | CS301, MATH201 |

### AI generation

AI calls are disabled until a key is configured. Either set a Gemini key (never commit it):

```bash
cd backend/ExamPlatform.Api
dotnet user-secrets set "AI:ApiKey" "<your-gemini-api-key>"
```

…or try the full generate → review → accept workflow offline with the development-only sample provider:

```bash
AI__Provider=Mock dotnet run --project ExamPlatform.Api --launch-profile http
```

## Tests

```bash
cd backend
dotnet test
```

49 xUnit tests cover questions, exams, AI generation and authentication (see [docs/setup.md](docs/setup.md#tests)).

## Documentation

- [docs/setup.md](docs/setup.md) — installation, configuration, environment variables, migrations, seed data, exports
- [docs/architecture.md](docs/architecture.md) — Clean Architecture layout, CQRS, security model, business rules
- [docs/database.md](docs/database.md) — tables, relationships, indexes, constraints
- [docs/api.md](docs/api.md) — endpoints, response envelope, status codes

## Repository layout

```text
backend/
├── ExamPlatform.Api/             HTTP only: controllers, auth setup, exception handler, Swagger
├── ExamPlatform.Application/     CQRS commands/queries, validators, DTOs, interfaces (by feature)
├── ExamPlatform.Domain/          Entities, enums, domain rules (no dependencies)
├── ExamPlatform.Infrastructure/  EF Core, migrations, JWT, hashing, AI providers, PDF/Word exporters
└── tests/ExamPlatform.Tests/     xUnit tests
frontend/
└── src/ app · layouts · pages · features · components · services · stores · types · utils
docs/
```
