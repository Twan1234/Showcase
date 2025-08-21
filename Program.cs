using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Showcase.Areas.Identity.Data;
using Showcase.Data;
using Showcase.DataService;
using Showcase.Hubs;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ---- Connection strings uit env (Azure) laten overriden ----
void OverrideConn(string key)
{
    var v = Environment.GetEnvironmentVariable($"ConnectionStrings__{key}");
    if (!string.IsNullOrWhiteSpace(v))
        builder.Configuration[$"ConnectionStrings:{key}"] = v;
}
OverrideConn("AuthDbContextConnection");
OverrideConn("TicTacToeDbConnection");

// ---- DbContexts ----
var authCs = builder.Configuration.GetConnectionString("AuthDbContextConnection")
    ?? throw new InvalidOperationException("Connection string 'AuthDbContextConnection' not found.");
builder.Services.AddDbContext<AuthDbContext>(o => o.UseSqlite(authCs));

var tttCs = builder.Configuration.GetConnectionString("TicTacToeDbConnection")
    ?? throw new InvalidOperationException("Connection string 'TicTacToeDbConnection' not found.");
builder.Services.AddDbContext<TicTacToeDbContext>(o => o.UseSqlite(tttCs));

// ---- Services / Identity / SignalR ----
builder.Services.AddScoped<ITicTacToeDbService, TicTacToeDbService>();
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ---- CORS (optioneel configureerbaar) ----
var allowed = builder.Configuration["AllowedCorsOrigins"]; // bv. "https://showcase-twan.azurewebsites.net;https://jouwfrontend.vercel.app"
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("reactApp", p =>
    {
        if (!string.IsNullOrWhiteSpace(allowed))
            p.WithOrigins(allowed.Split(';', StringSplitOptions.RemoveEmptyEntries))
             .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        else
            p.WithOrigins("http://localhost:3000")
             .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
}).AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<AuthDbContext>();

// (Aanrader op Azure achter proxy: forwarded headers voor correcte scheme/redirects)
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// ---- DB migraties bij start (beide contexten) ----
using (var scope = app.Services.CreateScope())
{
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    authDb.Database.Migrate();

    var gameDb = scope.ServiceProvider.GetRequiredService<TicTacToeDbContext>();
    gameDb.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(); // vóór HTTPS redirect
app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("reactApp");

app.UseAuthentication();   // <- stond nog niet in je code
app.UseAuthorization();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR hub
app.MapHub<TicTacToeHub>("/ReactTicTacToe");

app.MapRazorPages();

// ---- Role + user seeding ----
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Member" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
}

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync("twan.kloek@gmail.com");
    if (user != null)
        await userManager.AddToRoleAsync(user, "Admin");
}

app.Run();
