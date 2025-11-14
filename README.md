# EPECPS - Employee Performance Evaluation and Career Progression System

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- SQL Server (LocalDB or full)
- Azure AD tenant (for authentication)

### Backend Setup
```powershell
cd backend/Epecps.Api
dotnet restore
dotnet run
```
Backend runs on: `https://localhost:7275`

### Frontend Setup
```powershell
cd frontend/epecps-web
npm install
ng serve
```
Frontend runs on: `http://localhost:4200`

### Database Setup
Update connection string in `backend/Epecps.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EpecpsDb;Trusted_Connection=True;"
}
```

### Azure AD Configuration
Update MSAL settings in `frontend/epecps-web/src/app/core/auth/msal-config.ts` with your Azure AD details.

## Project Structure

```
epecps/
??? backend/
?   ??? Epecps.Api/          # Web API
?   ??? Epecps.Application/  # Business Logic
?   ??? Epecps.Domain/       # Entities & Enums
?   ??? Epecps.Infrastructure/ # Data Access
??? frontend/
    ??? epecps-web/          # Angular App
```

## Features

### Admin Module - Score Templates
- Create/edit evaluation templates
- Manage categories with weight percentages
- Publish templates for use
- Clone and archive templates
- Weight validation (must total 100%)

## API Documentation

Swagger UI available at: `https://localhost:7275/swagger`

## Technology Stack

**Backend:**
- .NET 8
- Entity Framework Core
- SQL Server
- Azure AD Authentication

**Frontend:**
- Angular 19
- TypeScript
- Tailwind CSS
- MSAL for Angular

## License

Proprietary
