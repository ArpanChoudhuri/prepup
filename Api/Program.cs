using Api.Data;
using Api.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using static Api.models.ProductDtos;
using static Api.models.ProductUpdateDto;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// CORS (Angular dev)
const string CorsDev = "cors-dev";
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsDev, p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var jwt = builder.Configuration.GetSection("Jwt");

// In tests the TestWebApplicationFactory may inject Jwt values but the timing can vary.
// If the key is missing and we're running under the "Testing" environment, substitute a test-only key.
// In production (non-testing) require the key and fail fast.
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        // test-only fallback key (safe because TestWebApplicationFactory should ideally supply its own)
        jwtKey = "test-secret-key-0123456789";
    }
    else
    {
        throw new InvalidOperationException("Configuration value 'Jwt:Key' is required. Set Jwt:Key in appsettings or in your test host configuration.");
    }
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o =>
  {
      o.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwt["Issuer"],
          ValidAudience = jwt["Audience"],
          IssuerSigningKey = key,
          ClockSkew = TimeSpan.Zero
      };
  });

builder.Services.AddAuthorization();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddSingleton<IMessageSender, ConsoleMessageSender>();

// OpenAPI & Health
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var cs = builder.Configuration.GetConnectionString("AppDb");

// Register SQL Server for normal runs, skip registration when running tests.
// TestWebApplicationFactory runs with builder.UseEnvironment("Testing") and replaces the DbContext.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(cs).EnableSensitiveDataLogging(false)); // keep false in prod
}

// Fake in-memory data
//var products = new List<Product> { new(1, "Explorer Backpack"), new(2, "Travel Adapter"), new(3, "First Aid Kit") };
var orders = new List<Order> { new Order { Id = 1, CreatedUtc = DateTime.UtcNow, ProductId = 1, Tenant = "T1" } };

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors(CorsDev);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ProblemDetails-like simple handler
app.UseExceptionHandler(a => a.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { title = "Unexpected error", traceId = ctx.TraceIdentifier });
}));

app.Use(async (ctx, next) =>
{
    const string k = "x-correlation-id";
    var cid = ctx.Request.Headers.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)
        ? v.ToString()
        : Guid.NewGuid().ToString("n");
    ctx.Response.Headers[k] = cid;
    Serilog.Context.LogContext.PushProperty("CorrelationId", cid);
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

// Public
app.MapGet("/products", async (AppDbContext db, int? take, int? afterId, string? q) =>
{
    var size = Math.Clamp(take ?? 20, 1, 100);

    var qry = db.Products
        .AsNoTracking()
        .OrderBy(p => p.Id)
        .Where(p => afterId == null || p.Id > afterId)
        .Where(p => q == null || p.Name.Contains(q))
        .TagWith("GET /products list keyset");

    var items = await qry.Take(size).ToListAsync();
    var nextAfter = items.Count > 0 ? items[^1].Id : (int?)null;

    return Results.Ok(new { items, nextAfter });
});

// GET /products/{id}
app.MapGet("/products/{id:int}", async (AppDbContext db, int id) =>
{
    var item = await db.Products
        .AsNoTracking()
        .Where(p => p.Id == id)
        .TagWith("GET /products/{id}")
        .FirstOrDefaultAsync();

    return item is null ? Results.NotFound() : Results.Ok(item);
});

// PUT /products/{id}
app.MapPut("/products/{id:int}", async (AppDbContext db, int id, ProductUpdDto dto) =>
{
    var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
    if (entity is null) return Results.NotFound();

    // apply incoming values
    entity.Name = dto.Name;
    entity.Price = dto.Price;
   
    // attach incoming rowversion for concurrency check
    db.Entry(entity).Property(p => p.RowVersion).OriginalValue = dto.RowVersion;

    try
    {
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (DbUpdateConcurrencyException)
    {
        // reload current
        var current = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.Name, p.Price, p.RowVersion })
            .FirstAsync();

        return Results.Conflict(new
        {
            title = "Concurrency conflict",
            message = "The record was modified by someone else. Refresh and retry your changes.",
            current
        });
    }
});


// Protected

app.MapGet("/orders", () => orders)
   .RequireAuthorization();
app.MapPost("/orders", async (AppDbContext db, OrderCreateDto dto) =>
{
    using var tx = await db.Database.BeginTransactionAsync();

    var order = new Order { Tenant = dto.Tenant, ProductId = dto.ProductId };
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    var evt = new { order.Id, order.Tenant, order.ProductId, OccurredUtc = DateTime.UtcNow };
    var outbox = new OutboxMessage
    {
        Type = "OrderCreated",
        Payload = System.Text.Json.JsonSerializer.Serialize(evt),
        NextAttemptUtc = DateTime.UtcNow
    };
    db.Outbox.Add(outbox);

    await db.SaveChangesAsync();
    await tx.CommitAsync();
    Log.Information("Created order {OrderId} for {Tenant}", order.Id, order.Tenant);


    return Results.Accepted($"/orders/{order.Id}", new { order.Id });
}).RequireAuthorization();

// GET /orders/{id}/status -> { id, dispatched: bool }
app.MapGet("/orders/{id:int}/status", async (AppDbContext db, int id) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();
    // Consider dispatched when corresponding outbox message for OrderCreated has DispatchedUtc set
    var dispatched = await db.Outbox.AnyAsync(x =>
        x.Type == "OrderCreated" && x.Payload.Contains($"\"Id\":{id}") && x.DispatchedUtc != null);
    return Results.Ok(new { id, dispatched });
});

app.MapPost("/auth/token", (TokenRequest req) =>
{
    // DEV ONLY: issue a short-lived token
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwt["Issuer"],
        audience: jwt["Audience"],
        claims: new[] { new System.Security.Claims.Claim("sub", req.User ?? "demo", req.Password ?? "demo") },
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: creds
    );
    var jwtStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { access_token = jwtStr, token_type = "Bearer", expires_in = 600 });
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public record OrderCreateDto(string Tenant, int ProductId);
public record TokenRequest(string? User, string? Password);