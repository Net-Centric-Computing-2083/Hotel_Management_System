using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementSystem.Models
{
    public partial class Bill
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public DateTime BillDate { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }

        public bool IsPaid { get; set; }

        // These are calculated values.
        // They are NOT stored in the Bills table.
        [NotMapped]
        public decimal RoomCharge { get; set; }

        [NotMapped]
        public decimal Tax { get; set; }

        [NotMapped]
        public decimal AdditionalCharge { get; set; }

        [NotMapped]
        public DateTime? PaymentDate { get; set; }

        public virtual Booking Booking { get; set; } = null!;
    }
}