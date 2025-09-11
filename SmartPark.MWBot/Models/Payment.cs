using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        // Riferimenti utili per reporting
        public int? ParkingSessionId { get; set; }
        public ParkingSession? ParkingSession { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Totale finale (somma delle righe)
        [Range(0, 100000)]
        public double TotalAmount { get; set; }

        // Tipo utente al momento del pagamento (per storicizzare)
        public UserType UserTypeAtPayment { get; set; }
    }

    public class PaymentLine
    {
        public int Id { get; set; }

        [Required]
        public int PaymentId { get; set; }
        public Payment? Payment { get; set; }

        [Required, MaxLength(32)]
        public string LineType { get; set; } = ""; // "Parking" | "Charging"

        // Quantità e prezzo unitario (per trasparenza)
        public double Quantity { get; set; } // ore, kWh, ecc.
        public double UnitPrice { get; set; } // €/ora o €/kWh

        public double LineTotal { get; set; } // Quantity * UnitPrice (dopo sconti)
    }
}
