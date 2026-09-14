using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HotelManagementSystem.Models
{
    public partial class Booking
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int RoomId { get; set; }

        public DateTime BookingDate { get; set; }

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        [NotMapped]
        public string Status { get; set; } = "Confirmed";

        public virtual Bill? Bill { get; set; }

        public virtual CheckIn? CheckIn { get; set; }

        [ValidateNever]
        public virtual Customer Customer { get; set; } = null!;

        [ValidateNever]
        public virtual Room Room { get; set; } = null!;
    }
}