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
using AdminPart.Data;
using AdminPart.Services.EmailsSender;
using AdminPart.Services.FileParsers;
using AdminPart.Services.RefereeServices;
using AdminPart.Services.AdminServices;
using AdminPart.Models;
using AdminPart.Hubs;
using AdminPart.Services.RouteServices;
using Microsoft.Extensions.FileProviders;
using Aspose.Cells.Charts;
// Get access information for AzureKey Vault
var builder = WebApplication.CreateBuilder(args);

string vaultUri = File.ReadAllText("/run/secrets/AzureVaultURI").Trim();
string tenantId = File.ReadAllText("/run/secrets/AzureTenantId").Trim();
string clientId = File.ReadAllText("/run/secrets/AzureClientId").Trim();
string clientSecret = File.ReadAllText("/run/secrets/AzureClientSecret").Trim();

builder.Configuration.AddAzureKeyVault(
    new Uri(vaultUri),
    new ClientSecretCredential(
        tenantId: tenantId,
        clientId: clientId,
        clientSecret: clientSecret
        ));


// Retrieve PostgreSQL credentials

string postgreUsername = builder.Configuration["UsernameAdmin"];
string postgrePassword = builder.Configuration["PasswordAdmin"];
string postgreRefereeUsername = builder.Configuration["UsernameReferee"];
string postgreRefereePassword = builder.Configuration["PasswordReferee"];
string mapyczapikey = builder.Configuration["Mapyczapikey"];
string mapsgoogleapikey = builder.Configuration["Mapsgoogleapikey"];

const string _host = "172.18.1.3";
const string _database = "mydb";
const string _port = "5432";
string connectionString = $"Host={_host};Port={_port};Database={_database};Username={postgreUsername};Password={postgrePassword};";

const string _hostReferee = "172.18.2.4";
const string _databaseReferee = "mydb";
const string _portReferee = "5432";
string connectionStringReferee = $"Host={_hostReferee};Port={_portReferee};Database={_databaseReferee};Username={postgreRefereeUsername};Password={postgreRefereePassword};";

// Configure SignalR for real-time communication between server and clients
builder.Services.AddSignalR(options => 
{
    // Increase timeout for slow connections
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    
    // Enable detailed error messages
    options.EnableDetailedErrors = true;
    
    options.MaximumReceiveMessageSize = 102400; // 100 KB
})
.AddJsonProtocol(options => 
{
    // Configure JSON serialization options
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Add HTTP client factory for SignalR connections
builder.Services.AddHttpClient();

// Configure CORS
builder.Services.AddCors(options => 
{
    options.AddPolicy("SignalRPolicy", policy => 
    {
        policy.WithOrigins("https://rozhodcipraha.cz")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// If using a reverse proxy, configure forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRouting(options => 
{
});

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
    client.DefaultRequestHeaders.Add("X-Mapy-Api-Key",mapyczapikey);
});

// Register route planning services as singletons (long-lived instances)
builder.Services.AddSingleton<IRouteCarPlanner,RouteByCarPlanner>(sp =>
   new RouteByCarPlanner(
       sp.GetRequiredService<ILogger<RouteByCarPlanner>>(),
       sp.GetRequiredService<IHttpClientFactory>(),
       mapyczapikey
   )
);

// Bus route planning service
builder.Services.AddSingleton<IRouteBusPlanner,RouteByBusPlanner>(sp =>
   new RouteByBusPlanner(
       sp.GetRequiredService<ILogger<RouteByBusPlanner>>(),
       sp.GetRequiredService<IHttpClientFactory>(),
     mapsgoogleapikey
   )
);

// Register scoped services ()
builder.Services.AddScoped<IExcelParser, GetData>();       // Excel data import
builder.Services.AddScoped<IExcelExporter, ExportData>();  // Excel data export
builder.Services.AddScoped<EmailsToLoginDbSender>();       //Email sender to login database
builder.Services.AddScoped<IRefereeService, RefereeService>(); // Business logic for referees
builder.Services.AddScoped<IAdminService, AdminService>();     // Business logic for admins
builder.Services.AddScoped<IRefereeRepo, RefereeRepo>();       // Data access for referees
builder.Services.AddScoped<IAdminRepo, AdminRepo>();           // Data access for admins

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

// Configure forwarded headers for proxy scenarios
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

app.UseCors(builder =>
{
    builder.WithOrigins("https://rozhodcipraha.cz")
           .AllowAnyHeader()
           .AllowAnyMethod()
           .AllowCredentials(); // Required for SignalR
});

// Enable serving static files (CSS, JS, images)
app.UseStaticFiles();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
    ReceiveBufferSize = 4 * 1024 // 4 KB
});

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
