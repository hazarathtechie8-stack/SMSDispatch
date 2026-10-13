using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using SmsDispatch.Api.Behaviors;
using SmsDispatch.Api.Data;
using SmsDispatch.Api.Services;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── MediatR — scans this assembly for all IRequestHandler<,> implementations ─
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();

    // Pipeline behaviors run in order for every Command and Query
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

// ── Database ──────────────────────────────────────────────────────────────────

builder.Services.AddDbContext<SmsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SmsDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// ── Azure Service Bus ─────────────────────────────────────────────────────────
var sbConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
var topicName          = builder.Configuration["AzureServiceBus:TopicName"] ?? "sms-dispatch";

builder.Services.AddSingleton(_ => new ServiceBusClient(sbConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(topicName));
builder.Services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

// ── Application Insights (optional — only activates when connection string present) ──
var appInsightsConnection =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ??
    builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(appInsightsConnection))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
        options.ConnectionString = appInsightsConnection);
}

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",")
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins(
              allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

 app.UseSwagger();
 app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
