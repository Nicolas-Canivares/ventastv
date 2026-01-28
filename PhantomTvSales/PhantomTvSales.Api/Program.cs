using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Models;
using PhantomTvSales.Api.Services;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using System.Data;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Phantom
builder.Services.Configure<PhantomOptions>(builder.Configuration.GetSection("Phantom"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<PhantomClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<AuthMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !context.Request.Path.StartsWithSegments("/api/auth"))
    {
        if (context.GetCurrentUser() is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Login requerido.");
            return;
        }
    }

    await next();
});
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    db.Database.Migrate();
    EnsureAuthSchema(db);

    var seedUsername = builder.Configuration["AdminSeed:Username"];
    var seedPassword = builder.Configuration["AdminSeed:Password"];

    if (!string.IsNullOrWhiteSpace(seedUsername) &&
        !string.IsNullOrWhiteSpace(seedPassword) &&
        !db.Users.Any())
    {
        var admin = new User
        {
            Username = seedUsername.Trim(),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, seedPassword);
        db.Users.Add(admin);
        db.SaveChanges();
    }
}

app.Run();

static void EnsureAuthSchema(AppDbContext db)
{
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "Users" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
            "Username" TEXT NOT NULL,
            "PasswordHash" TEXT NOT NULL,
            "Role" INTEGER NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "UpdatedAt" TEXT NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "UserSessions" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_UserSessions" PRIMARY KEY AUTOINCREMENT,
            "Token" TEXT NOT NULL,
            "UserId" INTEGER NOT NULL,
            "ExpiresAt" TEXT NOT NULL,
            CONSTRAINT "FK_UserSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );
        """);

    db.Database.ExecuteSqlRaw("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");""");
    db.Database.ExecuteSqlRaw("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserSessions_Token" ON "UserSessions" ("Token");""");

    TryAddColumn(db, "Clients", "LastContactedByUserId", "INTEGER");
    TryAddColumn(db, "Sales", "CreatedByUserId", "INTEGER");
}

static void TryAddColumn(AppDbContext db, string table, string column, string type)
{
    using var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State == ConnectionState.Closed;
    if (shouldClose) connection.Open();

    try
    {
        if (!TableExists(connection, table)) return;
        if (ColumnExists(connection, table, column)) return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type} NULL;";
        cmd.ExecuteNonQuery();
    }
    finally
    {
        if (shouldClose) connection.Close();
    }
}

static bool TableExists(DbConnection connection, string table)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name = $name AND type = 'table';";
    var param = cmd.CreateParameter();
    param.ParameterName = "$name";
    param.Value = table;
    cmd.Parameters.Add(param);
    var count = Convert.ToInt32(cmd.ExecuteScalar());
    return count > 0;
}

static bool ColumnExists(DbConnection connection, string table, string column)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var name = reader.GetString(1);
        if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}
