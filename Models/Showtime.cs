using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class Showtime
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public int CinemaHallId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public decimal BasePrice { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual CinemaHall CinemaHall { get; set; } = null!;

    public virtual Movie Movie { get; set; } = null!;
}
