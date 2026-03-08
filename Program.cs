using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Showcase.Areas.Identity.Data;
using Showcase.Data;
using Showcase.Hubs;
using Showcase.Infra;

var builder = WebApplication.CreateBuilder(args);

void OverrideConnectionStringsFromEnv()
{
    foreach (var key in new[] { "AuthDbContextConnection", "TicTacToeDbConnection" })
    {
        var value = Environment.GetEnvironmentVariable($"ConnectionStrings__{key}");
        if (!string.IsNullOrWhiteSpace(value))
            builder.Configuration[$"ConnectionStrings:{key}"] = value;
    }
}
OverrideConnectionStringsFromEnv();

builder.Services.AddShowcaseStorage(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy("reactApp", policy =>
    {
        var origins = builder.Configuration["AllowedCorsOrigins"];
        var allowed = string.IsNullOrWhiteSpace(origins)
            ? new[] { "http://localhost:3000" }
            : origins.Split(';', StringSplitOptions.RemoveEmptyEntries);
        policy.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

EnsureSqliteDirectories(app);
RunMigrations(app);

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("reactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapHub<TicTacToeHub>("/ReactTicTacToe");
app.MapRazorPages();

await SeedRolesAndAdminAsync(app);

app.Run();

static void EnsureSqliteDirectories(WebApplication app)
{
    var cfg = app.Services.GetRequiredService<IConfiguration>();
    if (!StorageServiceCollectionExtensions.IsUsingSqlite(cfg)) return;

    foreach (var key in new[] { "AuthDbContextConnection", "TicTacToeDbConnection" })
    {
        var cs = cfg.GetConnectionString(key);
        if (string.IsNullOrWhiteSpace(cs)) continue;
        const string prefix = "Data Source=";
        var i = cs.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (i < 0) continue;
        var path = cs.Substring(i + prefix.Length).Trim();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}

static void RunMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    authDb.Database.Migrate();
}

static async Task SeedRolesAndAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Member" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var admin = await userManager.FindByEmailAsync("twan.kloek@gmail.com");
    if (admin != null && !await userManager.IsInRoleAsync(admin, "Admin"))
        await userManager.AddToRoleAsync(admin, "Admin");
}
