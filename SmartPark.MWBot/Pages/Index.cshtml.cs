using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartPark.MWBot.Pages
{
    // Pagina Home (landing) dell'applicazione.
    // In questa soluzione funge da semplice entry-point con contenuto renderizzato da Index.cshtml.
    // Qui si potrebbero:
    //  - loggare accessi/metriche (uso di _logger),
    //  - pre-caricare dati sintetici da mostrare in home,
    //  - personalizzare il contenuto in base all'utente (es. Admin vs utente base).
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger; // logger DI per eventuali diagnostiche/telemetria

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Handler GET: al momento non fa nulla (pagina statica).
        // Se necessario, qui si potrebbero popolare proprietà da usare nella view.
        public void OnGet()
        {

        }
    }
}
