using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Scalar.AspNetCore;
using authModule.DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using authModule.Models;
using authModule.Services.Client;
using authModule.Services.Auth;
using authModule.src.Services.Client;
using authModule.src.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// -- config database 
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();
var connectionString = config.GetConnectionString("Default");

if (connectionString == null)
{
    throw new Exception("Connection string not found.");
}

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add Authentication & JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
})
.AddCookie("AdminCookie", options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExternalService, ExternalService>();
builder.Services.AddScoped<authModule.Utilities.JwtHelper>();

// Add MVC with Views support (for Razor)
// Customize the view location to /src/Views/
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/src/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/src/Views/Shared/{0}.cshtml");
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed Admin User Fake Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await authModule.DataContext.DataSeeder.SeedAll(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine("An error occurred while seeding the database: " + ex.Message);
    }
}

Console.WriteLine("====================================");
Console.WriteLine("Connect to Database PostgreSQL Successfully");
Console.WriteLine("====================================");
// -- end config database


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Default route 
app.MapGet("/", () => "Auth Service Running");


// Enable OpenAPI + Scalar ui
app.MapOpenApi();
app.MapScalarApiReference();
// ----

app.Run();
