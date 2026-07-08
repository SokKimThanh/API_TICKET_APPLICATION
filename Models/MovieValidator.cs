using System;
using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models
{
    /// <summary>
    /// MovieValidator - Isolated validation logic for Movie entity
    /// Safe for DB-First approach as it stays separate from the generated Movie model.
    /// </summary>
    public static class MovieValidator
    {
        /// <summary>
        /// Validates a full Movie object
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) Validate(Movie movie)
        {
            if (movie == null) return (false, "Dữ liệu phim không được để trống");

            // Validate Title
            var titleResult = ValidateField("title", movie.Title);
            if (!titleResult.IsValid) return titleResult;

            // Validate Genre
            var genreResult = ValidateField("genre", movie.Genre);
            if (!genreResult.IsValid) return genreResult;

            // Validate Duration
            var durationResult = ValidateField("durationinminutes", movie.DurationInMinutes);
            if (!durationResult.IsValid) return durationResult;

            // Validate PosterUrl (Optional but has max length)
            var posterResult = ValidateField("posterurl", movie.PosterUrl);
            if (!posterResult.IsValid) return posterResult;

            return (true, null);
        }

        /// <summary>
        /// Validates an individual field. Useful for PATCH (Partial Update)
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) ValidateField(string fieldName, object? value)
        {
            switch (fieldName.ToLower())
            {
                case "title":
                    var title = value?.ToString();
                    if (string.IsNullOrWhiteSpace(title))
                        return (false, "Tên phim không được để trống");
                    if (title.Length > 255)
                        return (false, "Tên phim không được vượt quá 255 ký tự");
                    break;

                case "genre":
                    if (string.IsNullOrWhiteSpace(value?.ToString()))
                        return (false, "Thể loại không được để trống");
                    break;

                case "durationinminutes":
                    if (value == null) return (false, "Thời lượng phim không được để trống");
                    if (!int.TryParse(value.ToString(), out var duration) || duration <= 0)
                        return (false, "Thời lượng phim phải lớn hơn 0");
                    break;

                case "posterurl":
                    var url = value?.ToString();
                    if (!string.IsNullOrEmpty(url) && url.Length > 255)
                        return (false, "Poster URL không được vượt quá 255 ký tự");
                    break;
            }

            return (true, null);
        }
    }
}
