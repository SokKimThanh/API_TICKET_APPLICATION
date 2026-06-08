using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class Seat
{
    public int Id { get; set; }

    public int CinemaHallId { get; set; }

    public string Row { get; set; } = null!;

    public int Number { get; set; }

    public string SeatType { get; set; } = null!;

    public virtual CinemaHall CinemaHall { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
