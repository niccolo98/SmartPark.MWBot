
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartPark.MWBot.Pages
{
    // Pagina Home (landing) dell'applicazione.
 
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger; // logger DI per eventuali diagnostiche/telemetria

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        
        public void OnGet()
        {

        }
    }
}
