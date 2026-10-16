# RaceDay

![CI](https://github.com/YOUR-USERNAME/RDPG/actions/workflows/ci.yml/badge.svg)

RaceDay is a platform for organising and entering running, cycling and walking events in South Africa. Organisers create events, add race categories (e.g. 10km, 42km) and capture results. Participants browse events, enrol in a category and view their own results history.

## Repository contents

| File | Purpose |
|---|---|
| `Prog ERD.docx` | Entity Relationship Diagram (8 entities) |
| `raceday_schema.sql` | SQL Server script: creates the `RaceDay` database, tables, constraints and seed data |
| `api_endpoint_plan.md` | Planned REST API endpoints, roles, request bodies and responses |
| `.github/workflows/ci.yml` | Continuous Integration pipeline |

## Database design

Entities: **Roles, Users, EventTypes, Events, Categories, Routes, Enrolments, Results**.

- A Role has many Users (Organiser or Participant).
- An EventType (Running, Cycling, Walking) has many Events.
- An Organiser (User) creates many Events.
- An Event offers many Categories; each Category has exactly one Route.
- A Participant (User) enrols in Categories (no duplicate enrolments).
- Each Enrolment produces at most one Result.

## Running the database script

1. Open `raceday_schema.sql` in SQL Server Management Studio.
2. Connect to your SQL Server instance (e.g. `localhost\SQLEXPRESS`).
3. Click **Execute**. The script is safe to re-run: it creates the database if missing and drops/recreates the tables.

## Continuous Integration

On every push and pull request to `main`, GitHub Actions:

1. Checks that the required project files exist.
2. Starts a SQL Server 2022 container.
3. Runs `raceday_schema.sql` and fails on any SQL error.
4. Verifies that all 8 tables were created.

See the **Actions** tab for run history.

## API

All planned endpoints are documented in [`api_endpoint_plan.md`](api_endpoint_plan.md).
