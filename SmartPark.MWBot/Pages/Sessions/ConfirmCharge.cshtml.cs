using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Sessions
{
    // Pagina di conferma della richiesta di ricarica:
    // - GET: mostra i dettagli della richiesta (stato "Proposed") e chiede all'utente di accettare o rifiutare.
    // - POST Accept: passa la richiesta a "Pending" e crea un ChargeJob in coda (Queued).
    // - POST Reject: annulla la richiesta (Cancelled).
    //
    // Nota: qui NON parte la ricarica; l'avvio effettivo è simulato dal pannello Admin/MWBot.
    [Authorize]
    public class ConfirmChargeModel : PageModel
    {
        private readonly IChargeRequestRepository _requests;
        private readonly IChargeJobRepository _jobs;

        public ConfirmChargeModel(IChargeRequestRepository requests, IChargeJobRepository jobs)
        {
            _requests = requests;
            _jobs = jobs;
        }

        // Dato per la view: richiesta caricata con la relativa sessione/auto (via include nel repository)
        public ChargeRequest? RequestItem { get; set; }

        // GET: carica la richiesta e verifica:
        //  - esistenza,
        //  - ownership (la sessione deve appartenere all'utente loggato),
        //  - stato sessione (deve essere Open),
        //  - stato richiesta (deve essere Proposed; se già gestita, si reindirizza alla index).
        public async Task<IActionResult> OnGet(int id)
        {
            // Carico la richiesta (contiene anche la ParkingSession e la Car via repo)
            RequestItem = await _requests.GetByIdAsync(id);
            if (RequestItem == null) return NotFound();

            // Autorizzazione: la richiesta deve appartenere all'utente e la sessione deve essere aperta
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (RequestItem.ParkingSession == null || RequestItem.ParkingSession.UserId != userId)
                return NotFound();
            if (RequestItem.ParkingSession.Status != ParkingSessionStatus.Open)
                return NotFound();

            if (RequestItem.Status != ChargeRequestStatus.Proposed)
                return RedirectToPage("Index"); // già accettata/rifiutata

            return Page();
        }

        // POST Accept:
        // - controlla ancora ownership/stato,
        // - imposta Request -> Pending,
        // - crea ChargeJob -> Queued (verrà gestito dall'Admin),
        // - salva i cambiamenti su DB e notifica via TempData.
        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            var req = await _requests.GetByIdAsync(id);
            if (req == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (req.ParkingSession == null || req.ParkingSession.UserId != userId)
                return NotFound();
            if (req.ParkingSession.Status != ParkingSessionStatus.Open)
                return NotFound();

            if (req.Status != ChargeRequestStatus.Proposed)
            {
                TempData["Msg"] = "Questa richiesta è già stata gestita.";
                return RedirectToPage("Index");
            }

            // Accettazione: passa a Pending e crea il job in coda
            req.Status = ChargeRequestStatus.Pending;
            _requests.Update(req);
            await _requests.SaveChangesAsync(); // persiste la transizione di stato della richiesta

            var job = new ChargeJob
            {
                ChargeRequestId = req.Id,
                Status = ChargeJobStatus.Queued
            };
            await _jobs.AddAsync(job);
            await _jobs.SaveChangesAsync(); // persiste la creazione del job in coda

            TempData["Msg"] = "Richiesta accettata. Sei in coda per la ricarica.";
            return RedirectToPage("Index");
        }

        // POST Reject:
        // - controlla ownership/stato,
        // - imposta Request -> Cancelled,
        // - salva e notifica via TempData.
        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var req = await _requests.GetByIdAsync(id);
            if (req == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (req.ParkingSession == null || req.ParkingSession.UserId != userId)
                return NotFound();

            if (req.Status != ChargeRequestStatus.Proposed)
            {
                TempData["Msg"] = "Questa richiesta è già stata gestita.";
                return RedirectToPage("Index");
            }

            req.Status = ChargeRequestStatus.Cancelled;
            _requests.Update(req);
            await _requests.SaveChangesAsync(); // persiste l'annullamento

            TempData["Msg"] = "Richiesta rifiutata. Nessuna ricarica sarà effettuata.";
            return RedirectToPage("Index");
        }
    }
}
