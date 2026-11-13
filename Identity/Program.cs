using Identity.Context;
using Identity.Contracts.Repositories;
using Identity.Contracts.Services;
using Identity.Model;
using Identity.Repositories;
using Identity.Security;
using Identity.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb")));

// Options
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Repositories
builder.Services.AddScoped<IUserRepository<User>, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Domain + orchestration services
builder.Services.AddSingleton<IPasswordHasherAdapter<User>, IdentityPasswordHasherAdapter<User>>();
builder.Services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<ISecurityNotificationPublisher, MassTransitSecurityNotificationPublisher>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Message broker (security alerts)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
    });
});

// JWT bearer validation for [Authorize] endpoints
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });
// Role-based authorization (roles come from ClaimTypes.Role claims in the access token)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole(AppRoles.Admin))
    .AddPolicy(AuthPolicies.StaffOrAdmin, p => p.RequireRole(AppRoles.Admin, AppRoles.Staff))
    .AddPolicy(AuthPolicies.Customer, p => p.RequireRole(AppRoles.Customer));

// CORS: explicit origins only, credentials allowed so the browser sends/stores the HttpOnly refresh cookie
var allowedOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins ?? [];
if (allowedOrigins.Any(o => o.Contains('*')))
{
    throw new InvalidOperationException("Cors:AllowedOrigins must list exact origins; wildcards cannot be combined with AllowCredentials().");
}
builder.Services.AddCors(options =>
    options.AddPolicy(CorsOptions.PolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowCredentials()
        .WithMethods("GET", "POST")
        .WithHeaders("Authorization", "Content-Type")));

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(CorsOptions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
