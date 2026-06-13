using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class CinemaHall
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int TotalSeats { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual User? UpdatedByNavigation { get; set; }
}
