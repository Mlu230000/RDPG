using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Database Context (SQL Server or InMemory for tests)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }));
}
else
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseInMemoryDatabase("RaceDayDb"));
}

// 2. Add Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// 3. Add Authentication & JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"] ?? "RaceDaySouthAfricaSuperSecretKey2026!MustBe32CharsLong";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaceDayApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RaceDayClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganiserOnly", policy => policy.RequireRole("Organiser"));
    options.AddPolicy("ParticipantOnly", policy => policy.RequireRole("Participant"));
});

// 5. Add Distributed Cache & Session Management (Requirement 2)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 6. Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 8. Swagger / OpenAPI Integration (Requirement 4)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay South Africa - RESTful API",
        Version = "v1",
        Description = "API powering RaceDay: sports event organisation, entry management, and chip timing for running, cycling, and walking events in South Africa.",
        Contact = new OpenApiContact
        {
            Name = "RaceDay Team",
            Email = "support@raceday.co.za",
            Url = new Uri("https://github.com/Mlu230000/RDPG")
        }
    });

    // Enable Bearer Auth in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.EnableAnnotations();
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseCors("AllowAll");

// Swagger in Development and Production for demonstration
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = "swagger"; // available at /swagger
});

app.UseHttpsRedirection();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database migration / ensure created on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database connection not ready on startup; using fallback or waiting for migrations.");
    }
}

app.Run();

// For WebApplicationFactory in integration tests
public partial class Program { }
