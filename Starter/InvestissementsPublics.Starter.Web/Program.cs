using InvestissementsPublics.Starter.Web.Data;
using InvestissementsPublics.Starter.Web.Controllers;
using InvestissementsPublics.Starter.Web.Service;
using IdentificationProjet.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Domain.Interfaces;
using Shared.Infrastructure;
using Shared.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
.AddApplicationPart(typeof(TableauxDeBord.Web.Areas.TableauxDeBord.Controllers.HomeController).Assembly)
.AddApplicationPart(typeof(IdentificationProjet.Web.Areas.IdentificationProjet.Controllers.FichesController).Assembly)
//.AddApplicationPart(typeof(InvestissementsPublics.Starter.Web.Controllers.RoleController).Assembly)
.AddRazorRuntimeCompilation();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));

// Module "Identification projet" (tables objet Oracle : Modules/IdentificationProjet/Database)
builder.Services.AddIdentificationProjet(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.Tokens.ProviderMap["Default"] = new TokenProviderDescriptor(
        typeof(IUserTwoFactorTokenProvider<ApplicationUser>)
    );
    options.Tokens.EmailConfirmationTokenProvider = "Default";
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();

builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IPasswordGenerator, PasswordGenerator>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
