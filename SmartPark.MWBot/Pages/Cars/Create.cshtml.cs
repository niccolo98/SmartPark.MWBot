using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPark.MWBot.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartPark.MWBot.Pages.Cars
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ICarRepository _cars;
        private readonly ICarModelRepository _models;

        // Popola la tendina dei modelli disponibili (read-only)
        public List<CarModel> AvailableModels { get; set; } = new();

        // Dati del form (bind dal POST)
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // ViewModel per l’inserimento di una nuova auto
        public class InputModel
        {
            [Required]
            public int CarModelId { get; set; }  // FK verso CarModel

            [Required, MaxLength(16)]
            public string Plate { get; set; } = ""; // Targa (verrà normalizzata in uppercase)
        }

        public CreateModel(ICarRepository cars, ICarModelRepository models)
        {
            _cars = cars;
            _models = models;
        }

        // GET: carica i modelli per la select
        public async Task OnGet()
        {
            AvailableModels = await _models.ListAsync();
        }

        // POST: valida input, verifica univocità targa, crea entità e salva
        public async Task<IActionResult> OnPostAsync()
        {
            // Ricarica i modelli in caso di validazione fallita per ripopolare la select
            AvailableModels = await _models.ListAsync();
            if (!ModelState.IsValid) return Page();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            // Normalizza targa
            var plate = Input.Plate.Trim().ToUpperInvariant();

            // Check univocità targa
            var existing = await _cars.GetByPlateAsync(plate);
            if (existing != null)
            {
                ModelState.AddModelError("Input.Plate", "Questa targa è già registrata.");
                return Page();
            }

            // Crea entità


            var entity = new Car
            {
                CarModelId = Input.CarModelId,
                Plate = Input.Plate.Trim().ToUpperInvariant(), // coerenza con normalizzazione usata nel controllo
                UserId = userId
            };

            // AddAsync prepara l’inserimento nel change tracker; la persistenza avviene con SaveChangesAsync
            await _cars.AddAsync(entity);
            await _cars.SaveChangesAsync();

            // PRG pattern: redirect per evitare il repost del form
            return RedirectToPage("Index");
        }
    }
}
