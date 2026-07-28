using System;

namespace API_TICKET_APPLICATION.Models
{
    /// <summary>
    /// CinemaHallValidator - Isolated validation logic for CinemaHall entity
    /// Safe for DB-First approach as it stays separate from the generated CinemaHall model.
    /// </summary>
    public static class CinemaHallValidator
    {
        /// <summary>
        /// Validates a full CinemaHall object
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) Validate(CinemaHall cinemaHall)
        {
            if (cinemaHall == null) return (false, "Dữ liệu phòng chiếu không được để trống");

            // Validate Name
            var nameResult = ValidateField("name", cinemaHall.Name);
            if (!nameResult.IsValid) return nameResult;

            // Validate TotalSeats
            var totalSeatsResult = ValidateField("totalseats", cinemaHall.TotalSeats);
            if (!totalSeatsResult.IsValid) return totalSeatsResult;

            return (true, null);
        }

        /// <summary>
        /// Validates an individual field. Useful for PATCH (Partial Update)
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) ValidateField(string fieldName, object? value)
        {
            switch (fieldName.ToLower())
            {
                case "name":
                    var name = value?.ToString();
                    if (string.IsNullOrWhiteSpace(name))
                        return (false, "Tên phòng chiếu không được để trống");
                    if (name.Length > 255)
                        return (false, "Tên phòng chiếu không được vượt quá 255 ký tự");
                    break;

                case "totalseats":
                    if (value == null) return (false, "Số lượng ghế không được để trống");
                    if (!int.TryParse(value.ToString(), out var totalSeats) || totalSeats <= 0)
                        return (false, "Số lượng ghế phải lớn hơn 0");
                    break;
            }

            return (true, null);
        }
    }
}
