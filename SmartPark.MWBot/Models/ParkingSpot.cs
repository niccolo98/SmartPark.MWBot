using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class ParkingSpot
    {
        public int Id { get; set; }

        [Required, MaxLength(16)]
        public string Code { get; set; } = ""; // es. "P01"

        public bool IsOccupied { get; set; }
        public DateTime SensorLastUpdateUtc { get; set; }
    }
}
