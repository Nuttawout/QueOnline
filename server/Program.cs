using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using QueOnline.Api.Data;
using QueOnline.Api.Models;
using QueOnline.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "ClientApp";

// branches.json is now only a one-time seed (see BranchRegistry) for a dev machine that already
// had one — the real, editable-at-runtime store is server/App_Data/branches.db via the super-admin
// UI. optional:true so a fresh checkout with neither file still starts (empty branch list — every
// {branch} route 404s, and the super-admin UI is where branches get added).
builder.Configuration.AddJsonFile("branches.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
// Encrypts Branch.ConnectionString/AdminPassword at rest in branches.db — keys persisted next to
// the DB file so they survive restarts/redeploys on the same machine (losing them would make every
// stored branch secret unrecoverable, not just unreadable).
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("QueOnline")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));
if (OperatingSystem.IsWindows())
{
    // Encrypts the key file itself at rest via the Windows user profile's DPAPI store — without
    // this, anyone who can read App_Data/keys can decrypt every branch's connection string/admin
    // password regardless of branches.db's own encryption. No-op (with a startup warning) on non-
    // Windows deployments, which is why it's conditional rather than assumed.
    dataProtection.ProtectKeysWithDpapi();
}
builder.Services.AddSingleton<IBranchRegistry, BranchRegistry>();
builder.Services.AddSingleton<ISuperAdminSessionStore, SuperAdminSessionStore>();
builder.Services.AddScoped<IBranchContext, BranchContext>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddSingleton<IPushSubscriptionStore, PushSubscriptionStore>();
builder.Services.AddSingleton<IAppSettingsStore, AppSettingsStore>();
builder.Services.AddSingleton<IAdminSessionStore, AdminSessionStore>();
builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection("WebPush"));
builder.Services.AddHostedService<QueueAlertBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        // Reads Cors:AllowedOrigins fresh on every request (via SetIsOriginAllowed) rather than
        // once at startup — appsettings.json's reloadOnChange means editing the list takes effect
        // immediately, no server restart needed, same as SuperAdmin:Password already behaves.
        policy.SetIsOriginAllowed(origin =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173"];
            return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IBranchRegistry>().InitializeAsync();
    await scope.ServiceProvider.GetRequiredService<IAppSettingsStore>().InitializeAsync();
    await scope.ServiceProvider.GetRequiredService<IPushSubscriptionStore>().InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(ClientCorsPolicy);

// Uploaded logo/sound files (server/App_Data/uploads) — served at /media, separate from any
// branch's Que_lcb data entirely, same as the App_Data SQLite stores. One shared folder for every
// branch; filenames are branch-prefixed by AdminController so they don't collide.
var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(uploadsDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/media",
});

// Routing must run explicitly (rather than relying on ASP.NET Core's implicit auto-insertion)
// so the branch-resolution middleware below — which needs the {branch} route value — has a
// guaranteed place to sit: after routing has matched the endpoint and populated route values,
// but before that endpoint (a controller action) actually executes.
app.UseRouting();

app.Use(async (context, next) =>
{
    var branchSlug = context.GetRouteValue("branch") as string;
    if (branchSlug is not null)
    {
        var registry = context.RequestServices.GetRequiredService<IBranchRegistry>();
        var branch = await registry.FindBySlugAsync(branchSlug, context.RequestAborted);
        if (branch is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = $"ไม่พบสาขา \"{branchSlug}\"" });
            return;
        }

        context.RequestServices.GetRequiredService<IBranchContext>().Current = branch;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
