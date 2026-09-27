# Database

SQL Server via EF Core 10. Schema is created exclusively through migrations (`InitialCreate`).

Conventions:
- Primary keys are `uniqueidentifier`, generated client-side (`Guid.NewGuid()`); join tables use composite keys.
- All persisted `DateTime` values are UTC (`CreatedAt`, `UpdatedAt`, `ExamDate`, `LastLoginAt`).
- `CreatedAt / CreatedBy / UpdatedAt / UpdatedBy` are stamped by `AuditableEntityInterceptor` from the authenticated user.
- Enums are stored as strings (`nvarchar(32)`); decimals as `decimal(9,2)`.
- Queries use split queries for collection includes.

## Entity relationships

```text
Users ─< UserRoles >─ Roles

Departments ─< Courses ─< Topics
                  │  └─< CourseTeachers >─ Users
                  ├─< Questions ─< QuestionOptions
                  │       │   └─< QuestionTags >─ Tags
                  │       └── (TopicId → Topics, optional)
                  ├─< Exams ─< ExamQuestions >─ Questions
                  │     └─< ExamVersions ─< ExamVersionQuestions >─ Questions
                  └─< AIGenerations (TopicId → Topics, optional)
```

## Tables

| Table | Purpose | Key columns |
|-------|---------|-------------|
| `Users` | accounts | `Email` (unique), `PasswordHash`, `IsActive`, `LastLoginAt`, audit |
| `Roles` | `Admin`, `Teacher` (seeded by migration) | `Name` (unique) |
| `UserRoles` | user ↔ role | PK `(UserId, RoleId)` |
| `Departments` | academic departments | `Code` (unique), `Name` |
| `Courses` | courses | `Code` (unique), `DepartmentId`, `CreditHours` |
| `CourseTeachers` | teacher assignments | unique `(CourseId, TeacherId)` |
| `Topics` | syllabus topics | unique `(CourseId, Name)`, `Order` |
| `Questions` | question bank | `CourseId`, `TopicId?`, `Text`, `Type`, `Difficulty`, `BloomLevel`, `Points`, `Explanation`, `ExpectedAnswer`, `Status`, `Source`, audit |
| `QuestionOptions` | choices / matching pairs / ordering items | `QuestionId`, `Text`, `IsCorrect`, `Order`, `MatchText?` |
| `Tags` | global tags | `Name` (unique) |
| `QuestionTags` | question ↔ tag | PK `(QuestionId, TagId)` |
| `Exams` | exams | `CourseId`, `Title`, `Type`, `DurationMinutes`, `TotalPoints` (derived), `ExamDate`, `Status`, audit |
| `ExamQuestions` | ordered exam composition | unique `(ExamId, QuestionId)`, `Order`, `Points`, `Section` |
| `ExamVersions` | immutable snapshot per publish | unique `(ExamId, VersionNumber)`, title/duration/points/date at publish |
| `ExamVersionQuestions` | snapshot composition | unique `(ExamVersionId, QuestionId)`, `Order`, `Points`, `Section` |
| `AIGenerations` | audit of AI calls | `CourseId`, `TopicId?`, `Operation`, `Status`, `Provider`, `Model`, counts, request/response JSON, `ErrorMessage`, `DurationMs` |

### Option semantics by question type

| Type | Options | `IsCorrect` | `MatchText` | `ExpectedAnswer` |
|------|---------|-------------|-------------|------------------|
| MultipleChoice | 2–10 | exactly one | – | – |
| MultipleSelect | 2–10 | ≥ 1 | – | – |
| TrueFalse | exactly 2 | exactly one | – | – |
| Matching | 2–20 pairs | – | right-hand item | – |
| Ordering | 2–20, stored in correct order | – | – | – |
| ShortAnswer / FillBlank | none | – | – | required |
| Essay | none | – | – | optional rubric |

## Indexes

| Index | Why |
|-------|-----|
| `Questions(CourseId)`, `(TopicId)`, `(Type)`, `(Difficulty)`, `(BloomLevel)`, `(Status)`, `(CourseId, Status)` | question bank filtering |
| `Exams(CourseId)`, `Exams(Status)` | exam lists, dashboard counters |
| `ExamQuestions(ExamId)`, `(QuestionId)`, unique `(ExamId, QuestionId)` | composition, usage checks, no duplicates |
| `CourseTeachers(CourseId, TeacherId)` unique, `(TeacherId)` | access checks |
| `QuestionTags(TagId)`, `Tags(Name)` unique | tag search |
| `ExamVersionQuestions(QuestionId)` | history checks before deleting questions |
| `AIGenerations(CourseId)`, `(CreatedBy)` | audit queries |

## Delete behaviour

Cascades only flow to owned children (course → topics/assignments/AI logs, question → options/tags,
exam → exam questions/versions). References that must preserve history are `Restrict`:
questions from exams and versions, courses from questions/exams, topics from questions (handlers detach first).
Application rules then decide between delete and archive.
