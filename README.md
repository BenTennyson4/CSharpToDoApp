# Project Title: C# To-Do List App

# Table of Contents
- Installation Instructions
- Usage
# C# To-Do List App

A simple minimal web API and static frontend for a daily to-do list built with .NET (minimal APIs) and SQL Server. The project exposes endpoints for user authentication, creating lists, inserting/updating tasks, retrieving lists for a date, and deleting lists. A small static frontend in `wwwroot` provides the client UI.

## Table of Contents
- [Purpose](#purpose)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Installation (Local)](#installation-local)
- [Installation (Docker)](#installation-docker)
- [Running the App](#running-the-app)
- [API Endpoints](#api-endpoints)
- [Usage Examples](#usage-examples)
- [Development notes](#development-notes)
- [Author](#author)

## Purpose

This project is a lightweight To-Do list application demonstrating:

- A minimal .NET web API (no controllers — endpoints declared in `Program.cs`).
- Simple ADO.NET usage for SQL Server via `Microsoft.Data.SqlClient`.
- Static frontend served from `wwwroot`.
- Docker support for containerized deployment.

It is suitable as a learning project, a starting point for a personal task manager, or as a base for adding authentication and persistence improvements.

## Features

- Create and authenticate simple user accounts (passwords are hashed).
- Create or upsert a list for a date and add/update tasks in bulk.
- Retrieve a list and its tasks for a given date.
- Delete a list and its tasks.
- Static HTML/CSS frontend under `wwwroot`.

## Prerequisites

- .NET SDK (recommended: 7+ / 8+ compatible — project targets .NET 9 binary in `bin/` but please use the SDK matching your environment).
- SQL Server (or Azure SQL) accessible to the app.
- Docker & Docker Compose (optional, for containerized deployment).

## Configuration

The app reads a `DBConnection` section from `appsettings.json`. The `DBConnectionUtility` (in `DBConnectionUtil.cs`) will choose between `Server_Host` and `Server_Docker` depending on the `DOTNET_RUNNING_IN_DOCKER` environment variable.

Example `appsettings.json` snippet (replace with your values):

```json
{
   "DBConnection": {
      "Server_Host": "localhost,1433",
      "Server_Docker": "mssqlserver,1433",
      "Database": "ToDoDb",
      "IntegratedSecurity": "False",
      "Encrypt": "True",
      "TrustServerCertificate": "True",
      "UserID": "sa",
      "Password": "YourStrong!Passw0rd"
   }
}
```

Notes:

- If `UserID` is provided, the connection will use SQL authentication; otherwise Integrated Security will be used.
- Set the environment variable `DOTNET_RUNNING_IN_DOCKER=true` in your container to make the app pick `Server_Docker`.

Database schema (high-level — these are the tables/columns the app expects):

- `AppUser` (Username, UserPassword)
- `List` (ListID, UserID, ListCompleted, ListCreatedAt)
- `Task` (TaskID, Completed, Priority, TaskText, TaskName, ListID, ListPos, TaskCreatedAt)

The project does not include migration scripts; create these tables manually or use your preferred migration tool.

## Installation (Local)

1. Clone the repository and enter the project folder:

```powershell
git clone https://github.com/BenTennyson4/CSharpToDoApp.git; cd C#ToDoApp/C#ToDoApp
```

2. Update `appsettings.json` with your database connection info.

3. Restore and run the app:

```powershell
dotnet restore
dotnet run
```

By default the app serves static files (index.html in `wwwroot`) and exposes the API. In development mode Swagger UI is available.

## Installation (Docker)

This repository includes a `Dockerfile` and `compose.yaml` for containerized deployment.

1. Build and start containers using Docker Compose (from the project root where `compose.yaml` is located):

```powershell
docker-compose build
docker-compose up -d
```

2. Ensure environment variables and `appsettings.json` inside the container are configured for your SQL Server container or external DB.

3. Open the app via the container's published port (or use Docker Desktop to open the app link).

## Running the App

- The app serves static files from `wwwroot` (the frontend HTML lives there).
- The minimal API endpoints are declared in `Program.cs` (see the section below).

## API Endpoints

The app exposes the following endpoints (all under the root path):

- POST /login — body: { username, password } → returns true/false or error.
- POST /createAccount — body: { username, password } → creates user with hashed password.
- PUT /putList — body: { listID, userID } → creates a list record if not exists.
- PUT /putTasks — body: [ { task fields... }, ... ] → upserts tasks for a list (bulk).
- GET /getList?Date=YYYY-MM-DD — returns tasks for the list created on that date.
- DELETE /deleteList — deletes the current list (implementation uses current date / ListID formation in code — review `Program.cs`).

Refer to `Program.cs` for exact DTO shapes (records at the bottom of the file).

## Usage Examples

Basic curl-like examples (replace host/port):

Login:

```powershell
curl -Method Post -Uri https://localhost:5001/login -Body (@{ username='bob'; password='pass' } | ConvertTo-Json) -ContentType 'application/json' -UseBasicParsing
```

Get list for a date:

```powershell
curl -Method Get -Uri "https://localhost:5001/getList?Date=2025-10-06"
```

Upsert tasks (example JSON body):

```json
[ { "taskID": 0, "priority": 1, "taskText": "Buy milk", "taskName": "Groceries", "completed": false, "listID": 20251006, "listPosition": 0 } ]
```

Use the static HTML pages in `wwwroot` for a simple UI.

## Development notes

- Password hashing is implemented in `PasswordHasher.cs`.
- Database connections are created by `DBConnectionUtility.cs` and expect the `DBConnection` section in config.
- The codebase uses minimal APIs — extending or adding controllers is straightforward if you prefer MVC.

If you'd like, I can add:

- SQL schema creation scripts.
- A docker-compose configuration that includes a SQL Server container and example environment variables.
- Unit tests for the database utility and password hasher.

## Author

Ben Tennyson
