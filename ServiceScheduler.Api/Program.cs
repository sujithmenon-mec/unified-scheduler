using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using ServiceScheduler.Api.Application;
using ServiceScheduler.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var schedulerConnectionString = builder.Configuration.GetConnectionString("SchedulerDb")
    ?? throw new InvalidOperationException("Connection string 'SchedulerDb' was not found.");

builder.Host.UseSerilog((_, services, configuration) => configuration
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed.",
            Detail = "One or more request values are invalid."
        };
        problem.Extensions["code"] = "VALIDATION_ERROR";
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddDbContext<SchedulerDbContext>(options =>
    options.UseNpgsql(schedulerConnectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SchedulerMetrics>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddNpgSql(schedulerConnectionString, name: "postgresql");
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("ServiceScheduler")
        .AddConsoleExporter());

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

app.Run();


public partial class Program { }