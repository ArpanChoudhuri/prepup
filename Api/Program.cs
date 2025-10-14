using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

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
app.MapGet("/products", () => products);

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
        claims: new[] { new System.Security.Claims.Claim("sub", req.User ?? "demo") },
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: creds
    );
    var jwtStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { access_token = jwtStr, token_type = "Bearer", expires_in = 600 });
});

app.Run();

public record Product(int Id, string Name);
public record Order(int Id, string Tenant, int ProductId);
public record TokenRequest(string? User, string? Password);