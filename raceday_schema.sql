/* 

   RaceDay - Full Database 
   Matches: docs erd exactly (6 entities)
   */

IF DB_ID('RaceDay') IS NULL
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

/* Drop tables in FK-safe order if re-running the script */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Routes', 'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/*
   1. USERS
   Stores both Organisers and Participants, distinguished by "role".
    */
CREATE TABLE dbo.Users (
    user_id         INT             IDENTITY(1,1)   NOT NULL,
    full_name       VARCHAR(100)   NOT NULL,
    email           VARCHAR(150)   NOT NULL,
    password_hash   VARCHAR(255)   NOT NULL,
    role            VARCHAR(20)    NOT NULL DEFAULT 'Participant',
    phone           VARCHAR(20)    NULL,
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (user_id),
    CONSTRAINT UQ_Users_Email UNIQUE (email),
    CONSTRAINT CK_Users_Role CHECK (role IN ('Organiser', 'Participant'))
);
GO

/* 
   2. EVENTS
   Each event is created by one Organiser (a User).
   */
CREATE TABLE dbo.Events (
    event_id        INT             IDENTITY(1,1)   NOT NULL,
    organiser_id    INT             NOT NULL,
    event_name      VARCHAR(150)   NOT NULL,
    description     VARCHAR(MAX)   NULL,
    event_date      DATE            NOT NULL,
    start_time      TIME            NULL,
    location        VARCHAR(150)   NOT NULL,
    province        VARCHAR(50)    NULL,
    event_type      VARCHAR(20)    NOT NULL DEFAULT 'Running',
    status          VARCHAR(20)    NOT NULL DEFAULT 'Upcoming',
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (event_id),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (organiser_id)
        REFERENCES dbo.Users (user_id),
    CONSTRAINT CK_Events_Type CHECK (event_type IN ('Running', 'Cycling', 'Walking')),
    CONSTRAINT CK_Events_Status CHECK (status IN ('Upcoming', 'Open', 'Closed', 'Completed', 'Cancelled'))
);
GO

/* 
   3. CATEGORIES
   Each Event offers many Categories (e.g. 5km, 10km, 21km).
    */
CREATE TABLE dbo.Categories (
    category_id     INT             IDENTITY(1,1)   NOT NULL,
    event_id        INT             NOT NULL,
    category_name   VARCHAR(50)    NOT NULL,
    distance_km     DECIMAL(5,2)    NOT NULL,
    min_age         INT             NULL,
    max_age         INT             NULL,
    entry_fee       DECIMAL(8,2)    NOT NULL DEFAULT 0,
    max_participants INT            NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (category_id),
    CONSTRAINT FK_Categories_Event FOREIGN KEY (event_id)
        REFERENCES dbo.Events (event_id),
    CONSTRAINT CK_Categories_Distance CHECK (distance_km > 0),
    CONSTRAINT CK_Categories_Fee CHECK (entry_fee >= 0)
);
GO

/*
   4. ROUTES
   Each Category has exactly one Route (one-to-one).
    */
CREATE TABLE dbo.Routes (
    route_id            INT             IDENTITY(1,1)   NOT NULL,
    category_id         INT             NOT NULL,
    route_name          VARCHAR(100)   NOT NULL,
    start_point         VARCHAR(150)   NULL,
    end_point           VARCHAR(150)   NULL,
    elevation_gain_m    INT             NULL,
    route_map_url       VARCHAR(255)   NULL,
    CONSTRAINT PK_Routes PRIMARY KEY (route_id),
    CONSTRAINT FK_Routes_Category FOREIGN KEY (category_id)
        REFERENCES dbo.Categories (category_id),
    CONSTRAINT UQ_Routes_Category UNIQUE (category_id)
);
GO

/*
   5. ENROLMENTS
   A Participant (User) enters a Category. One participant cannot
   enrol in the same category twice.
    */
CREATE TABLE dbo.Enrolments (
    enrolment_id    INT             IDENTITY(1,1)   NOT NULL,
    participant_id  INT             NOT NULL,
    category_id     INT             NOT NULL,
    race_number     VARCHAR(10)    NULL,
    enrolment_date  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    payment_status  VARCHAR(20)    NOT NULL DEFAULT 'Pending',
    CONSTRAINT PK_Enrolments PRIMARY KEY (enrolment_id),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (participant_id)
        REFERENCES dbo.Users (user_id),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (category_id)
        REFERENCES dbo.Categories (category_id),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (participant_id, category_id),
    CONSTRAINT CK_Enrolments_Payment CHECK (payment_status IN ('Pending', 'Paid', 'Refunded'))
);
GO

/* 
   6. RESULTS
   Each Enrolment produces at most one Result (one-to-one).
    */
CREATE TABLE dbo.Results (
    result_id           INT             IDENTITY(1,1)   NOT NULL,
    enrolment_id        INT             NOT NULL,
    finish_time         TIME            NULL,
    overall_position     INT            NULL,
    category_position    INT            NULL,
    status               VARCHAR(20)   NOT NULL DEFAULT 'Finished',
    CONSTRAINT PK_Results PRIMARY KEY (result_id),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (enrolment_id)
        REFERENCES dbo.Enrolments (enrolment_id),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (enrolment_id),
    CONSTRAINT CK_Results_Status CHECK (status IN ('Finished', 'DNF', 'DSQ'))
);
GO

/* 
   SEED DATA
  */

-- Organisers (2)
INSERT INTO dbo.Users (full_name, email, password_hash, role, phone) VALUES
('Thandiwe Mokoena', 'thandiwe@raceday.co.za', 'hashed_pw_1', 'Organiser', '0821234567'),
('Johan van der Merwe', 'johan@raceday.co.za', 'hashed_pw_2', 'Organiser', '0837654321');

-- Participants (2)
INSERT INTO dbo.Users (full_name, email, password_hash, role, phone) VALUES
('Lindiwe Dlamini', 'lindiwe@gmail.com', 'hashed_pw_3', 'Participant', '0721112222'),
('Pieter Botha', 'pieter@gmail.com', 'hashed_pw_4', 'Participant', '0733334444');

-- Events (3)
INSERT INTO dbo.Events (organiser_id, event_name, description, event_date, start_time, location, province, event_type, status) VALUES
(1, 'Soweto Marathon', 'Annual road running event through the historic streets of Soweto.', '2026-11-01', '06:00', 'Soweto, Johannesburg', 'Gauteng', 'Running', 'Open'),
(2, 'Cape Town Cycle Tour', 'One of the world''s largest timed cycling events.', '2027-03-08', '06:30', 'Cape Town', 'Western Cape', 'Cycling', 'Open'),
(1, 'Park Run Charity Walk', 'Community charity walk raising funds for local schools.', '2026-09-20', '07:30', 'Pretoria', 'Gauteng', 'Walking', 'Open');

-- Categories (spread across the 3 events)
INSERT INTO dbo.Categories (event_id, category_name, distance_km, min_age, max_age, entry_fee, max_participants) VALUES
(1, '10km Fun Run', 10.00, 12, NULL, 150.00, 2000),
(1, '42km Marathon', 42.20, 18, NULL, 350.00, 5000),
(2, '109km Cycle Tour', 109.00, 16, NULL, 550.00, 15000),
(2, '56km Cycle Tour', 56.00, 14, NULL, 400.00, 8000),
(3, '5km Charity Walk', 5.00, NULL, NULL, 0.00, 1000);

-- Routes (one per category)
INSERT INTO dbo.Routes (category_id, route_name, start_point, end_point, elevation_gain_m, route_map_url) VALUES
(1, 'Soweto 10km Route', 'FNB Stadium', 'Orlando Stadium', 120, 'https://raceday.co.za/routes/soweto-10km'),
(2, 'Soweto 42km Route', 'FNB Stadium', 'FNB Stadium', 410, 'https://raceday.co.za/routes/soweto-42km'),
(3, 'Cape Town 109km Route', 'Hertzog Boulevard', 'Green Point', 980, 'https://raceday.co.za/routes/cpt-109km'),
(4, 'Cape Town 56km Route', 'Hertzog Boulevard', 'Camps Bay', 520, 'https://raceday.co.za/routes/cpt-56km'),
(5, 'Pretoria 5km Route', 'Union Buildings', 'Church Square', 45, 'https://raceday.co.za/routes/pta-5km');

-- Enrolments (sample)
INSERT INTO dbo.Enrolments (participant_id, category_id, race_number, payment_status) VALUES
(3, 1, 'A1023', 'Paid'),
(4, 2, 'A2044', 'Paid'),
(3, 3, 'C3067', 'Pending'),
(4, 5, 'W1002', 'Paid');

-- Results (sample, for finished enrolments)
INSERT INTO dbo.Results (enrolment_id, finish_time, overall_position, category_position, status) VALUES
(1, '00:52:14', 34, 12, 'Finished'),
(2, '04:12:03', 210, 88, 'Finished');
GO

/* 
   Quick sanity checks
    */
-- SELECT * FROM dbo.Users;
-- SELECT * FROM dbo.Events;
-- SELECT * FROM dbo.Categories;
-- SELECT * FROM dbo.Routes;
-- SELECT * FROM dbo.Enrolments;
-- SELECT * FROM dbo.Results;
