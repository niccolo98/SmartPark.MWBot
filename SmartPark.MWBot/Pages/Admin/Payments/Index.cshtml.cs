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

namespace SmartPark.MWBot.Pages.Admin.Payments
{
    // Pagina di reportistica pagamenti riservata agli amministratori.
    // Consente filtro per intervallo temporale e calcola:
    //  - totali complessivi (sosta/ricarica),
    //  - split Base/Premium,
    //  - dettaglio riga per singolo pagamento (con email utente).
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IPaymentRepository _payments;
        private readonly IPaymentLineRepository _lines;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            IPaymentRepository payments,
            IPaymentLineRepository lines,
            UserManager<ApplicationUser> userManager)
        {
            _payments = payments;
            _lines = lines;
            _userManager = userManager;
        }

        // -------------------------
        // Parametri di filtro (bind dal querystring)
        // From/To sono in ORA LOCALE (vengono poi convertiti in UTC per interrogare il DB).
        // -------------------------
        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; } // local time
        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }   // local time

        // -------------------------
        // Dati per la view
        // -------------------------
        public List<Row> Items { get; set; } = new();

        // Totali complessivi
        public double TotalParking { get; set; }
        public double TotalCharging { get; set; }
        public double GrandTotal { get; set; }

        // Totali per tipo utente
        public double TotalParkingBase { get; set; }
        public double TotalChargingBase { get; set; }
        public double TotalParkingPremium { get; set; }
        public double TotalChargingPremium { get; set; }

        // Conteggi
        public int CountPayments { get; set; }
        public int CountBase { get; set; }
        public int CountPremium { get; set; }

        // DTO per la griglia
        public class Row
        {
            public int Id { get; set; }
            public DateTime CreatedUtc { get; set; }    // timestamp in UTC (valore nel DB)
            public DateTime CreatedLocal { get; set; }  // conversione a ora locale per UI
            public string UserId { get; set; } = "";
            public string? UserEmail { get; set; }
            public UserType UserTypeAtPayment { get; set; } // fotografia del tipo utente al momento del pagamento

            // Breakdown importi
            public double Parking { get; set; }
            public double Charging { get; set; }
            // Totale riga arrotondato a 2 decimali (round half away from zero per prezzi)
            public double Total => Math.Round(Parking + Charging, 2, MidpointRounding.AwayFromZero);
        }

        public async Task OnGet()
        {
            // -------------------------
            // Intervallo di default: ultimi 7 giorni (in locale)
            // From: mezzanotte di 7 giorni fa
            // To:   adesso
            // -------------------------
            var nowLocal = DateTime.Now;
            var fromLocal = From ?? nowLocal.AddDays(-7).Date; // da mezzanotte 7 gg fa
            var toLocal = To ?? nowLocal;                      // adesso

            // -------------------------
            // Conversione a UTC per il filtro DB:
            // CreatedUtc nel DB è in UTC, quindi si filtra coerentemente in UTC.
            // -------------------------
            var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var toUtc = DateTime.SpecifyKind(toLocal, DateTimeKind.Local).ToUniversalTime();

            // Recupera i pagamenti nell'intervallo richiesto (in UTC).
            var pays = await _payments.ListByRangeAsync(fromUtc, toUtc);

            CountPayments = pays.Count;

            // Elaborazione di ogni pagamento:
            //  - carica le righe (PaymentLine) per calcolare sosta/ricarica,
            //  - recupera l'email utente (facoltativa, se l'utente esiste ancora),
            //  - popola la lista Items,
            //  - aggiorna i totali globali e per tipo utente.
            foreach (var p in pays)
            {
                var lines = await _lines.ListByPaymentAsync(p.Id);

                // Somma per tipologia riga con arrotondamento commerciale (2 decimali, away from zero).
                var parking = Math.Round(lines.Where(l => l.LineType == "Parking").Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);
                var charging = Math.Round(lines.Where(l => l.LineType == "Charging").Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);

                // Email utente (potrebbe essere null se l'utente è stato rimosso).
                string? userEmail = null;
                var user = await _userManager.FindByIdAsync(p.UserId);
                if (user != null) userEmail = user.Email;

                Items.Add(new Row
                {
                    Id = p.Id,
                    CreatedUtc = p.CreatedUtc,
                    CreatedLocal = p.CreatedUtc.ToLocalTime(), // conversione per visualizzazione
                    UserId = p.UserId,
                    UserEmail = userEmail,
                    UserTypeAtPayment = p.UserTypeAtPayment,
                    Parking = parking,
                    Charging = charging
                });

                // Aggregazioni globali (complessivi)
                TotalParking += parking;
                TotalCharging += charging;

                // Split per tipo utente: i contatori e i totali parziali
                if (p.UserTypeAtPayment == UserType.Premium)
                {
                    CountPremium++;
                    TotalParkingPremium += parking;
                    TotalChargingPremium += charging;
                }
                else
                {
                    CountBase++;
                    TotalParkingBase += parking;
                    TotalChargingBase += charging;
                }
            }

            // Totale generale arrotondato a 2 decimali (coerente con singole voci).
            GrandTotal = Math.Round(TotalParking + TotalCharging, 2, MidpointRounding.AwayFromZero);
        }
    }
}
