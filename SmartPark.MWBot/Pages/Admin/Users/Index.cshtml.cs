using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Pages.Admin.Users
{
    // Pagina amministrativa: elenco utenti e toggle rapido del ruolo "Admin".
    // Richiede che l'utente corrente appartenga al ruolo Admin.
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager; // gestione utenti Identity (CRUD, ruoli, claim, ecc.)
        private readonly RoleManager<IdentityRole> _roleManager;    // gestione ruoli (creazione, esistenza, ecc.)

        public IndexModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // DTO per la tabella: evita di esporre entità Identity alla view e semplifica il binding/rendering.
        public class Row
        {
            public string Id { get; set; } = "";
            public string? Email { get; set; }
            public UserType Type { get; set; }                 // Base/Premium (campo custom su ApplicationUser)
            public double? ParkingDiscount { get; set; }       // sconto sosta (0..1) snapshot dalla user table
            public double? ChargingDiscount { get; set; }      // sconto ricarica (0..1)
            public bool IsAdmin { get; set; }                  // comodità per mostrare pulsante "Rendi/Rimuovi Admin"
        }

        // Collezione da renderizzare nella view
        public List<Row> Items { get; set; } = new();

        // GET: carica tutti gli utenti in sola lettura (AsNoTracking)
        // e, per ciascuno, valuta se appartiene al ruolo "Admin".
        public async Task OnGet()
        {
            var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
            foreach (var u in users)
            {
                Items.Add(new Row
                {
                    Id = u.Id,
                    Email = u.Email,
                    Type = u.Type,
                    ParkingDiscount = u.ParkingDiscount,
                    ChargingDiscount = u.ChargingDiscount,
                    IsAdmin = await _userManager.IsInRoleAsync(u, "Admin")
                });
            }
        }

        // POST: alterna (toggle) il ruolo Admin per l'utente indicato.
        // - Se il ruolo non esiste, lo crea.
        // - Se l'utente è già Admin, lo rimuove dal ruolo; altrimenti lo aggiunge.
        public async Task<IActionResult> OnPostToggleAdminAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            // Assicura che il ruolo "Admin" esista (idempotente)
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            // Toggle membership nel ruolo
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                await _userManager.AddToRoleAsync(user, "Admin");

            TempData["Msg"] = "Ruolo Admin aggiornato.";
            return RedirectToPage();
        }
    }
}
