using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Sessions
{
    // Pagina "Le mie sessioni": mostra all'utente le proprie sessioni APERTE.
    // L'accesso è consentito solo ad utenti autenticati.
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IParkingSessionRepository _sessions; // Repository per accesso alle sessioni

        // Collezione che la view renderizza (tabella delle sessioni aperte)
        public List<ParkingSession> Items { get; set; } = new();

        // DI del repository
        public IndexModel(IParkingSessionRepository sessions) => _sessions = sessions;

        // GET: carica le sessioni aperte dell'utente corrente (read-only)
        public async Task OnGet()
        {
            // Recupera l'Id dell'utente loggato dai claim Identity
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Carica solo le sessioni in stato "Open" appartenenti all'utente
            Items = await _sessions.ListOpenByUserAsync(userId);
        }
    }
}
