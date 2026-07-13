# ReferWell E2E (Playwright)

Smoke tests against the running Next.js app + API.

## Prerequisites

1. Backend API on `http://localhost:5188` (`dotnet run` in `backend/ReferWell.Api`)
2. Frontend on `http://localhost:4000` (`npm run dev` in `frontend`)
3. Seeded users (see root README)

First-time browser install:

```powershell
npx playwright install chromium
```

## Run

```powershell
cd frontend
npm run test:e2e
```

Optional:

```powershell
npm run test:e2e:ui      # interactive UI mode
npm run test:e2e:report # open last HTML report
```

`playwright.config.ts` reuses an already-running frontend when not in CI.
