# Architecture

## Clean Architecture

```text
            ┌──────────────────────────┐
            │     ExamPlatform.Api     │  controllers, JWT/policies, rate limiting,
            │   (composition root)     │  exception → HTTP mapping, Swagger
            └────────────┬─────────────┘
                         │ ISender (MediatR)
            ┌────────────▼─────────────┐
            │ ExamPlatform.Application │  commands, queries, handlers, validators, DTOs,
            │                          │  interfaces (IApplicationDbContext, IAIQuestionGenerator,
            │                          │  IExamPdfExporter, IExamWordExporter, …)
            └──────┬──────────────▲────┘
                   │              │ implements
            ┌──────▼──────┐  ┌────┴──────────────────────┐
            │   Domain    │  │ ExamPlatform.Infrastructure│  EF Core DbContext + migrations,
            │ entities,   │  │                            │  auditing interceptor, JWT, hashing,
            │ enums, rules│  │                            │  Gemini/Mock AI, QuestPDF, OpenXml
            └─────────────┘  └────────────────────────────┘
```

- **Domain** has no dependencies. Invariants live on entities — e.g. `Exam.AddQuestion` rejects questions from
  another course, duplicates, archived questions and non-positive points; `Exam.Publish` creates the version snapshot.
- **Application** depends only on Domain plus abstractions. It uses EF Core's provider-agnostic API
  (`DbSet<T>`, `IQueryable` async operators) through `IApplicationDbContext`; it never references the SQL Server
  provider, migrations or the concrete `ApplicationDbContext`. This keeps queries composable (server-side filtering
  and paging) without a repository layer per entity.
- **Infrastructure** implements every Application interface.
- **Api** contains only HTTP concerns. Controllers translate route/body to a command/query and wrap the result;
  there is no business logic in controllers.

## CQRS

Each use case is a MediatR request with its own handler and (where it takes input) a FluentValidation validator,
organized by feature:

```text
Application/Features/
  Auth/  Users/  Departments/  Courses/  Topics/  Questions/  Exams/ (+ Preview/)  Exports/  AI/  Dashboard/
```

- `ValidationBehavior` runs validators before every handler and throws `ValidationException` (→ 400 with field errors).
- Shared rules are reused, not duplicated:
  - `QuestionContentValidator` validates question content for **manual questions, AI proposals and AI improvements**.
  - `QuestionWriter` maps options/tags for create, update and AI accept.
  - `ExamDocumentBuilder` produces the preview and answer-key models consumed by the web preview **and** both exporters,
    so labelling and scrambling are identical everywhere.
  - `ICourseAccessService` is the single implementation of "teachers can only access assigned courses".

## Security model

| Concern | Implementation |
|---------|----------------|
| Passwords | ASP.NET Core Identity `PasswordHasher` (PBKDF2-HMAC-SHA512, 100k iterations, per-user salt) |
| Authentication | JWT bearer (HS256), issuer/audience/lifetime validated, 1 min clock skew; inactive users rejected on every request |
| Authorization | policies `AdminOnly`, `TeacherOnly`, `Staff` (Teacher or Admin); fallback policy requires authentication |
| Ownership | `ICourseAccessService` filters queries to accessible courses and guards every course/question/exam/topic mutation (403) |
| Identity | `CurrentUserService` reads the user id from the validated token only; `CreatedBy/UpdatedBy` are stamped by `AuditableEntityInterceptor` — client values are never trusted |
| Input | FluentValidation for every command/query; model-binding errors use the same envelope |
| Uploads | extension allow-list, size limit, magic-byte check, in-memory processing, no files or paths persisted/exposed |
| Secrets | JWT key and AI key come from configuration/user-secrets/env; nothing secret is committed except clearly marked development-only values |
| Abuse | rate limiting: login 10/min per IP, AI 20/min per user |
| Errors | `GlobalExceptionHandler` maps exceptions to 400/401/403/404/409/502/503; stack traces only in Development |

Roles: **Admin** manages departments, users and teacher assignments and can access everything.
**Teacher** manages courses they are assigned to (a teacher who creates a course is assigned automatically),
their topics, questions and exams, and uses AI generation.

## Business rules

| # | Rule | Where |
|---|------|-------|
| 1 | Teachers manage only assigned courses | `CourseAccessService` |
| 2 | Questions only in accessible courses | `QuestionWriter.EnsureValidTargetAsync`, query filters |
| 3 | No question from another course in an exam | `Exam.AddQuestion` |
| 4 | No duplicate question per exam | `Exam.AddQuestion` + unique index `(ExamId, QuestionId)` |
| 5 | Published exams are not freely editable | `Exam.EnsureEditable`; explicit `POST /exams/{id}/draft` re-opens |
| 6–7 | AI output needs approval / is untrusted | generate returns proposals only; `POST /ai/questions/accept` stores them as `Draft` with `Source = AI`; proposals are validated and issues shown |
| 8 | No answers in the exam preview | `ExamPreviewDto` has no correctness data; Matching/Ordering items are scrambled |
| 9 | Answer key is separate | `/answer-key` endpoints and page |
| 10 | Used questions are archived, not deleted | `DeleteQuestionCommandHandler` |
| 11 | Total points derived from exam questions | `Exam.RecalculateTotalPoints` (private setter) |
| 12 | Question content not copied into exams | `ExamQuestion` references `Question` (+ order/points/section only) |
| 13 | History preserved | `ExamVersion` snapshots on publish; questions used by a published exam are content-locked (duplicate to edit); archived questions still render in historical exams |

## Frontend

```text
src/
├── app/          App, route table, router root (toasts, 401 redirect)
├── layouts/      AuthLayout, DashboardLayout (sidebar, RTL-aware)
├── pages/        one folder per area; thin pages composing features
├── features/     auth guards, departments, courses, questions (form, filters, preview),
│                 exams (builder list, picker, paper), ai (generator panel), exports (menu)
├── components/   ui (Button, Dialog, Primitives, Icon), forms (fields), tables, feedback (toast, states)
├── services/     axios instance + per-module API clients
├── stores/       auth.store (session, roles)
├── types/        API envelope + domain types
└── utils/        i18n/RTL, formatting, download, clone
```

- Data loading uses `createResource`; every list/detail view renders explicit loading (skeleton), empty, and
  error-with-retry states. There is intentionally no app-wide `<Suspense>` so refetches never blank the page.
- **Auth flow**: login → JWT stored with its expiry (localStorage, short-lived) → attached as `Bearer` by an Axios
  interceptor → `/api/auth/me` validates restored sessions → 401 clears the session and redirects to `/login?redirect=…`;
  403 shows a toast or the Forbidden page.
- **Accessibility**: Kobalte dialogs/menus/toasts (focus trap, Esc, ARIA), labelled form controls with `aria-invalid`
  and error descriptions, keyboard alternatives (move up/down) for drag-and-drop ordering.
- **i18n/RTL**: `utils/i18n.ts` switches `lang`/`dir`; layout uses logical utilities (`ms-/me-/start-/end-`). The
  shell is translated (English/Arabic); page content is English-only in this MVP.
