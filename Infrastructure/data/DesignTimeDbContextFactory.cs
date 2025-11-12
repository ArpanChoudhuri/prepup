using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables; // <-- Add this line
using Microsoft.Extensions.Configuration.Json; // <-- Add this line

namespace Infra.Data
{
    // Design-time factory used by EF Core tools to create AppDbContext
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();

            // Read configuration (appsettings.*.json) and environment variables so dotnet-ef
            // uses the same connection string you use in SSMS when available.
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Avoid SetBasePath extension (may require Microsoft.Extensions.Configuration.FileExtensions package).
            // Build absolute paths explicitly so AddJsonFile works without SetBasePath.
            var basePath = Directory.GetCurrentDirectory();
            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(basePath, $"appsettings.{env}.json"), optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Prefer explicit env var, then configuration, then a safe fallback SQL-auth string
            var conn =
                Environment.GetEnvironmentVariable("ConnectionStrings__AppDb")
                ?? config.GetConnectionString("AppDb")
                ?? "Data Source=localhost,1433;Persist Security Info=True;User ID=sa;Password=YourStrong!Passw0rd;Application Name=\"EF Core Tools\";Command Timeout=0;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;";

            builder.UseSqlServer(conn);

            return new AppDbContext(builder.Options);
        }
    }
}