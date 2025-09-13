using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Admin.Users
{
    // Pagina amministrativa per modificare le proprietà custom dell'utente:
    // - Type (Base/Premium)
    // - Sconti come PERCENTUALI 0..100 (in UI), salvati come frazioni 0..1 nel database
    // Motivo: evitare problemi di localizzazione (virgola/punto) con input come "0.1".
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager; // servizio Identity per gestire utenti

        public EditModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Solo per visualizzazione in pagina (header)
        public string? UserEmail { get; set; }

        // Modello bindato dal form (POST)
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // ViewModel di input: campi editabili dell'utente
        public class InputModel
        {
            [Required]
            public string Id { get; set; } = "";  // Id Identity dell'utente da aggiornare

            [Required]
            public UserType Type { get; set; }    // Base/Premium

            // In UI usiamo percentuali (0..100) per evitare ambiguità con separatori decimali.
            [Range(0, 100, ErrorMessage = "Inserire un valore tra 0 e 100")]
            public double? ParkingDiscountPercent { get; set; }

            [Range(0, 100, ErrorMessage = "Inserire un valore tra 0 e 100")]
            public double? ChargingDiscountPercent { get; set; }
        }

        // GET: carica i dati dell'utente per precompilare il form
        public async Task<IActionResult> OnGet(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            UserEmail = user.Email; // mostrato in pagina ma non modificabile
            Input = new InputModel
            {
                Id = user.Id,
                Type = user.Type,
                // Converti da frazione (0..1) a percentuale (0..100) per la UI
                ParkingDiscountPercent = user.ParkingDiscount.HasValue ? user.ParkingDiscount.Value * 100.0 : (double?)null,
                ChargingDiscountPercent = user.ChargingDiscount.HasValue ? user.ChargingDiscount.Value * 100.0 : (double?)null
            };
            return Page();
        }

        // POST: salva le modifiche apportate nel form
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page(); // validazione server-side dei campi

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user is null) return NotFound();

            // Aggiorna i campi custom sull'utente
            user.Type = Input.Type;
            // Converti da percentuale (0..100) a frazione (0..1) e clampa entro i limiti
            user.ParkingDiscount = ToFraction(Input.ParkingDiscountPercent);
            user.ChargingDiscount = ToFraction(Input.ChargingDiscountPercent);

            // Persistenza via UserManager (gestisce concurrency stamp, validazioni Identity, ecc.)
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                // Se Identity ritorna errori, li riversa nel ModelState per la visualizzazione in pagina
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            TempData["Msg"] = "Utente aggiornato."; // feedback utente
            return RedirectToPage("Index");          // ritorna alla lista
        }

        // Converte da percentuale (0..100) a frazione (0..1) con clamp e gestione null.
        private double? ToFraction(double? percent)
        {
            if (!percent.HasValue) return null;
            var p = percent.Value;
            if (p < 0) p = 0;
            if (p > 100) p = 100;
            return p / 100.0;
        }
    }
}
