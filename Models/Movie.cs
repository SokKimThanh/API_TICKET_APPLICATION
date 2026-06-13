using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models;

public partial class Movie
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Genre { get; set; } = null!;

    public int DurationInMinutes { get; set; }

    public string? PosterUrl { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual User? UpdatedByNavigation { get; set; }
}
