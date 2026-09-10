using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models;

public partial class CheckIn
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public DateTime CheckInDateTime { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual CheckOut? CheckOut { get; set; }
}
