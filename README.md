RaceDay REST API

Project Overview

RaceDay is a RESTful Web API developed for the PROG6212 Programming 2B Portfolio of Evidence (PoE). The system is designed to support race event management by allowing users to register, manage profiles, browse events, enrol in events, and access race results according to their roles.

The API is developed using ASP.NET Core Web API with C# and .NET 8, with Microsoft SQL Server used for data storage.
Technologies Used

C#

ASP.NET Core Web API (.NET 8)

Entity Framework Core 8

Microsoft SQL Server Express

Swagger / OpenAPI

xUnit for automated testing

Git and GitHub for version control

Solution Structure

The solution contains the following projects:

RaceDay.Api — Contains the API controllers, models, database context, authentication configuration, and application setup.

RaceDay.Contracts — Contains request and response DTOs used to exchange data with the API.

RaceDay.Api.Tests — Contains automated tests for the API.

Main Features

The planned functionality includes:

User registration and login.

Organiser and Participant roles.

Viewing and updating user profiles.

Creating, viewing, updating, and deleting race events.

Browsing event categories.

Enrolling Participants in events.

Managing enrolments.

Recording and viewing race results.
Database

The application uses Microsoft SQL Server Express and Entity Framework Core.

The database is named RaceDayDb. The connection string is configured in appsettings.json.

For security, do not commit passwords, private keys, access tokens, or other sensitive configuration values to the repository.
API documentation through Swagger.

Automated testing.
Getting Started

Prerequisites

Ensure that the following are installed:

Visual Studio 2022

.NET 8 SDK

SQL Server Express

SQL Server Management Studio (SSMS), if required for database management

Git

Setup Instructions

Clone or download the repository.

Open the RaceDay.sln solution in Visual Studio 2022.

Check the SQL Server connection string in RaceDay.Api/appsettings.json.

Ensure that SQL Server Express is running and that the configured database is accessible.

Open the Package Manager Console or a terminal in the solution directory.

Apply the Entity Framework Core migrations to create or update the database.

Build the solution.

Run the RaceDay.Api project.
Features should be considered complete once they have been implemented and tested successfully.

Running Database Migrations

If the database has not yet been created or updated, use the following command from the solution directory, adjusting the project paths if necessary:

dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api

Ensure the EF Core tools are installed before running this command.
