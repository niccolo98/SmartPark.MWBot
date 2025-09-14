using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartPark.MWBot.Pages.Sessions
{
    // Pagina per richiedere una ricarica su una sessione di parcheggio APERTA dell'utente.
    // Flusso:
    //  - GET: verifica ownership/stato della sessione e mostra il form (target SoC e opzionale SoC iniziale).
    //  - POST: valida input, blocca eventuali richieste duplicate sulla stessa sessione,
    //          stima tempi attesa/completamento e crea una ChargeRequest in stato Proposed.
    //          Redireziona poi alla pagina di conferma (ConfirmCharge).
    [Authorize]
    public class RequestChargeModel : PageModel
    {
        private readonly IParkingSessionRepository _sessions;
        private readonly IChargeRequestRepository _requests;
        private readonly IChargeJobRepository _jobs;

        // La sessione target su cui richiedere la ricarica (usata nella view)
        public ParkingSession? Session { get; set; }

        // Modello bindato dal form (target SoC obbligatorio, SoC iniziale facoltativo)
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Range(1, 100)]
            public int TargetSoCPercent { get; set; } = 80;

            [Range(0, 100)]
            public int? InitialSoCPercent { get; set; } // opzionale
        }

        public RequestChargeModel(IParkingSessionRepository sessions, IChargeRequestRepository requests, IChargeJobRepository jobs)
        {
            _sessions = sessions;
            _requests = requests;
            _jobs = jobs;
        }

        // GET: carica la sessione e verifica:
        //  - appartenenza all'utente loggato,
        //  - stato della sessione (deve essere Open).
        public async Task<IActionResult> OnGet(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            Session = await _sessions.GetByIdAsync(id);
            if (Session == null || Session.UserId != userId || Session.Status != ParkingSessionStatus.Open)
                return NotFound();

            return Page();
        }

        // ... all'inizio del file hai già gli using e la classe come prima ...

        // POST: crea una nuova richiesta di ricarica in stato Proposed e reindirizza alla conferma.
        // Include un controllo anti-duplicazione per evitare più richieste attive sulla stessa sessione.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            Session = await _sessions.GetByIdAsync(id);
            if (Session == null || Session.UserId != userId || Session.Status != ParkingSessionStatus.Open)
                return NotFound();

            if (!ModelState.IsValid) return Page();

            // EVITA RICHIESTE DUPLICATE SULLA STESSA SESSIONE
            // Usa ListBySessionAsync se l'hai aggiunto; altrimenti fallback con ListAsync + Where.
            var existingReqs = await _requests.ListBySessionAsync(Session.Id);
            // Fallback se non hai ListBySessionAsync:
            // var existingReqs = (await _requests.ListAsync()).Where(r => r.ParkingSessionId == Session.Id).ToList();

            // Se esiste una richiesta già Proposed/Pending/InProgress per questa sessione → blocca
            if (existingReqs.Any(r =>
                r.Status == ChargeRequestStatus.Proposed ||
                r.Status == ChargeRequestStatus.Pending ||
                r.Status == ChargeRequestStatus.InProgress))
            {
                ModelState.AddModelError(string.Empty, "Esiste già una richiesta di ricarica in corso per questa sessione.");
                return Page();
            }

            // Stima semplice: numero di job in coda + (eventuale) job in esecuzione
            // (30' per auto in attesa; +60' per la propria ricarica, valori volutamente semplici per la demo)
            var queued = await _jobs.ListQueuedAsync();
            var running = await _jobs.GetRunningAsync();
            var carsBefore = queued.Count + (running != null ? 1 : 0);

            var req = new ChargeRequest
            {
                ParkingSessionId = Session.Id,
                TargetSoCPercent = Input.TargetSoCPercent,
                InitialSoCPercent = Input.InitialSoCPercent,
                RequestedAtUtc = DateTime.UtcNow,
                Status = ChargeRequestStatus.Proposed, // solo proposta (l'utente deve confermare)
                EstimatedWaitMinutes = carsBefore * 30,
                EstimatedCompletionMinutes = carsBefore * 30 + 60
            };
            await _requests.AddAsync(req);
            await _requests.SaveChangesAsync(); // persiste la nuova richiesta (necessario per avere l'Id)

            // Redireziona alla pagina di conferma dove l'utente può accettare o rifiutare la stima
            return RedirectToPage("ConfirmCharge", new { id = req.Id });
        }


    }
}
