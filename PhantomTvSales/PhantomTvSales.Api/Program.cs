using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Models;
using PhantomTvSales.Api.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

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
    try
    {
        db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type} NULL;");
    }
    catch
    {
        // columna ya existe o la tabla no está lista
    }
}
