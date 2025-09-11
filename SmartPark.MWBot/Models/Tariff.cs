using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class Tariff
    {
        public int Id { get; set; }

        // €/ora sosta
        [Range(0, 1000)]
        public double ParkingPerHour { get; set; }

        // €/kWh ricarica
        [Range(0, 1000)]
        public double EnergyPerKWh { get; set; }

        // Validità tariffa
        public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ValidToUtc { get; set; } // null = corrente
    }
}
