using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class ChargeRequest
    {
        public int Id { get; set; }

        [Required]
        public int ParkingSessionId { get; set; }
        public ParkingSession? ParkingSession { get; set; }

        // Stato di carica target desiderato dall’utente (0–100)
        [Range(1, 100)]
        public int TargetSoCPercent { get; set; }

        // SoC iniziale stimato al momento della richiesta (opzionale)
        [Range(0, 100)]
        public int? InitialSoCPercent { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

        public ChargeRequestStatus Status { get; set; } = ChargeRequestStatus.Pending;

        // Stima consegnata all’utente (facoltativa)
        public int? EstimatedWaitMinutes { get; set; }
        public int? EstimatedCompletionMinutes { get; set; }
    }
}
