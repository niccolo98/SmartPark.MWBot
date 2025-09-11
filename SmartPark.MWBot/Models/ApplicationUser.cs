using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartPark.MWBot.Models
{
    public class ApplicationUser : IdentityUser
    {
        public UserType Type { get; set; } = UserType.Base;

        // Eventuali sconti (opzionali)
        public double? ChargingDiscount { get; set; }
        public double? ParkingDiscount { get; set; }
    }
}
