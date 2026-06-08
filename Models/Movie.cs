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

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
