using System.Diagnostics.Metrics;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.AspNetCore;
using OpenTelemetry.Trace;
using McpCfdi.Api.Mcp;
using McpCfdi.Api.Middleware;
using McpCfdi.Application.Behaviours;
using McpCfdi.Application.Interfaces;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Infrastructure.Catalogs;
using McpCfdi.Infrastructure.Cryptography;
using McpCfdi.Infrastructure.Messaging;
using McpCfdi.Infrastructure.Persistence;
using McpCfdi.Infrastructure.Xml;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .WriteTo.OpenTelemetry()
    .Enrich.WithProperty("Service", ctx.Configuration["Service"] ?? "McpCfdi.Api"));

// --- MediatR with pipeline behaviours ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IApplicationEventPublisher).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
});

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssembly(typeof(IApplicationEventPublisher).Assembly);

// --- EF Core with Npgsql ---
builder.Services.AddDbContext<McpCfdiDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("McpCfdiDb")));

// --- Event publisher ---
builder.Services.AddScoped<IApplicationEventPublisher, MassTransitEventPublisher>();

// --- CFDI Domain Services ---
builder.Services.AddScoped<ICfdiSerializer, CfdiXmlSerializer>();
builder.Services.AddScoped<ICadenaOriginalGenerator, XsltCadenaOriginalGenerator>();
builder.Services.AddScoped<ISelloDigitalService, RsaSelloDigitalService>();
builder.Services.AddScoped<ICatalogoSatService, CatalogoSatService>();

// --- MassTransit (InMemory for development) ---
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// --- JWT Bearer Authentication ---
builder.Services.AddAuthentication()
    .AddJwtBearer(opt =>
    {
        opt.Authority = builder.Configuration["Jwt:Authority"];
        opt.Audience = builder.Configuration["Jwt:Audience"];
    });

builder.Services.AddAuthorization();

// --- OpenTelemetry ---
builder.Services.AddOpenTelemetry()
    .WithTracing(tp => tp
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter());

// --- MCP Model Tier & Context Budget ---
builder.Services.Configure<ModelTierOptions>(builder.Configuration.GetSection("Mcp:ModelTiers"));
builder.Services.Configure<ContextBudgetOptions>(builder.Configuration.GetSection("Mcp:ContextBudget"));
builder.Services.AddSingleton<ContextBudgetEnforcer>();

// --- Distributed Cache (in-memory fallback) ---
builder.Services.AddDistributedMemoryCache();

// --- MCP Semantic Cache ---
builder.Services.Configure<McpSemanticCacheOptions>(
    builder.Configuration.GetSection("Mcp:SemanticCache"));

var mcpMeter = new Meter("McpCfdi.Mcp");
var mcpCacheHitsCounter = mcpMeter.CreateCounter<long>("mcp.cache.hits", description: "Number of MCP semantic cache hits");
builder.Services.AddSingleton(mcpCacheHitsCounter);
builder.Services.AddSingleton<McpSemanticCache>();

// --- MCP Rate Limiting ---
builder.Services.Configure<McpRateLimitOptions>(
    builder.Configuration.GetSection("Mcp:RateLimit"));

// --- MCP Server ---
builder.Services.AddMcpServer()
    .WithToolsFromAssembly();

// --- MCP Token Instrumentation ---
builder.Services.AddSingleton<McpTokenInstrumentation>();

var app = builder.Build();

// --- Middleware pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "McpCfdi API");
app.UseMiddleware<McpRateLimiterMiddleware>();
app.MapMcp();

app.Run();
