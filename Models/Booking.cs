using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ShowtimeId { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? BookingTime { get; set; }

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User User { get; set; } = null!;
}
