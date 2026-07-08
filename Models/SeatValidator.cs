namespace API_TICKET_APPLICATION.Models
{
    public static class SeatValidator
    {
        public static (bool IsValid, string? ErrorMessage) Validate(Seat seat)
        {
            if (seat == null) return (false, "Dữ liệu ghế không được để trống");

            if (seat.CinemaHallId <= 0)
                return (false, "ID phòng chiếu không hợp lệ");

            if (string.IsNullOrWhiteSpace(seat.Row))
                return (false, "Hàng ghế không được để trống");

            if (seat.Number <= 0)
                return (false, "Số ghế phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(seat.SeatType))
                return (false, "Loại ghế không được để trống");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateField(string fieldName, object? value)
        {
            switch (fieldName.ToLower())
            {
                case "cinemahallid":
                    if (!int.TryParse(value?.ToString(), out var hallId) || hallId <= 0)
                        return (false, "ID phòng chiếu không hợp lệ");
                    break;
                case "row":
                    if (string.IsNullOrWhiteSpace(value?.ToString()))
                        return (false, "Hàng ghế không được để trống");
                    break;
                case "number":
                    if (!int.TryParse(value?.ToString(), out var num) || num <= 0)
                        return (false, "Số ghế phải lớn hơn 0");
                    break;
                case "seattype":
                    if (string.IsNullOrWhiteSpace(value?.ToString()))
                        return (false, "Loại ghế không được để trống");
                    break;
            }
            return (true, null);
        }
    }
}
