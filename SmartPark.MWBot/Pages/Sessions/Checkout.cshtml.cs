using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Sessions
{
    // Pagina di checkout: chiude una sessione aperta, calcola costi, crea Payment + PaymentLine,
    // libera il posto e annulla eventuali richieste/job non conclusi.
    // Accesso consentito solo all'utente proprietario (controllo su UserId).
    [Authorize]
    public class CheckoutModel : PageModel
    {
        // Repo necessari per leggere/aggiornare sessione, posto, tariffe, ricariche e pagamenti
        private readonly IParkingSessionRepository _sessions;
        private readonly IParkingSpotRepository _spots;
        private readonly ITariffRepository _tariffs;
        private readonly IChargeRequestRepository _requests;
        private readonly IChargeJobRepository _jobs;
        private readonly IPaymentRepository _payments;
        private readonly IPaymentLineRepository _paymentLines;
        private readonly UserManager<ApplicationUser> _userManager;

        // Dati per la view
        public ParkingSession? Session { get; set; }
        public Tariff? CurrentTariff { get; set; }

        // Riepilogo calcolato (anteprima + conferma)
        public int TotalMinutes { get; set; }
        public double TotalHours { get; set; }
        public double EnergyKWh { get; set; }
        public double ParkingRatePerHour { get; set; }
        public double EnergyRatePerKWh { get; set; }
        public double ParkingCost { get; set; }
        public double EnergyCost { get; set; }
        public double TotalCost { get; set; }

        public CheckoutModel(
            IParkingSessionRepository sessions,
            IParkingSpotRepository spots,
            ITariffRepository tariffs,
            IChargeRequestRepository requests,
            IChargeJobRepository jobs,
            IPaymentRepository payments,
            IPaymentLineRepository paymentLines,
            UserManager<ApplicationUser> userManager)
        {
            _sessions = sessions;
            _spots = spots;
            _tariffs = tariffs;
            _requests = requests;
            _jobs = jobs;
            _payments = payments;
            _paymentLines = paymentLines;
            _userManager = userManager;
        }

        // GET: mostra anteprima di costi/energia sulla sessione da chiudere.
        public async Task<IActionResult> OnGet(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            Session = await _sessions.GetByIdAsync(id);
            if (Session == null || Session.UserId != userId || Session.Status != ParkingSessionStatus.Open)
                return NotFound();

            var now = DateTime.UtcNow;
            CurrentTariff = await _tariffs.GetCurrentAsync(now);
            if (CurrentTariff == null)
            {
                // Nessuna tariffa in vigore: impedisce il checkout 
                ModelState.AddModelError(string.Empty, "Nessuna tariffa attiva. Contattare l'amministratore.");
                return Page();
            }

            // Calcolo anteprima (senza sconti): minuti/ore, kWh finiti, costi correnti
            await ComputePreviewAsync(Session, CurrentTariff, now);

            return Page();
        }

        // POST: conferma il checkout, ricalcola i valori per sicurezza,
        // applica eventuali sconti Premium, chiude la sessione, libera il posto,
        // annulla richieste/job pendenti e crea Payment + righe.
        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            Session = await _sessions.GetByIdAsync(id);
            if (Session == null || Session.UserId != userId || Session.Status != ParkingSessionStatus.Open)
                return NotFound();

            var now = DateTime.UtcNow;
            CurrentTariff = await _tariffs.GetCurrentAsync(now);
            if (CurrentTariff == null)
            {
                ModelState.AddModelError(string.Empty, "Nessuna tariffa attiva. Contattare l'amministratore.");
                return Page();
            }

            // Ricalcola al momento del POST (difende da manipolazioni lato client)
            await ComputePreviewAsync(Session, CurrentTariff, now);

            // Applica sconti per utenti Premium (se presenti)
            var appUser = await _userManager.GetUserAsync(User);
            var parkingRate = ParkingRatePerHour;
            var energyRate = EnergyRatePerKWh;

            if (appUser?.Type == UserType.Premium)
            {
                if (appUser.ParkingDiscount.HasValue && appUser.ParkingDiscount.Value > 0)
                    parkingRate = Math.Max(0, parkingRate * (1 - appUser.ParkingDiscount.Value));
                if (appUser.ChargingDiscount.HasValue && appUser.ChargingDiscount.Value > 0)
                    energyRate = Math.Max(0, energyRate * (1 - appUser.ChargingDiscount.Value));
            }

            // Calcoli finali arrotondati (prezzi al centesimo; ore/kWh con 3 decimali nelle righe)
            var parkingCost = Math.Round(TotalHours * parkingRate, 2, MidpointRounding.AwayFromZero);
            var energyCost = Math.Round(EnergyKWh * energyRate, 2, MidpointRounding.AwayFromZero);
            var total = Math.Round(parkingCost + energyCost, 2, MidpointRounding.AwayFromZero);

            // --- Chiusura sessione ---
            Session.EndUtc = now;
            Session.Status = ParkingSessionStatus.Closed;
            Session.TotalMinutes = TotalMinutes;

            // (Opzionale) minuti ricarica: somma durate dei job conclusi (Finished) della sessione
            var requests = await _requests.ListBySessionAsync(Session.Id);

            var finishedJobs = new List<ChargeJob>();
            foreach (var r in requests)
            {
                var jobs = await _jobs.ListByRequestAsync(r.Id);
                finishedJobs.AddRange(jobs.Where(j => j.Status == ChargeJobStatus.Finished && j.StartUtc.HasValue && j.EndUtc.HasValue));
            }
            var chargingMinutes = finishedJobs.Sum(j => (int)Math.Max(0, (j.EndUtc!.Value - j.StartUtc!.Value).TotalMinutes));
            Session.ChargingMinutes = chargingMinutes;

            _sessions.Update(Session);

            // Libera il posto (lo stato sensore è simulato via flag IsOccupied)
            if (Session.ParkingSpotId != 0)
            {
                var spot = await _spots.GetByIdAsync(Session.ParkingSpotId);
                if (spot != null)
                {
                    spot.IsOccupied = false;
                    spot.SensorLastUpdateUtc = now;
                    _spots.Update(spot);
                }
            }

            // Annulla eventuali richieste/Job non conclusi legati alla sessione (coerenza sistema)
            foreach (var r in requests)
            {
                if (r.Status == ChargeRequestStatus.Pending || r.Status == ChargeRequestStatus.InProgress)
                {
                    r.Status = ChargeRequestStatus.Cancelled;
                    _requests.Update(r);
                }

                var jobs = await _jobs.ListByRequestAsync(r.Id);
                foreach (var j in jobs.Where(j => j.Status == ChargeJobStatus.Queued || j.Status == ChargeJobStatus.Running))
                {
                    j.Status = ChargeJobStatus.Aborted;
                    j.EndUtc = now;
                    _jobs.Update(j);
                }
            }

            // --- Creazione pagamento (testa) ---
            var payment = new Payment
            {
                UserId = userId,
                ParkingSessionId = Session.Id,
                CreatedUtc = now,
                UserTypeAtPayment = appUser?.Type ?? UserType.Base, // fotografia del tipo utente al momento del pagamento
                TotalAmount = total
            };

            await _payments.AddAsync(payment);
            await _payments.SaveChangesAsync(); // necessario per ottenere l'Id della testa (PaymentId delle righe)

            // --- Righe pagamento (sosta sempre, ricarica solo se > 0) ---
            var lines = new List<PaymentLine>
            {
                new PaymentLine
                {
                    PaymentId = payment.Id,
                    LineType = "Parking",
                    Quantity = Math.Round(TotalHours, 3, MidpointRounding.AwayFromZero), // ore pro-rata
                    UnitPrice = parkingRate,
                    LineTotal = parkingCost
                }
            };

            if (energyCost > 0.0 && EnergyKWh > 0.0)
            {
                lines.Add(new PaymentLine
                {
                    PaymentId = payment.Id,
                    LineType = "Charging",
                    Quantity = Math.Round(EnergyKWh, 3, MidpointRounding.AwayFromZero), // kWh erogati
                    UnitPrice = energyRate,
                    LineTotal = energyCost
                });
            }

            await _paymentLines.AddRangeAsync(lines);
            await _paymentLines.SaveChangesAsync(); // persiste le righe

            // Messaggio di conferma per l'utente (riepilogo importi)
            TempData["Msg"] = $"Sessione chiusa. Totale: € {total:0.00} (Sosta € {parkingCost:0.00}{(energyCost > 0 ? $", Ricarica € {energyCost:0.00}" : "")}).";
            return RedirectToPage("Index");
        }

        // Calcolo dell’anteprima di checkout (GET e ricalcolo nel POST):
        // - minuti/ore trascorsi,
        // - somma kWh dei soli job FINISHED,
        // - applicazione tariffe correnti (senza sconti),
        // - calcolo costi base e totale.
        private async Task ComputePreviewAsync(ParkingSession session, Tariff tariff, DateTime nowUtc)
        {
            // Totale minuti sosta (non negativo)
            TotalMinutes = (int)Math.Max(0, (nowUtc - session.StartUtc).TotalMinutes);
            TotalHours = TotalMinutes / 60.0;

            // Somma energia ricaricata: solo job FINISHED (gli altri stati non fatturano)
            var requests = (await _requests.ListAsync()).Where(r => r.ParkingSessionId == session.Id).ToList();
            double energy = 0;
            foreach (var r in requests)
            {
                var jobs = await _jobs.ListByRequestAsync(r.Id);
                energy += jobs.Where(j => j.Status == ChargeJobStatus.Finished && j.EnergyKWh.HasValue)
                              .Sum(j => j.EnergyKWh!.Value);
            }
            EnergyKWh = Math.Round(energy, 3, MidpointRounding.AwayFromZero);

            // Tariffe correnti (senza sconti; gli sconti si applicano solo nel POST)
            ParkingRatePerHour = tariff.ParkingPerHour;
            EnergyRatePerKWh = tariff.EnergyPerKWh;

            // Pre-calcolo anteprima (senza sconti, due decimali)
            ParkingCost = Math.Round(TotalHours * ParkingRatePerHour, 2, MidpointRounding.AwayFromZero);
            EnergyCost = Math.Round(EnergyKWh * EnergyRatePerKWh, 2, MidpointRounding.AwayFromZero);
            TotalCost = Math.Round(ParkingCost + EnergyCost, 2, MidpointRounding.AwayFromZero);
        }
    }
}
