# RaceDay - API Endpoint Plan

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as an Organiser or Participant. | None (public) | `{ fullName, email, password, role, phone }` | 201 Created – user object (no password); 400 Bad Request – validation error; 409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and returns a JWT for subsequent requests. | None (public) | `{ email, password }` | 200 OK – `{ token, user }`; 401 Unauthorized – invalid credentials |
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK – user profile object; 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any (logged in) | `{ fullName, phone }` | 200 OK – updated profile; 400 Bad Request |
| GET | /api/users/me/enrolments | Lists the logged-in participant's own event enrolments. | Participant | None | 200 OK – array of enrolments with event/category info |
| GET | /api/users/me/results | Returns the logged-in participant's personal performance history across all past events. | Participant | None | 200 OK – array of results |
| GET | /api/events | Lists all events, with optional filters (date, type, province) for browsing upcoming events. | None (public) | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns full detail for one event, including its categories. | None (public) | None | 200 OK – event object; 404 Not Found |
| POST | /api/events | Creates a new event. | Organiser | `{ eventName, description, eventDate, startTime, location, province, eventType }` | 201 Created – event object; 400 Bad Request |
| PUT | /api/events/{id} | Updates an existing event's details. | Organiser (owner) | `{ eventName, description, eventDate, startTime, location, province, eventType, status }` | 200 OK – updated event; 403 Forbidden; 404 Not Found |
| DELETE | /api/events/{id} | Cancels/removes an event that has no paid enrolments. | Organiser (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found; 409 Conflict – enrolments exist |
| GET | /api/events/{id}/weather | Returns a live weather forecast for the event's location and date, for race-day prep. | Any (logged in) | None | 200 OK – `{ forecast, temperature, windSpeed, ... }`; 404 Not Found |
| GET | /api/events/{id}/categories | Lists all categories offered for a specific event. | None (public) | None | 200 OK – array of categories |
| POST | /api/events/{id}/categories | Adds a new category (distance/age group) to an event. | Organiser (owner) | `{ categoryName, distanceKm, minAge, maxAge, entryFee, maxParticipants }` | 201 Created – category object; 403 Forbidden |
| PUT | /api/categories/{id} | Updates a category's details. | Organiser (owner) | `{ categoryName, distanceKm, minAge, maxAge, entryFee, maxParticipants }` | 200 OK – updated category; 403 Forbidden; 404 Not Found |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found |
| POST | /api/categories/{id}/enrol | Enrols the logged-in participant into a category, generating a race number. | Participant | `{ }` (payment reference optional) | 201 Created – enrolment object; 400 Bad Request – category full; 409 Conflict – already enrolled |
| GET | /api/events/{id}/enrolments | Lists every enrolment received for an event, for the organiser to manage entries. | Organiser (owner) | None | 200 OK – array of enrolments; 403 Forbidden |
| DELETE | /api/enrolments/{id} | Cancels/withdraws a participant's own enrolment. | Participant (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found |
| POST | /api/enrolments/{id}/results | Captures a finish-time result against a participant's enrolment. | Organiser (owner of event) | `{ finishTime, overallPosition, categoryPosition, status }` | 201 Created – result object; 403 Forbidden; 404 Not Found |
| PUT | /api/results/{id} | Corrects an already-captured result. | Organiser (owner of event) | `{ finishTime, overallPosition, categoryPosition, status }` | 200 OK – updated result; 403 Forbidden; 404 Not Found |
| GET | /api/categories/{id}/results | Returns the results/leaderboard for a category, sorted by finish position. | None (public) | None | 200 OK – array of results |
