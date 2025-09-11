using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class CarModel
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Make { get; set; } = ""; // es. Tesla

        [Required, MaxLength(64)]
        public string Model { get; set; } = ""; // es. Model 3

        // Capacità batteria in kWh (non kW!)
        [Range(5, 300)]
        public double BatteryCapacityKWh { get; set; }
    }
}
