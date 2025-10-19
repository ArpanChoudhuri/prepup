using Api.Data;
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
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

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


// OpenAPI & Health
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var cs = builder.Configuration.GetConnectionString("AppDb");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(cs).EnableSensitiveDataLogging().LogTo(Console.WriteLine, LogLevel.Information)); ; // keep false in prod

// Fake in-memory data
var products = new List<Product> { new(1, "Explorer Backpack"), new(2, "Travel Adapter"), new(3, "First Aid Kit") };
var orders = new List<Order> { new(1001, "SOS1WEB", 1), new(1002, "SOS1WEB", 3) };

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
        //.Select(p => new ProductListItem(p.Id, p.Name, p.Price))
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
        //.Select(p => new ProductListItem(p.Id, p.Name, p.Price))
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

public record Product(int Id, string Name);
public record Order(int Id, string Tenant, int ProductId);
public record TokenRequest(string? User, string? Password);