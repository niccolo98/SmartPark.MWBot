using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class ChargeJob
    {
        public int Id { get; set; }

        [Required]
        public int ChargeRequestId { get; set; }
        public ChargeRequest? ChargeRequest { get; set; }

        public ChargeJobStatus Status { get; set; } = ChargeJobStatus.Queued;

        public DateTime? StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }

        // Energia realmente erogata (kWh) per il costo
        [Range(0, 1000)]
        public double? EnergyKWh { get; set; }

        // SoC finale raggiunto
        [Range(0, 100)]
        public int? FinalSoCPercent { get; set; }
    }
}
