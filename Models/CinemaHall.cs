using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class CinemaHall
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int TotalSeats { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
