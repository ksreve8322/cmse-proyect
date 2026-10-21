
using cmse.Repositories;
using cmse.Repositories.Models;
using cmse.Utilities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Conexión a la base de datos.
builder.Services.AddDbContext<CmseContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Test")));

// Repositorios.
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<NotificacionRepository>();
builder.Services.AddScoped<NotificacionEnvioRepository>();

// Servicios.
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<EmailManager>();
builder.Services.AddScoped<PlantillaCorreo>();

// Configuración de Session.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CMSE.Session";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Session debe ejecutarse antes de los controladores.
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
