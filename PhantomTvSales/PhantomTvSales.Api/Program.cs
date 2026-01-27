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
