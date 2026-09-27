# API

Base URL (development): `http://localhost:5259`. Interactive docs: `/swagger` (Development).
All endpoints except `POST /api/auth/login` require `Authorization: Bearer <token>`. Enums are strings.

## Response envelope

```json
{ "success": true, "message": "Question created successfully.", "data": { }, "errors": [] }
```

Paginated:

```json
{ "success": true, "message": "", "data": [ ],
  "pagination": { "page": 1, "pageSize": 20, "totalCount": 120, "totalPages": 6 }, "errors": [] }
```

Errors: `success: false`, a human-readable `message` and, for validation, `errors: [{ "field": "Options", "message": "…" }]`.

| Status | When |
|--------|------|
| 200 / 201 | success / created |
| 400 | validation failed |
| 401 | missing/invalid/expired token, invalid credentials, inactive account |
| 403 | wrong role, or course not assigned to the teacher |
| 404 | entity not found |
| 409 | business-rule conflict (duplicate code, question already in exam, published exam edit, …) |
| 429 | rate limit |
| 502 / 503 | AI provider failure / AI provider not configured |
| 500 | unexpected error (details only in Development) |

Policies: **Admin** = `AdminOnly`, **Staff** = Teacher or Admin.

## Auth & users

| Method | Path | Policy | Description |
|--------|------|--------|-------------|
| POST | `/api/auth/login` | anonymous (10/min/IP) | `{ email, password }` → `{ accessToken, expiresAt, user }` |
| GET | `/api/auth/me` | authenticated | current user `{ id, fullName, email, roles }` |
| GET | `/api/users?role=&search=` | Admin | list users |
| POST | `/api/users` | Admin | `{ fullName, email, password, roles[] }` |
| PATCH | `/api/users/{id}/status` | Admin | `{ isActive }` |

## Departments, courses, topics

| Method | Path | Policy | Notes |
|--------|------|--------|-------|
| GET | `/api/departments` | Staff | |
| POST | `/api/departments` | Admin | `{ name, code, description }` |
| PUT | `/api/departments/{id}` | Admin | |
| DELETE | `/api/departments/{id}` | Admin | 409 if it has courses |
| GET | `/api/courses?departmentId=&search=` | Staff | teachers see assigned courses only |
| POST | `/api/courses` | Staff | `{ departmentId, code, name, description, creditHours }`; creator (teacher) is auto-assigned |
| GET | `/api/courses/{id}` | Staff + access | includes topics and teachers |
| PUT | `/api/courses/{id}` | Staff + access | |
| DELETE | `/api/courses/{id}` | Staff + access | 409 if it has questions or exams |
| POST | `/api/courses/{id}/teachers` | Admin | `{ teacherId }` |
| DELETE | `/api/courses/{id}/teachers/{teacherId}` | Admin | |
| GET | `/api/courses/{id}/topics` | Staff + access | |
| POST | `/api/courses/{id}/topics` | Staff + access | `{ name, description, order? }` |
| PUT | `/api/topics/{id}` | Staff + access | |
| DELETE | `/api/topics/{id}` | Staff + access | questions keep existing without a topic |

## Question bank

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/questions` | `?page&pageSize(≤100)&courseId&topicId&type&difficulty&bloomLevel&status&source&search&excludeExamId` — server-side paging; archived hidden unless `status=Archived`; `search` matches text and tags |
| GET | `/api/questions/{id}` | full question incl. options, tags, usage |
| POST | `/api/questions` | see body below |
| PUT | `/api/questions/{id}` | 409 if used in a published exam (duplicate it instead) |
| PATCH | `/api/questions/{id}/status` | `{ status: Draft \| Approved \| Archived }` |
| DELETE | `/api/questions/{id}` | deletes unused questions; archives used ones → `data.archived` |
| POST | `/api/questions/{id}/duplicate` | returns the new draft copy |
| GET | `/api/tags?search=` | tag suggestions |

```json
{
  "courseId": "…", "topicId": "…", "text": "Which data structure is LIFO?",
  "type": "MultipleChoice", "difficulty": "Easy", "bloomLevel": "Remember", "points": 1,
  "explanation": "…", "expectedAnswer": null, "status": "Draft",
  "options": [ { "text": "Stack", "isCorrect": true }, { "text": "Queue", "isCorrect": false } ],
  "tags": ["stacks"]
}
```

Matching options use `matchText` for the right-hand item; Ordering options are sent in the correct order.

## Exams

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/exams` | `?page&pageSize&courseId&status&type&search` |
| GET | `/api/exams/{id}` | exam with ordered questions (teacher view, includes correctness) |
| POST | `/api/exams` | `{ courseId, title, description, instructions, type, durationMinutes, examDate }` |
| PUT | `/api/exams/{id}` | save draft settings; 409 when published/archived |
| DELETE | `/api/exams/{id}` | deletes never-published exams; archives others → `data.archived` |
| POST | `/api/exams/{id}/questions` | `{ questionIds[], section?, points? }` (points default to each question's points) |
| PUT | `/api/exams/{id}/questions/order` | `{ questionIds[] }` — must contain every question exactly once |
| PUT | `/api/exams/{id}/questions/{questionId}` | `{ points, section }` |
| DELETE | `/api/exams/{id}/questions/{questionId}` | |
| POST | `/api/exams/{id}/ready` | Draft → Ready |
| POST | `/api/exams/{id}/publish` | creates an `ExamVersion` snapshot; exam becomes read-only |
| POST | `/api/exams/{id}/draft` | explicit re-open of a Ready/Published exam |

Validation: question must exist (404), belong to the exam's course (409), not be archived or duplicated (409);
points > 0; duration > 0; total points are always recalculated from exam questions.

## Preview & export

| Method | Path | Returns |
|--------|------|---------|
| GET | `/api/exams/{id}/preview` | `{ header, questions[] }` — no ids, no correct answers; matching/ordering items scrambled deterministically |
| GET | `/api/exams/{id}/answer-key` | `{ header, items[{ number, type, points, correctAnswer, explanation }] }` |
| GET | `/api/exams/{id}/export/pdf` | `application/pdf` |
| GET | `/api/exams/{id}/export/word` | `.docx` |
| GET | `/api/exams/{id}/answer-key/pdf` | `application/pdf` |
| GET | `/api/exams/{id}/answer-key/word` | `.docx` |

`header` = university, department, course code/name, title, description, instructions, type, status, date,
duration, total points, question count. Exported files include `Content-Disposition` with a safe file name.

## AI (Staff, 20 calls/min/user)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/ai/questions/generate` | returns proposals only (nothing saved) |
| POST | `/api/ai/questions/improve` | `{ questionId }` or `{ courseId, topicId?, question }` + `instructions?` → improved proposal |
| POST | `/api/ai/questions/accept` | `{ generationId?, questions: [QuestionInput…] }` → saves as `Draft`, `source = AI` |
| POST | `/api/ai/materials/extract` | multipart `file` (.pdf/.docx/.pptx, ≤ 10 MB) → `{ fileName, text, characterCount, truncated }` |

Generate request / response:

```json
{ "courseId": "…", "topicId": "…", "numberOfQuestions": 10, "questionType": "MultipleChoice",
  "difficulty": "Medium", "bloomLevel": "Understand", "additionalInstructions": "", "sourceMaterial": null }
```

```json
{ "generationId": "…", "provider": "Gemini", "model": "gemini-2.5-flash",
  "questions": [ { "text": "…", "type": "MultipleChoice", "difficulty": "Medium", "bloomLevel": "Understand",
                   "points": 2, "explanation": "…", "expectedAnswer": null,
                   "options": [ { "text": "…", "isCorrect": false, "matchText": null } ],
                   "isValid": true, "issues": [] } ] }
```

`isValid`/`issues` come from the same validator used for manually authored questions.

## Dashboard

`GET /api/dashboard` → course/question/draft/published counts, AI drafts awaiting review, courses, 5 recent exams, 5 recent questions.
