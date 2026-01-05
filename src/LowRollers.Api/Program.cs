using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Pots;
using LowRollers.Api.Domain.Services;
using LowRollers.Api.Domain.StateMachine;
using LowRollers.Api.Domain.StateMachine.Handlers;
using LowRollers.Api.Features.GameEngine;
using LowRollers.Api.Features.GameEngine.ActionTimer;
using LowRollers.Api.Features.GameEngine.Broadcasting;
using LowRollers.Api.Features.GameEngine.Connections;
using LowRollers.Api.Features.GameEngine.Showdown;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (Aspire)
builder.AddServiceDefaults();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Register domain services
builder.Services.AddSingleton<IShuffleService, ShuffleService>();
builder.Services.AddSingleton<IPotManager, PotManager>();
builder.Services.AddSingleton<IHandEventStore, InMemoryHandEventStore>();
builder.Services.AddSingleton<IHandEvaluationService, HandEvaluationService>();

// Register phase handlers for state machine
builder.Services.AddSingleton<IHandPhaseHandler, WaitingPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, PreflopPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, FlopPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, TurnPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, RiverPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, ShowdownPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, CompletePhaseHandler>();
builder.Services.AddSingleton<HandStateMachine>();

// Register showdown handler
builder.Services.AddSingleton<IShowdownHandler, ShowdownHandler>();

// Register game orchestrator
builder.Services.AddSingleton<IGameOrchestrator, GameOrchestrator>();

// Register table manager (in-memory for development)
// TODO: Replace with Redis/database-backed implementation for production
builder.Services.AddSingleton<ITableManager, InMemoryTableManager>();

// Register table provider function for services that need it
builder.Services.AddSingleton<Func<Guid, Table?>>(sp =>
{
    var tableManager = sp.GetRequiredService<ITableManager>();
    return tableId => tableManager.GetTable(tableId);
});

// Register action timer broadcaster (SignalR adapter)
builder.Services.AddSingleton<IActionTimerBroadcaster, SignalRActionTimerBroadcaster>();

// Register action timer service
builder.Services.AddSingleton<IActionTimerService, ActionTimerService>();

// Register connection manager for tracking SignalR connections
builder.Services.AddSingleton<IConnectionManager, InMemoryConnectionManager>();

// Register game state sanitizer (per-player view generation)
builder.Services.AddSingleton<IGameStateSanitizer, GameStateSanitizer>();

// Register game state broadcaster (SignalR adapter with per-player sanitization)
builder.Services.AddSingleton<IGameStateBroadcaster, SignalRGameStateBroadcaster>();

// Add OpenAPI
builder.Services.AddOpenApi();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Map SignalR hubs
app.MapHub<GameHub>("/hubs/game");

app.Run();
