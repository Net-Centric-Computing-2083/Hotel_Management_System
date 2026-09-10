using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models;

public partial class CheckOut
{
    public int Id { get; set; }

    public int CheckInId { get; set; }

    public DateTime CheckOutDateTime { get; set; }

    public virtual CheckIn CheckIn { get; set; } = null!;
}
