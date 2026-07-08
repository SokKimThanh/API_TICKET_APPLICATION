namespace API_TICKET_APPLICATION.Models
{
    public static class ShowtimeValidator
    {
        public static (bool IsValid, string? ErrorMessage) Validate(Showtime showtime)
        {
            if (showtime == null) return (false, "Dữ liệu lịch chiếu không được để trống");

            if (showtime.MovieId <= 0)
                return (false, "ID phim không hợp lệ");

            if (showtime.CinemaHallId <= 0)
                return (false, "ID phòng chiếu không hợp lệ");

            if (showtime.StartTime >= showtime.EndTime)
                return (false, "Thời gian bắt đầu phải trước thời gian kết thúc");

            if (showtime.StartTime < DateTime.UtcNow)
                return (false, "Thời gian bắt đầu không được trong quá khứ");

            if (showtime.BasePrice <= 0)
                return (false, "Giá vé cơ bản phải lớn hơn 0");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateField(string fieldName, object? value)
        {
            switch (fieldName.ToLower())
            {
                case "movieid":
                    if (!int.TryParse(value?.ToString(), out var mId) || mId <= 0)
                        return (false, "ID phim không hợp lệ");
                    break;
                case "cinemahallid":
                    if (!int.TryParse(value?.ToString(), out var chId) || chId <= 0)
                        return (false, "ID phòng chiếu không hợp lệ");
                    break;
                case "baseprice":
                    if (!decimal.TryParse(value?.ToString(), out var price) || price <= 0)
                        return (false, "Giá vé cơ bản phải lớn hơn 0");
                    break;
                case "starttime":
                case "endtime":
                    if (!DateTime.TryParse(value?.ToString(), out _))
                        return (false, "Định dạng thời gian không hợp lệ");
                    break;
            }
            return (true, null);
        }
    }
}
