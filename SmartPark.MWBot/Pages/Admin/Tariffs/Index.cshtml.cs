using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;

namespace SmartPark.MWBot.Pages.Admin.Tariffs;

// Pagina admin per visualizzare la tariffa corrente e pubblicarne una nuova.
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ITariffRepository _tariffs;

    // Tariffa attualmente in vigore (se presente)
    public Tariff? Current { get; set; }

    // Modello bindato dal form per l'inserimento di una nuova tariffa
    // (ParkingPerHour, EnergyPerKWh; ValidFromUtc viene impostato lato server)
    [BindProperty]
    public Tariff Input { get; set; } = new();

    public IndexModel(ITariffRepository tariffs) => _tariffs = tariffs;

    // GET: carica la tariffa corrente usando l'orario UTC "adesso"
    public async Task OnGet()
    {
        Current = await _tariffs.GetCurrentAsync(DateTime.UtcNow);
    }

    // POST handler: pubblica una nuova tariffa "da adesso"
    // - valida il modello
    // - chiude la tariffa corrente (impostando ValidToUtc = now - 1s)
    // - crea la nuova con ValidFromUtc = now
    public async Task<IActionResult> OnPostNewAsync()
    {
        // Se validazione fallisce, ricarica la corrente e torna alla pagina
        if (!ModelState.IsValid) { await OnGet(); return Page(); }

        // Chiudi la tariffa corrente (se c'è)
        var now = DateTime.UtcNow;
        var cur = await _tariffs.GetCurrentAsync(now);
        if (cur != null)
        {
            // Evita overlap: chiude un secondo PRIMA dell'istante now
            cur.ValidToUtc = now.AddSeconds(-1);
            _tariffs.Update(cur);
            await _tariffs.SaveChangesAsync(); // persiste la chiusura della tariffa corrente
        }

        // Aggiungi la nuova (ValidFromUtc impostata server-side per coerenza)
        Input.ValidFromUtc = now;
        await _tariffs.AddAsync(Input);
        await _tariffs.SaveChangesAsync(); // persiste la nuova tariffa

        // Redirect per evitare repost del form (PRG pattern)
        return RedirectToPage();
    }
}
