using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Features.Academies.Services;
using SkillHive.Features.Auth.Services;
using SkillHive.Features.Email.Services;
using SkillHive.Features.Users.Services;
using SkillHive.Features.Notifications.Services;
using SkillHive.Features.Categories.Services;
using SkillHive.Features.Courses.Services;
using SkillHive.Features.Lessons.Services;
using SkillHive.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Controllers & API documentation
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT authentication
var jwtSecret = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });
builder.Services.AddAuthorization();

// Custom services
builder.Services.AddSingleton<Logger>();
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SkillHive.Features.Academies.Services.AcademyService>();
builder.Services.AddScoped<SkillHive.Features.Users.Services.StaffService>();
builder.Services.AddScoped<SkillHive.Features.Users.Services.UserService>();
builder.Services.AddScoped<SkillHive.Features.Categories.Services.CategoryService>();
builder.Services.AddScoped<SkillHive.Features.Courses.Services.CourseService>();
builder.Services.AddScoped<SkillHive.Features.Lessons.Services.LessonService>();

var app = builder.Build();
// Run seeders on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seeder = new DbSeeder(db, scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>());
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Cookie-to-header bridge: if Authorization header is missing but AuthToken cookie exists,
// copy it to the header so JWT validation works for both Bearer and cookie clients.
app.UseMiddleware<CookieAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();