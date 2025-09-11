using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Data;   // DbContext
using SmartPark.MWBot.Models; // ApplicationUser

var builder = WebApplication.CreateBuilder(args);


// DbContext + Identity
// - Registra il DbContext EF Core usando SQLite con la connection string "DefaultConnection" da appsettings.json.
// - Il DbContext viene risolto per tutte le classi che lo richiedono.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Eccezioni sviluppatore per le pagine/migrazioni 
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity (autenticazione + autorizzazione di base)
// - ApplicationUser è l'estensione di IdentityUser (contiene Type, sconti, ecc.)
// - AddRoles abilita la gestione dei ruoli (necessario per RoleManager e [Authorize(Roles="...")])
// - AddEntityFrameworkStores collega Identity all'ApplicationDbContext
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>() // ⬅️ necessario per RoleManager/ruoli
.AddEntityFrameworkStores<ApplicationDbContext>();

// =========================
// REPOSITORIES

builder.Services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();
builder.Services.AddScoped<ITariffRepository, TariffRepository>();
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
builder.Services.AddScoped<IChargeRequestRepository, ChargeRequestRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IChargeJobRepository, ChargeJobRepository>();
builder.Services.AddScoped<IMWBotModelRepository, MWBotModelRepository>();
builder.Services.AddScoped<ICarModelRepository, CarModelRepository>();
builder.Services.AddScoped<IPaymentLineRepository, PaymentLineRepository>();

// Razor Pages (motore di rendering server-side)
builder.Services.AddRazorPages();

var app = builder.Build();

// =========================
// SEED AMMINISTRATORE (solo admin)
// - Esegue migrazioni DB all'avvio.
// - Garantisce l'esistenza del ruolo "Admin".
// - Crea (se manca) l'utente admin con credenziali da appsettings.json: AdminSeed:Email/Password.
// - Aggiunge l'utente admin al ruolo "Admin".
// - Il seed avviene a runtime, NON in OnModelCreating (evita valori dinamici in migrazione).
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    //Applica eventuali migrazioni
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();

    const string adminRole = "Admin";
    if (!await roleMgr.RoleExistsAsync(adminRole))
        await roleMgr.CreateAsync(new IdentityRole(adminRole));

    // Leggi da appsettings.json -> "AdminSeed": { "Email": "...", "Password": "..." }
    // Se non configurate, usa i default sottostanti.
    var adminEmail = builder.Configuration["AdminSeed:Email"] ?? "admin@demo.local";
    var adminPwd = builder.Configuration["AdminSeed:Password"] ?? "Passw0rd!";

    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Type = UserType.Premium,   // impostazione facoltativa: l'admin è Premium per test sconti
            ParkingDiscount = 0.0,     // sconti di default a 0 (si possono poi modificare in Admin → Utenti)
            ChargingDiscount = 0.0
        };

        var res = await userMgr.CreateAsync(admin, adminPwd);
        if (!res.Succeeded)
        {
            // In caso di errore, aggrega i messaggi Identity in un'unica eccezione leggibile.
            var msg = string.Join("; ", res.Errors.Select(e => $"{e.Code}:{e.Description}"));
            throw new InvalidOperationException("Seed admin failed: " + msg);
        }
    }

    if (!await userMgr.IsInRoleAsync(admin, adminRole))
        await userMgr.AddToRoleAsync(admin, adminRole);
}
// ====== FINE SEED ADMIN ======

// PIPELINE HTTP 
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint(); // pagina di errore migrazioni/DB 
}
else
{
    app.UseExceptionHandler("/Error"); // pagina di errore generica in produzione
    app.UseHsts();                     // abilita HSTS (sicurezza HTTP)
}

app.UseHttpsRedirection(); // forza HTTPS
app.UseStaticFiles();      // abilita wwwroot (css/js/img)

app.UseRouting();          // attiva il routing endpoint

app.UseAuthentication();   // identifica l'utente (cookie/identity)
app.UseAuthorization();    // applica policy/ruoli/authorize

// Mapping delle Razor Pages (endpoints)
app.MapRazorPages();

app.Run(); // avvio dell'app
