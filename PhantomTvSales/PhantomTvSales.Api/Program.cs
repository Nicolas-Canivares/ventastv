using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

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
app.MapControllers();
app.Run();
