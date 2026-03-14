using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
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

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

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
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddPasswordValidator<MaxLengthPasswordValidator>()
    .AddPasswordValidator<BreachedPasswordValidator>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 64;
    options.Password.RequireDigit = options.Password.RequireLowercase = options.Password.RequireUppercase = options.Password.RequireNonAlphanumeric = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.AddServerHeader = false);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

builder.Services.Configure<HstsOptions>(options =>
{
    options.MaxAge = TimeSpan.FromSeconds(15724800);
    options.IncludeSubDomains = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

EnsureSqliteDirectories(app);
RunMigrations(app);

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    if (Array.IndexOf(new[] { "GET", "POST", "HEAD", "OPTIONS" }, context.Request.Method.ToUpperInvariant()) < 0)
    {
        context.Response.StatusCode = 405;
        app.Logger.LogWarning("Invalid method: {Method} {Path}", context.Request.Method, context.Request.Path);
        return;
    }
    await next();
});
app.Use(async (context, next) =>
{
    var path = (context.Request.Path.Value ?? "").Replace('\\', '/');
    if (path.Contains("/.git", StringComparison.OrdinalIgnoreCase) || path.Contains("/.svn", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".ds_store", StringComparison.OrdinalIgnoreCase) || path.Contains("thumbs.db", StringComparison.OrdinalIgnoreCase) || path.Contains("/.env", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        return;
    }
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' https://www.google.com https://www.gstatic.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; frame-ancestors 'self'; base-uri 'self'; form-action 'self' https://api.web3forms.com; connect-src 'self' https://api.web3forms.com https://www.google.com";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    if (!path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("/static/", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/Identity/", StringComparison.OrdinalIgnoreCase) && !path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, private";
        context.Response.Headers["Pragma"] = "no-cache";
    }
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        var ct = context.Response.ContentType ?? "";
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var baseCt = ct.Split(';')[0].Trim();
            var filename = baseCt.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0 ? "api.json"
                : baseCt.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0 ? "api.xml" : "api.bin";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
        }
        if (string.IsNullOrEmpty(ct)) context.Response.Headers["Content-Type"] = "application/octet-stream";
        else if (!ct.Contains("charset=", StringComparison.OrdinalIgnoreCase) &&
            (ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || ct.Contains("+xml", StringComparison.OrdinalIgnoreCase) || ct.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)))
            context.Response.Headers["Content-Type"] = ct.TrimEnd() + "; charset=utf-8";
        return Task.CompletedTask;
    });
    await next();
});
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseIpRateLimiting();
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

class MaxLengthPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> m, ApplicationUser u, string p) =>
        Task.FromResult(p != null && p.Length > 128 ? IdentityResult.Failed(new IdentityError { Code = "PasswordTooLong", Description = "Password must not exceed 128 characters." }) : IdentityResult.Success);
}

class BreachedPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly HashSet<string> Breached = new(StringComparer.OrdinalIgnoreCase) { "123456", "password", "12345678", "qwerty", "123456789", "12345", "1234", "111111", "1234567", "dragon", "123123", "baseball", "abc123", "football", "monkey", "letmein", "696969", "shadow", "master", "666666", "qwertyuiop", "123321", "mustang", "1234567890", "michael", "654321", "pussy", "superman", "1qaz2wsx", "7777777", "fuckyou", "121212", "000000", "qazwsx", "123qwe", "killer", "trustno1", "jordan", "jennifer", "zxcvbnm", "asdfgh", "hunter", "buster", "soccer", "harley", "batman", "andrew", "tigger", "sunshine", "iloveyou", "fuckme", "2000", "charlie", "robert", "thomas", "hockey", "ranger", "daniel", "starwars", "klaster", "112233", "george", "asshole", "computer", "michelle", "jessica", "pepper", "1111", "zxcvbn", "555555", "11111111", "131313", "freedom", "777777", "pass", "fuck", "maggie", "159753", "aaaaaa", "ginger", "princess", "joshua", "cheese", "amanda", "summer", "love", "ashley", "6969", "nicole", "chelsea", "biteme", "matthew", "access", "yankees", "987654321", "dallas", "austin", "thunder", "taylor", "matrix" };
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> m, ApplicationUser u, string p) =>
        Task.FromResult(p != null && Breached.Contains(p) ? IdentityResult.Failed(new IdentityError { Code = "PasswordBreached", Description = "This password is commonly used and not allowed. Choose a different one." }) : IdentityResult.Success);
}
