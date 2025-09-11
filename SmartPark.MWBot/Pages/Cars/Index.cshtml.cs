using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;                // <-- per IActionResult
using SmartPark.MWBot.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Pages.Cars
{
    // Pagina "Le mie auto": mostra le auto dell'utente loggato e permette la cancellazione.
    // Accesso riservato ad utenti autenticati.
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ICarRepository _cars;                 // Repo per accesso/CRUD sulle auto
        private readonly IParkingSessionRepository _sessions;  // Repo per verificare sessioni collegate all'auto (coerenza)

        // Collezione che la view renderizza (tabella auto)
        public List<Car> Items { get; set; } = new();

        // DI dei repository necessari (auto + sessioni)
        public IndexModel(ICarRepository cars, IParkingSessionRepository sessions) // <-- inject
        {
            _cars = cars;
            _sessions = sessions;
        }

        // GET: carica l'elenco delle auto dell'utente corrente (read-only)
        public async Task OnGet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!; // Id Identity dell'utente loggato
            Items = await _cars.ListByUserAsync(userId);
        }

        // POST: elimina un'auto di proprietà dell'utente.
        // Vincoli di coerenza:
        //  - l'auto deve esistere ed essere dell'utente corrente;
        //  - viene impedita la cancellazione se esistono sessioni collegate (anche chiuse),
        //    per evitare violazioni FK e preservare lo storico.
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var car = await _cars.GetByIdAsync(id);
            if (car == null || car.UserId != userId)
                return NotFound(); // auto inesistente o non di proprietà → 404

            // Blocco se esistono sessioni (anche chiuse) collegate a quell’auto
            if (await _sessions.AnyByCarAsync(id))
            {
                TempData["Err"] = "Impossibile eliminare: l'auto ha sessioni associate.";
                return RedirectToPage(); // PRG: ritorna alla pagina con messaggio di errore
            }

            // Cancellazione auto + persistenza
            _cars.Remove(car);
            await _cars.SaveChangesAsync(); // salva le modifiche nel DbContext corrente

            TempData["Msg"] = "Auto eliminata."; // feedback positivo per l'utente
            return RedirectToPage();             // redirect per evitare repost (PRG pattern)
        }
    }
}
