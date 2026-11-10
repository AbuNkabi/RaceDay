# Integration notes and verification limitations

## What was merged

Source A: `RaceDay2.zip`: original Part 2 project, API, Contracts, xUnit tests, migrations.
Source B: `RaceDay-Part3.zip`, with corrections from earlier reviewed integration: RaceDay.Web MVC frontend, controllers, views and Dockerfiles.

Part 2's `RaceDay.sln` now includes all four projects. Part 2 JWT auth is retained; MVC stores its JWT in server-side Session and sends Bearer tokens in outgoing requests.

## Changes in this integration

- Added API endpoints for public event categories, Organiser category create/edit/delete, Organiser enrolment status, event enrolment list and event results.
- Added server-side event ownership checks around race result creation/update/deletion.
- Added new EF Core migration for banner image, profile image, category fee/capacity, enrolment category and status.
- Added controlled Organiser account registration via secret code stored in configuration; default registration is Participant.
- Added private Azure blob upload/retrieval API and MVC media proxy for event banners/profile pictures. **Old public blob URLs from a different build are not automatically compatible**; newly uploaded images use private blob names.
- Added MVC category pages, Participant category selection, enrolment confirmation, Organiser dashboard, Participant results and profile image upload.
- Added workflow, Dockerfile and README.

## Limitations and important test needs

- The integration has not been compiled or run because the tool environment has no .NET SDK, SQL Server or Docker engine. All code still requires the student's Visual Studio build and live feature tests.
- The manual EF migration is intended for the original Part 2 EF-managed SQL database. Confirm migration history first. If the database was created by an unrelated SQL script or its schema differs, reconcile it before applying.
- The old Part 2 test suite was preserved, but no new integration tests have yet been run successfully here.
- `docker-compose.yml` is optional. It may require SQL Server Docker host resources and needs validation on the student's machine. The primary Docker demonstration is MVC container + host API.
- Results and enrolment state updates depend on database records created after the new migration; ensure end-to-end tests using BOTH roles.
- No Git repository history or specific GitHub link was included in the ZIP. Clone her own Classroom repo and preserve commit history.
- This is a student project reference implementation, not a production-security audit. Test file content validation, rate limits, concurrency and database constraints before any public deployment.
