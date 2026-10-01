# Deployment

```text
https://mo7amin04.github.io/ExamPlatform/   ← frontend  (GitHub Pages, built by GitHub Actions)
                 │  HTTPS + JWT
                 ▼
https://<your-site>.runasp.net/api           ← API       (MonsterASP.NET)
                 │
                 ▼
<db>.public.databaseasp.net                  ← SQL Server (MonsterASP.NET)
```

## 1. API on MonsterASP.NET

### Configuration

Production settings live in `backend/ExamPlatform.Api/appsettings.Production.json`. The file is **git-ignored**
(never commit it) and is included automatically by `dotnet publish`. Copy
`appsettings.Production.example.json` if you need to recreate it.

| Key | Value |
|-----|-------|
| `ConnectionStrings:DefaultConnection` | the MonsterASP database connection string (Databases → your DB → connection string) |
| `Jwt:SigningKey` | random secret, ≥ 32 characters (generated for you already) |
| `Cors:AllowedOrigins` | `https://mo7amin04.github.io` (origin only — no `/ExamPlatform` path) |
| `Database:ApplyMigrationsOnStartup` | `true` — tables are created/upgraded automatically on start |
| `Bootstrap:AdminEmail` / `AdminPassword` | first administrator, created only when the database has no users |
| `AI:ApiKey` | optional Gemini key |

After the first successful login you can remove `Bootstrap:AdminPassword` from the file and redeploy
(the admin already exists; the bootstrap only runs on an empty user table).

### Publish

Option A — Visual Studio: right-click **ExamPlatform.Api → Publish → Import profile**, select the
`.publishsettings` file downloaded from the MonsterASP panel (**Deploy → WebDeploy**), then **Publish**.

Option B — command line + FTP:

```bash
dotnet publish backend/ExamPlatform.Api -c Release -o publish
```

Upload the *contents* of `publish/` to the site's `wwwroot` using the FTP credentials from the MonsterASP panel.
If files are locked, stop the site in the panel (or upload an `app_offline.htm` first), upload, then start it again.

`publish/web.config` runs the app in-process on IIS; `ASPNETCORE_ENVIRONMENT` defaults to `Production`.

### Verify

- `https://<your-site>.runasp.net/health` → `{"status":"ok"}`
- `POST https://<your-site>.runasp.net/api/auth/login` with the bootstrap admin → `200`

If the site fails to start, enable stdout logging in `web.config` (`stdoutLogEnabled="true"`) and check the
`logs` folder via FTP. The most common causes are an invalid connection string or a missing `Jwt:SigningKey`.

## 2. Frontend on GitHub Pages

The workflow `.github/workflows/deploy-frontend.yml` builds the frontend on every push that touches `frontend/`
and publishes `dist/` to the `gh-pages` branch, which GitHub Pages serves at
`https://mo7amin04.github.io/ExamPlatform/`.

- If Pages is not enabled automatically after the first run: **Settings → Pages → Source: Deploy from a branch →
  `gh-pages` / `(root)`**.
- The API address defaults to `https://examplatform.runasp.net`. Override it with the repository variable
  `API_BASE_URL` (Settings → Secrets and variables → Actions → Variables).
- The API **must be served over HTTPS** (enable SSL for the site in the MonsterASP panel): browsers block an HTTPS
  page from calling an `http://` API.

How it works:
- `VITE_BASE_PATH=/ExamPlatform/` makes assets and routes work under the repository sub-path.
- `404.html` (a copy of `index.html`) lets deep links and page refreshes load the SPA.
- The API's CORS policy must allow the Pages origin `https://mo7amin04.github.io`.

## 3. CI

`.github/workflows/ci.yml` builds and tests the backend and builds the frontend on every push and pull request.

## Security checklist

- [ ] `appsettings.Production.json` is not committed (`git check-ignore -v backend/ExamPlatform.Api/appsettings.Production.json`)
- [ ] Database password rotated if it was ever committed
- [ ] `Jwt:SigningKey` is unique to production
- [ ] Bootstrap admin password changed / removed after first login
