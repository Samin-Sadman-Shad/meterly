---
name: implement-jwt-authentication
description: implement jwt authentication with rotating refresh tokens. Add services and endpoints for register, login, request refresh token, revoke refresh token and logout
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(dotnet new:*), Bash(dotnet sln:*), Bash(dotnet add:*), Bash(dotnet build:*), Bash(dotnet test:*), Bash(dotnet user-secrets:*), Bash(ls:*), Bash(mkdir:*)
---
## Target Language & Stack
- **Default Stack:** C# / ASP.NET Core Web API (Controllers + Services + Repositories) with Entity Framework Core and Microsoft Identity `PasswordHasher<TUser>`.
- **Override:** If a target language or framework is supplied in `$ARGUMENTS`, translate the exact layered architecture, endpoint sequences, domain behaviors, and security rules defined in the `jwt-auth` skill into `$ARGUMENTS`.

## Execution Instructions
1. **Load Skill & Reference Scripts:** Invoke the `jwt-auth` skill and read the files in `.claude/skills/jwt-auth/scripts/` 
2. **Inspect Workspace:** Use `Glob` and `Read` to inspect the target project structure, existing `DbContext`, DI container (`Program.cs`), and NuGet packages (`System.IdentityModel.Tokens.Jwt`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core`).
3. **Implement Layered Architecture (Follow `JwtAuthReference.cs`):**
   - **Domain Models:** Implement `User` (supporting `[Authorize(Roles = "...")]` claims) and `RefreshToken` (including `IsActive()`, `IsExpired()`, and `Revoke()` member methods).
   - **Repositories:** Implement `IUserRepository<TUser>` and `IRefreshTokenRepository` backed by Entity Framework Core.
   - **Security Adapters & Notifications:** Implement `IPasswordHasherAdapter<TUser>` wrapping `PasswordHasher<TUser>` (for hashing, verification, and `SuccessRehashNeeded` upgrades) and `ISecurityNotificationPublisher` for publishing token-reuse alerts to a message broker.
   - **Services:** Implement `IAccessTokenService`, `IRefreshTokenService`, `IUserService`, and `IAuthService` (containing `IssueTokenAsync()` and all endpoint business logic).
   - **Controllers:** Implement `AuthController` (`register`, `login`, `refresh`, `logout`, `revoke-all`, `change-password`) injected strictly with `IAuthService`.
4. **Configure Middleware & CORS:** Configure JWT Bearer authentication, Role-based Authorization, and CORS with explicit origins and `.AllowCredentials()`.
5. **Implement & Run Tests:** Add the reuse-detection test suite from `TokenReuseTests.cs` to the test project and run `dotnet build` and `dotnet test` to verify all scenarios pass.