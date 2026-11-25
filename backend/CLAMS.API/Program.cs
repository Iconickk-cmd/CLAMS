using Microsoft.AspNetCore.Authorization;

using System.Text;
using CLAMS.API.Services;
using CLAMS.Infrastructure.Data;
using CLAMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers and hosted services.
builder.Services.AddControllers();

builder.Services.AddHostedService<ScheduledPositionAssignmentService>();
builder.Services.AddHostedService<MonthlyLeaveAccrualService>();

// Leave management services.
builder.Services.AddScoped<WorkingDayCalculator>();
builder.Services.AddScoped<LeaveAllocationService>();
builder.Services.AddScoped<LeaveCancellationService>();

// PostgreSQL database.
builder.Services.AddDbContext<CLAMSHRDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CLAMSDatabase")
    )
);

// ASP.NET Core Identity.
builder.Services
    .AddIdentity<CLAMSApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<CLAMSHRDbContext>()
    .AddDefaultTokenProviders();

// JWT configuration.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

var signingKey = new SymmetricSecurityKey(
    Convert.FromBase64String(jwtKey)
);

// Authentication.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultForbidScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata =
            !builder.Environment.IsDevelopment();

        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),

                RoleClaimType = "role",
                NameClaimType = "name"
            };
    });

// Authorization.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// OpenAPI.
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Angular frontend CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("CLAMSFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Build application.
var app = builder.Build();

// Seed application security roles once.
await CLAMSRoleSeeder.SeedAsync(app.Services);

// Create initial administrator when credentials are supplied.
await CLAMSAdminBootstrapper.SeedAsync(app.Services);

// Development OpenAPI endpoint.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTP request pipeline.
app.UseCors("CLAMSFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

