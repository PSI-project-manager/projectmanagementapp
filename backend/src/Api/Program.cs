using Api.Data;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// database connection
string connectionString = builder.Configuration["Db:ConnString"]!;
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// allow frontend to call the api
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DevelopmentCors",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000");
            policy.AllowAnyMethod();
            policy.AllowAnyHeader();
        }
    );
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// authentication before api
var signingKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim names as "role" and not the long URIs
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(signingKey)
            ),
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

// projects
builder.Services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProjectRequest>, UpdateProjectRequestValidator>();
builder.Services.AddScoped<ProjectService>();

// login
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();

// items
builder.Services.AddScoped<IValidator<CreateItemRequest>, CreateItemRequestValidator>();
builder.Services.AddScoped<ItemService>();

// item types
builder.Services.AddScoped<IValidator<CreateItemTypeRequest>, CreateItemTypeRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateItemTypeRequest>, UpdateItemTypeRequestValidator>();
builder.Services.AddScoped<ItemTypeService>();

// users
builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // make the admin account
    DevDataSeeder.Seed(app.Services);
}

// turn exceptions into error responses
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        int status = StatusCodes.Status500InternalServerError;
        string title = "An unexpected error occurred";

        if (exception is ValidationException)
        {
            status = StatusCodes.Status400BadRequest;
            title = "Validation failed";
        }
        else if (exception is KeyNotFoundException)
        {
            status = StatusCodes.Status404NotFound;
            title = "Not found";
        }
        else if (exception is UnauthorizedAccessException)
        {
            status = StatusCodes.Status401Unauthorized;
            title = "Unauthorized";
        }

        // dont show the message for 500 errors
        string? detail = null;
        if (status != StatusCodes.Status500InternalServerError)
        {
            detail = exception?.Message;
        }

        var problem = new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        problem.Detail = detail;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseCors("DevelopmentCors");

// use authentication and authorization before mapping controllers
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
