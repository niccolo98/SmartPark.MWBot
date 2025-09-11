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
    // - ParkingDiscount / ChargingDiscount (0..1)
    // Richiede ruolo Admin.
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

            // Sconti come frazioni decimali (0..1). Validazione lato server.
            [Range(0, 1, ErrorMessage = "Inserire un valore tra 0 e 1 (es. 0.1 = 10%)")]
            public double? ParkingDiscount { get; set; }

            [Range(0, 1, ErrorMessage = "Inserire un valore tra 0 e 1 (es. 0.1 = 10%)")]
            public double? ChargingDiscount { get; set; }
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
                ParkingDiscount = user.ParkingDiscount,
                ChargingDiscount = user.ChargingDiscount
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
            user.ParkingDiscount = Normalize(Input.ParkingDiscount);
            user.ChargingDiscount = Normalize(Input.ChargingDiscount);

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

        // Normalizza il valore sconto:
        // - null rimane null,
        // - valori < 0 portati a 0,
        // - valori > 1 portati a 1.
        private double? Normalize(double? value)
        {
            if (!value.HasValue) return null;
            if (value.Value < 0) return 0;
            if (value.Value > 1) return 1;
            return value.Value;
        }
    }
}
