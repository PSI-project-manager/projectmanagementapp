using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// database connection
string connectionString = builder.Configuration["Db:ConnString"]!;
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// allow frontend to call the api
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000");
        policy.AllowAnyMethod();
        policy.AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PasswordHasher<User>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // make the admin account
    DevDataSeeder.Seed(app.Services);
}

app.UseCors("DevelopmentCors");

app.MapControllers();

app.Run();
