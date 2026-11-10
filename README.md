# RaceDay REST API

**Student:** Abulele Nkabi  
**Student Number:** ST10483049  
**Module:** PROG6212 – Programming 2B  
**Portfolio:** Part 2

## 1. Project Overview

RaceDay is a RESTful Web API developed for the PROG6212 Programming 2B Portfolio of Evidence. The system supports race event management by allowing users to register, authenticate, manage profiles, browse events, enrol in events and access race results according to their roles.

The API is developed using ASP.NET Core Web API with C# and .NET 8. Microsoft SQL Server Express is used for data storage.

## 2. Technologies Used

- C#
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core 8
- Microsoft SQL Server Express
- Swagger / OpenAPI
- xUnit
- Git and GitHub
- GitHub Actions

## 3. Project Objectives

The main objective of RaceDay is to provide a structured backend system for managing race events and participants.

The project demonstrates the following programming and software development concepts:

- Developing a RESTful API using ASP.NET Core Web API.
- Implementing user registration and login.
- Supporting Organiser and Participant roles.
- Implementing authentication and role-based authorisation.
- Managing user profiles.
- Providing CRUD operations for race events.
- Enabling users to browse available events.
- Supporting participant enrolments.
- Managing race results.
- Using Entity Framework Core to communicate with SQL Server.
- Documenting API endpoints using Swagger.
- Implementing automated tests using xUnit.
- Tracking development using Git and GitHub.

## 4. Solution Structure

The solution contains three projects:

- **RaceDay.Api** – Contains the API controllers, database models, database context, authentication configuration, migrations and application setup.
- **RaceDay.Contracts** – Contains the request and response Data Transfer Objects (DTOs) used to exchange information with the API.
- **RaceDay.Api.Tests** – Contains automated tests for the application.

## 5. Main Features

The API includes the following functionality:

- User registration and login.
- JWT-based authentication.
- Organiser and Participant roles.
- Profile viewing and updating.
- Public event browsing.
- Organiser event creation, updating and deletion.
- Viewing events created by the authenticated Organiser.
- Participant enrolment management.
- Race result management.
- Role-based access to protected API endpoints.

Organisers can manage their own events, while authenticated users can access protected functionality according to their permissions.

## 6. API Architecture

RaceDay follows a structured architecture that separates application responsibilities into different components.

The **API controllers** handle incoming HTTP requests, process application operations and return appropriate HTTP responses.

The **Models** represent the application's database entities.

The **RaceDayDbContext** provides access to the database through Entity Framework Core.

The **Contracts** project contains DTOs that define the information accepted and returned by the API. This separates API request and response structures from database entities.

The **Migrations** folder contains Entity Framework Core database migrations used to create and update the database schema.

The **Tests** project verifies selected application functionality through automated tests.

## 7. Database

The application uses Microsoft SQL Server Express with Entity Framework Core 8.

The database is named `RaceDayDb`.

The database connection string and JWT signing key are configured using development secrets or environment-specific configuration. Sensitive values such as passwords, signing keys and access tokens must not be committed to GitHub.

### Database migrations

The project uses Entity Framework Core migrations to manage database changes.

To apply migrations, open a terminal in the solution directory and run:

```bash
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

Ensure that the .NET Entity Framework Core tools are installed and that the SQL Server connection settings are configured before running this command.

## 8. Getting Started

### Prerequisites

Install the following software:

- Visual Studio 2022
- .NET 8 SDK
- SQL Server Express
- SQL Server Management Studio (SSMS), if required
- Git

### Setup instructions

1. Clone or download the repository.
2. Open `RaceDay.sln` in Visual Studio 2022.
3. Ensure that SQL Server Express is running.
4. Configure the database connection string and JWT signing key using User Secrets or environment-specific configuration.
5. Apply the Entity Framework Core migrations.
6. Build the solution.
7. Run the `RaceDay.Api` project.

### Configure development secrets

From the solution directory, configure the database connection string and JWT key using the .NET User Secrets feature.

Example commands (replace the placeholders with your own values):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SQL_SERVER_CONNECTION_STRING" --project RaceDay.Api

dotnet user-secrets set "Jwt:Key" "YOUR_SECURE_JWT_SIGNING_KEY" --project RaceDay.Api
```

Do not use the example placeholders as actual credentials.

## 9. Using Swagger

When the API is running, open the Swagger UI using the address displayed by the application.

Swagger provides an interface for:

- Viewing available API endpoints.
- Inspecting request and response formats.
- Sending HTTP requests.
- Testing authentication-protected endpoints.

To access protected endpoints:

1. Register a user using the registration endpoint.
2. Log in using the login endpoint.
3. Copy the returned JWT access token.
4. Select **Authorize** in Swagger.
5. Enter the token using the format expected by the Swagger configuration.
6. Test the protected endpoints according to the user's role.

## 10. Automated Testing

The project uses xUnit for automated testing.

To execute the tests, open a terminal in the solution directory and run:

```bash
dotnet test RaceDay.sln
```

To build the complete solution, run:

```bash
dotnet build RaceDay.sln
```

The automated tests cover selected DTO and authentication functionality.

## 11. Continuous Integration

GitHub Actions is configured to automatically restore dependencies, build the solution and execute automated tests when changes are pushed to the configured branches or when pull requests are created.

This helps verify that the solution builds and that the automated tests pass.

## 12. Version Control

Git is used to track changes throughout development.

The source code is hosted on GitHub:

[RaceDay GitHub Repository](https://github.com/AbuNkabi/RaceDay)

The repository contains the solution projects, application source code, tests, database migrations and GitHub Actions workflow.

## 13. Video Presentation

The project demonstration and presentation can be viewed using the following link:

[Watch the RaceDay Video Presentation](https://youtu.be/tEhAEbx6l-w?si=w8cgeLkU6BnJqVy)

## 14. Author

**Abulele Nkabi**  
**Student Number:** ST10483049  
**Module:** PROG6212 – Programming 2B

## 15. Project Status

The RaceDay REST API implementation has been completed for this portfolio submission. The solution has been built and tested locally, and the GitHub Actions workflow has successfully completed its build and automated test process.
