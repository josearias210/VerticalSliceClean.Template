using Acme.Api;
using Acme.AppHost;
using Acme.AppHost.Extensions;
using Acme.Application;
using Acme.Infrastructure;
using Acme.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.ConfigureSerilog();

// Register services by layer
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation()
    .AddHost(builder.Configuration);

var app = builder.Build();

// Use Serilog request logging
app.UseSerilogRequestLogging();

// Initialize database
await app.InitializeDatabaseAsync();

// Configure middleware pipeline
app.ConfigurePipeline();

await app.RunAsync();
