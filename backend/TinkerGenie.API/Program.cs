using Microsoft.EntityFrameworkCore;
using TinkerGenie.API.Data;
using TinkerGenie.API.Services;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
    "Host=161.35.5.159;Database=tinker_genie;Username=genie_admin;Password=***PASSWORD***";
builder.Services.AddDbContext<TinkerGenieContext>(options =>
    options.UseNpgsql(connectionString));

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try 
    {
        return ConnectionMultiplexer.Connect("24.144.119.69:6379,password=***PASSWORD***");
    }
    catch 
    {
        return null!; // Return null if Redis unavailable
    }
});

// Register all services that exist
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IConversationService, ConversationService>();

// Add Authentication with the TBB-provided JWT secret
var jwtKey = "TinkerGenieJWTSecretKey2025VeryLongAndSecure";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTinker", policy =>
    {
        policy.WithOrigins("https://tinker.twobrain.ai")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline - CORRECT ORDER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowTinker");
app.UseRouting();           // MUST come first
app.UseAuthentication();    // Then authentication
app.UseAuthorization();     // Then authorization
app.MapControllers();       // Finally controllers

// Health endpoint
app.MapGet("/health", () => Results.Ok(new {
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.Run();

public class ChatRequest
{
    public string Message { get; set; } = "";
    public string? UserEmail { get; set; }
}
