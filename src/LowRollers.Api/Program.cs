using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Pots;
using LowRollers.Api.Domain.Services;
using LowRollers.Api.Domain.StateMachine;
using LowRollers.Api.Domain.StateMachine.Handlers;
using LowRollers.Api.Features.GameEngine;
using LowRollers.Api.Features.GameEngine.ActionTimer;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (Aspire)
builder.AddServiceDefaults();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Register domain services
builder.Services.AddSingleton<IShuffleService, ShuffleService>();
builder.Services.AddSingleton<IPotManager, PotManager>();
builder.Services.AddSingleton<IHandEventStore, InMemoryHandEventStore>();

// Register phase handlers for state machine
builder.Services.AddSingleton<IHandPhaseHandler, WaitingPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, PreflopPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, FlopPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, TurnPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, RiverPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, ShowdownPhaseHandler>();
builder.Services.AddSingleton<IHandPhaseHandler, CompletePhaseHandler>();
builder.Services.AddSingleton<HandStateMachine>();

// Register game orchestrator
builder.Services.AddSingleton<IGameOrchestrator, GameOrchestrator>();

// TODO: Register ActionTimerService when dependencies are available (core-gameplay-11/12)
// Requires:
//   - IActionTimerBroadcaster implementation (SignalR adapter)
//   - Table provider function (from table management service)
// builder.Services.AddSingleton<IActionTimerService>(sp =>
//     new ActionTimerService(
//         sp.GetRequiredService<IActionTimerBroadcaster>(),
//         sp.GetRequiredService<IGameOrchestrator>(),
//         tableId => tableManager.GetTable(tableId),
//         sp.GetRequiredService<ILogger<ActionTimerService>>()));

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
// app.MapHub<GameHub>("/hubs/game"); // Will be added when GameHub is created

app.Run();
