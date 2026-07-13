# Game Competition Analysis System — Backend

ASP.NET Core 8 REST API for automated leaderboard tracking of Võ Lâm Truyền Kỳ (VLTK Mobile & 2.0). Screenshots captured by an Airtest bot are uploaded to Cloudinary, converted to text by an OCR service, parsed into structured fields with regex, and stored in PostgreSQL.

---

## Architecture

```
Airtest Bot (Python)
    └─> POST screenshot
            └─> ASP.NET Core 8 API (this repo)
                    ├─> Cloudinary (image storage + background sync worker)
                    ├─> OCR Service (Hugging Face Space / FastAPI)
                    ├─> Regex-based field parser (rank, player name, score...)
                    └─> PostgreSQL (Entity Framework Core)
```

**Project layers:**

| Layer          | Folder                              | Responsibility                     |
| -------------- | ------------------------------------ | ----------------------------------- |
| API            | `Controllers/`                       | HTTP endpoints, auth, routing       |
| Business Logic | `BIL/Service/`                       | Orchestration, pagination, mapping  |
| Data Access    | `DAL/Repository/`, `DAL/Entities/`   | EF Core, OCR calls, regex parsing, DTOs |

---

## Tech Stack

- **Runtime:** .NET 8 / ASP.NET Core Web API
- **Database:** PostgreSQL via Entity Framework Core (Npgsql)
- **Auth:** JWT Bearer (roles: `admin`, `user`)
- **OCR:** Custom FastAPI OCR server hosted on Hugging Face Spaces
- **Data extraction:** Regex parsing of OCR text (no external AI/LLM call)
- **Storage:** Cloudinary (image hosting + background sync worker)
- **Deployment:** Docker, Render.com

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A running PostgreSQL instance
- Cloudinary account
- Access to an OCR endpoint compatible with the `/api/v1/extract` contract (or run your own)

### Configuration

Copy the example config and fill in your values:

```bash
cp appsettings.example.json appsettings.json
```

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
  },
  "Jwt": {
    "Key": "<min 32 char secret>",
    "Issuer": "GameAIAnalysis",
    "Audience": "GameAIAnalysisUsers",
    "DurationInMinutes": 60
  },
  "Cloudinary": {
    "CloudName": "...",
    "ApiKey": "...",
    "ApiSecret": "..."
  },
  "OcrApi": {
    "BaseUrl": "https://<your-ocr-server>",
    "ExtractEndpoint": "/api/v1/extract",
    "Language": "eng"
  }
}
```

> **Security note:** never commit real secrets (DB passwords, API keys, JWT signing key) to `appsettings.json`, `render.yaml`, or any tracked file. Use environment variables set directly in your deployment platform's dashboard, and rotate any credential that has ever been committed to the repository history.

### Run locally

```bash
dotnet restore
dotnet run
```

Swagger UI is available at `http://localhost:<port>/swagger`.

### Run with Docker

```bash
docker build -t gcas-api .
docker run -p 10000:10000 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Cloudinary__CloudName="..." \
  -e Cloudinary__ApiKey="..." \
  -e Cloudinary__ApiSecret="..." \
  -e OcrApi__BaseUrl="..." \
  gcas-api
```

---

## API Reference

All protected routes require `Authorization: Bearer <token>`.

### Auth

| Method | Endpoint             | Auth   | Description               |
| ------ | --------------------- | ------ | -------------------------- |
| POST   | `/api/auth/register`  | Public | Create a new user account  |
| POST   | `/api/auth/login`     | Public | Returns a JWT token        |

### AI Analysis

| Method | Endpoint                              | Auth     | Description                                  |
| ------ | -------------------------------------- | -------- | ---------------------------------------------|
| POST   | `/api/ai/analyze?gameName=`            | Public   | Upload a screenshot for OCR + parsing         |
| POST   | `/api/ai/analyze/automatic?gameName=`  | Public   | Analyze the latest image from Cloudinary      |
| GET    | `/api/ai`                              | Required | Paginated analysis history (admin sees all)   |
| GET    | `/api/ai/{id}`                         | Required | Get a single analysis record                  |
| GET    | `/api/ai/{id}/result`                  | Public   | Get extracted fields from an analysis         |
| GET    | `/api/ai/airtest-uploads`              | Public   | List images in the Cloudinary upload folder   |
| GET    | `/api/ai/heatmap`                      | Public   | Aggregated heatmap data                       |
| DELETE | `/api/ai/{id}`                         | Admin    | Delete an analysis record                     |

`gameName` values: `VLTK_Mobile`, `VLTK_2_0`

### Leaderboard

| Method | Endpoint                                  | Auth     | Description                       |
| ------ | ------------------------------------------ | -------- | ---------------------------------- |
| GET    | `/api/leaderboard`                         | Public   | Paginated leaderboard list         |
| GET    | `/api/leaderboard/top/{n}`                 | Public   | Top N leaderboard entries          |
| GET    | `/api/leaderboard/{id}`                    | Public   | Get leaderboard by ID              |
| GET    | `/api/leaderboard/{id}/entries`            | Public   | Raw entries for a leaderboard      |
| GET    | `/api/leaderboard/{id}/sorted`             | Public   | Rank-sorted entries                |
| POST   | `/api/leaderboard/from-ocr/{analysisId}`   | Required | Parse OCR result into leaderboard  |
| DELETE | `/api/leaderboard/{id}`                    | Admin    | Delete a leaderboard               |

### Game Data (CRUD)

All list endpoints support pagination via `?pageNumber=&pageSize=`.

| Resource  | Base Route         | Notes                                       |
| --------- | ------------------- | -------------------------------------------- |
| Players   | `/api/players`       | Search by name, filter by game/server/guild   |
| Servers   | `/api/servers`       |                                                |
| Guilds    | `/api/guilds`        |                                                |
| Games     | `/api/games`         |                                                |
| Companies | `/api/companies`     |                                                |
| Events    | `/api/events`        |                                                |
| Users     | `/api/users`         | Admin only for write operations               |

### System

| Method | Endpoint  | Description             |
| ------ | ---------- | ------------------------ |
| GET    | `/health` | Health check              |
| GET    | `/`       | Redirects to Swagger UI  |

---

## Deployment (Render)

The repo includes a `render.yaml` for one-click Docker deployment. Set the following environment variables **in the Render dashboard** (do not hardcode them in `render.yaml`):

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__DurationInMinutes`
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`
- `OcrApi__BaseUrl`, `OcrApi__ExtractEndpoint`, `OcrApi__Language`

---

## Project Structure

```
├── Controllers/        # API endpoints (Auth, AI, Leaderboard, Players, Servers, Guilds, Games, Companies, Events, Users)
├── BIL/
│   └── Service/         # Business logic & interfaces
├── DAL/
│   ├── DTO/             # Data transfer objects
│   ├── Entities/        # EF Core entity models
│   └── Repository/      # Data access, OCR calls, regex-based field parsing
├── Models/              # Shared request/response models (e.g. OcrResult)
├── Dockerfile           # Multi-stage Docker build
├── render.yaml          # Render.com deployment config
├── appsettings.example.json  # Config template (copy to appsettings.json)
└── db_schema.sql / database_schema.sql  # Database schema
```