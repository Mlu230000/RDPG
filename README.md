# RaceDay

RaceDay is a full-stack, cloud-aware event management platform built for South Africa's road running, walking, and cycling community. It replaces the paper-based registration, spreadsheets, and disconnected communication that currently burden event organisers, giving them a single system to create and manage events, categories, and results — while participants can browse events, enter races, track their personal performance history, and prepare for race day with live weather and route information.

This repository covers **Part 1** of the project: data modelling, API planning, and database implementation, completed before any application code is written.

## System Description

RaceDay is organised around two user roles that share a single account system:

- **Organiser** — creates and manages events, defines categories (e.g. 10km, 21km) for each event, defines each category's route, reviews enrolments, and captures/corrects results.
- **Participant** — browses upcoming events, enrols into a category, views their own enrolments, checks live weather and route info ahead of race day, and reviews their personal performance history across past events.

### Data model

Six entities: **Users**, **Events**, **Categories**, **Routes**, **Enrolments**, **Results**.

- A Users row is either an Organiser or a Participant (`role` column) — one login/profile table, two behavioural roles.
- An Organiser (User) creates many Events (1–\*).
- An Event offers many Categories (1–\*).
- Each Category has exactly one Route (1–1).
- A Participant (User) submits many Enrolments (1–\*), and each Category receives many Enrolments (1–\*) — a participant can only enrol once per category (enforced by a `UNIQUE(participant_id, category_id)` constraint).
- Each Enrolment produces at most one Result (1–1).

See `/docs/erd.png` for the full diagram with attributes, primary keys, foreign keys, and cardinality.

## Repository Structure

```
/docs
  prog erd.docs           - Entity Relationship Diagram (Section A)
  api_endpoint_plan.md    - Full API endpoint specification table (Section B)
  raceday_schema.sql      - T-SQL schema + seed data, SSMS-compatible (Section C)
  ci-screenshot.png       - [Screenshot 2026-09-03 214303]
README.md                 - this file
```

## Setup Notes

1. Open `docs/raceday_schema.sql` in SQL Server Management Studio (SSMS).
2. Run the script against a fresh SQL Server instance — it creates the `RaceDay` database, all six tables with keys and constraints, and seeds realistic sample data (2 organisers, 2 participants, 3 events, categories and routes per event, and sample enrolments/results).
3. The script is idempotent: re-running it drops and recreates the tables in FK-safe order, so it's safe to run repeatedly during development.
4. Sanity-check queries are commented out at the bottom of the script — uncomment to spot-check the seeded data.

## Video Walkthrough

[add: link to video presentation]

The video explains the reasoning behind the ERD's entity/relationship choices, the API endpoint plan's structure, and demonstrates the SQL script running live in SSMS.
