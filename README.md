# RaceDay – PROG6212 Programming 2B Part 3

RaceDay is an ASP.NET Core 8 event management platform developed for road running, cycling and walking events. The project integrates the existing Part 2 JWT-authenticated REST API with a Part 3 ASP.NET Core MVC frontend, SQL Server database, Azure Blob Storage and Docker support.

## 1. Project Structure

```text
RaceDay.sln
│
├── RaceDay.Api/             REST API, authentication and database access
├── RaceDay.Contracts/       Shared request and response models
├── RaceDay.Api.Tests/       xUnit automated tests
├── RaceDay.Web/             ASP.NET Core MVC frontend
├── .github/workflows/       GitHub Actions workflow
├── docker-compose.yml       Optional Docker Compose configuration
├── README.md
└── .gitignore
```

## 2. Application Architecture

The application uses a layered architecture:

- The browser communicates with the `RaceDay.Web` MVC application.
- The MVC application communicates with `RaceDay.Api` using HTTP requests.
- JWT authentication is used to authenticate requests.
- The API communicates with the SQL Server database through Entity Framework Core.
- Azure Blob Storage is used to store uploaded event banners and profile images.
- Uploaded images are retrieved through the API and MVC media endpoints.

### User Roles

**Organiser**

Organisers can:
- Register and log in.
- View the organiser dashboard.
- Create, edit and delete events.
- Upload event banner images.
- Create, update and delete event categories.
- View participant enrolments.
- Confirm or reject enrolments.
- Record and remove race results.

**Participant**

Participants can:
- Register and log in.
- Browse available events.
- View event details and categories.
- Select a category and enrol in an event.
- View enrolment status.
- View personal race results.
- Update profile information and upload a profile picture.

Public registration creates a Participant account by default. Organiser registration requires a private registration code configured in API User Secrets.

## 3. Technologies Used

- C#
- ASP.NET Core 8
- ASP.NET Core Web API
- ASP.NET Core MVC
- Entity Framework Core 8
- SQL Server / SQL Server Express
- JWT Bearer Authentication
- Azure Blob Storage
- xUnit
- Docker
- Git and GitHub
- GitHub Actions

## 4. Prerequisites

The following software and services are required:

- Visual Studio 2022 with the ASP.NET and web development workload.
- .NET 8 SDK.
- SQL Server or SQL Server Express.
- SQL Server Management Studio (SSMS), recommended.
- Azure subscription and Storage Account for image uploads.
- Docker Desktop for the Docker demonstration.
- Git for Windows.

## 5. Database Configuration

The application uses SQL Server and Entity Framework Core.

Before making database changes, back up any existing RaceDay database.

Open `RaceDay.sln` in Visual Studio and open a terminal in the solution directory.

Configure the API connection string using User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;" --project RaceDay.Api
```

Replace `YOUR_SERVER` with the SQL Server instance on your computer.

Configure the JWT signing key:

```powershell
dotnet user-secrets set "Jwt:Key" "YOUR_UNIQUE_SECURE_JWT_KEY" --project RaceDay.Api
```

Configure the private Organiser registration code:

```powershell
dotnet user-secrets set "Registration:OrganiserCode" "YOUR_PRIVATE_ORGANISER_CODE" --project RaceDay.Api
```

**Important:** Never commit JWT keys, database passwords, Azure connection strings or Organiser registration codes to GitHub.

### Database Migration

The Part 3 migration is:

`20261110190000_AddPart3Features`

It introduces the additional fields needed for event categories, enrolments and image storage.

The migration was successfully applied to the development database.

For a fresh or appropriately prepared database, the migration can be applied using:

```powershell
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

Check the existing `__EFMigrationsHistory` table before applying migrations to an existing database. Do not drop or recreate an existing database without checking its contents.

## 6. Building and Testing

Run the following commands from the folder containing `RaceDay.sln`.

Restore the NuGet packages:

```powershell
dotnet restore RaceDay.sln
```

Build the complete solution:

```powershell
dotnet build RaceDay.sln
```

Run the automated API tests:

```powershell
dotnet test RaceDay.Api.Tests/RaceDay.Api.Tests.csproj
```

### Development Verification

The following checks have been completed successfully:

- The complete RaceDay solution builds successfully.
- All 8 automated API tests pass.
- The Part 3 database migration was applied successfully.
- Participant registration and login work.
- Organiser registration and login work.
- The Participant dashboard and Browse Events pages load.
- The Organiser dashboard and event management functions work.
- Participant event enrolment works.
- Event images upload to the private Azure Blob container and display on the website.

Additional testing of the Docker deployment and GitHub Actions workflow remains part of the final verification.

## 7. Running the Application Locally

The API and MVC website must run in separate terminal windows.

### Terminal 1 – Start the API

From the solution directory:

```powershell
dotnet run --project RaceDay.Api --launch-profile http
```

API address:

`http://localhost:5062`

Swagger documentation:

`http://localhost:5062/swagger`

### Terminal 2 – Start the MVC Website

Open another terminal in the solution directory:

```powershell
dotnet run --project RaceDay.Web --launch-profile http
```

MVC website:

`http://localhost:5100`

Keep both terminals running while using the application.

## 8. Azure Blob Storage Configuration

Azure Blob Storage is used for storing event banner images and participant profile images.

The configured container name is:

`raceday-images`

The container is configured with **Private** access, meaning anonymous public access is disabled.

Configure the Azure connection string through API User Secrets:

```powershell
dotnet user-secrets set "AzureStorage:ConnectionString" "YOUR_AZURE_CONNECTION_STRING" --project RaceDay.Api
```

The connection string must remain private and must never be committed to GitHub.

The application uses the Azure Storage SDK to upload and retrieve images.

Supported image formats include:

- JPG
- PNG
- GIF
- WEBP

The maximum permitted image size is 5 MB.

Azure image uploading and display were tested successfully during development.

## 9. Docker Configuration

Docker support is included through the Dockerfiles in:

```text
RaceDay.Api/Dockerfile
RaceDay.Web/Dockerfile
```

Docker Desktop with Linux containers must be running.

### Build the MVC Docker Image

Run from the solution directory:

```powershell
docker build -f RaceDay.Web/Dockerfile -t raceday-web:part3 .
```

### Run the API Locally

The API must be available to the MVC container:

```powershell
dotnet run --project RaceDay.Api --launch-profile http --urls http://0.0.0.0:5062
```

### Run the MVC Container

Open another PowerShell window:

```powershell
docker rm -f raceday-web
```

Then run:

```powershell
docker run -d --name raceday-web -p 5100:8080 -e Api__BaseUrl=http://host.docker.internal:5062/ raceday-web:part3
```

Check the running container:

```powershell
docker ps
```

View its logs:

```powershell
docker logs raceday-web
```

Open the website:

`http://localhost:5100`

The `host.docker.internal` address allows the MVC container to communicate with the API running on the Windows host.

### Docker Compose

An optional `docker-compose.yml` file is included for running SQL Server, the API and MVC application together.

It requires a correctly configured local `.env` file based on `.env.example`.

**Never upload the real `.env` file to GitHub.** It contains private configuration values.

The complete Docker Compose deployment still requires successful startup and application testing before it can be claimed as verified.

## 10. GitHub Actions

The repository includes a GitHub Actions workflow under:

```text
.github/workflows/raceday-ci.yml
```

The workflow is intended to:

- Check out the source code.
- Install the .NET 8 SDK.
- Restore project dependencies.
- Build the solution.
- Run the automated tests.
- Build the MVC Docker image.

The workflow should be checked under the **Actions** tab on GitHub.

A successful green workflow screenshot should be added to the repository once the workflow has completed successfully.

## 11. Manual Testing Checklist

The following checklist can be used for final verification and assessment evidence.

- [x] API Swagger opens.
- [x] Solution builds successfully.
- [x] All 8 automated API tests pass.
- [x] Database migration applied successfully.
- [x] Participant registration and login.
- [x] Organiser registration and login.
- [x] Participant dashboard and Browse Events pages.
- [x] Organiser dashboard and event creation.
- [x] Participant event enrolment.
- [x] Azure event image upload and display.
- [ ] Participant enrolment status and results verification.
- [ ] Participant profile picture upload verification.
- [ ] Complete Docker deployment verification.
- [ ] GitHub Actions successful workflow screenshot.
- [ ] Presentation video recorded and uploaded.

Only mark additional items as complete after testing them.

## 12. GitHub Repository

**GitHub repository:**

https://github.com/AbuNkabi/RaceDay

**Part 3 branch:**

https://github.com/AbuNkabi/RaceDay/tree/part3

The original Part 2 work is preserved on the `main` branch. The Part 3 implementation has been pushed to the separate `part3` branch.

## 13. Submission Evidence

The final submission should include:

- GitHub repository URL.
- Evidence of successful application functionality.
- Successful GitHub Actions workflow screenshot.
- Docker demonstration evidence, if required.
- Part 3 presentation video uploaded to YouTube as an Unlisted video.
- Any additional evidence required by the lecturer or assessment rubric.

**Presentation video:** Add your own YouTube Unlisted video link here.

**GitHub Actions screenshot:** Add a genuine screenshot after the workflow succeeds.

Example Markdown for the screenshot:

```markdown
![Successful RaceDay GitHub Actions Workflow](docs/raceday-ci-success.png)
```

The assessment requires meaningful development commits. Preserve genuine Git history and do not create artificial commits just to increase the commit count.

## 14. Troubleshooting

**Database connection problems**

- Check that SQL Server is running.
- Verify the SQL Server instance name.
- Check the API connection string in User Secrets.

**JWT authentication problems**

- Verify that `Jwt:Key` is configured in API User Secrets.
- Ensure the JWT issuer and audience configuration matches the application.

**MVC cannot communicate with the API**

- Ensure the API is running.
- Check that the API is listening on port 5062.
- Verify the `Api:BaseUrl` setting.

**Azure image upload problems**

- Check that the Azure connection string is configured.
- Confirm that the `raceday-images` container exists.
- Check that the container is private and the application has permission to access it.

**Docker problems**

- Start Docker Desktop.
- Check Docker using `docker info`.
- Review container output using `docker logs raceday-web`.

---

**Project:** RaceDay  
**Module:** PROG6212 – Programming 2B  
**Submission:** Practical Outcome of Evidence (PoE), Part 3
