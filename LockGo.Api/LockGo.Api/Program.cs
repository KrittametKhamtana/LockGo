using LockGo.Api.Contracts;
using LockGo.Api.Middleware;
using LockGo.Application;
using LockGo.Infrastructure;
using LockGo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Keep the same {error:{code,message}} shape for model-binding/validation
        // failures as for our own AppException hierarchy, instead of the default
        // ValidationProblemDetails shape.
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .SelectMany(kvp => kvp.Value?.Errors ?? [])
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request.";

            return new BadRequestObjectResult(new ErrorResponse(new ErrorDetail("VALIDATION_ERROR", firstError)));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "LockGo API",
        Version = "v1",
        Description = "Find & Reserve Locker API",
    });
});

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();

// Under "Testing", LockGoWebApplicationFactory registers its own InMemory
// DbContext + IUnitOfWork — registering Npgsql here too would give EF Core
// two competing provider configurations for the same context type.
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddRepositories();
}
else
{
    builder.Services.AddInfrastructure(builder.Configuration);
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);
app.UseHttpsRedirection();
app.MapControllers();

// Skipped under the "Testing" environment: WebApplicationFactory-based tests
// swap in an InMemory DbContext that doesn't support relational migrations,
// and seed their own data instead (see LockGoWebApplicationFactory).
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();

// Makes the top-level Program accessible to WebApplicationFactory<Program> in LockGo.Tests.
public partial class Program;
