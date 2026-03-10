using Hangfire;
using Hangfire.PostgreSql;
using LaBot.Api.Data;
using LaBot.Api.Hubs;
using LaBot.Api.Jobs;
using LaBot.Api.Middleware;
using LaBot.Api.Models.Domain;
using LaBot.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text;

// Bootstrap Serilog immediately for startup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LaBot Kripto API...");

    var builder = WebApplication.CreateBuilder(args);

    // ===== Serilog =====
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/labotkripto-.log", rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    });

    // ===== Database =====
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(3)));

    // ===== Identity =====
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // ===== JWT Authentication =====
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret is not configured.");
    var key = Encoding.UTF8.GetBytes(jwtSecret);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        // Support JWT in SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

    // ===== CORS =====
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ===== Hangfire =====
    var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseRecommendedSerializerSettings()
              .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConn),
                  new PostgreSqlStorageOptions { SchemaName = "hangfire" });
    });
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.ServerName = "LaBot-Hangfire";
    });

    // ===== SignalR =====
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
    });

    // ===== HttpClient =====
    builder.Services.AddHttpClient();
    builder.Services.AddHttpClient<BitgetApiClient>();

    // ===== Application Services =====
    builder.Services.AddScoped<ISignalGeneratorService, SignalGeneratorService>();
    builder.Services.AddScoped<IChartAnalysisService, ChartAnalysisService>();
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // Hangfire job classes
    builder.Services.AddScoped<MarketDataPollingJob>();
    builder.Services.AddScoped<SignalEvaluationJob>();

    // Bitget WebSocket as hosted service
    builder.Services.AddSingleton<BitgetWebSocketService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<BitgetWebSocketService>());

    // ===== Controllers =====
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddEndpointsApiExplorer();

    // ===== Swagger / OpenAPI =====
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "LaBot Kripto API",
            Version = "v1",
            Description = "Crypto trading signal platform API"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token. Example: Bearer {token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // ===== Static Files / Uploads =====
    builder.Services.AddDirectoryBrowser();

    var app = builder.Build();

    // ===== Database Migration & Seeding =====
    await InitializeDatabaseAsync(app);

    // ===== Middleware Pipeline =====
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LaBot Kripto API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseStaticFiles();

    // Ensure uploads directory exists and is accessible
    var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "charts");
    Directory.CreateDirectory(uploadsPath);

    app.UseCors("AllowFrontend");

    app.UseMiddleware<RateLimitingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<SubscriptionTierMiddleware>();

    // Hangfire Dashboard (admin only in production)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    app.MapControllers();
    app.MapHub<SignalHub>("/hubs/signals");

    // ===== Hangfire Recurring Jobs =====
    RecurringJob.AddOrUpdate<MarketDataPollingJob>(
        "market-data-polling",
        job => job.ExecuteAsync(),
        Cron.Minutely);

    RecurringJob.AddOrUpdate<SignalEvaluationJob>(
        "signal-evaluation",
        job => job.ExecuteAsync(),
        "*/5 * * * *"); // Every 5 minutes

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ===== Database initialization =====
async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        await SeedDataAsync(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database initialization. The app will continue but may not work correctly.");
    }
}

async Task SeedDataAsync(IServiceProvider services, Microsoft.Extensions.Logging.ILogger logger)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var db = services.GetRequiredService<AppDbContext>();
    var config = services.GetRequiredService<IConfiguration>();

    // Seed roles
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Created role: {Role}", role);
        }
    }

    // Seed admin user
    var adminEmail = config["Seed:AdminEmail"] ?? "admin@labotkripto.com";
    var adminPassword = config["Seed:AdminPassword"] ?? "Admin@123456";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            SubscriptionTier = SubscriptionTier.ProPlus,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            await userManager.AddToRoleAsync(adminUser, "User");
            logger.LogInformation("Admin user seeded: {Email}", adminEmail);
        }
        else
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // Seed initial feature gates
    var featureGates = new[]
    {
        new { name = "signals_access", tier = SubscriptionTier.Free, enabled = true, delay = 0 },
        new { name = "chart_analysis", tier = SubscriptionTier.Pro, enabled = true, delay = 0 },
        new { name = "spot_trading", tier = SubscriptionTier.ProPlus, enabled = true, delay = 0 },
        new { name = "futures_trading", tier = SubscriptionTier.ProPlus, enabled = true, delay = 0 },
        new { name = "auto_signals", tier = SubscriptionTier.Pro, enabled = true, delay = 15 }
    };

    foreach (var gateInfo in featureGates)
    {
        if (!await db.FeatureGates.AnyAsync(f => f.FeatureName == gateInfo.name))
        {
            db.FeatureGates.Add(new FeatureGate
            {
                FeatureName = gateInfo.name,
                MinTier = gateInfo.tier,
                IsEnabled = gateInfo.enabled,
                DelayMinutes = gateInfo.delay,
                UpdatedBy = adminUser?.Id,
                UpdatedAt = DateTime.UtcNow
            });
            logger.LogInformation("Seeded feature gate: {Name}", gateInfo.name);
        }
    }

    await db.SaveChangesAsync();
}

// ===== Hangfire Authorization Filter =====
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Use reflection to get HttpContext from AspNetCoreDashboardContext
        var httpContextProp = context.GetType().GetProperty("HttpContext");
        if (httpContextProp?.GetValue(context) is Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            return httpContext.User.Identity?.IsAuthenticated == true
                && httpContext.User.IsInRole("Admin");
        }
        return false;
    }
}

public partial class Program { }

