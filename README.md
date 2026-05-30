<<<<<<< HEAD
﻿# Plan mate

Plan mate is an AI schedule management assistant that analyzes natural language events, prioritizes tasks, detects conflicts, and proposes optimized plans.

## Suggested Stack

- Backend: ASP.NET Core Web API (.NET 10)
- Frontend: ASP.NET Razor or separate web client
- Database: SQLite (starter) with cloud DB migration later
- AI: OpenAI API or ML.NET (replaceable strategy)
- Notifications: Windows notifications or email API

## Core Features

- Natural language schedule input
- Date/time/priority extraction
- Conflict detection and alternative suggestions
- Daily recommendation generation
- Reminder/notification pipeline

## MVP Roadmap (Agile Sprints)

1. User + schedule CRUD with SQLite
2. NLP parsing pipeline and priority assignment
3. Conflict detector and recommendation engine
4. Calendar UI + notification integration

## How to run (after installing .NET SDK)

1. Install .NET 10 SDK
2. Open `PlanMate/src/PlanMate.Api`
3. Run:

```bash
dotnet restore
dotnet run
```

4. Open the browser at `http://localhost:5280` (or the URL shown in the terminal)

If you see **port already in use**, an old server is still running. Stop it with:

```powershell
netstat -ano | findstr :5280
taskkill /PID <PID번호> /F
```

Or run on another port: `dotnet run --urls "http://localhost:5290"`

### Current UI flow

1. Enter a task in the input box
2. Plan mate asks for the time
3. Enter/select a time
4. Task appears as a card (task + time + AI advice) and the list is sorted by time

### Google Gemini (AI chat + schedule advice)

Get a key from [Google AI Studio](https://aistudio.google.com/apikey).

**Do not put the key in `appsettings.json`** (it may be committed to git). Use one of these instead:

**Option A — local dev file (recommended)**

Edit `src/PlanMate.Api/appsettings.Development.json`:

```json
{
  "Ai": {
    "GoogleApiKey": "your-new-key-here"
  }
}
```

**Option B — user secrets**

```powershell
cd PlanMate/src/PlanMate.Api
dotnet user-secrets set "Ai:GoogleApiKey" "your-new-key-here"
```

Restart the server (`dotnet run`). **AI tab** and **schedule card advice** use Gemini when a key is set. Responses tagged `(Gemini)` mean the real API is working.

If the old key stopped working with HTTP 403, Google may have blocked it as **leaked** — create a **new** key and revoke the old one in AI Studio.
=======
## 실행 방법
1. .NET 10 SDK 설치
2. `PlanMate/src/PlanMate.Api` 폴더에서:
   dotnet restore
   dotnet run
3. 브라우저: http://localhost:5280
4. AI 사용: appsettings.json 또는 appsettings.Development.json에 Google Gemini API 키 설정
>>>>>>> d1eab5c073e69c041634b2467634b6db3696fbb5
