# RaceDay —  API Platform (Part 2)

![RaceDay CI/CD](https://github.com/Mlu230000/RDPG/actions/workflows/ci.yml/badge.svg)

RaceDay is a platform for organising and entering running, cycling, and walking events in South Africa. This repository contains the complete **ASP.NET Core Web API** backend built with **Entity Framework Core (Code-First Approach)**, connected to a **SQL Server** database, with **Role-Based Session & JWT Authentication**, **Swagger UI Documentation**, and comprehensive **xUnit Unit Tests** automated via **GitHub Actions CI/CD**.



##  Architecture & Entities (8 Relational Entities)

The database schema strictly adheres to the approved Part 1 ERD and SQL Server script (`raceday_schema.sql`):

| Entity | Primary Key | Foreign Keys | Purpose |
|---|---|---|---|
| **Roles** | `role_id` | — | Lookup for `Organiser` (1) and `Participant` (2). |
| **Users** | `user_id` | `role_id` | Stores user credentials with hashed passwords (`BCrypt`), contact details, and assigned role. |
| **EventTypes** | `event_type_id` | — | Lookup for `Running` (1), `Cycling` (2), and `Walking` (3). |
| **Events** | `event_id` | `organiser_id`, `event_type_id` | Created by Organisers with date, location, province, start time, and status (`Open`, `Upcoming`, `Completed`). |
| **Categories** | `category_id` | `event_id` | Race distances (e.g., 10km, 21.1km, 42.2km), entry fees in ZAR ($R$), participant caps, and min/max age. |
| **Routes** | `route_id` | `category_id` (1:1) | Exactly one route per category containing elevation gain, start/end points, and course URLs. |
| **Enrolments** | `enrolment_id` | `participant_id`, `category_id` | Links participant to category; prevents duplicate enrolments; issues unique bib numbers. |
| **Results** | `result_id` | `enrolment_id` (1:1) | Records official finish times, overall/category rankings, and statuses (`Finished`, `DNF`, `DSQ`). |

---

## 👥 Description of the Two User Roles

### 1. Organiser (`role_id: 1`)
- **Event Management**: Create new endurance events, update event dates, venues, or status, and delete events (if no paid enrolments exist).
- **Categories & Routes**: Add race distance categories to their events, define entry fees in ZAR, participant caps, and configure elevation routes.
- **Enrolment Management**: View rosters of all participants registered for their events.
- **Result Capture**: Record official electronic chip finish times and assign finishing positions. Correct result times when required.

### 2. Participant (`role_id: 2`)
- **Browse & Discovery**: Explore events across South Africa filtered by sport discipline and province.
- **Race Enrolment**: Register into race categories, triggering automated race number (bib) allocation with capacity enforcement.
- **Personal Dashboard**: View upcoming race enrolments, download digital race bibs, and cancel/withdraw entries.
- **Performance History**: Review past finish times, overall and category rankings, and official completion records.

---

## 🚀 Setup & Run Instructions

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (with ASP.NET and web development workload) or Visual Studio Code
- [Microsoft SQL Server](https://www.microsoft.com/sql-server/) (or LocalDB / SQL Express)

### Step 1: Clone the Repository
```bash
git clone https://github.com/Mlu230000/RDPG.git
cd RDPG
```

### Step 2: Configure Connection String
Open `RaceDay.Api/appsettings.json` and ensure the connection string matches your local SQL Server instance:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaceDay;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
}
```

### Step 3: Run Database Migrations (Code-First Approach)
In Visual Studio **Package Manager Console**:
```powershell
Update-Database
```
*Or via .NET CLI:*
```bash
dotnet ef database update --project RaceDay.Api
```
*(The DbContext also includes automatic schema creation and seed data execution on startup if the database is missing).*

### Step 4: Run the API
```bash
dotnet run --project RaceDay.Api
```
Navigate to:
- **Swagger UI**: `http://localhost:5000/swagger` or `https://localhost:7001/swagger`

---

## 🧪 Running Unit Tests

The test suite in `RaceDay.Tests` covers all functional requirements, positive paths, negative paths, and role-based rejection:

```bash
dotnet test RaceDay.sln --verbosity normal
```

### Test Coverage Highlights:
- **`AuthTests`**: Successful participant and organiser registration, password hashing verification via BCrypt, duplicate email rejection (409 Conflict), invalid role validation (400 Bad Request), valid login with JWT issuance, invalid password rejection (401 Unauthorized).
- **`EventManagementTests`**: Organisers creating and updating events, non-owners rejected with 403 Forbidden, safe event deletion (204 No Content), conflict rejection when paid enrolments exist (409 Conflict).
- **`RoleAuthorizationTests`**: Participants blocked from capturing results (403), participants blocked from cancelling other users' enrolments (403), non-owners blocked from adding categories (403).
- **`EnrolmentTests`**: Participant enrolment with race bib allocation (201 Created), duplicate category enrolment rejection (409 Conflict), category capacity limit rejection (400 Bad Request), participant withdrawal (204 No Content).
- **`ResultTests`**: Capturing finish times, duplicate result rejection on same enrolment (409 Conflict), organiser result correction (200 OK).

---

## 🔄 GitHub Actions CI/CD Pipeline

The `.github/workflows/ci.yml` workflow automatically runs on every push and pull request to `main` and feature branches:
1. Checks out the repository.
2. Sets up .NET Core 8.0 SDK.
3. Restores NuGet dependencies.
4. Builds the solution in `Release` mode.
5. Executes all unit tests with TRX test logging.

### CI/CD Green Build Screenshot
*(Include screenshot of GitHub Actions green build badge and workflow run summary here)*:
```
[✔] Build Solution ......................... SUCCESS (0m 12s)
[✔] Run Unit Tests ......................... SUCCESS (18 passed, 0 failed)
```

---

## 📋 API Endpoints Summary (24 Endpoints)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/api/roles` | Public | Lists roles (Organiser, Participant) |
| `GET` | `/api/event-types` | Public | Lists event types (Running, Cycling, Walking) |
| `POST` | `/api/auth/register` | Public | Register new user with hashed password |
| `POST` | `/api/auth/login` | Public | Authenticate user, return JWT and set session |
| `GET` | `/api/users/me` | Logged In | Get profile of logged-in user |
| `PUT` | `/api/users/me` | Logged In | Update user's name and phone number |
| `GET` | `/api/users/me/enrolments` | Participant | Get personal event enrolments |
| `GET` | `/api/users/me/results` | Participant | Get personal results history |
| `GET` | `/api/events` | Public | Browse events with filters (`date`, `eventTypeId`, `province`) |
| `GET` | `/api/events/{id}` | Public | Get event detail with categories and routes |
| `POST` | `/api/events` | Organiser | Create a new event |
| `PUT` | `/api/events/{id}` | Organiser (Owner) | Update event details |
| `DELETE` | `/api/events/{id}` | Organiser (Owner) | Delete event (fails if paid enrolments exist) |
| `GET` | `/api/events/{id}/weather` | Logged In | Live race-day weather forecast and hydration advisory |
| `GET` | `/api/events/{id}/categories` | Public | List categories for an event |
| `POST` | `/api/events/{id}/categories` | Organiser (Owner) | Add category and generate 1:1 route |
| `PUT` | `/api/categories/{id}` | Organiser (Owner) | Update category details |
| `DELETE` | `/api/categories/{id}` | Organiser (Owner) | Remove category from event |
| `POST` | `/api/categories/{id}/enrol` | Participant | Enrol in category; generate race number |
| `GET` | `/api/events/{id}/enrolments` | Organiser (Owner) | List all participant entries for event |
| `DELETE` | `/api/enrolments/{id}` | Participant (Owner) | Withdraw own race enrolment |
| `POST` | `/api/enrolments/{id}/results` | Organiser (Owner) | Record finish time and position |
| `PUT` | `/api/results/{id}` | Organiser (Owner) | Correct already captured result |
| `GET` | `/api/categories/{id}/results` | Public | Get leaderboard sorted by finish position |
