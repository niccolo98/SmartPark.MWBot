using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.Collections.Generic;
using System.Linq;              // per Any()
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Sessions
{
    // Pagina "Le mie sessioni": mostra all'utente le proprie sessioni APERTE.
    // L'accesso è consentito solo ad utenti autenticati.
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IParkingSessionRepository _sessions;   // Repository per accesso alle sessioni
        private readonly IChargeRequestRepository _requests;    // Repository per richieste di ricarica (per capire se c'è una ricarica attiva)

        // Collezione che la view renderizza (tabella delle sessioni aperte)
        public List<ParkingSession> Items { get; set; } = new();

        // Mappa: per ogni SessionId indica se esiste una richiesta ricarica attiva
        // (stati Pending o InProgress). Serve a disabilitare il bottone "Richiedi ricarica"
        // mostrando "Ricarica in corso" nella UI.
        public Dictionary<int, bool> HasActiveCharge { get; set; } = new();

        // DI dei repository necessari
        public IndexModel(IParkingSessionRepository sessions, IChargeRequestRepository requests)
        {
            _sessions = sessions;
            _requests = requests;
        }

        // GET: carica le sessioni aperte dell'utente corrente (read-only)
        public async Task OnGet()
        {
            // Recupera l'Id dell'utente loggato dai claim Identity
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Carica solo le sessioni in stato "Open" appartenenti all'utente
            Items = await _sessions.ListOpenByUserAsync(userId);

            // Per ciascuna sessione, verifica se esiste una richiesta ricarica "attiva"
            // (cioè già ACCETTATA = Pending, o in esecuzione = InProgress).
            HasActiveCharge.Clear();
            foreach (var s in Items)
            {
                var reqs = await _requests.ListBySessionAsync(s.Id);
                var active = reqs.Any(r =>
                    r.Status == ChargeRequestStatus.Pending ||
                    r.Status == ChargeRequestStatus.InProgress);
                HasActiveCharge[s.Id] = active;
            }
        }
    }
}
