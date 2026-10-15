RaceDay REST API

Project Overview

RaceDay is a RESTful Web API developed for the PROG6212 Programming 2B Portfolio of Evidence (PoE). The system is designed to support race event management by allowing users to register, manage profiles, browse events, enrol in events, and access race results according to their roles.

The API is developed using ASP.NET Core Web API with C# and .NET 8, with Microsoft SQL Server used for data storage.
Technologies Used

## Project Objectives

The main objective of the RaceDay REST API is to develop a reliable backend system for managing race events and participants. The application aims to provide a structured way for users to register, authenticate, manage their profiles, and participate in organised race events.

The project focuses on applying the programming concepts and software development techniques covered in PROG6212 Programming 2B.

The key objectives include:

- Developing a RESTful API using ASP.NET Core Web API and C#.
- Implementing secure user registration and login functionality.
- Supporting Organiser and Participant roles.
- Managing user profiles and personal information.
- Providing CRUD operations for race events.
- Allowing users to browse events and event categories.
- Enabling Participants to enrol in race events.
- Allowing Organisers to manage enrolments and record race results.
- Using Entity Framework Core to interact with the SQL Server database.
- Documenting API endpoints using Swagger/OpenAPI.
- Implementing automated tests using xUnit.
- Using Git and GitHub to track changes throughout development.



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

Using Swagger

When the API is running, open the Swagger UI at the address displayed in the application's launch settings or console output.

Swagger allows users to inspect available endpoints, view request and response formats, and send requests to the API.

For endpoints that require authentication, obtain a valid access token through the login endpoint and provide it using Swagger's Authorize feature.

Testing

The RaceDay.Api.Tests project is intended for automated testing.

To run the tests from the solution directory, execute:

dotnet test

Review the test results to confirm that the tests pass.

Version Control

Git is used to track changes during development. The source code can be hosted on GitHub, where commits document the implementation and development progress.

Author

Developed as part of the PROG6212 Programming 2B Portfolio of Evidence.

Project Status

The project is under development. Features, endpoints, authentication, database operations, and automated tests should be verified before the final submission

 Project Objectives

The main objective of the RaceDay REST API is to develop a reliable backend system for managing race events and participants. The application aims to provide a structured way for users to register, authenticate, manage their profiles, and participate in organised race events.

The project focuses on applying the programming concepts and software development techniques covered in PROG6212 Programming 2B.

The key objectives include:

- Developing a RESTful API using ASP.NET Core Web API and C#.
- Implementing secure user registration and login functionality.
- Supporting Organiser and Participant roles.
- Managing user profiles and personal information.
- Providing CRUD operations for race events.
- Allowing users to browse events and event categories.
- Enabling Participants to enrol in race events.
- Allowing Organisers to manage enrolments and record race results.
- Using Entity Framework Core to interact with the SQL Server database.
- Documenting API endpoints using Swagger/OpenAPI.
- Implementing automated tests using xUnit.
- Using Git and GitHub to track changes throughout development.

The application will be developed in stages, with each feature implemented and tested before being considered complete.

