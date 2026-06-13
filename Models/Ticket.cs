using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class Ticket
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int SeatId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Seat Seat { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
