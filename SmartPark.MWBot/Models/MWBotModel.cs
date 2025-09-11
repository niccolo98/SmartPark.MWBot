using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class MWBotModel
    {
        public int Id { get; set; } // avremo un record con Id=1

        // Posto su cui si trova (se null sta girando o in transito)
        public int? CurrentSpotId { get; set; }
        public ParkingSpot? CurrentSpot { get; set; }

        // Stato batteria del robot (se serve)
        [Range(0, 100)]
        public int? BatteryPercent { get; set; }

        // Potenza massima di erogazione (kW) per calcolo tempi
        [Range(1, 500)]
        public double MaxPowerKW { get; set; } = 22;

        public bool IsBusy { get; set; } // sta ricaricando?
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;
    }
}
