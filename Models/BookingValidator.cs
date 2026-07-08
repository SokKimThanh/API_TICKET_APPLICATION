using System;

namespace API_TICKET_APPLICATION.Models
{
    public static class BookingValidator
    {
        public static (bool IsValid, string? ErrorMessage) Validate(Booking booking)
        {
            if (booking == null) return (false, "Dữ liệu đặt vé không được để trống");

            if (booking.UserId <= 0)
                return (false, "ID người dùng không hợp lệ");

            if (booking.ShowtimeId <= 0)
                return (false, "ID lịch chiếu không hợp lệ");

            if (booking.TotalPrice < 0)
                return (false, "Tổng tiền không được nhỏ hơn 0");

            if (string.IsNullOrWhiteSpace(booking.Status))
                return (false, "Trạng thái đặt vé không được để trống");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateField(string fieldName, object? value)
        {
            switch (fieldName.ToLower())
            {
                case "userid":
                    if (!int.TryParse(value?.ToString(), out var uId) || uId <= 0)
                        return (false, "ID người dùng không hợp lệ");
                    break;
                case "showtimeid":
                    if (!int.TryParse(value?.ToString(), out var sId) || sId <= 0)
                        return (false, "ID lịch chiếu không hợp lệ");
                    break;
                case "totalprice":
                    if (!decimal.TryParse(value?.ToString(), out var price) || price < 0)
                        return (false, "Tổng tiền không được nhỏ hơn 0");
                    break;
                case "status":
                    if (string.IsNullOrWhiteSpace(value?.ToString()))
                        return (false, "Trạng thái không được để trống");
                    break;
            }
            return (true, null);
        }
    }
}
