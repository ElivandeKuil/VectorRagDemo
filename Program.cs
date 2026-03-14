using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Filters;
using VectorRagDemo.Services;
using AppLogger = VectorRagDemo.Services.AppLogger;

// Must be set before any Npgsql connections are opened.
// Allows DateTime.Now (Kind=Local) to be written to PostgreSQL timestamp columns
// without requiring UTC conversion — equivalent to SQL Server's datetime behavior.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add global authorization filter - all pages require authentication
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add<ConsentRequiredFilter>();
});

builder.Services.AddDbContext<VectorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseLowerCaseNamingConvention());

builder.Services.AddDbContext<LogboekDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LogboekConnection"))
           .UseLowerCaseNamingConvention());

builder.Services.AddDbContext<ManagementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ManagementConnection"))
           .UseLowerCaseNamingConvention());

// Register UserAuthenticationService
builder.Services.AddScoped<UserAuthenticationService>();

// Register ProjectAccessService
builder.Services.AddScoped<ProjectAccessService>();

// GDPR toestemmingsbeheer
builder.Services.AddScoped<ConsentService>();
builder.Services.AddScoped<UserDeletionService>();
builder.Services.AddScoped<DataExportService>();

// Configure cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddScoped<ChatService>(provider =>
{
    var context = provider.GetRequiredService<VectorDbContext>();
    var logboekContext = provider.GetRequiredService<LogboekDbContext>();
    return new ChatService(context, logboekContext, builder.Configuration);
});

// Register LinkPreviewService with HttpClient
builder.Services.AddHttpClient<LinkPreviewService>();

// Achtergrondservice voor automatisch verwijderen van verlopen logs (GDPR Art. 5 opslagbeperking)
builder.Services.AddHostedService<LogRetentionService>();

builder.Services.AddGrpc();

var app = builder.Build();

// Geeft ConnectionProcessor toegang tot appsettings.{Environment}.json
ConnectionProcessor.Configure(builder.Configuration);

AppLogger.Initialize(app.Services.GetRequiredService<IServiceScopeFactory>());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGrpcService<GrpcService>();

app.Run();