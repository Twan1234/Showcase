using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Showcase.Areas.Identity.Data;
using Showcase.Data;
using Showcase.DataService;
using Showcase.Hubs;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("AuthDbContextConnection") 
    ?? throw new InvalidOperationException("Connection string 'AuthDbContextConnection' not found.");

builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddDbContext<TicTacToeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TicTacToeDbConnection")
        ?? throw new InvalidOperationException("Connection string 'TicTacToeDbConnection' not found.")));


//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<AuthDbContext>();
builder.Services.AddScoped<TicTacToeDbService>();
builder.Services.AddSignalR();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();



builder.Services.AddCors(opt =>
{
    opt.AddPolicy("reactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
}).AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AuthDbContext>();


//builder.Services.AddSingleton<SharedDb>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseCors("reactApp");

app.UseDefaultFiles(); // looks for index.html
app.UseStaticFiles(); // serves static files from wwwroot

app.UseStaticFiles();


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<TicTacToeHub>("/ReactTicTacToe");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[] { "Admin", "Member" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

   if (await userManager.FindByEmailAsync("twan.kloek@gmail.com") != null)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Email == "twan.kloek@gmail.com");
        await userManager.AddToRoleAsync(user, "Admin");
    }
}

app.Run();
