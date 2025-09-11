using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Admin.MWBot
{
    // Pagina di controllo del MWBot riservata agli amministratori.
    // Consente di:
    //  - visualizzare lo stato del bot (busy/spot corrente),
    //  - vedere il job in esecuzione,
    //  - vedere la coda dei job,
    //  - avviare il prossimo job o un job specifico,
    //  - terminare un job (impostando kWh e SoC finale),
    //  - abortire un job in coda/corso.
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IMWBotModelRepository _botRepo;
        private readonly IChargeJobRepository _jobs;
        private readonly IChargeRequestRepository _requests;

        public IndexModel(IMWBotModelRepository botRepo, IChargeJobRepository jobs, IChargeRequestRepository requests)
        {
            _botRepo = botRepo;
            _jobs = jobs;
            _requests = requests;
        }

        // Stato per la view
        public MWBotModel? Bot { get; set; }                    // Stato del robot (IsBusy, CurrentSpotId, ecc.)
        public ChargeJob? RunningJob { get; set; }              // Eventuale job in esecuzione
        public ChargeRequest? RunningRequest { get; set; }      // Richiesta associata al job in esecuzione
        public List<(ChargeJob Job, ChargeRequest Req)> Queue { get; set; } = new(); // Coda dei job (con relativa richiesta)

        public async Task OnGet()
        {
            await LoadStateAsync();
        }

        // Carica lo stato corrente per popolare la pagina:
        //  - bot
        //  - job in esecuzione (+ relativa richiesta)
        //  - coda dei job (+ relative richieste)
        private async Task LoadStateAsync()
        {
            Bot = await _botRepo.GetSingletonAsync();
            RunningJob = await _jobs.GetRunningAsync();

            if (RunningJob != null)
                RunningRequest = await _requests.GetByIdAsync(RunningJob.ChargeRequestId);

            var queued = await _jobs.ListQueuedAsync();
            Queue.Clear();
            foreach (var j in queued)
            {
                var r = await _requests.GetByIdAsync(j.ChargeRequestId);
                Queue.Add((j, r!));
            }
        }

        // Avvia il prossimo job in coda (FIFO in base a RequestedAtUtc/Id).
        // Se non ci sono job, mostra messaggio informativo.
        public async Task<IActionResult> OnPostStartNextAsync()
        {
            var next = await _jobs.NextQueuedAsync();
            if (next == null)
            {
                TempData["Msg"] = "Nessun job in coda.";
                return RedirectToPage();
            }
            return await StartJobInternalAsync(next.Id);
        }

        // Avvia uno specifico job (invocato dai pulsanti per riga nella tabella coda).
        public async Task<IActionResult> OnPostStartAsync(int jobId)
        {
            return await StartJobInternalAsync(jobId);
        }

        // Logica condivisa di avvio job:
        //  - verifica che il job sia in stato Queued,
        //  - verifica che la richiesta sia Pending e abbia una sessione valida,
        //  - verifica che il bot esista e non sia occupato,
        //  - passa job -> Running (StartUtc),
        //  - passa request -> InProgress,
        //  - setta bot -> Busy + CurrentSpot,
        //  - salva le modifiche (unico SaveChangesAsync: stesso DbContext scoped).
        private async Task<IActionResult> StartJobInternalAsync(int jobId)
        {
            var job = await _jobs.GetByIdAsync(jobId);
            if (job == null || job.Status != ChargeJobStatus.Queued)
            {
                TempData["Err"] = "Job non valido o non in coda.";
                return RedirectToPage();
            }

            var req = await _requests.GetByIdAsync(job.ChargeRequestId);
            if (req == null || req.Status != ChargeRequestStatus.Pending || req.ParkingSession == null)
            {
                TempData["Err"] = "Richiesta non valida per avvio.";
                return RedirectToPage();
            }

            var bot = await _botRepo.GetSingletonAsync();
            if (bot == null)
            {
                TempData["Err"] = "MWBot non inizializzato.";
                return RedirectToPage();
            }
            if (bot.IsBusy)
            {
                TempData["Err"] = "Il MWBot è già occupato.";
                return RedirectToPage();
            }

            var now = DateTime.UtcNow;
            job.Status = ChargeJobStatus.Running;
            job.StartUtc = now;

            req.Status = ChargeRequestStatus.InProgress;

            bot.IsBusy = true;
            bot.CurrentSpotId = req.ParkingSession.ParkingSpotId;
            bot.LastUpdateUtc = now;

            _jobs.Update(job);
            _requests.Update(req);
            _botRepo.Update(bot);
            await _jobs.SaveChangesAsync(); // stesso DbContext: salva tutto

            TempData["Msg"] = $"Job {job.Id} avviato.";
            return RedirectToPage();
        }

        // Conclude il job in esecuzione impostando:
        //  - energia erogata (kWh),
        //  - SoC finale (%),
        // e aggiornando gli stati:
        //  - job -> Finished (EndUtc),
        //  - request -> Completed,
        //  - bot -> non busy, CurrentSpotId null.
        public async Task<IActionResult> OnPostFinishAsync(int jobId, double energyKWh, int finalSoCPercent)
        {
            // Validazione input: energia non negativa; SoC 0..100
            if (energyKWh < 0 || finalSoCPercent < 0 || finalSoCPercent > 100)
            {
                TempData["Err"] = "Valori non validi.";
                return RedirectToPage();
            }

            var job = await _jobs.GetByIdAsync(jobId);
            if (job == null || job.Status != ChargeJobStatus.Running)
            {
                TempData["Err"] = "Nessun job in esecuzione con l'id indicato.";
                return RedirectToPage();
            }

            var req = await _requests.GetByIdAsync(job.ChargeRequestId);
            if (req == null)
            {
                TempData["Err"] = "Richiesta associata non trovata.";
                return RedirectToPage();
            }

            var bot = await _botRepo.GetSingletonAsync();
            var now = DateTime.UtcNow;

            job.Status = ChargeJobStatus.Finished;
            job.EndUtc = now;
            job.EnergyKWh = energyKWh;
            job.FinalSoCPercent = finalSoCPercent;

            req.Status = ChargeRequestStatus.Completed;

            if (bot != null)
            {
                bot.IsBusy = false;
                bot.CurrentSpotId = null;
                bot.LastUpdateUtc = now;
                _botRepo.Update(bot);
            }

            _jobs.Update(job);
            _requests.Update(req);
            await _jobs.SaveChangesAsync();

            TempData["Msg"] = $"Job {job.Id} concluso: {energyKWh:0.###} kWh, SoC {finalSoCPercent}%.";
            return RedirectToPage();
        }

        // Annulla/abortisce un job:
        //  - ammesso per job in coda (Queued) o in esecuzione (Running),
        //  - job -> Aborted (se era Running, setta EndUtc),
        //  - request associata -> Cancelled se era Proposed/Pending/InProgress,
        //  - bot -> resettato se era occupato.
        public async Task<IActionResult> OnPostAbortAsync(int jobId)
        {
            var job = await _jobs.GetByIdAsync(jobId);
            if (job == null || (job.Status != ChargeJobStatus.Queued && job.Status != ChargeJobStatus.Running))
            {
                TempData["Err"] = "Job non in stato abortibile.";
                return RedirectToPage();
            }

            var req = await _requests.GetByIdAsync(job.ChargeRequestId);
            var bot = await _botRepo.GetSingletonAsync();
            var now = DateTime.UtcNow;

            job.Status = ChargeJobStatus.Aborted;
            if (job.Status == ChargeJobStatus.Running) job.EndUtc = now; // se era in esecuzione

            if (req != null && (req.Status == ChargeRequestStatus.Pending || req.Status == ChargeRequestStatus.InProgress || req.Status == ChargeRequestStatus.Proposed))
            {
                req.Status = ChargeRequestStatus.Cancelled;
                _requests.Update(req);
            }

            if (bot != null && bot.IsBusy)
            {
                bot.IsBusy = false;
                bot.CurrentSpotId = null;
                bot.LastUpdateUtc = now;
                _botRepo.Update(bot);
            }

            _jobs.Update(job);
            await _jobs.SaveChangesAsync();

            TempData["Msg"] = $"Job {job.Id} abortito.";
            return RedirectToPage();
        }
    }
}
