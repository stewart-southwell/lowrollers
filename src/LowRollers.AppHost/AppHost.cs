var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for session state and real-time caching
var redis = builder.AddRedis("redis");

// Add PostgreSQL for game state and hand history
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("lowrollers");

// Add the API project
var api = builder.AddProject<Projects.LowRollers_Api>("api")
    .WithReference(redis)
    .WithReference(postgres)
    .WithExternalHttpEndpoints();

builder.Build().Run();
