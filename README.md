# RaceDay | PROG6212 Programming 2B Part 3

RaceDay is an ASP.NET Core 8 event management platform for road running, cycling and walking. This project integrates the student's **existing Part 2 JWT-authenticated REST API** with a Part 3 **ASP.NET Core MVC frontend**, SQL Server, private Azure Blob Storage, Docker and GitHub Actions.

> **Integration status:** Prepared from the uploaded `RaceDay2.zip` (Part 2) and `RaceDay-Part3.zip` (Part 3), including corrected Part 3 files. **Compilation and live database/Azure/Docker testing must still be completed in Visual Studio.** Do not claim these tests passed before running them. Do not overwrite your original GitHub repository before verification.

## Source structure

```text
RaceDay.sln
RaceDay.Api/            REST API: controllers, EF Core, JWT, Azure Blob handling
RaceDay.Contracts/      Shared request and response contracts
RaceDay.Api.Tests/      Original Part 2 xUnit tests
RaceDay.Web/            ASP.NET Core MVC website for Part 3
.github/workflows/      Build / test / MVC Docker build workflow
docker-compose.yml      Optional full-stack Docker configuration
```

## Architecture and roles

The browser talks to `RaceDay.Web`. The website communicates with `RaceDay.Api` using `HttpClient` and the JWT saved in server-side MVC session storage. The API alone accesses SQL Server and private Azure Blob Storage. The website accesses uploaded images through MVC media endpoints, which proxy the API image endpoints.

- **Organiser:** login, dashboard, create/edit/delete events, upload banners, create/update/delete event categories, view/confirm/reject enrolments, record and remove results.
- **Participant:** register/login, browse upcoming events, select a category, enrol, see enrolment status, view personal results and update profile/picture.

Public registration creates a Participant by default. Organiser registration requires a **private code stored in API User Secrets** and supplied only by the authorised organiser. Do not put this code in GitHub.

## Prerequisites

- Visual Studio 2022 with ASP.NET/web workload, .NET 8 SDK.
- SQL Server or SQL Server Express and optionally SSMS.
- Docker Desktop with Linux containers and WSL 2 (for Docker demo).
- Azure subscription with a Storage account (for image uploads).
- Git for Windows.

NuGet packages are declared in project files and restored automatically. The API uses EF Core SQL Server, JWT Bearer authentication, Swashbuckle and `Azure.Storage.Blobs`. The tests use xUnit and EF Core InMemory.

## Local configuration, credentials and database

**Back up your existing `RaceDayDB` before database changes. Never delete or recreate the existing database without checking its contents.**

Open `RaceDay.sln` in Visual Studio. Open Terminal in the solution folder. Set secrets in the **API** project, not the MVC project.

```powershell
# SQL example: replace the instance as needed
# If using LocalDB or another instance, adapt the connection string accordingly.
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;" --project RaceDay.Api

# Generate a unique, strong key. Copy the resulting value locally, do not commit it.
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_YOUR_GENERATED_KEY" --project RaceDay.Api

dotnet user-secrets set "Registration:OrganiserCode" "REPLACE_WITH_YOUR_PRIVATE_ORGANISER_CODE" --project RaceDay.Api
```

The API's `appsettings.json` includes non-secret JWT issuer/audience and the Azure container name. User Secrets are loaded when running in **Development**.

**Database migration:** The integrated project includes `20261110190000_AddPart3Features`, which adds category fees/capacity, enrolment category/status, event-image and profile-image columns. First inspect your SQL Server database and existing `__EFMigrationsHistory` table; then run in Visual Studio **Tools → NuGet Package Manager → Package Manager Console**:

```powershell
Update-Database -Project RaceDay.Api -StartupProject RaceDay.Api
```

Alternatively, after installing `dotnet-ef` version 8, use:

```powershell
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

The API attempts `Database.Migrate()` on startup (except in the Testing environment). If an existing database was built manually and does not track migrations, **stop and reconcile the migration history**; don't drop tables.

## Start locally (two terminals)

**Terminal 1** (API, development configuration):

```powershell
dotnet run --project RaceDay.Api --launch-profile http
```

API URL: `http://localhost:5062` and Swagger at `http://localhost:5062/swagger`.

**Terminal 2** (MVC):

```powershell
dotnet run --project RaceDay.Web --launch-profile http
```

MVC URL: `http://localhost:5100`. Its `RaceDay.Web/appsettings.json` uses `Api:BaseUrl` of `http://localhost:5062/`.

Build/test commands:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet test RaceDay.Api.Tests/RaceDay.Api.Tests.csproj
```

If a DLL is locked during build, stop the API and MVC processes before rebuilding.

## Azure Blob Storage setup

1. In Azure Portal, create a **Storage account** in a region permitted by your subscription. Standard/LRS is a reasonable student demo option, though charges may apply.
2. Create a Blob container named **`raceday-images`**. Set access to **Private (no anonymous access)**.
3. From Storage account → Access keys, copy the **connection string** (not just the key). Never share it or push it to GitHub.
4. Set it in API User Secrets:

```powershell
dotnet user-secrets set "AzureStorage:ConnectionString" "REPLACE_WITH_AZURE_CONNECTION_STRING" --project RaceDay.Api
```

The application uploads images **MVC → API → Azure**, stores private blob names in SQL Server and displays them via the API/MVC image endpoints. The container need not allow public anonymous access. Event banners support JPG/PNG/GIF/WEBP and profile images JPG/PNG/WEBP, maximum 5 MB. Test both uploads and verify the files in Azure Portal.

## Docker demo: MVC running in a container

Docker Desktop must be running. Run commands from the folder containing `RaceDay.sln`.

```powershell
# Build the website image
docker build -f RaceDay.Web/Dockerfile -t raceday-web:part3 .

# Stop the API if already running on that port, then run it in a terminal on Windows
dotnet run --project RaceDay.Api --launch-profile http --urls http://0.0.0.0:5062

# In another PowerShell window, create the container (remove an old one if needed)
docker rm -f raceday-web
docker run -d --name raceday-web -p 5100:8080 -e Api__BaseUrl=http://host.docker.internal:5062/ raceday-web:part3

docker ps
docker logs raceday-web
```

Visit `http://localhost:5100`. Use `host.docker.internal` because `localhost` inside the container points back to the container. Stop a separately running **local MVC** on port 5100 before running the container. The API remains on Windows and uses SQL Server and Azure secrets.

The repository also includes `docker-compose.yml` as an **optional** three-service demonstration (SQL Server + API + MVC). That setup needs `.env` copied from `.env.example`, valid secrets and a Docker host capable of running SQL Server. It has **not** been tested in this environment. You do not have to use Compose to demonstrate the required MVC Docker container.

## GitHub Actions

`.github/workflows/raceday-ci.yml` checks out source code, installs .NET 8, restores dependencies, builds all projects, runs tests and builds the MVC Docker image. After your first push, open GitHub → Actions and resolve any failing steps. Save an authentic **green CI screenshot** in `docs/` and embed it here:

```markdown
![Successful Part 3 GitHub Actions](docs/REPLACE_WITH_CI_SCREENSHOT.png)
```

The assessment requires **20 meaningful commits per part**. Preserve genuine version history: do not create empty or artificial commits to increase the count.

## Manual testing for submission

- [ ] API Swagger opens; Part 2 tests pass
- [ ] Register and log in as Participant
- [ ] Register and log in as Organiser with secret code
- [ ] Organiser dashboard and event CRUD work
- [ ] Categories are created and visible on an event
- [ ] Participant selects a category and enrols
- [ ] Organiser confirms enrolment
- [ ] Organiser records a result; Participant sees it in My Results
- [ ] Participant profile details and photo upload work
- [ ] Organiser banner upload works and image displays from private Azure container
- [ ] MVC Docker image builds and container logs show it running
- [ ] Docker website logs in and retrieves API data
- [ ] GitHub Actions shows a green run
- [ ] README has real CI screenshot and unlisted YouTube video link
- [ ] At least 20 meaningful Part 3 commits are present, if achieved

## Submission evidence

**GitHub repository:** `REPLACE_WITH_HER_OWN_GITHUB_CLASSROOM_REPOSITORY_LINK`

**Part 3 presentation video (YouTube, Unlisted, student's own voice):** `REPLACE_WITH_HER_VIDEO_LINK`

**CI screenshot:** Add a real image under `docs/` and embed with Markdown as shown above.

Submit the **GitHub repository URL on ARC**, not a ZIP, unless the lecturer's specific ARC instructions explicitly require otherwise. Do not claim Docker, Azure or CI tests succeeded without completing them on the student's computer.

## Troubleshooting

- **JWT key missing:** set `Jwt:Key` in API User Secrets.
- **SQL connection failed:** verify SQL Server/instance name, authentication and the existing database.
- **Migration reports table exists:** inspect `__EFMigrationsHistory` before making changes.
- **Swagger 500:** inspect the actual ASP.NET error, check form upload signatures and the server log.
- **MVC can't reach API:** ensure API is running on port 5062; check `Api:BaseUrl`.
- **Docker connection error:** start Docker Desktop, verify `docker info`, then rebuild.
- **Images don't display:** check Azure user secrets, container Private policy and MVC `/Media/Event/{id}` proxy.
