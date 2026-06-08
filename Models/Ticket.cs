using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class Ticket
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int SeatId { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;
}
