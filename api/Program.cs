using Serilog;

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

// OpenAPI & Health
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Fake in-memory data
var products = new List<Product> {
    new(1, "Explorer Backpack"), new(2, "Travel Adapter"), new(3, "First Aid Kit")
};

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

app.MapHealthChecks("/healthz");
app.MapGet("/products", () => products);

app.Run();

public record Product(int Id, string Name);
