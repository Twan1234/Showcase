using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Showcase.Areas.Identity.Data;
using Showcase.Data;
using Showcase.Hubs;
using Showcase.Infra;

var builder = WebApplication.CreateBuilder(args);

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
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

var isProduction = builder.Environment.IsProduction();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    if (isProduction)
        options.Cookie.Name = "__Host-.AspNetCore.Cookies";
});

builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.AddServerHeader = false);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.Configure<HstsOptions>(options =>
{
    options.MaxAge = TimeSpan.FromSeconds(15_724_800);
    options.IncludeSubDomains = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.EnsureSqliteDirectories();
app.RunMigrations();

app.UseForwardedHeaders();
app.UseAllowedMethodsOnly();
app.UseShowcaseSecurityHeaders();
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

await app.SeedRolesAndAdminAsync();

app.Run();
