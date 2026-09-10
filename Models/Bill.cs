using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models;

public partial class Bill
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public DateTime BillDate { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsPaid { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
