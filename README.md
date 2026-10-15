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

API documentation through Swagger.

Automated testing.

Features should be considered complete once they have been implemented and tested successfully.
