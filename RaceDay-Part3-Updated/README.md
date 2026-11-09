# RaceDay South Africa - Part 3 Final POE Submission

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-MVC%20+%20REST%20API-blue.svg)]()
[![Azure Blob Storage](https://img.shields.io/badge/Azure-Blob%20Storage-0078D4.svg)](https://azure.microsoft.com/en-us/products/storage/blobs/)
[![Docker](https://img.shields.io/badge/Docker-Multi--Stage%20Build-2496ED.svg)](https://www.docker.com/)
[![CI Build](https://img.shields.io/badge/CI%2FCD-Passing%20(Green)-success.svg)]()

Official Part 3 Final Project.

---

## Video Presentation & Demo Link
- **Presentation Contents**:
  1. Architecture walkthrough: MVC Web App calling REST API (strictly zero database queries in MVC).
  2. Organiser Interface: Dashboard KPI metrics, Event CRUD with Azure Blob banner image upload, Category management, Enrolments roster with color-coded status badges, and Results entry with finish times.
  3. Participant Interface: Browse upcoming events with multi-criteria filters, Event detail with instant 1-click enrolment, My Enrolments view, My Results displaying finishing position (*e.g. 47th place out of 312 finishers*), and Athlete Profile with Azure Blob photo upload.
  4. Azure Blob Storage integration walkthrough in `RaceDay.Api`.
  5. Docker setup and single-command startup demonstration.

---

## CI/CD Workflow & Green Build Status
Automated continuous integration is configured in `.github/workflows/ci.yml`. On every push to `main`, the workflow validates:
1. Repository structure and required project files.
2. NuGet dependency restoration and `.NET 8.0` build of `RaceDay.sln`.
3. Execution of unit and integration tests in `RaceDay.Tests`.
4. Multi-stage Docker image builds for both `RaceDay.Web` and `RaceDay.Api`.

![CI Green Build Status](docs/ci_green_build.png)

---

## Single-Command Docker Instructions

### Option A: Run MVC Web Application with Single Docker Run Command
```bash
docker run -d -p 8080:80 \
  -e "RaceDayApi__BaseUrl=[http://host.docker.internal:5000](http://host.docker.internal:5000)" \
  --name raceday-web \
  raceday-web:latest
---

## Detailed Project Folder Structure
```
RaceDay-Part3-Submission/
├── .github/
│   └── workflows/
│       └── ci.yml                   # CI/CD pipeline (build, test, docker validate)
├── docs/
│   └── ci_green_build.png           # CI green build screenshot
├── RaceDay.sln                      # Visual Studio 2022 Solution (all 3 projects)
├── docker-compose.yml               # Multi-container orchestration (Web + API + Azurite)
├── raceday_schema.sql               # Base relational database schema
├── raceday_part3_migration.sql      # Part 3 SQL migration (Azure Blob URL columns)
├── README.md                        # Master documentation & video presentation script
│
├── RaceDay.Web/                     # ASP.NET Core 8.0 MVC Application
│   ├── Controllers/
│   │   ├── AccountController.cs     # Login, Register, Logout, Session management
│   │   ├── HomeController.cs        # Public / Participant race browser with filters
│   │   ├── EventsController.cs      # Event details, categories, instant enrolment
│   │   ├── OrganiserController.cs   # Dashboard, Event CRUD, Categories, Enrolments, Results
│   │   └── ParticipantController.cs # Enrol, My Enrolments, My Results, Athlete Profile
│   ├── Services/
│   │   ├── IRaceDayApiClient.cs     # Typed API Client Interface
│   │   └── RaceDayApiClient.cs      # Consumes REST API (NO direct database access)
│   ├── Filters/
│   │   └── AuthorizeRoleAttribute.cs# [AuthorizeOrganiser] and [AuthorizeParticipant]
│   ├── Models/
│   │   └── ViewModels.cs            # Strongly-typed MVC ViewModels
│   ├── Views/
│   │   ├── Account/ (Login.cshtml, Register.cshtml)
│   │   ├── Home/ (Index.cshtml)
│   │   ├── Events/ (Details.cshtml)
│   │   ├── Organiser/ (Dashboard, Events, CreateEvent, EditEvent, Categories, Enrolments, ResultsEntry)
│   │   ├── Participant/ (MyEnrolments, MyResults, Profile)
│   │   └── Shared/ (_Layout.cshtml, _ValidationScriptsPartial.cshtml)
│   ├── wwwroot/
│   │   ├── css/ (raceday.css)       # Sporting energetic design system
│   │   └── js/ (site.js)
│   ├── Dockerfile                   # Multi-stage production Dockerfile
│   ├── Program.cs                   # Session state, HttpClient, Cookie authentication
│   └── appsettings.json             # API base URL configuration
│
├── RaceDay.Api/                     # RESTful Web API (Part 2 upgraded with Azure Blob)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── EventsController.cs      # Includes POST /{id}/banner Azure Blob endpoint
│   │   ├── UsersController.cs       # Includes POST /{id}/profile-picture Azure Blob endpoint
│   │   ├── FilesController.cs       # Generic Azure Blob upload endpoint
│   │   ├── CategoriesController.cs
│   │   ├── EnrolmentsController.cs
│   │   └── ResultsController.cs
│   ├── Services/
│   │   ├── IAzureBlobService.cs     # Azure Blob Storage Interface
│   │   ├── AzureBlobService.cs      # Azure Blob implementation with fallback simulation
│   │   ├── IPasswordHasher.cs
│   │   └── ITokenService.cs
│   ├── Models/ (Event.cs, User.cs, Category.cs, Enrolment.cs, Result.cs, Role.cs, Route.cs)
│   ├── DTOs/ (Dtos.cs)
│   ├── Data/ (RaceDayDbContext.cs)
│   ├── Dockerfile
│   ├── Program.cs
│   └── appsettings.json             # Azure Blob Storage connection strings
│
└── RaceDay.Tests/                   # Automated Unit & Integration Tests (xUnit)
    ├── AuthTests.cs
    ├── EnrolmentTests.cs
    ├── EventManagementTests.cs
    ├── ResultTests.cs
    ├── RoleAuthorizationTests.cs
    └── TestDbHelper.cs
```
