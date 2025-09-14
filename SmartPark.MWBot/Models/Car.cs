using System.ComponentModel.DataAnnotations;


namespace SmartPark.MWBot.Models
{
    public class Car
    {
        public int Id { get; set; }

        [Required]
        public int CarModelId { get; set; }
        public CarModel? CarModel { get; set; }

        [Required, MaxLength(16)]
        public string Plate { get; set; } = ""; // targa

        [Required]
        public string UserId { get; set; } = ""; // FK verso AspNetUsers.Id

        // Stato di carica noto all’ingresso (opzionale)
        [Range(0, 100)]
        public int? InitialSoCPercent { get; set; }
    }
}
