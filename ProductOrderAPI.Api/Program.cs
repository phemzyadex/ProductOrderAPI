using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Application.Services;
using ProductOrderAPI.Infrastructure.Persistence;
using ProductOrderAPI.Infrastructure.Repositories;
using ProductOrderAPI.Infrastructure.Security;
using ProductOrderAPI.Infrastructure.Services;
using ProductOrderAPI.Shared.Middleware;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// 1. Configure Services
// ---------------------------

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application & Infrastructure Services (DI)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddHttpContextAccessor();

// ---------------------------
// JWT Authentication
// ---------------------------
// JWT settings from configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),

            // Temporary default values; will be replaced in OnTokenValidated
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };

        // Event to dynamically adjust claims after token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                // Extract username from the JWT (usually stored in a claim)
                var tokenUsername = context.Principal?.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(tokenUsername))
                {
                    context.Fail("Username claim missing in token");
                    return;
                }

                // Fetch the user from the database
                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == tokenUsername);

                if (user == null)
                {
                    context.Fail("User not found in database");
                    return;
                }

                // Create a new ClaimsIdentity with dynamic claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),   // map to NameClaimType
                    new Claim(ClaimTypes.Role, user.Role),       // map to RoleClaimType
                    new Claim("UserId", user.Id.ToString())      // optional custom claim
                };

                var identity = new ClaimsIdentity(claims, context.Scheme.Name);
                context.Principal = new ClaimsPrincipal(identity);
            }
        };
    });
// ---------------------------
// Role-Based Authorization
// ---------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// ---------------------------
// Controllers
// ---------------------------
builder.Services.AddControllers();

// ---------------------------
// Swagger / OpenAPI
// ---------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductOrderAPI", Version = "v1" });

    // JWT Authorization in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------------------------
// 2. Configure Middleware
// ---------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductOrderAPI v1"));
}

// Global Exception Handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
