using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class ParkingSession
    {
        public int Id { get; set; }

        [Required]
        public int ParkingSpotId { get; set; }
        public ParkingSpot? ParkingSpot { get; set; }

        [Required]
        public int CarId { get; set; }
        public Car? Car { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        public DateTime StartUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndUtc { get; set; }

        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Open;

        // Calcoli denormalizzati/rapidi (facoltativi)
        public int? TotalMinutes { get; set; }
        public int? ChargingMinutes { get; set; }
    }
}
