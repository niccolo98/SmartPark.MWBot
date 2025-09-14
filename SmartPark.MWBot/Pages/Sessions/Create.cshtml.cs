using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;


namespace SmartPark.MWBot.Pages.Sessions
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IParkingSpotRepository _spots;
        private readonly ICarRepository _cars;
        private readonly IParkingSessionRepository _sessions;

        // Liste per popolare le select nella view
        public List<ParkingSpot> FreeSpots { get; set; } = new();
        public List<Car> MyCars { get; set; } = new();

        // Modello bindato dal form
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // ViewModel di input: scelta posto libero + auto dell’utente
        public class InputModel
        {
            [Required]
            public int ParkingSpotId { get; set; }

            [Required]
            public int CarId { get; set; }
        }

        public CreateModel(IParkingSpotRepository spots, ICarRepository cars, IParkingSessionRepository sessions)
        {
            _spots = spots;
            _cars = cars;
            _sessions = sessions;
        }

        // GET: carica le auto dell'utente e i posti liberi (snapshot)
        public async Task OnGet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            MyCars = await _cars.ListByUserAsync(userId);
            var allSpots = await _spots.ListAsync();
            FreeSpots = allSpots.Where(s => !s.IsOccupied).ToList();
        }

        // POST: crea una nuova sessione di sosta
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Ricarico liste per la view in caso di errore (così la pagina ha i dati per ripresentare il form)
            MyCars = await _cars.ListByUserAsync(userId);
            var allSpots = await _spots.ListAsync();
            FreeSpots = allSpots.Where(s => !s.IsOccupied).ToList();

            if (!ModelState.IsValid) return Page();

            // Verifica che il posto selezionato sia tra quelli liberi nello snapshot
            var spot = FreeSpots.FirstOrDefault(s => s.Id == Input.ParkingSpotId);
            if (spot == null)
            {
                ModelState.AddModelError(string.Empty, "Il posto selezionato non è disponibile.");
                return Page();
            }

            // Verifica che l’auto appartenga all’utente
            var car = MyCars.FirstOrDefault(c => c.Id == Input.CarId);
            if (car == null)
            {
                ModelState.AddModelError(string.Empty, "Auto non valida.");
                return Page();
            }
            // Ricontrollo che il posto sia ancora libero (sensore o altra sessione potrebbe averlo occupato)
            var spotFresh = await _spots.GetByIdAsync(spot.Id);
            if (spotFresh == null || spotFresh.IsOccupied)
            {
                ModelState.AddModelError(string.Empty, "Il posto selezionato risulta occupato. Aggiorna l'elenco e riprova.");
                return Page();
            }

            // Vincolo: una sola sessione aperta su questo posto
            var openOnSpot = await _sessions.GetOpenBySpotAsync(spot.Id);
            if (openOnSpot != null)
            {
                ModelState.AddModelError(string.Empty, "Esiste già una sessione aperta su questo posto.");
                return Page();
            }

            // Vincolo: una sola sessione aperta per la stessa auto dell’utente
            var myOpenSessions = await _sessions.ListOpenByUserAsync(userId);
            if (myOpenSessions.Any(s => s.CarId == car.Id))
            {
                ModelState.AddModelError(string.Empty, "Hai già una sessione aperta con questa auto.");
                return Page();
            }


            // Crea sessione (stato Open)
            var session = new ParkingSession
            {
                ParkingSpotId = spotFresh.Id,
                CarId = car.Id,
                UserId = userId,
                Status = ParkingSessionStatus.Open,
                StartUtc = DateTime.UtcNow
            };

            await _sessions.AddAsync(session);

            // Marca il posto occupato (sullo snapshot aggiornato)
            spotFresh.IsOccupied = true;
            spotFresh.SensorLastUpdateUtc = DateTime.UtcNow;
            _spots.Update(spotFresh);

            // Persistenza modifiche:
            // - Add sessione
            // - Update posto
            // NB: i repository condividono lo stesso DbContext scoped
            await _sessions.SaveChangesAsync();


            // Redirect alla lista sessioni (PRG pattern)
            return RedirectToPage("Index");
        }
    }
}
