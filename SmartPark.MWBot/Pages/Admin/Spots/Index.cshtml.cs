using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;

namespace SmartPark.MWBot.Pages.Admin.Spots;

// Pagina di sola consultazione per lo stato dei posti auto (solo Admin).
// Mostra l'elenco dei ParkingSpot con i relativi flag (IsOccupied, ecc.).
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IParkingSpotRepository _spots; // Repo per accesso ai posti

    // Collezione che la view renderizza (tabella dei posti)
    public List<ParkingSpot> Items { get; set; } = new();

    // DI del repository
    public IndexModel(IParkingSpotRepository spots) => _spots = spots;

    // GET: carica tutti i posti in sola lettura (ListAsync usa AsNoTracking nel repo)
    public async Task OnGet()
    {
        Items = await _spots.ListAsync();
    }
}
