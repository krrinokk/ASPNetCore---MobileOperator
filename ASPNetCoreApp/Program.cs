using ASPNetCoreApp.DAL.Models;
using ASPNetCoreApp.DAL.Repository;
using ASPNetCoreApp.Data;
using BLL.Interfaces;
using DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using System.Text.Json.Serialization;

// �������� ������� builder ��� ���������� ���-����������
var builder = WebApplication.CreateBuilder(args);

// ������� ����������� ����������� � ���������� ���������� Console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ���������� �������� CORS, ����������� ������� ������ � ����� http://localhost:3000
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod();

    });
});

// ���������� �������� � ���������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddIdentity<User, IdentityRole>()
.AddEntityFrameworkStores<OperatorContext>();
builder.Services.AddDbContext<OperatorContext>();
builder.Services.AddControllers().AddJsonOptions(x =>
x.JsonSerializerOptions.ReferenceHandler =
ReferenceHandler.IgnoreCycles);
builder.Services.AddScoped(typeof(UnitOfWork));
builder.Services.AddScoped(typeof(DbDataOperations));
builder.Services.AddTransient<IDbRepos, UnitOfWork>();
builder.Services.AddTransient<IDbCrud, DbDataOperations>();

// ���������������� IdentityOptions
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

// ���������������� ApplicationCookieOptions
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "OperatorApp";
    options.LoginPath = "/";
    options.AccessDeniedPath = "/";
    options.LogoutPath = "/";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

// ���������� ������� ����������
var app = builder.Build();
Log.Information("Application started");

// ���������� ���� ������ ���������� �������
try
{
    using (var scope = app.Services.CreateScope())
    {
        var OperatorContext =
        scope.ServiceProvider.GetRequiredService<OperatorContext>();
        await OperatorContextSeed.SeedAsync(OperatorContext);

        await IdentitySeed.CreateUserRoles(scope.ServiceProvider);
    }
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred while seeding the database.");
}

// ���������������� ��������� ��������� HTTP-��������
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
Log.Information("Application stopped");