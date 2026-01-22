# SSO Authentication Module

ASP.NET Core Web API project for Single Sign-On authentication system.

## Description

Project provides API endpoints for managing:

- User authentication (Login, Register)
- JWT tokens (Access Token, Refresh Token)
- Client management
- External authentication (OAuth)

## Project Structure

- **Controllers**: API endpoints
- **Services**: Business logic
- **Models**: Database entities
- **DTOs**: Data transfer objects
- **Data**: Database context
- **Utilities**: Helper functions (JWT, etc.)

## Technology Stack

- .NET 10
- Entity Framework Core
- JWT Authentication
- SQL : PostgrestSQL(Neon cloud)

## Setup

1. Clone project
2. Configure connection string in `appsettings.json`
3. Run migrations: `dotnet ef database update`
4. Start server: `dotnet run`

## API Endpoints

- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/refresh` - Refresh token
- `GET /api/client` - Get client list
- `POST /api/external/callback` - External auth callback
