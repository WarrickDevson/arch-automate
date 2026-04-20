using ArchAutomate.AI.Services;
using ArchAutomate.Api.Middleware;
using ArchAutomate.BIM.Engines;
using ArchAutomate.CAD.Generators;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.NameTranslation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// PostgreSQL / EF Core
// NpgsqlDataSourceBuilder registers PostgreSQL enum types at the driver level so
// EF Core can read/write project_status, stakeholder_role etc. as native PG enums.
var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured."));
// NpgsqlSnakeCaseNameTranslator converts PascalCase members to snake_case so
// e.g. ProjectStatus.InProgress maps to the PostgreSQL enum value "in_progress".
var snakeCase = new NpgsqlSnakeCaseNameTranslator();
npgsqlDataSourceBuilder.MapEnum<ProjectStatus>("project_status", nameTranslator: snakeCase);
npgsqlDataSourceBuilder.MapEnum<StakeholderRole>("stakeholder_role", nameTranslator: snakeCase);
npgsqlDataSourceBuilder.MapEnum<RejectionCategory>("rejection_category", nameTranslator: snakeCase);
npgsqlDataSourceBuilder.MapEnum<RejectionStatus>("rejection_status", nameTranslator: snakeCase);
var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(npgsqlDataSource));

// JWT Authentication — Supabase ECC P-256 tokens validated via JWKS auto-discovery.
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url is not configured.");

var supabaseAuthBase = $"{supabaseUrl.TrimEnd('/')}/auth/v1";

// Fetch Supabase JWKS once at startup — bypasses OIDC discovery entirely so
// there is no issuer/metadata mismatch. Supabase rarely rotates keys; restart
// the API if they ever do.
using var jwksClient = new HttpClient();
var jwksJson = jwksClient
    .GetStringAsync($"{supabaseAuthBase}/.well-known/jwks.json")
    .GetAwaiter().GetResult();
var signingKeys = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson).GetSigningKeys();

Log.Information("Loaded {Count} Supabase signing key(s) from JWKS", signingKeys.Count());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = [supabaseAuthBase, supabaseAuthBase + "/"],
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Log.Warning("JWT authentication failed: {Error}",
                    ctx.Exception.GetBaseException().Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                if (ctx.AuthenticateFailure != null)
                    Log.Warning("JWT challenge — no valid bearer token. Failure: {Error}",
                        ctx.AuthenticateFailure.GetBaseException().Message);
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5289", "https://localhost:5289"])
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Domain services
builder.Services.AddScoped<ZoningEngine>();
builder.Services.AddScoped<Sans10400Engine>();
builder.Services.AddScoped<CouncilTableGenerator>();
builder.Services.AddScoped<PdfOcrService>();
builder.Services.AddScoped<RejectionParserService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ArchAutomate API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
