using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class Bill
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public bool IsPaid { get; set; } = false;
    }
}