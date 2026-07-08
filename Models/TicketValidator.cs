using System;

namespace API_TICKET_APPLICATION.Models
{
    public static class TicketValidator
    {
        public static (bool IsValid, string? ErrorMessage) Validate(Ticket ticket)
        {
            if (ticket == null) return (false, "Dữ liệu vé không được để trống");

            if (ticket.BookingId <= 0)
                return (false, "ID đặt vé không hợp lệ");

            if (ticket.SeatId <= 0)
                return (false, "ID ghế không hợp lệ");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateField(string fieldName, object? value)
        {
            switch (fieldName.ToLower())
            {
                case "bookingid":
                    if (!int.TryParse(value?.ToString(), out var bId) || bId <= 0)
                        return (false, "ID đặt vé không hợp lệ");
                    break;
                case "seatid":
                    if (!int.TryParse(value?.ToString(), out var sId) || sId <= 0)
                        return (false, "ID ghế không hợp lệ");
                    break;
            }
            return (true, null);
        }
    }
}
