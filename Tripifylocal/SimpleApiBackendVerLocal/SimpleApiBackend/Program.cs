using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);





// Konfiguracja Entity Framework (połączenie z bazą danych)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfiguracja JWT oraz Facebook OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = "ExternalCookie";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
    };
})
.AddCookie("ExternalCookie", options =>
{
    options.Cookie.Name = "ExternalAuthCookie";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    options.CallbackPath = "/signin-facebook";

    options.SignInScheme = "ExternalCookie";

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            Console.WriteLine("Tworzenie biletu OAuth...");
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            Console.WriteLine($"Facebook Authentication Failed: {context.Failure?.Message}");
            if (context.Failure?.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {context.Failure.InnerException.Message}");
            }

            context.Response.Redirect("http://127.0.0.1:5500/login");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});


// Dodanie kontrolerów
builder.Services.AddControllers();

// Konfiguracja CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Dodanie Swagger dla dokumentacji API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Wprowadź token JWT w formacie: Bearer [token]"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Włączenie Swagger w trybie deweloperskim
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();   // Wymuszenie HTTPS
app.UseCors("AllowFrontend"); // Użycie wcześniej zdefiniowanego CORS
app.UseAuthentication();     // Middleware do autentykacji
app.UseAuthorization();      // Middleware do autoryzacji

app.MapControllers();        // Mapowanie kontrolerów

app.Run();  // Uruchomienie aplikacji