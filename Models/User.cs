using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Booking> BookingCreatedByNavigations { get; set; } = new List<Booking>();

    public virtual ICollection<Booking> BookingUpdatedByNavigations { get; set; } = new List<Booking>();

    public virtual ICollection<Booking> BookingUsers { get; set; } = new List<Booking>();

    public virtual ICollection<CinemaHall> CinemaHallCreatedByNavigations { get; set; } = new List<CinemaHall>();

    public virtual ICollection<CinemaHall> CinemaHallUpdatedByNavigations { get; set; } = new List<CinemaHall>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<User> InverseUpdatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<Movie> MovieCreatedByNavigations { get; set; } = new List<Movie>();

    public virtual ICollection<Movie> MovieUpdatedByNavigations { get; set; } = new List<Movie>();

    public virtual ICollection<Seat> SeatCreatedByNavigations { get; set; } = new List<Seat>();

    public virtual ICollection<Seat> SeatUpdatedByNavigations { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> ShowtimeCreatedByNavigations { get; set; } = new List<Showtime>();

    public virtual ICollection<Showtime> ShowtimeUpdatedByNavigations { get; set; } = new List<Showtime>();

    public virtual ICollection<Ticket> TicketCreatedByNavigations { get; set; } = new List<Ticket>();

    public virtual ICollection<Ticket> TicketUpdatedByNavigations { get; set; } = new List<Ticket>();

    public virtual User? UpdatedByNavigation { get; set; }
}
