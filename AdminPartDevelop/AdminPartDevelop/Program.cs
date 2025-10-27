using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNet.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;
using System.Text.Json;
using AdminPartDevelop.Data;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Services.RefereeServices;
using AdminPartDevelop.Services.EmailsSender;
using Microsoft.Extensions.FileProviders;
using AdminPartDevelop.Services.AdminServices;
using AdminPartDevelop.Models;
using AdminPartDevelop.Hubs;
using AdminPartDevelop.Services.RouteServices;
using Aspose.Cells.Charts;
using AdminPartDevelop.Services.AdminServices;
//TOP LEVEL STATEMENTS
// Get access information for AzureKey Vault
var builder = WebApplication.CreateBuilder(args);

/*
string vaultUri = File.ReadAllText("/run/secrets/AzureVaultURI").Trim();
string tenantId = File.ReadAllText("/run/secrets/AzureTenantId").Trim();
string clientId = File.ReadAllText("/run/secrets/AzureClientId").Trim();
string clientSecret = File.ReadAllText("/run/secrets/AzureClientSecret").Trim();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault(
    new Uri(vaultUri),
    new ClientSecretCredential(
        tenantId: tenantId,
        clientId: clientId,
        clientSecret: clientSecret
        ));
*/

// Retrieve PostgreSQL credentials
/*
string postgreUsername = builder.Configuration["UsernameAdmin"];
string postgrePassword = builder.Configuration["PasswordAdmin"];
string postgreUsernameReferee = builder.Configuration["UsernameReferee"];
string postgrePasswordReferee = builder.Configuration["PasswordReferee"];
string mapyczapikey = builder.Configuration["Mapyczapikey"];
*/
var _mapyczapikey = builder.Configuration["ApiKeys:MapyCz"];
var _mapsgoogleapikey = builder.Configuration["ApiKeys:GoogleMaps"];
string connectionString = builder.Configuration.GetConnectionString("DefaultConnectionAdmin");
string connectionStringReferee = builder.Configuration.GetConnectionString("DefaultConnectionReferee");
// Configure SignalR for real-time communication between server and clients
builder.Services.AddSignalR();

// Configure MVC Controllers with specific JSON serialization options
builder.Services.AddControllersWithViews()
   .AddJsonOptions(options =>
   {
       // Use camelCase for JSON property names (JavaScript convention)
       options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
       // Make property name matching case-sensitive
       options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
   });

// Add HttpContextAccessor to access current HttpContext throughout the app
builder.Services.AddHttpContextAccessor();

// Configure database contexts
// Admin database connection
builder.Services.AddDbContext<AdminDbContext>(options =>
   options.UseNpgsql(connectionString));

// Referee database connection (separate database)
builder.Services.AddDbContext<RefereeDbContext>(options =>
   options.UseNpgsql(connectionStringReferee));

// Configure named HTTP clients for external API calls
// Google Maps client for location/routing services
builder.Services.AddHttpClient("GoogleMapsClient");

// Mapy.cz client with API key in default headers
builder.Services.AddHttpClient("MapyClient", client =>
{
    client.DefaultRequestHeaders.Add("X-Mapy-Api-Key", _mapyczapikey);
});

// Register route planning services as singletons (long-lived instances)
// Car route planning service
builder.Services.AddSingleton<IRouteCarPlanner,RouteByCarPlanner>(sp =>
   new RouteByCarPlanner(
       sp.GetRequiredService<ILogger<RouteByCarPlanner>>(),
       sp.GetRequiredService<IHttpClientFactory>(),
       _mapyczapikey
   )
);

// Bus route planning service
builder.Services.AddSingleton<IRouteBusPlanner,RouteByBusPlanner>(sp =>
   new RouteByBusPlanner(
       sp.GetRequiredService<ILogger<RouteByBusPlanner>>(),
       sp.GetRequiredService<IHttpClientFactory>(),
       _mapsgoogleapikey
   )
);

// Register scoped services (one instance per request)
builder.Services.AddScoped<IExcelParser, GetData>();       // Excel data import
builder.Services.AddScoped<IExcelExporter, ExportData>();  // Excel data export
builder.Services.AddScoped<IRefereeService, RefereeService>(); // Business logic for referees
builder.Services.AddScoped<IAdminService, AdminService>();     // Business logic for admins
builder.Services.AddScoped<IRefereeRepo, RefereeRepo>();       // Data access for referees
builder.Services.AddScoped<IAdminRepo, AdminRepo>();           // Data access for admins
builder.Services.AddScoped<AdminPartDevelop.Services.CacheServices.IMatchesCacheService, AdminPartDevelop.Services.CacheServices.MatchesCacheService>();
//builder.Services.AddScoped<IEmailsToLoginDbSender,EmailsToLoginDbSender>();       //Email sender to login database

// Add memory caching for performance optimization
builder.Services.AddMemoryCache();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Suppress EF Core SQL command logging
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
// Minimize Microsoft and System logging
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// Build the application
var app = builder.Build();

// Map SignalR hub for real-time UI updates
app.MapHub<HubForReendering>("/hubForReendering");

// Configure forwarded headers for proxy scenarios
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    // In production, use error handling and HSTS
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

// Enable serving static files (CSS, JS, images)
app.UseStaticFiles();

// Enable endpoint routing
app.UseRouting();

// Enable authorization middleware
app.UseAuthorization();


// Configure endpoints - this must come after UseRouting

app.UseEndpoints(endpoints =>

{

    // Map the hub with the correct path

    endpoints.MapHub<HubForReendering>("/Admin/hubForReendering");



    // Your other endpoint mappings

    endpoints.MapControllerRoute(

        name: "default",

        pattern: "{controller=Home}/{action=Index}/{id?}");

});

// Start the application
app.Run();